namespace RepoBook.Model;

public sealed class RepositoryReport
{
    public required string RepositoryName { get; init; }
    public required string RepositoryRoot { get; init; }
    public required DateTimeOffset GeneratedAtUtc { get; init; }
    public int CommitCount { get; init; }
    public int ContributorCount { get; init; }
    public DateOnly? FirstCommitDate { get; init; }
    public DateOnly? LastCommitDate { get; init; }
    public int FileCount { get; init; }
    public int DirectoryCount { get; init; }
    public long TotalLines { get; init; }
    public long TotalBytes { get; init; }
    public string PrimaryLanguage { get; init; } = "Unknown";
    public required IReadOnlyList<LanguageStat> Languages { get; init; }
    public required IReadOnlyList<TechnologyEvidence> Technologies { get; init; }
    public required IReadOnlyList<DirectoryStat> DirectoryStats { get; init; }
    public required IReadOnlyList<DirectoryStat> LargestModules { get; init; }
    public required IReadOnlyList<FileStat> OldestFiles { get; init; }
    public required IReadOnlyList<DirectoryStat> MostActiveAreas { get; init; }
    public required IReadOnlyList<ContributorStat> Contributors { get; init; }
    public required IReadOnlyList<TimelineYearStat> Timeline { get; init; }
    public required IReadOnlyList<string> StructureTreeLines { get; init; }
}
