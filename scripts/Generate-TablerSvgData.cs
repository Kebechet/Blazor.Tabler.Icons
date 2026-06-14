// Downloads the @tabler/icons SVG package and generates the embedded dataset used by the
// SVG source generator (TablerSvgData.txt: "Name|||o|f|||innerSvgBody" per line).
//
// Canonical icons come from the package's outline/filled SVG files. Tabler font aliases
// (e.g. discount-2 -> rosette-discount) have no SVG file of their own, so they are filled in
// from aliases.json by pointing the alias name at the canonical icon's SVG body.
//
// Run (from the repo root):  dotnet run scripts/Generate-TablerSvgData.cs -- [version] [outputFile]

using System.Formats.Tar;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var version = args.Length > 0 ? args[0] : "3.44.0";
var scriptDir = Path.GetDirectoryName(GetScriptPath())!;
var repoRoot = Path.GetFullPath(Path.Combine(scriptDir, ".."));
var outputFile = args.Length > 1
    ? args[1]
    : Path.Combine(repoRoot, "src", "Blazor.Tabler.Icons.Svg.Generator", "TablerSvgData.txt");

var tarballUrl = $"https://registry.npmjs.org/@tabler/icons/-/icons-{version}.tgz";
var aliasesUrl = $"https://raw.githubusercontent.com/tabler/tabler-icons/v{version}/aliases.json";
var tempDir = Path.Combine(Path.GetTempPath(), $"tabler-svg-{Guid.NewGuid():N}");
Directory.CreateDirectory(tempDir);

using var http = new HttpClient();

try
{
    Console.WriteLine($"Downloading @tabler/icons {version} from {tarballUrl}...");
    var tgz = Path.Combine(tempDir, "icons.tgz");
    File.WriteAllBytes(tgz, await http.GetByteArrayAsync(tarballUrl));

    Console.WriteLine("Extracting...");
    await using (var fileStream = File.OpenRead(tgz))
    await using (var gzip = new GZipStream(fileStream, CompressionMode.Decompress))
    {
        await TarFile.ExtractToDirectoryAsync(gzip, tempDir, overwriteFiles: true);
    }

    var iconsRoot = Path.Combine(tempDir, "package", "icons");
    if (!Directory.Exists(iconsRoot))
    {
        throw new DirectoryNotFoundException($"Could not find {iconsRoot} in the extracted package");
    }

    // Canonical icons from the SVG files, written in (style, filename) order.
    var entries = new Dictionary<string, (string Flag, string Inner)>(StringComparer.Ordinal);
    var builder = new StringBuilder();
    var outlineCount = 0;
    var filledCount = 0;

    foreach (var (style, flag, suffix) in new[] { ("outline", "o", ""), ("filled", "f", "Filled") })
    {
        var styleDir = Path.Combine(iconsRoot, style);
        if (!Directory.Exists(styleDir))
        {
            Console.WriteLine($"Warning: missing style directory {styleDir}");
            continue;
        }

        foreach (var file in Directory.EnumerateFiles(styleDir, "*.svg").OrderBy(x => x, StringComparer.Ordinal))
        {
            var name = ToPascalCase(Path.GetFileNameWithoutExtension(file)) + suffix;
            var inner = GetInnerSvg(File.ReadAllText(file));
            entries[name] = (flag, inner);
            builder.Append(name).Append("|||").Append(flag).Append("|||").Append(inner).Append('\n');
            if (style == "outline")
            {
                outlineCount++;
            }
            else
            {
                filledCount++;
            }
        }
    }

    // Aliases: map each font-only alias name onto its canonical icon's SVG body.
    var aliasesJson = await GetAliasesJsonAsync(http, tempDir, aliasesUrl);
    var aliasLines = new List<string>();
    using (var doc = JsonDocument.Parse(aliasesJson))
    {
        foreach (var (section, suffix) in new[] { ("outline", ""), ("filled", "Filled") })
        {
            if (!doc.RootElement.TryGetProperty(section, out var sectionObject))
            {
                continue;
            }

            foreach (var alias in sectionObject.EnumerateObject())
            {
                var aliasName = ToPascalCase(alias.Name) + suffix;
                var canonicalName = ToPascalCase(alias.Value.GetString() ?? string.Empty) + suffix;
                if (entries.TryGetValue(canonicalName, out var canonical) && !entries.ContainsKey(aliasName))
                {
                    entries[aliasName] = canonical;
                    aliasLines.Add($"{aliasName}|||{canonical.Flag}|||{canonical.Inner}");
                }
            }
        }
    }

    aliasLines.Sort(StringComparer.Ordinal);
    foreach (var line in aliasLines)
    {
        builder.Append(line).Append('\n');
    }

    Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
    File.WriteAllText(outputFile, builder.ToString().TrimEnd(), new UTF8Encoding(false));
    Console.WriteLine($"Wrote {outlineCount} outline + {filledCount} filled + {aliasLines.Count} aliases to {outputFile}");
}
finally
{
    if (Directory.Exists(tempDir))
    {
        Directory.Delete(tempDir, recursive: true);
    }
}

static async Task<string> GetAliasesJsonAsync(HttpClient http, string tempDir, string aliasesUrl)
{
    var packageAliases = Path.Combine(tempDir, "package", "aliases.json");
    if (File.Exists(packageAliases))
    {
        return File.ReadAllText(packageAliases);
    }

    Console.WriteLine($"Downloading aliases from {aliasesUrl}...");
    return await http.GetStringAsync(aliasesUrl);
}

static string GetInnerSvg(string svg)
{
    var inner = Regex.Replace(svg, "(?s)^.*?<svg[^>]*>", "");
    inner = Regex.Replace(inner, @"(?s)</svg>\s*$", "");
    inner = Regex.Replace(inner, @"\s+", " ");
    return inner.Trim();
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
