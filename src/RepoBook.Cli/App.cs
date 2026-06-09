using RepoBook.Git;
using RepoBook.Model;
using RepoBook.Reporting;
using RepoBook.Scanning;

namespace RepoBook;

public sealed class App
{
    private readonly GitClient _gitClient;
    private readonly GitHistoryAnalyzer _historyAnalyzer;
    private readonly FileScanner _fileScanner;
    private readonly TechnologyDetector _technologyDetector;
    private readonly MarkdownReportWriter _reportWriter;

    public App()
        : this(
            new GitClient(),
            new GitHistoryAnalyzer(new GitClient()),
            new FileScanner(),
            new TechnologyDetector(),
            new MarkdownReportWriter())
    {
    }

    public App(
        GitClient gitClient,
        GitHistoryAnalyzer historyAnalyzer,
        FileScanner fileScanner,
        TechnologyDetector technologyDetector,
        MarkdownReportWriter reportWriter)
    {
        _gitClient = gitClient;
        _historyAnalyzer = historyAnalyzer;
        _fileScanner = fileScanner;
        _technologyDetector = technologyDetector;
        _reportWriter = reportWriter;
    }

    public Task<string> RunAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        _ = _gitClient;
        _ = _historyAnalyzer;
        _ = _fileScanner;
        _ = _technologyDetector;
        _ = _reportWriter;

        throw new NotImplementedException("Phase 2+ will implement the application workflow.");
    }
}
