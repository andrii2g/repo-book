namespace RepoBook.Git;

public sealed class GitCommandException : Exception
{
    public GitCommandException(string message)
        : base(message)
    {
    }

    public GitCommandException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
