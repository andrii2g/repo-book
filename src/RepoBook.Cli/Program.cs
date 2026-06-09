using RepoBook;
using RepoBook.Git;

if (args.Length != 1 || args[0].StartsWith("-", StringComparison.Ordinal))
{
    Console.Error.WriteLine("Usage: repo-book <repository-path>");
    return 1;
}

try
{
    var app = new App();
    var outputPath = await app.RunAsync(args[0]);
    Console.WriteLine($"Repository encyclopedia written to: {outputPath}");
    return 0;
}
catch (GitCommandException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 3;
}
