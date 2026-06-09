namespace RepoBook.Model;

public sealed class DirectoryStat
{
    public required string Path { get; init; }
    public int FileCount { get; init; }
    public long Lines { get; init; }
    public long Bytes { get; init; }
    public int CommitTouches { get; init; }
}
