namespace RepoBook.Scanning;

public sealed class LanguageDetector
{
    private static readonly Dictionary<string, string> ExtensionMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = "C#",
        [".fs"] = "F#",
        [".vb"] = "Visual Basic",
        [".js"] = "JavaScript",
        [".jsx"] = "JavaScript",
        [".mjs"] = "JavaScript",
        [".cjs"] = "JavaScript",
        [".ts"] = "TypeScript",
        [".tsx"] = "TypeScript",
        [".py"] = "Python",
        [".go"] = "Go",
        [".rs"] = "Rust",
        [".java"] = "Java",
        [".kt"] = "Kotlin",
        [".kts"] = "Kotlin",
        [".swift"] = "Swift",
        [".c"] = "C",
        [".h"] = "C/C++ Header",
        [".cpp"] = "C++",
        [".cc"] = "C++",
        [".cxx"] = "C++",
        [".hpp"] = "C++ Header",
        [".rb"] = "Ruby",
        [".php"] = "PHP",
        [".scala"] = "Scala",
        [".sh"] = "Shell",
        [".bash"] = "Shell",
        [".zsh"] = "Shell",
        [".ps1"] = "PowerShell",
        [".sql"] = "SQL",
        [".html"] = "HTML",
        [".htm"] = "HTML",
        [".css"] = "CSS",
        [".scss"] = "SCSS",
        [".sass"] = "Sass",
        [".less"] = "Less",
        [".json"] = "JSON",
        [".yaml"] = "YAML",
        [".yml"] = "YAML",
        [".xml"] = "XML",
        [".md"] = "Markdown",
        [".txt"] = "Text",
        [".dockerfile"] = "Dockerfile",
        [".tf"] = "Terraform",
        [".hcl"] = "HCL",
        [".lua"] = "Lua",
        [".r"] = "R",
        [".dart"] = "Dart",
        [".ex"] = "Elixir",
        [".exs"] = "Elixir",
        [".erl"] = "Erlang",
        [".hrl"] = "Erlang",
        [".clj"] = "Clojure",
        [".cljs"] = "Clojure",
    };

    private static readonly Dictionary<string, string> FileNameMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dockerfile"] = "Dockerfile",
        ["dockerfile"] = "Dockerfile",
        ["Makefile"] = "Makefile",
        ["CMakeLists.txt"] = "CMake",
        ["Jenkinsfile"] = "Jenkins",
    };

    public string DetectLanguage(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (FileNameMappings.TryGetValue(fileName, out var mappedFileNameLanguage))
        {
            return mappedFileNameLanguage;
        }

        var extension = Path.GetExtension(filePath);
        if (ExtensionMappings.TryGetValue(extension, out var mappedExtensionLanguage))
        {
            return mappedExtensionLanguage;
        }

        return "Other";
    }
}
