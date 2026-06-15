using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blazor.Tabler.Icons.Svg.Generator;

/// <summary>
/// Emits a module initializer into the consuming assembly that registers the SVG body for
/// only the <c>TablerIconType</c> members the consumer actually references. Icons that are
/// never referenced are never generated, so the trimmer/bundle only ever carries used icons.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class TablerSvgGenerator : IIncrementalGenerator
{
    private const string EnumMetadataName = "Blazor.Tabler.Icons.TablerIconType";
    private const string IncludeAttributeMetadataName = "Blazor.Tabler.Icons.Svg.IncludeTablerIconsAttribute";
    private const string ComponentName = "TablerIcon";

    private static readonly DiagnosticDescriptor DynamicUsageRule = new(
        id: "TABLERSVG001",
        title: "Dynamically selected Tabler icon",
        messageFormat: "A <TablerIcon> uses a non-constant Type; the generator cannot include it automatically. Add [assembly: IncludeTablerIcons(...)] for icons chosen at runtime.",
        category: "TablerIcons",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingDataRule = new(
        id: "TABLERSVG002",
        title: "Tabler icon has no SVG data",
        messageFormat: "Icon '{0}' has no SVG data in this build; it will render empty with the SVG backend.",
        category: "TablerIcons",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Any `<identifier>.<member>` is a candidate; the semantic check resolves the symbol, so
        // references through an enum alias (using X = TablerIconType) are matched too.
        var csharpNames = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax },
                transform: static (ctx, _) =>
                {
                    var maes = (MemberAccessExpressionSyntax) ctx.Node;
                    var symbol = ctx.SemanticModel.GetSymbolInfo(maes).Symbol;
                    if (symbol is IFieldSymbol field &&
                        field.ContainingType?.ToDisplayString() == EnumMetadataName)
                    {
                        return field.Name;
                    }

                    return null;
                })
            .Where(static name => name is not null)
            .Collect();

        // Enum aliases (using/global using X = TablerIconType) discovered from the compilation,
        // so markup text like "X.Plus" can be scanned without hardcoding any alias name.
        var aliasNames = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is UsingDirectiveSyntax { Alias: not null },
                transform: static (ctx, _) =>
                {
                    var usingDirective = (UsingDirectiveSyntax) ctx.Node;
                    if (usingDirective.Name is null)
                    {
                        return null;
                    }

                    var symbol = ctx.SemanticModel.GetSymbolInfo(usingDirective.Name).Symbol;
                    return symbol is INamedTypeSymbol named && named.ToDisplayString() == EnumMetadataName
                        ? usingDirective.Alias!.Name.Identifier.Text
                        : null;
                })
            .Where(static name => name is not null)
            .Collect();

        var markupScans = context.AdditionalTextsProvider
            .Where(static at =>
                at.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
                at.Path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            .Combine(aliasNames)
            .Select(static (pair, ct) => ScanMarkup(pair.Left.GetText(ct)?.ToString() ?? string.Empty, pair.Right))
            .Collect();

        var input = csharpNames.Combine(markupScans).Combine(context.CompilationProvider);

        context.RegisterSourceOutput(input, static (spc, data) =>
        {
            var ((csharp, scans), compilation) = data;
            Execute(spc, csharp, scans, compilation);
        });
    }

    private static void Execute(
        SourceProductionContext spc,
        ImmutableArray<string?> csharpNames,
        ImmutableArray<MarkupScan> scans,
        Compilation compilation)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in csharpNames)
        {
            if (name is not null)
            {
                used.Add(name);
            }
        }

        var hasDynamicUsage = false;
        foreach (var scan in scans)
        {
            foreach (var name in scan.Names)
            {
                used.Add(name);
            }

            hasDynamicUsage |= scan.HasDynamicUsage;
        }

        foreach (var name in ReadIncludeAttributeNames(compilation))
        {
            used.Add(name);
        }

        if (hasDynamicUsage)
        {
            spc.ReportDiagnostic(Diagnostic.Create(DynamicUsageRule, Location.None));
        }

        var dataset = Dataset.Value;
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine("namespace Blazor.Tabler.Icons.Svg.Generated");
        builder.AppendLine("{");
        builder.AppendLine("    internal static class __TablerSvgRegistrations");
        builder.AppendLine("    {");
        builder.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
        builder.AppendLine("        internal static void Initialize()");
        builder.AppendLine("        {");

        foreach (var name in used.OrderBy(static x => x, StringComparer.Ordinal))
        {
            if (!dataset.TryGetValue(name, out var entry))
            {
                spc.ReportDiagnostic(Diagnostic.Create(MissingDataRule, Location.None, name));
                continue;
            }

            var literal = SymbolDisplay.FormatLiteral(entry.Inner, quote: true);
            var isFilled = entry.IsFilled ? "true" : "false";
            builder.AppendLine(
                $"            global::Blazor.Tabler.Icons.Svg.TablerSvgRegistry.Register(global::Blazor.Tabler.Icons.TablerIconType.{name}, {isFilled}, {literal});");
        }

        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        spc.AddSource("TablerSvgRegistrations.g.cs", builder.ToString());
    }

    private static IEnumerable<string> ReadIncludeAttributeNames(Compilation compilation)
    {
        var enumType = compilation.GetTypeByMetadataName(EnumMetadataName);
        if (enumType is null)
        {
            yield break;
        }

        var valueToName = new Dictionary<object, string>();
        foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.HasConstantValue && member.ConstantValue is not null)
            {
                valueToName[member.ConstantValue] = member.Name;
            }
        }

        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != IncludeAttributeMetadataName)
            {
                continue;
            }

            foreach (var argument in attribute.ConstructorArguments)
            {
                foreach (var element in argument.Kind == TypedConstantKind.Array ? argument.Values : ImmutableArray.Create(argument))
                {
                    if (element.Value is not null && valueToName.TryGetValue(element.Value, out var name))
                    {
                        yield return name;
                    }
                }
            }
        }
    }

    private static MarkupScan ScanMarkup(string text, ImmutableArray<string?> aliases)
    {
        var tokens = new List<string> { "TablerIconType" };
        foreach (var alias in aliases)
        {
            if (!string.IsNullOrEmpty(alias) && !tokens.Contains(alias!))
            {
                tokens.Add(alias!);
            }
        }

        var names = new List<string>();
        foreach (var token in tokens)
        {
            var search = token + ".";
            var index = 0;
            while ((index = text.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
            {
                // Require a word boundary before the token so the "IconType" alias does not match
                // inside "TablerIconType." and a token does not match mid-identifier.
                if (index > 0 && (char.IsLetterOrDigit(text[index - 1]) || text[index - 1] == '_'))
                {
                    index += search.Length;
                    continue;
                }

                var start = index + search.Length;
                var end = start;
                while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_'))
                {
                    end++;
                }

                if (end > start)
                {
                    names.Add(text.Substring(start, end - start));
                }

                index = end;
            }
        }

        var hasDynamicUsage = false;
        foreach (Match match in ComponentTypeAttribute.Matches(text))
        {
            var value = match.Groups["v"].Value;
            if (!tokens.Any(token => value.Contains(token + ".")))
            {
                hasDynamicUsage = true;
                break;
            }
        }

        return new MarkupScan(names.ToImmutableArray(), hasDynamicUsage);
    }

    private static readonly Regex ComponentTypeAttribute = new(
        "<" + ComponentName + "\\b[^>]*?\\bType\\s*=\\s*\"(?<v>[^\"]*)\"",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Lazy<IReadOnlyDictionary<string, SvgEntry>> Dataset = new(LoadDataset);

    private static IReadOnlyDictionary<string, SvgEntry> LoadDataset()
    {
        var map = new Dictionary<string, SvgEntry>(StringComparer.Ordinal);
        var assembly = typeof(TablerSvgGenerator).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(static n => n.EndsWith("TablerSvgData.txt", StringComparison.Ordinal));
        if (resourceName is null)
        {
            return map;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return map;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0)
            {
                continue;
            }

            var parts = line.Split(new[] { "|||" }, 3, StringSplitOptions.None);
            if (parts.Length != 3)
            {
                continue;
            }

            map[parts[0]] = new SvgEntry(parts[1] == "f", parts[2]);
        }

        return map;
    }

    private readonly struct MarkupScan
    {
        public MarkupScan(ImmutableArray<string> names, bool hasDynamicUsage)
        {
            Names = names;
            HasDynamicUsage = hasDynamicUsage;
        }

        public ImmutableArray<string> Names { get; }
        public bool HasDynamicUsage { get; }
    }

    private readonly struct SvgEntry
    {
        public SvgEntry(bool isFilled, string inner)
        {
            IsFilled = isFilled;
            Inner = inner;
        }

        public bool IsFilled { get; }
        public string Inner { get; }
    }
}
