using RepoBook.Model;

namespace RepoBook.Git;

public sealed class GitHistoryAnalyzer
{
    private readonly GitClient _gitClient;

    public GitHistoryAnalyzer(GitClient gitClient)
    {
        _gitClient = gitClient;
    }

    public Task<string> GetRepositoryRootAsync(string inputPath)
    {
        _ = _gitClient;
        throw new NotImplementedException();
    }

    public Task<bool> HasCommitsAsync(string repoRoot)
    {
        _ = _gitClient;
        throw new NotImplementedException();
    }

    public Task<int> GetCommitCountAsync(string repoRoot)
    {
        _ = _gitClient;
        throw new NotImplementedException();
    }

    public Task<(DateOnly? First, DateOnly? Last)> GetCommitDateRangeAsync(string repoRoot)
    {
        _ = _gitClient;
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ContributorStat>> GetContributorsAsync(string repoRoot, int totalCommits)
    {
        _ = _gitClient;
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, DateOnly>> GetFirstAddedDatesAsync(string repoRoot)
    {
        _ = _gitClient;
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, int>> GetCommitTouchesByModuleAsync(string repoRoot)
    {
        _ = _gitClient;
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<TimelineYearStat>> GetTimelineAsync(string repoRoot)
    {
        _ = _gitClient;
        throw new NotImplementedException();
    }
}
