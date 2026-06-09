using RepoBook.Model;
using System.Text;

namespace RepoBook.Scanning;

public sealed class FileScanner
{
    private const long LargeFileThresholdBytes = 10 * 1024 * 1024;
    private const int BinaryProbeBytes = 8192;
    private readonly LanguageDetector _languageDetector = new();

    public Task<ScanResult> ScanAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        var files = new List<FileStat>();
        var allDirectoryStats = new Dictionary<string, MutableDirectoryStat>(StringComparer.Ordinal);
        var topLevelDirectoryStats = new Dictionary<string, MutableDirectoryStat>(StringComparer.Ordinal);
        var languageStats = new Dictionary<string, MutableLanguageStat>(StringComparer.Ordinal);
        var directoriesWithIncludedFiles = new HashSet<string>(StringComparer.Ordinal);
        long totalBytes = 0;
        long totalLines = 0;

        foreach (var filePath in EnumerateIncludedFiles(repositoryRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = NormalizePath(Path.GetRelativePath(repositoryRoot, filePath));
            var fileName = Path.GetFileName(filePath);
            if (FileSystemRules.ShouldSkipFile(fileName))
            {
                continue;
            }

            var fileInfo = new FileInfo(filePath);
            var bytes = SafeGetLength(fileInfo);
            totalBytes += bytes;

            var extension = Path.GetExtension(fileName);
            var isBinary = IsBinaryFile(filePath, extension);
            var isLockFile = FileSystemRules.IsLockFile(fileName);
            var language = isBinary
                ? "Binary"
                : bytes > LargeFileThresholdBytes
                    ? DetectLargeFileLanguage(filePath)
                    : _languageDetector.DetectLanguage(filePath);

            long lines = 0;
            if (!isBinary && !isLockFile && bytes <= LargeFileThresholdBytes)
            {
                lines = CountLinesSafe(filePath);
            }

            totalLines += lines;

            var fileStat = new FileStat
            {
                RelativePath = relativePath,
                Extension = extension,
                Language = language,
                Bytes = bytes,
                Lines = lines,
                IsBinary = isBinary,
                FirstAddedDate = null,
            };

            files.Add(fileStat);
            UpdateDirectoryStats(relativePath, bytes, lines, allDirectoryStats, topLevelDirectoryStats, directoriesWithIncludedFiles);
            UpdateLanguageStats(language, bytes, lines, languageStats);
        }

        var orderedFiles = files
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult(new ScanResult
        {
            Files = orderedFiles,
            DirectoryStats = topLevelDirectoryStats
                .Values
                .Select(ToDirectoryStat)
                .OrderBy(stat => stat.Path, StringComparer.Ordinal)
                .ToArray(),
            Languages = languageStats
                .Values
                .Select(stat => new LanguageStat
                {
                    Language = stat.Language,
                    FileCount = stat.FileCount,
                    Lines = stat.Lines,
                    Bytes = stat.Bytes,
                })
                .OrderByDescending(stat => stat.Lines)
                .ThenByDescending(stat => stat.FileCount)
                .ThenBy(stat => stat.Language, StringComparer.Ordinal)
                .ToArray(),
            StructureTreeLines = BuildStructureTreeLines(repositoryRoot),
            DirectoryCount = directoriesWithIncludedFiles.Count,
            TotalBytes = totalBytes,
            TotalLines = totalLines,
        });
    }

    private static IEnumerable<string> EnumerateIncludedFiles(string repositoryRoot)
    {
        var pending = new Stack<string>();
        pending.Push(repositoryRoot);

        while (pending.Count > 0)
        {
            var currentDirectory = pending.Pop();

            IEnumerable<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(currentDirectory)
                    .Where(path => !FileSystemRules.ShouldSkipDirectory(Path.GetFileName(path)))
                    .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch
            {
                continue;
            }

            foreach (var directory in directories.Reverse())
            {
                pending.Push(directory);
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(currentDirectory)
                    .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch
            {
                continue;
            }

            foreach (var filePath in files)
            {
                yield return filePath;
            }
        }
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static long SafeGetLength(FileInfo fileInfo)
    {
        try
        {
            return fileInfo.Length;
        }
        catch
        {
            return 0;
        }
    }

    private static bool IsBinaryFile(string filePath, string extension)
    {
        if (FileSystemRules.IsKnownBinaryExtension(extension))
        {
            return true;
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            var buffer = new byte[BinaryProbeBytes];
            var read = stream.Read(buffer, 0, buffer.Length);

            for (var index = 0; index < read; index++)
            {
                if (buffer[index] == 0)
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static string DetectLargeFileLanguage(string filePath)
    {
        var language = new LanguageDetector().DetectLanguage(filePath);
        return string.Equals(language, "Other", StringComparison.Ordinal) ? "Large file" : language;
    }

    private static long CountLinesSafe(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream, new UTF8Encoding(false, false), detectEncodingFromByteOrderMarks: true);
            long count = 0;
            while (reader.ReadLine() is not null)
            {
                count++;
            }

            return count;
        }
        catch
        {
            return 0;
        }
    }

    private static void UpdateDirectoryStats(
        string relativePath,
        long bytes,
        long lines,
        Dictionary<string, MutableDirectoryStat> allDirectoryStats,
        Dictionary<string, MutableDirectoryStat> topLevelDirectoryStats,
        HashSet<string> directoriesWithIncludedFiles)
    {
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length <= 1)
        {
            UpdateDirectoryStat("/", bytes, lines, topLevelDirectoryStats);
            return;
        }

        for (var index = 0; index < segments.Length - 1; index++)
        {
            var directoryPath = string.Join('/', segments.Take(index + 1));
            directoriesWithIncludedFiles.Add(directoryPath);
            UpdateDirectoryStat(directoryPath, bytes, lines, allDirectoryStats);
        }

        UpdateDirectoryStat(segments[0], bytes, lines, topLevelDirectoryStats);
    }

    private static void UpdateDirectoryStat(
        string path,
        long bytes,
        long lines,
        Dictionary<string, MutableDirectoryStat> stats)
    {
        if (!stats.TryGetValue(path, out var stat))
        {
            stat = new MutableDirectoryStat(path);
            stats.Add(path, stat);
        }

        stat.FileCount++;
        stat.Bytes += bytes;
        stat.Lines += lines;
    }

    private static void UpdateLanguageStats(
        string language,
        long bytes,
        long lines,
        Dictionary<string, MutableLanguageStat> languageStats)
    {
        if (!languageStats.TryGetValue(language, out var stat))
        {
            stat = new MutableLanguageStat(language);
            languageStats.Add(language, stat);
        }

        stat.FileCount++;
        stat.Bytes += bytes;
        stat.Lines += lines;
    }

    private static DirectoryStat ToDirectoryStat(MutableDirectoryStat stat)
    {
        return new DirectoryStat
        {
            Path = stat.Path,
            FileCount = stat.FileCount,
            Lines = stat.Lines,
            Bytes = stat.Bytes,
            CommitTouches = 0,
        };
    }

    private static IReadOnlyList<string> BuildStructureTreeLines(string repositoryRoot)
    {
        var lines = new List<string> { "." };
        BuildTreeRecursive(repositoryRoot, lines, string.Empty, 1, 3);
        return lines;
    }

    private static void BuildTreeRecursive(string directoryPath, List<string> lines, string prefix, int depth, int maxDepth)
    {
        if (depth > maxDepth)
        {
            return;
        }

        var entries = GetVisibleEntries(directoryPath).ToList();
        var visibleEntries = entries.Take(12).ToArray();

        for (var index = 0; index < visibleEntries.Length; index++)
        {
            var entry = visibleEntries[index];
            var isLastVisible = index == visibleEntries.Length - 1 && visibleEntries.Length == entries.Count;
            var branch = isLastVisible ? "└── " : "├── ";
            lines.Add(prefix + branch + Path.GetFileName(entry.Path));

            if (entry.IsDirectory && depth < maxDepth)
            {
                var childPrefix = prefix + (isLastVisible ? "    " : "│   ");
                BuildTreeRecursive(entry.Path, lines, childPrefix, depth + 1, maxDepth);
            }
        }

        if (entries.Count > visibleEntries.Length)
        {
            lines.Add(prefix + "└── ... " + (entries.Count - visibleEntries.Length).ToString() + " more");
        }
    }

    private static IEnumerable<TreeEntry> GetVisibleEntries(string directoryPath)
    {
        IEnumerable<string> directories;
        try
        {
            directories = Directory.EnumerateDirectories(directoryPath)
                .Where(path => !FileSystemRules.ShouldSkipDirectory(Path.GetFileName(path)));
        }
        catch
        {
            directories = [];
        }

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(directoryPath)
                .Where(path => !FileSystemRules.ShouldSkipFile(Path.GetFileName(path)));
        }
        catch
        {
            files = [];
        }

        return directories
            .Select(path => new TreeEntry(path, true))
            .Concat(files.Select(path => new TreeEntry(path, false)))
            .OrderByDescending(entry => entry.IsDirectory)
            .ThenBy(entry => Path.GetFileName(entry.Path), StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => Path.GetFileName(entry.Path), StringComparer.Ordinal);
    }

    private sealed record TreeEntry(string Path, bool IsDirectory);

    private sealed class MutableDirectoryStat
    {
        public MutableDirectoryStat(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public int FileCount { get; set; }

        public long Lines { get; set; }

        public long Bytes { get; set; }
    }

    private sealed class MutableLanguageStat
    {
        public MutableLanguageStat(string language)
        {
            Language = language;
        }

        public string Language { get; }

        public int FileCount { get; set; }

        public long Lines { get; set; }

        public long Bytes { get; set; }
    }
}

public sealed class ScanResult
{
    public required IReadOnlyList<FileStat> Files { get; init; }
    public required IReadOnlyList<DirectoryStat> DirectoryStats { get; init; }
    public required IReadOnlyList<LanguageStat> Languages { get; init; }
    public required IReadOnlyList<string> StructureTreeLines { get; init; }
    public int DirectoryCount { get; init; }
    public long TotalBytes { get; init; }
    public long TotalLines { get; init; }
}
