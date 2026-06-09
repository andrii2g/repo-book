namespace RepoBook.Git;

public sealed class GitClient
{
    public Task<string> RunAsync(string repositoryPath, params string[] args)
    {
        throw new NotImplementedException("Phase 2 will implement Git process execution.");
    }

    public Task<IReadOnlyList<string>> RunLinesAsync(string repositoryPath, params string[] args)
    {
        throw new NotImplementedException("Phase 2 will implement Git line-oriented execution.");
    }
}
