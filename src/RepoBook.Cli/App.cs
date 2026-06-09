using RepoBook.Git;
using RepoBook.Model;
using RepoBook.Reporting;
using RepoBook.Scanning;

namespace RepoBook;

public sealed class App
{
    private readonly GitClient _gitClient;
    private readonly GitHistoryAnalyzer _historyAnalyzer;
    private readonly FileScanner _fileScanner;
    private readonly TechnologyDetector _technologyDetector;
    private readonly MarkdownReportWriter _reportWriter;

    public App()
        : this(CreateDefaultGitClient(), new FileScanner(), new TechnologyDetector(), new MarkdownReportWriter())
    {
    }

    public App(
        GitClient gitClient,
        FileScanner fileScanner,
        TechnologyDetector technologyDetector,
        MarkdownReportWriter reportWriter)
        : this(
            gitClient,
            new GitHistoryAnalyzer(gitClient),
            fileScanner,
            technologyDetector,
            reportWriter)
    {
    }

    public App(
        GitClient gitClient,
        GitHistoryAnalyzer historyAnalyzer,
        FileScanner fileScanner,
        TechnologyDetector technologyDetector,
        MarkdownReportWriter reportWriter)
    {
        _gitClient = gitClient;
        _historyAnalyzer = historyAnalyzer;
        _fileScanner = fileScanner;
        _technologyDetector = technologyDetector;
        _reportWriter = reportWriter;
    }

    public async Task<string> RunAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);

        var absoluteInputPath = Path.GetFullPath(repositoryPath);
        var repositoryRoot = await GetRepositoryRootAsync(absoluteInputPath);
        var hasCommits = await _historyAnalyzer.HasCommitsAsync(repositoryRoot);
        var scanResult = await _fileScanner.ScanAsync(repositoryRoot, cancellationToken);

        var commitCount = 0;
        var firstCommitDate = default(DateOnly?);
        var lastCommitDate = default(DateOnly?);
        var contributors = Array.Empty<ContributorStat>();
        var timeline = Array.Empty<TimelineYearStat>();
        Dictionary<string, DateOnly> firstAddedDates = new(StringComparer.Ordinal);
        Dictionary<string, int> commitTouchesByModule = new(StringComparer.Ordinal);

        if (hasCommits)
        {
            commitCount = await _historyAnalyzer.GetCommitCountAsync(repositoryRoot);
            (firstCommitDate, lastCommitDate) = await _historyAnalyzer.GetCommitDateRangeAsync(repositoryRoot);
            contributors = (await _historyAnalyzer.GetContributorsAsync(repositoryRoot, commitCount)).ToArray();
            firstAddedDates = await _historyAnalyzer.GetFirstAddedDatesAsync(repositoryRoot);
            commitTouchesByModule = await _historyAnalyzer.GetCommitTouchesByModuleAsync(repositoryRoot);
            timeline = (await _historyAnalyzer.GetTimelineAsync(repositoryRoot)).ToArray();
        }

        var filesWithDates = ApplyFirstAddedDates(scanResult.Files, firstAddedDates);
        var technologies = await _technologyDetector.DetectAsync(repositoryRoot, filesWithDates, cancellationToken);
        var largestModules = BuildLargestModules(filesWithDates);
        var oldestFiles = BuildOldestFiles(filesWithDates);
        var mostActiveAreas = BuildMostActiveAreas(commitTouchesByModule);

        var report = new RepositoryReport
        {
            RepositoryName = Path.GetFileName(repositoryRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            RepositoryRoot = repositoryRoot,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            CommitCount = commitCount,
            ContributorCount = contributors.Length,
            FirstCommitDate = firstCommitDate,
            LastCommitDate = lastCommitDate,
            FileCount = filesWithDates.Count,
            DirectoryCount = scanResult.DirectoryCount,
            TotalLines = scanResult.TotalLines,
            TotalBytes = scanResult.TotalBytes,
            PrimaryLanguage = DeterminePrimaryLanguage(scanResult.Languages),
            Languages = scanResult.Languages,
            Technologies = technologies,
            DirectoryStats = scanResult.DirectoryStats,
            LargestModules = largestModules,
            OldestFiles = oldestFiles,
            MostActiveAreas = mostActiveAreas,
            Contributors = contributors,
            Timeline = timeline,
            StructureTreeLines = scanResult.StructureTreeLines,
        };

        var outputPath = Path.Combine(repositoryRoot, "repository-encyclopedia.md");
        var markdown = _reportWriter.Write(report);
        await File.WriteAllTextAsync(outputPath, markdown, cancellationToken);
        return outputPath;
    }

    private async Task<string> GetRepositoryRootAsync(string absoluteInputPath)
    {
        try
        {
            return await _historyAnalyzer.GetRepositoryRootAsync(absoluteInputPath);
        }
        catch (GitCommandException ex) when (CanTreatPathFailureAsNotARepository(ex))
        {
            throw new GitCommandException("path is not a Git repository.");
        }
    }

    private static IReadOnlyList<FileStat> ApplyFirstAddedDates(
        IReadOnlyList<FileStat> files,
        IReadOnlyDictionary<string, DateOnly> firstAddedDates)
    {
        return files
            .Select(file => new FileStat
            {
                RelativePath = file.RelativePath,
                Extension = file.Extension,
                Language = file.Language,
                Bytes = file.Bytes,
                Lines = file.Lines,
                IsBinary = file.IsBinary,
                FirstAddedDate = firstAddedDates.TryGetValue(file.RelativePath, out var date) ? date : null,
            })
            .ToArray();
    }

    private static IReadOnlyList<DirectoryStat> BuildLargestModules(IReadOnlyList<FileStat> files)
    {
        var groups = files
            .GroupBy(file => GetModulePath(file.RelativePath), StringComparer.Ordinal)
            .Select(group => new DirectoryStat
            {
                Path = group.Key,
                FileCount = group.Count(),
                Lines = group.Sum(file => file.Lines),
                Bytes = group.Sum(file => file.Bytes),
                CommitTouches = 0,
            })
            .ToArray();

        var anyTextLines = groups.Any(group => group.Lines > 0);
        return groups
            .Where(group => group.Lines > 0 || !anyTextLines)
            .OrderByDescending(group => group.Lines)
            .ThenByDescending(group => group.FileCount)
            .ThenBy(group => group.Path, StringComparer.Ordinal)
            .Take(10)
            .ToArray();
    }

    private static IReadOnlyList<FileStat> BuildOldestFiles(IReadOnlyList<FileStat> files)
    {
        return files
            .Where(file => file.FirstAddedDate.HasValue)
            .OrderBy(file => file.FirstAddedDate)
            .ThenBy(file => file.RelativePath, StringComparer.Ordinal)
            .Take(10)
            .ToArray();
    }

    private static IReadOnlyList<DirectoryStat> BuildMostActiveAreas(IReadOnlyDictionary<string, int> commitTouchesByModule)
    {
        return commitTouchesByModule
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .Select(item => new DirectoryStat
            {
                Path = item.Key,
                FileCount = 0,
                Lines = 0,
                Bytes = 0,
                CommitTouches = item.Value,
            })
            .OrderByDescending(item => item.CommitTouches)
            .ThenBy(item => item.Path, StringComparer.Ordinal)
            .Take(10)
            .ToArray();
    }

    private static string DeterminePrimaryLanguage(IReadOnlyList<LanguageStat> languages)
    {
        var excluded = new HashSet<string>(StringComparer.Ordinal)
        {
            "Binary",
            "Text",
            "JSON",
            "YAML",
            "XML",
            "Markdown",
        };

        var preferred = languages
            .Where(language => !excluded.Contains(language.Language))
            .OrderByDescending(language => language.Lines)
            .ThenBy(language => language.Language, StringComparer.Ordinal)
            .FirstOrDefault();

        if (preferred is not null && preferred.Lines > 0)
        {
            return preferred.Language;
        }

        return languages
            .OrderByDescending(language => language.Lines)
            .ThenBy(language => language.Language, StringComparer.Ordinal)
            .FirstOrDefault()
            ?.Language ?? "Unknown";
    }

    private static string GetModulePath(string relativeFilePath)
    {
        var normalizedPath = relativeFilePath.Replace('\\', '/');
        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length <= 1)
        {
            return "/";
        }

        var firstSegment = segments[0];
        if (segments.Length >= 3 && IsTwoLevelModuleRoot(firstSegment))
        {
            return $"{firstSegment}/{segments[1]}";
        }

        return firstSegment;
    }

    private static bool IsTwoLevelModuleRoot(string segment)
    {
        return segment is "src"
            or "source"
            or "tests"
            or "test"
            or "apps"
            or "packages"
            or "services"
            or "tools"
            or "examples";
    }

    private static bool CanTreatPathFailureAsNotARepository(GitCommandException ex)
    {
        return !string.Equals(ex.Message, "git executable was not found on PATH.", StringComparison.Ordinal)
            && !string.Equals(ex.Message, "git command timed out.", StringComparison.Ordinal);
    }

    private static GitClient CreateDefaultGitClient()
    {
        return new GitClient();
    }
}
