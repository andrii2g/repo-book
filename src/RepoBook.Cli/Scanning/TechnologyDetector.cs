using RepoBook.Model;
using System.Text.RegularExpressions;

namespace RepoBook.Scanning;

public sealed class TechnologyDetector
{
    private const int MaxEvidenceRows = 30;
    private const long MaxContentScanBytes = 1024 * 1024;

    private static readonly (string PackageFragment, string Technology)[] CsprojPackages =
    [
        ("Microsoft.AspNetCore", "ASP.NET Core"),
        ("EntityFrameworkCore", "Entity Framework Core"),
        ("Npgsql", "PostgreSQL"),
        ("Pomelo.EntityFrameworkCore.MySql", "MySQL"),
        ("MySqlConnector", "MySQL"),
        ("StackExchange.Redis", "Redis"),
        ("Dapper", "Dapper"),
        ("Serilog", "Serilog"),
        ("MassTransit", "MassTransit"),
        ("RabbitMQ", "RabbitMQ"),
        ("Grpc", "gRPC"),
        ("MediatR", "MediatR"),
        ("xunit", "xUnit"),
        ("NUnit", "NUnit"),
        ("MSTest", "MSTest"),
    ];

    private static readonly (string PackageFragment, string Technology)[] PackageJsonPackages =
    [
        ("react", "React"),
        ("next", "Next.js"),
        ("vue", "Vue"),
        ("nuxt", "Nuxt"),
        ("@angular", "Angular"),
        ("express", "Express"),
        ("nestjs", "NestJS"),
        ("vite", "Vite"),
        ("webpack", "Webpack"),
        ("jest", "Jest"),
        ("vitest", "Vitest"),
        ("eslint", "ESLint"),
        ("prettier", "Prettier"),
        ("tailwindcss", "Tailwind CSS"),
    ];

    private static readonly (string PackageFragment, string Technology)[] PythonPackages =
    [
        ("django", "Django"),
        ("flask", "Flask"),
        ("fastapi", "FastAPI"),
        ("sqlalchemy", "SQLAlchemy"),
        ("pytest", "pytest"),
        ("numpy", "NumPy"),
        ("pandas", "pandas"),
        ("torch", "PyTorch"),
        ("tensorflow", "TensorFlow"),
    ];

    public Task<IReadOnlyList<TechnologyEvidence>> DetectAsync(
        string repositoryRoot,
        IReadOnlyList<FileStat> files,
        CancellationToken cancellationToken = default)
    {
        var evidenceByTechnology = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = file.RelativePath;
            var fileName = Path.GetFileName(relativePath);
            var extension = Path.GetExtension(relativePath);

            AddFileBasedDetections(evidenceByTechnology, repositoryRoot, relativePath, fileName, extension);
            AddContentBasedDetections(evidenceByTechnology, repositoryRoot, file);
        }

        return Task.FromResult<IReadOnlyList<TechnologyEvidence>>(evidenceByTechnology
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Take(MaxEvidenceRows)
            .Select(item => new TechnologyEvidence
            {
                Technology = item.Key,
                Evidence = item.Value,
            })
            .ToArray());
    }

    private static void AddFileBasedDetections(
        Dictionary<string, string> evidenceByTechnology,
        string repositoryRoot,
        string relativePath,
        string fileName,
        string extension)
    {
        var normalizedPath = relativePath.Replace('\\', '/');

        if (extension is ".csproj" or ".fsproj" or ".vbproj" || string.Equals(fileName, "global.json", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, ".NET", normalizedPath);
        }

        if (extension == ".cs")
        {
            AddEvidence(evidenceByTechnology, "C#", normalizedPath);
        }

        if (extension == ".fs")
        {
            AddEvidence(evidenceByTechnology, "F#", normalizedPath);
        }

        if (extension is ".ts" or ".tsx" || string.Equals(fileName, "tsconfig.json", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "TypeScript", normalizedPath);
        }

        if (extension == ".py" || IsAnyFileName(fileName, "pyproject.toml", "requirements.txt", "setup.py"))
        {
            AddEvidence(evidenceByTechnology, "Python", normalizedPath);
        }

        if (string.Equals(fileName, "manage.py", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Django", normalizedPath);
        }

        if (extension == ".go" || string.Equals(fileName, "go.mod", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Go", normalizedPath);
        }

        if (extension == ".rs" || string.Equals(fileName, "Cargo.toml", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Rust", normalizedPath);
        }

        if (extension == ".java" || IsAnyFileName(fileName, "pom.xml", "build.gradle", "build.gradle.kts"))
        {
            AddEvidence(evidenceByTechnology, "Java", normalizedPath);
        }

        if (string.Equals(fileName, "pom.xml", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Maven", normalizedPath);
        }

        if (IsAnyFileName(fileName, "build.gradle", "build.gradle.kts", "settings.gradle"))
        {
            AddEvidence(evidenceByTechnology, "Gradle", normalizedPath);
        }

        if (extension is ".kt" or ".kts")
        {
            AddEvidence(evidenceByTechnology, "Kotlin", normalizedPath);
        }

        if (extension is ".jsx" or ".tsx")
        {
            AddEvidence(evidenceByTechnology, "React", normalizedPath);
        }

        if (string.Equals(fileName, "package.json", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Node.js", normalizedPath);
        }

        if (string.Equals(fileName, "package-lock.json", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "npm", normalizedPath);
        }

        if (string.Equals(fileName, "pnpm-lock.yaml", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "pnpm", normalizedPath);
        }

        if (string.Equals(fileName, "yarn.lock", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Yarn", normalizedPath);
        }

        if (extension == ".vue" || string.Equals(fileName, "vue.config.js", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Vue", normalizedPath);
        }

        if (string.Equals(fileName, "angular.json", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Angular", normalizedPath);
        }

        if (string.Equals(fileName, "Dockerfile", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".Dockerfile", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Docker", normalizedPath);
        }

        if (IsAnyFileName(fileName, "docker-compose.yml", "compose.yaml"))
        {
            AddEvidence(evidenceByTechnology, "Docker Compose", normalizedPath);
        }

        if (normalizedPath.StartsWith(".github/workflows/", StringComparison.OrdinalIgnoreCase)
            && (extension == ".yml" || extension == ".yaml"))
        {
            AddEvidence(evidenceByTechnology, "GitHub Actions", normalizedPath);
        }

        if (extension == ".tf")
        {
            AddEvidence(evidenceByTechnology, "Terraform", normalizedPath);
        }

        if (IsAnyFileName(fileName, "ansible.cfg", "playbook.yml") || normalizedPath.StartsWith("roles/", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Ansible", normalizedPath);
        }

        if (extension == ".sql")
        {
            AddEvidence(evidenceByTechnology, "SQL", normalizedPath);
        }

        if (extension == ".md" || string.Equals(fileName, "README.md", StringComparison.OrdinalIgnoreCase) || normalizedPath.StartsWith("docs/", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Markdown Documentation", normalizedPath);
        }

        if (string.Equals(fileName, "Makefile", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Make", normalizedPath);
        }

        if (string.Equals(fileName, "CMakeLists.txt", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "CMake", normalizedPath);
        }

        if (string.Equals(fileName, "Jenkinsfile", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Jenkins", normalizedPath);
        }

        if (IsAnyFileName(fileName, "deployment.yaml", "service.yaml", "ingress.yaml", "kustomization.yaml")
            || normalizedPath.StartsWith("k8s/", StringComparison.OrdinalIgnoreCase))
        {
            AddEvidence(evidenceByTechnology, "Kubernetes", normalizedPath);
        }

        if (string.Equals(fileName, "Chart.yaml", StringComparison.OrdinalIgnoreCase) || IsHelmValuesFile(repositoryRoot, normalizedPath))
        {
            AddEvidence(evidenceByTechnology, "Helm", normalizedPath);
        }
    }

    private static void AddContentBasedDetections(
        Dictionary<string, string> evidenceByTechnology,
        string repositoryRoot,
        FileStat file)
    {
        if (file.IsBinary || file.Bytes > MaxContentScanBytes)
        {
            return;
        }

        var fullPath = Path.Combine(repositoryRoot, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        string content;
        try
        {
            content = File.ReadAllText(fullPath);
        }
        catch
        {
            return;
        }

        var normalizedPath = file.RelativePath.Replace('\\', '/');
        var fileName = Path.GetFileName(normalizedPath);

        if (file.Extension is ".csproj" or ".fsproj" or ".vbproj")
        {
            foreach (Match match in Regex.Matches(content, "<PackageReference\\s+Include=\"([^\"]+)\"", RegexOptions.IgnoreCase))
            {
                var packageName = match.Groups[1].Value;
                foreach (var (fragment, technology) in CsprojPackages)
                {
                    if (packageName.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                    {
                        AddEvidence(evidenceByTechnology, technology, $"{normalizedPath}: {packageName}");
                    }
                }
            }
        }

        if (string.Equals(fileName, "package.json", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var (fragment, technology) in PackageJsonPackages)
            {
                if (content.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                {
                    AddEvidence(evidenceByTechnology, technology, normalizedPath);
                }
            }
        }

        if (IsAnyFileName(fileName, "requirements.txt", "pyproject.toml", "setup.py", "Pipfile"))
        {
            foreach (var (fragment, technology) in PythonPackages)
            {
                if (content.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                {
                    AddEvidence(evidenceByTechnology, technology, normalizedPath);
                }
            }
        }
    }

    private static void AddEvidence(
        Dictionary<string, string> evidenceByTechnology,
        string technology,
        string evidence)
    {
        var normalizedEvidence = evidence.Replace('\\', '/').Trim();
        if (evidenceByTechnology.TryGetValue(technology, out var existing))
        {
            if (ShouldReplaceEvidence(existing, normalizedEvidence))
            {
                evidenceByTechnology[technology] = normalizedEvidence;
            }

            return;
        }

        evidenceByTechnology.Add(technology, normalizedEvidence);
    }

    private static bool ShouldReplaceEvidence(string existing, string candidate)
    {
        if (candidate.Length != existing.Length)
        {
            return candidate.Length < existing.Length;
        }

        return string.CompareOrdinal(candidate, existing) < 0;
    }

    private static bool IsAnyFileName(string fileName, params string[] candidates)
    {
        return candidates.Any(candidate => string.Equals(fileName, candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsHelmValuesFile(string repositoryRoot, string normalizedPath)
    {
        if (!normalizedPath.EndsWith("/values.yaml", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var directory = Path.GetDirectoryName(normalizedPath.Replace('/', Path.DirectorySeparatorChar));
        if (string.IsNullOrEmpty(directory))
        {
            return false;
        }

        var chartPath = Path.Combine(repositoryRoot, directory, "Chart.yaml");
        return File.Exists(chartPath);
    }
}
