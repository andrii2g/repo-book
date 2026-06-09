namespace RepoBook.Model;

public sealed class ContributorStat
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public int Commits { get; init; }
    public double CommitSharePercent { get; init; }
}
