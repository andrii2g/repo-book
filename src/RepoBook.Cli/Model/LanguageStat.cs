namespace RepoBook.Model;

public sealed class LanguageStat
{
    public required string Language { get; init; }
    public int FileCount { get; init; }
    public long Lines { get; init; }
    public long Bytes { get; init; }
}
