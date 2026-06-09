using RepoBook.Model;

namespace RepoBook.Scanning;

public sealed class TechnologyDetector
{
    public Task<IReadOnlyList<TechnologyEvidence>> DetectAsync(
        string repositoryRoot,
        IReadOnlyList<FileStat> files,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Phase 2 will implement technology detection.");
    }
}
