using System.Collections.Immutable;
using Blazor.Tabler.Icons;
using Blazor.Tabler.Icons.Svg;
using Blazor.Tabler.Icons.Svg.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Blazor.Tabler.Icons.Svg.Tests;

public class SvgGeneratorTests
{
    [Fact]
    public void Generates_OnlyReferencedIcons_FromCSharp()
    {
        // Arrange
        const string source = """
            using Blazor.Tabler.Icons;
            public static class Usage
            {
                public static readonly TablerIconType A = TablerIconType.Plus;
                public static readonly TablerIconType B = TablerIconType.X;
            }
            """;

        // Act
        var (generated, _) = RunGenerator(source);

        // Assert
        Assert.Contains("TablerIconType.Plus", generated);
        Assert.Contains("TablerIconType.X", generated);
        Assert.DoesNotContain("TablerIconType.Home,", generated);
        Assert.Equal(2, CountRegistrations(generated));
    }

    [Fact]
    public void Generates_IconsReferenced_InRazorMarkup()
    {
        // Arrange
        var razor = new InMemoryAdditionalText(
            "Widget.razor",
            "<TablerIcon Type=\"TablerIconType.Home\" />");

        // Act
        var (generated, _) = RunGenerator("public class Empty { }", razor);

        // Assert
        Assert.Contains("TablerIconType.Home,", generated);
        Assert.Equal(1, CountRegistrations(generated));
    }

    [Fact]
    public void Generates_IconsReferenced_ViaAlias_InCSharp()
    {
        // Arrange - referenced through an enum alias of any name
        const string source = """
            using Whatever = Blazor.Tabler.Icons.TablerIconType;
            public static class Usage
            {
                public static readonly Whatever A = Whatever.Plus;
            }
            """;

        // Act
        var (generated, _) = RunGenerator(source);

        // Assert
        Assert.Contains("TablerIconType.Plus", generated);
    }

    [Fact]
    public void Generates_IconsReferenced_ViaAlias_InRazor()
    {
        // Arrange - alias declared as a C# global using; razor references it
        const string source = "global using Whatever = Blazor.Tabler.Icons.TablerIconType;";
        var razor = new InMemoryAdditionalText(
            "Widget.razor",
            "<TablerIcon Type=\"Whatever.Home\" />");

        // Act
        var (generated, _) = RunGenerator(source, razor);

        // Assert
        Assert.Contains("TablerIconType.Home,", generated);
    }

    [Fact]
    public void Generates_FilledIcon_WithFilledFlag()
    {
        // Arrange
        const string source = """
            using Blazor.Tabler.Icons;
            public static class Usage
            {
                public static readonly TablerIconType A = TablerIconType.HomeFilled;
            }
            """;

        // Act
        var (generated, _) = RunGenerator(source);

        // Assert
        Assert.Contains("TablerIconType.HomeFilled, true", generated);
    }

    [Fact]
    public void Includes_IconsFromAttribute_ChosenAtRuntime()
    {
        // Arrange
        const string source = """
            using Blazor.Tabler.Icons;
            using Blazor.Tabler.Icons.Svg;
            [assembly: IncludeTablerIcons(TablerIconType.BrandGithub)]
            """;

        // Act
        var (generated, _) = RunGenerator(source);

        // Assert
        Assert.Contains("TablerIconType.BrandGithub", generated);
    }

    [Fact]
    public void Reports_DynamicUsageDiagnostic_ForNonConstantType()
    {
        // Arrange
        var razor = new InMemoryAdditionalText(
            "Widget.razor",
            "<TablerIcon Type=\"@_icon\" />");

        // Act
        var (_, diagnostics) = RunGenerator("public class Empty { }", razor);

        // Assert
        Assert.Contains(diagnostics, d => d.Id == "TABLERSVG001");
    }

    [Fact]
    public void IncludeAll_RegistersIcon_ReferencedNowhere()
    {
        // Arrange - no icon is referenced anywhere; only the include-all attribute is present.
        const string source = """
            using Blazor.Tabler.Icons.Svg;
            [assembly: IncludeAllTablerIcons]
            """;

        // Act
        var (generated, _) = RunGenerator(source);

        // Assert - an icon used in no source file is still registered, and the set is large.
        Assert.Contains("TablerIconType.Home,", generated);
        Assert.True(CountRegistrations(generated) > 1000);
    }

    [Fact]
    public void IncludeAll_SuppressesDynamicUsageDiagnostic()
    {
        // Arrange - dynamic usage that would normally raise TABLERSVG001, plus include-all.
        const string source = """
            using Blazor.Tabler.Icons.Svg;
            [assembly: IncludeAllTablerIcons]
            """;
        var razor = new InMemoryAdditionalText(
            "Widget.razor",
            "<TablerIcon Type=\"@_icon\" />");

        // Act
        var (_, diagnostics) = RunGenerator(source, razor);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "TABLERSVG001");
        Assert.Contains(diagnostics, d => d.Id == "TABLERSVG003");
    }

    [Fact]
    public void Resolves_FontAlias_ToCanonicalSvg()
    {
        // Arrange - Discount2 is a font alias (-> rosette-discount); alias resolution fills its SVG.
        const string source = """
            using Blazor.Tabler.Icons;
            public static class Usage
            {
                public static readonly TablerIconType A = TablerIconType.Discount2;
            }
            """;

        // Act
        var (generated, diagnostics) = RunGenerator(source);

        // Assert
        Assert.Contains("TablerIconType.Discount2", generated);
        Assert.DoesNotContain(diagnostics, d => d.Id == "TABLERSVG002");
    }

    private static (string Generated, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(
        string source,
        params InMemoryAdditionalText[] additionalTexts)
    {
        var compilation = CSharpCompilation.Create(
            "Tests.Generated",
            new[] { CSharpSyntaxTree.ParseText(source) },
            BuildReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver
            .Create(new TablerSvgGenerator().AsSourceGenerator())
            .AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalTexts));

        driver = driver.RunGenerators(compilation);

        var result = driver.GetRunResult();
        var generated = result.Results
            .Single()
            .GeneratedSources
            .Single()
            .SourceText
            .ToString();

        return (generated, result.Diagnostics);
    }

    private static int CountRegistrations(string generated)
    {
        var count = 0;
        var index = 0;
        while ((index = generated.IndexOf(".Register(", index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += ".Register(".Length;
        }

        return count;
    }

    private static MetadataReference[] BuildReferences()
    {
        var byName = new Dictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);
        var trustedPlatformAssemblies = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty)
            .Split(Path.PathSeparator)
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
        foreach (var path in trustedPlatformAssemblies)
        {
            byName[Path.GetFileName(path)] = MetadataReference.CreateFromFile(path);
        }

        foreach (var assembly in new[] { typeof(TablerIconType).Assembly, typeof(IncludeTablerIconsAttribute).Assembly })
        {
            byName[Path.GetFileName(assembly.Location)] = MetadataReference.CreateFromFile(assembly.Location);
        }

        return byName.Values.ToArray();
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly string _text;

        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            _text = text;
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(_text);
        }
    }
}
