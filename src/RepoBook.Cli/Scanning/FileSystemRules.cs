namespace RepoBook.Scanning;

public static class FileSystemRules
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".hg",
        ".svn",
        "bin",
        "obj",
        "node_modules",
        "bower_components",
        "packages",
        "vendor",
        "dist",
        "build",
        "out",
        "target",
        "coverage",
        ".next",
        ".nuxt",
        ".cache",
        ".tmp",
        "tmp",
        ".idea",
        ".vscode",
        ".vs",
        "__pycache__",
        ".pytest_cache",
        ".mypy_cache",
        ".gradle",
        "terraform.tfstate.d",
    };

    private static readonly HashSet<string> LockFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "package-lock.json",
        "yarn.lock",
        "pnpm-lock.yaml",
        "Cargo.lock",
        "poetry.lock",
        "Pipfile.lock",
        "composer.lock",
    };

    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".webp",
        ".ico",
        ".bmp",
        ".tiff",
        ".pdf",
        ".zip",
        ".gz",
        ".tar",
        ".7z",
        ".rar",
        ".exe",
        ".dll",
        ".so",
        ".dylib",
        ".bin",
        ".mp3",
        ".mp4",
        ".mov",
        ".avi",
        ".mkv",
        ".wav",
        ".ttf",
        ".otf",
        ".woff",
        ".woff2",
        ".db",
        ".sqlite",
        ".sqlite3",
        ".class",
        ".jar",
    };

    public static bool ShouldSkipDirectory(string directoryName)
    {
        return IgnoredDirectories.Contains(directoryName);
    }

    public static bool ShouldSkipFile(string fileName)
    {
        return string.Equals(fileName, "repository-encyclopedia.md", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsKnownBinaryExtension(string extension)
    {
        return BinaryExtensions.Contains(extension);
    }

    public static bool IsLockFile(string fileName)
    {
        return LockFiles.Contains(fileName);
    }
}
