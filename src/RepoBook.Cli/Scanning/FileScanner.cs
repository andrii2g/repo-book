using RepoBook.Model;

namespace RepoBook.Scanning;

public sealed class FileScanner
{
    public Task<ScanResult> ScanAsync(string repositoryRoot, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Phase 2 will implement filesystem scanning.");
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
