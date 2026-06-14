// Downloads the Tabler Icons webfont and generates the C# enum (-> Core), CSS-class constants
// and extension (-> font package), plus the processed CSS and woff2 fonts (-> font wwwroot).
//
// Run (from the repo root):  dotnet run scripts/Generate-TablerIcons.cs -- [version] [outputDir] [coreDir]
//   version    Tabler Icons version to fetch (default 3.44.0)
//   outputDir  font package dir (default src/Blazor.Tabler.Icons)
//   coreDir    shared enum dir  (default src/Core)

using System.Formats.Tar;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

var version = args.Length > 0 ? args[0] : "3.44.0";
var scriptDir = Path.GetDirectoryName(GetScriptPath())!;
var repoRoot = Path.GetFullPath(Path.Combine(scriptDir, ".."));
var outputDir = args.Length > 1 ? args[1] : Path.Combine(repoRoot, "src", "Blazor.Tabler.Icons");
var coreDir = args.Length > 2 ? args[2] : Path.Combine(repoRoot, "src", "Core");

var tarballUrl = $"https://registry.npmjs.org/@tabler/icons-webfont/-/icons-webfont-{version}.tgz";
var tempDir = Path.Combine(Path.GetTempPath(), $"tabler-icons-{Guid.NewGuid():N}");
Directory.CreateDirectory(tempDir);

try
{
    Console.WriteLine($"Downloading Tabler Icons webfont {version} from {tarballUrl}...");
    using var http = new HttpClient();
    var tgz = Path.Combine(tempDir, "icons-webfont.tgz");
    File.WriteAllBytes(tgz, await http.GetByteArrayAsync(tarballUrl));

    Console.WriteLine("Extracting archive...");
    await using (var fileStream = File.OpenRead(tgz))
    await using (var gzip = new GZipStream(fileStream, CompressionMode.Decompress))
    {
        await TarFile.ExtractToDirectoryAsync(gzip, tempDir, overwriteFiles: true);
    }

    var outlineCss = FindFile(tempDir, "tabler-icons.css");
    var filledCss = FindFile(tempDir, "tabler-icons-filled.css");

    var outlineIcons = ParseIconsCss(outlineCss, suffix: "");
    Console.WriteLine($"Found {outlineIcons.Count} outline icons");
    var filledIcons = ParseIconsCss(filledCss, suffix: "-filled");
    Console.WriteLine($"Found {filledIcons.Count} filled icons");

    var allIcons = new Dictionary<string, IconInfo>(StringComparer.Ordinal);
    foreach (var icon in outlineIcons)
    {
        allIcons[icon.Key] = icon.Value;
    }

    foreach (var icon in filledIcons)
    {
        allIcons[icon.Key] = icon.Value;
    }

    if (allIcons.Count == 0)
    {
        throw new InvalidOperationException("No icons found in CSS files");
    }

    var sortedIcons = allIcons.OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
    Console.WriteLine($"Total icons: {sortedIcons.Count}");

    var enumBuilder = new StringBuilder();
    enumBuilder.Append("namespace Blazor.Tabler.Icons;\n\n");
    enumBuilder.Append("/// <summary>\n");
    enumBuilder.Append("/// Enumeration of all available Tabler icons (outline and filled variants).\n");
    enumBuilder.Append("/// </summary>\n");
    enumBuilder.Append("public enum TablerIconType\n{\n");
    for (var index = 0; index < sortedIcons.Count; index++)
    {
        enumBuilder.Append("    ").Append(ToPascalCase(sortedIcons[index].Key));
        enumBuilder.Append(index < sortedIcons.Count - 1 ? ",\n" : "\n");
    }

    enumBuilder.Append("}\n");

    var constantsBuilder = new StringBuilder();
    constantsBuilder.Append("namespace Blazor.Tabler.Icons;\n\n");
    constantsBuilder.Append("/// <summary>\n");
    constantsBuilder.Append("/// CSS class constants for Tabler icons.\n");
    constantsBuilder.Append("/// </summary>\n");
    constantsBuilder.Append("public static class TablerIconConstants\n{\n");
    foreach (var icon in sortedIcons)
    {
        var pascalName = ToPascalCase(icon.Key);
        var cssClass = icon.Value.IsFilled
            ? $"tif tif-{icon.Value.OriginalName}"
            : $"ti ti-{icon.Value.OriginalName}";
        constantsBuilder.Append("    public const string ").Append(pascalName).Append(" = \"").Append(cssClass).Append("\";\n");
    }

    constantsBuilder.Append("}\n");

    var extensionBuilder = new StringBuilder();
    extensionBuilder.Append("namespace Blazor.Tabler.Icons;\n\n");
    extensionBuilder.Append("/// <summary>\n");
    extensionBuilder.Append("/// Extension methods for <see cref=\"TablerIconType\"/>.\n");
    extensionBuilder.Append("/// </summary>\n");
    extensionBuilder.Append("public static class TablerIconTypeExtensions\n{\n");
    extensionBuilder.Append("    /// <summary>\n");
    extensionBuilder.Append("    /// Converts the icon to its CSS class string.\n");
    extensionBuilder.Append("    /// </summary>\n");
    extensionBuilder.Append("    public static string ToCssClass(this TablerIconType icon) => icon switch\n    {\n");
    foreach (var icon in sortedIcons)
    {
        var pascalName = ToPascalCase(icon.Key);
        extensionBuilder.Append("        TablerIconType.").Append(pascalName).Append(" => TablerIconConstants.").Append(pascalName).Append(",\n");
    }

    extensionBuilder.Append("        _ => throw new System.ArgumentOutOfRangeException(nameof(icon), icon, \"Unknown icon\")\n");
    extensionBuilder.Append("    };\n}\n");

    // The enum is the shared contract consumed by both packages, so it lives in Core; the
    // font-specific constants/extensions stay with the font package.
    Directory.CreateDirectory(coreDir);
    var encoding = new UTF8Encoding(false);
    File.WriteAllText(Path.Combine(coreDir, "TablerIconType.cs"), enumBuilder.ToString(), encoding);
    File.WriteAllText(Path.Combine(outputDir, "TablerIconConstants.cs"), constantsBuilder.ToString(), encoding);
    File.WriteAllText(Path.Combine(outputDir, "TablerIconTypeExtensions.cs"), extensionBuilder.ToString(), encoding);

    var wwwrootDir = Path.Combine(outputDir, "wwwroot");
    var fontsDir = Path.Combine(wwwrootDir, "fonts");
    Directory.CreateDirectory(fontsDir);

    WriteProcessedCss(FindFile(tempDir, "tabler-icons.min.css"), Path.Combine(wwwrootDir, "tabler-icons.min.css"), isFilled: false);
    WriteProcessedCss(FindFile(tempDir, "tabler-icons-filled.min.css"), Path.Combine(wwwrootDir, "tabler-icons-filled.min.css"), isFilled: true);

    foreach (var fontName in new[] { "tabler-icons.woff2", "tabler-icons-filled.woff2" })
    {
        var source = FindFile(tempDir, fontName);
        File.Copy(source, Path.Combine(fontsDir, fontName), overwrite: true);
        Console.WriteLine($"  - Copied {fontName}");
    }

    Console.WriteLine($"Done! Generated {sortedIcons.Count} icons ({outlineIcons.Count} outline + {filledIcons.Count} filled).");
}
finally
{
    if (Directory.Exists(tempDir))
    {
        Directory.Delete(tempDir, recursive: true);
    }
}

static Dictionary<string, IconInfo> ParseIconsCss(string cssPath, string suffix)
{
    var css = File.ReadAllText(cssPath);
    var icons = new Dictionary<string, IconInfo>(StringComparer.Ordinal);
    var matches = Regex.Matches(css, @"\.ti-([a-z0-9-]+):before\s*\{\s*content:\s*""\\([a-fA-F0-9]+)""");
    foreach (Match match in matches)
    {
        var iconName = match.Groups[1].Value;
        if (iconName == "ti")
        {
            continue;
        }

        icons[iconName + suffix] = new IconInfo(iconName, suffix == "-filled");
    }

    return icons;
}

static void WriteProcessedCss(string sourcePath, string targetPath, bool isFilled)
{
    var css = File.ReadAllText(sourcePath);
    css = Regex.Replace(css, @"\.\./fonts/", "fonts/");
    css = Regex.Replace(css, @",url\(""[^""]+\.woff\?[^""]*""\)\s*format\(""woff""\)", "");
    css = Regex.Replace(css, @",url\(""[^""]+\.ttf[^""]*""\)\s*format\(""truetype""\)", "");
    if (isFilled)
    {
        css = Regex.Replace(css, @"\.ti\{", ".tif{");
        css = Regex.Replace(css, @"\.ti-([a-z0-9-]+):before", ".tif-$1:before");
    }

    File.WriteAllText(targetPath, css, new UTF8Encoding(false));
    Console.WriteLine($"  - Copied {Path.GetFileName(targetPath)}");
}

static string FindFile(string root, string fileName)
{
    var match = Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories).FirstOrDefault();
    return match ?? throw new FileNotFoundException($"Could not find {fileName} in the extracted package");
}

static string ToPascalCase(string text)
{
    var builder = new StringBuilder();
    foreach (var part in text.Split('-', StringSplitOptions.RemoveEmptyEntries))
    {
        builder.Append(char.ToUpperInvariant(part[0])).Append(part.AsSpan(1));
    }

    var result = builder.ToString();
    if (result.Length > 0 && char.IsDigit(result[0]))
    {
        result = "_" + result;
    }

    return result;
}

static string GetScriptPath([CallerFilePath] string path = "")
{
    return path;
}

readonly record struct IconInfo(string OriginalName, bool IsFilled);
