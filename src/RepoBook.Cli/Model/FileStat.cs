namespace RepoBook.Model;

public sealed class FileStat
{
    public required string RelativePath { get; init; }
    public required string Extension { get; init; }
    public required string Language { get; init; }
    public long Bytes { get; init; }
    public long Lines { get; init; }
    public bool IsBinary { get; init; }
    public DateOnly? FirstAddedDate { get; init; }
}
