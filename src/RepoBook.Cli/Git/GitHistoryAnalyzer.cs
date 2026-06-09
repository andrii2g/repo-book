using RepoBook.Model;
using System.Globalization;

namespace RepoBook.Git;

public sealed class GitHistoryAnalyzer
{
    private readonly GitClient _gitClient;

    public GitHistoryAnalyzer(GitClient gitClient)
    {
        _gitClient = gitClient;
    }

    public async Task<string> GetRepositoryRootAsync(string inputPath)
    {
        return await _gitClient.RunAsync(inputPath, "rev-parse", "--show-toplevel");
    }

    public async Task<bool> HasCommitsAsync(string repoRoot)
    {
        try
        {
            await _gitClient.RunAsync(repoRoot, "rev-parse", "--verify", "HEAD");
            return true;
        }
        catch (GitCommandException ex) when (CanTreatMissingHeadAsEmptyRepository(ex))
        {
            return false;
        }
    }

    public async Task<int> GetCommitCountAsync(string repoRoot)
    {
        var output = await _gitClient.RunAsync(repoRoot, "rev-list", "--count", "HEAD");
        return int.Parse(output, CultureInfo.InvariantCulture);
    }

    public async Task<(DateOnly? First, DateOnly? Last)> GetCommitDateRangeAsync(string repoRoot)
    {
        var firstLines = await _gitClient.RunLinesAsync(repoRoot, "log", "--format=%cs", "--reverse");
        var firstDate = firstLines
            .Select(ParseDate)
            .FirstOrDefault(date => date.HasValue);

        var lastOutput = await _gitClient.RunAsync(repoRoot, "log", "-1", "--format=%cs");
        var lastDate = ParseDate(lastOutput);

        return (firstDate, lastDate);
    }

    public async Task<IReadOnlyList<ContributorStat>> GetContributorsAsync(string repoRoot, int totalCommits)
    {
        if (totalCommits <= 0)
        {
            return [];
        }

        var lines = await _gitClient.RunLinesAsync(repoRoot, "log", "--format=%aN%x09%aE");
        var contributors = new Dictionary<string, ContributorAccumulator>(StringComparer.Ordinal);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split('\t', 2);
            var name = parts[0].Trim();
            var email = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            var key = NormalizeContributorKey(name, email);

            if (!contributors.TryGetValue(key, out var accumulator))
            {
                accumulator = new ContributorAccumulator(name, email);
                contributors.Add(key, accumulator);
            }

            accumulator.AddCommit(name, email);
        }

        return contributors
            .Values
            .Select(item => new ContributorStat
            {
                Name = item.Name,
                Email = item.Email,
                Commits = item.Commits,
                CommitSharePercent = item.Commits * 100d / totalCommits,
            })
            .OrderByDescending(item => item.Commits)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<Dictionary<string, DateOnly>> GetFirstAddedDatesAsync(string repoRoot)
    {
        var lines = await _gitClient.RunLinesAsync(repoRoot, "log", "--reverse", "--diff-filter=A", "--name-only", "--format=@@@%cs");
        var firstAddedDates = new Dictionary<string, DateOnly>(StringComparer.Ordinal);
        DateOnly? currentDate = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line.StartsWith("@@@", StringComparison.Ordinal))
            {
                currentDate = ParseDate(line[3..]);
                continue;
            }

            if (!currentDate.HasValue)
            {
                continue;
            }

            var path = NormalizePath(line);
            firstAddedDates.TryAdd(path, currentDate.Value);
        }

        return firstAddedDates;
    }

    public async Task<Dictionary<string, int>> GetCommitTouchesByModuleAsync(string repoRoot)
    {
        var lines = await _gitClient.RunLinesAsync(repoRoot, "log", "--name-only", "--format=@@@COMMIT");
        var commitTouches = new Dictionary<string, int>(StringComparer.Ordinal);
        var currentModules = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (line.StartsWith("@@@COMMIT", StringComparison.Ordinal))
            {
                FlushCommitTouches(currentModules, commitTouches);
                currentModules.Clear();
                continue;
            }

            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var module = GetModulePath(NormalizePath(line));
            if (!string.IsNullOrEmpty(module))
            {
                currentModules.Add(module);
            }
        }

        FlushCommitTouches(currentModules, commitTouches);
        return commitTouches;
    }

    public async Task<IReadOnlyList<TimelineYearStat>> GetTimelineAsync(string repoRoot)
    {
        var commitYears = await GetCommitCountsByYearAsync(repoRoot);
        var filesAddedByYear = await GetFilesAddedByYearAsync(repoRoot);
        var years = commitYears.Keys
            .Concat(filesAddedByYear.Keys)
            .Distinct()
            .OrderBy(year => year)
            .ToArray();

        return years
            .Select(year => new TimelineYearStat
            {
                Year = year,
                Commits = commitYears.GetValueOrDefault(year),
                FilesAdded = filesAddedByYear.GetValueOrDefault(year),
            })
            .ToArray();
    }

    private async Task<Dictionary<int, int>> GetCommitCountsByYearAsync(string repoRoot)
    {
        var lines = await _gitClient.RunLinesAsync(repoRoot, "log", "--format=%cs");
        var counts = new Dictionary<int, int>();

        foreach (var line in lines)
        {
            var date = ParseDate(line);
            if (!date.HasValue)
            {
                continue;
            }

            counts[date.Value.Year] = counts.GetValueOrDefault(date.Value.Year) + 1;
        }

        return counts;
    }

    private async Task<Dictionary<int, int>> GetFilesAddedByYearAsync(string repoRoot)
    {
        var lines = await _gitClient.RunLinesAsync(repoRoot, "log", "--reverse", "--diff-filter=A", "--name-only", "--format=@@@%cs");
        var counts = new Dictionary<int, int>();
        DateOnly? currentDate = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line.StartsWith("@@@", StringComparison.Ordinal))
            {
                currentDate = ParseDate(line[3..]);
                continue;
            }

            if (!currentDate.HasValue)
            {
                continue;
            }

            counts[currentDate.Value.Year] = counts.GetValueOrDefault(currentDate.Value.Year) + 1;
        }

        return counts;
    }

    private static void FlushCommitTouches(HashSet<string> currentModules, Dictionary<string, int> commitTouches)
    {
        foreach (var module in currentModules)
        {
            commitTouches[module] = commitTouches.GetValueOrDefault(module) + 1;
        }
    }

    private static DateOnly? ParseDate(string value)
    {
        return DateOnly.TryParseExact(
            value.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }

    private static string NormalizeContributorKey(string name, string email)
    {
        return $"{name.Trim().ToLowerInvariant()} <{email.Trim().ToLowerInvariant()}>";
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim();
    }

    private static bool CanTreatMissingHeadAsEmptyRepository(GitCommandException ex)
    {
        return !string.Equals(ex.Message, "git executable was not found on PATH.", StringComparison.Ordinal)
            && !string.Equals(ex.Message, "git command timed out.", StringComparison.Ordinal);
    }

    private static string GetModulePath(string relativeFilePath)
    {
        var segments = relativeFilePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length <= 1)
        {
            return "/";
        }

        var first = segments[0];
        if (segments.Length >= 3 && IsTwoLevelModuleRoot(first))
        {
            return $"{first}/{segments[1]}";
        }

        return first;
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

    private sealed class ContributorAccumulator
    {
        public ContributorAccumulator(string name, string email)
        {
            Name = name;
            Email = email;
            Commits = 0;
        }

        public string Name { get; private set; }

        public string Email { get; private set; }

        public int Commits { get; private set; }

        public void AddCommit(string name, string email)
        {
            if (string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(name))
            {
                Name = name;
            }

            if (string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(email))
            {
                Email = email;
            }

            Commits++;
        }
    }
}
