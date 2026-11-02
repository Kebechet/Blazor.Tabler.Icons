using System.Reflection;
using Blazor.Tabler.Icons;
using Blazor.Tabler.Icons.Svg;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Blazor.Tabler.Icons.Svg.Tests;

[Collection(RegistryCollection.Name)]
public class TablerIconSpriteTests : BunitContext
{
    private const string OutlineSvgOpenTag = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1em\" height=\"1em\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" class=\"tabler-icon\">";
    private const string HomeUse = "<use href=\"#tabler-icon-Home\"></use>";

    private const string BellInner = "<path stroke=\"none\" d=\"M0 0h24v24H0z\" fill=\"none\" /> <path d=\"M10 5a2 2 0 1 1 4 0a7 7 0 0 1 4 6v3a4 4 0 0 0 2 3h-16a4 4 0 0 0 2 -3v-3a7 7 0 0 1 4 -6\" /> <path d=\"M9 17v1a3 3 0 0 0 6 0v-1\" />";

    public TablerIconSpriteTests()
    {
        TestIcons.Register();
    }

    [Fact]
    public void SpriteMode_OutlineIcon_ReplacesThePathsWithAUseReference()
    {
        // Act
        var cut = Render<TablerIcon>(parameters => parameters
            .Add(x => x.Type, TablerIconType.Home)
            .Add(x => x.SvgMode, TablerSvgMode.Sprite));

        // Assert - same <svg> element as inline mode, only its body differs
        Assert.Equal($"{OutlineSvgOpenTag}{HomeUse}</svg>", cut.Markup);
        Assert.DoesNotContain("<path", cut.Markup);
    }

    [Fact]
    public void SpriteMode_FilledIcon_KeepsFillAttributesClassStyleAndCapturedAttributes()
    {
        // Act
        var cut = Render<TablerIcon>(parameters => parameters
            .Add(x => x.Type, TablerIconType.HomeFilled)
            .Add(x => x.SvgMode, TablerSvgMode.Sprite)
            .Add(x => x.Class, "size-6 text-red-500")
            .Add(x => x.Style, "opacity:.5")
            .AddUnmatched("aria-label", "Home"));

        // Assert
        Assert.Equal(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1em\" height=\"1em\" viewBox=\"0 0 24 24\" fill=\"currentColor\" stroke=\"none\" stroke-linecap=\"round\" stroke-linejoin=\"round\" class=\"tabler-icon size-6 text-red-500\" style=\"opacity:.5\" aria-label=\"Home\">" +
            "<use href=\"#tabler-icon-HomeFilled\"></use>" +
            "</svg>",
            cut.Markup);
    }

    [Fact]
    public void SpriteMode_UnregisteredIcon_RendersNothing()
    {
        // Act
        var cut = Render<TablerIcon>(parameters => parameters
            .Add(x => x.Type, TablerIconType.ZzzOff)
            .Add(x => x.SvgMode, TablerSvgMode.Sprite));

        // Assert
        Assert.Equal(string.Empty, cut.Markup);
    }

    [Fact]
    public void Host_RendersEveryRegisteredIconOnce_AsAPrefixedSymbol()
    {
        // Arrange - registering an icon again, as a second assembly's module initializer would
        TestIcons.Register();

        // Act
        var cut = Render<TablerIconSprite>();

        // Assert
        var symbolIds = cut.FindAll("symbol")
            .Select(x => x.Id)
            .ToList();
        var expectedSymbolIds = TablerSvgRegistry.GetEntries()
            .Select(x => $"tabler-icon-{x.Key}")
            .ToList();
        Assert.Equal(expectedSymbolIds.OrderBy(x => x, StringComparer.Ordinal), symbolIds.OrderBy(x => x, StringComparer.Ordinal));
        Assert.Contains($"<symbol id=\"tabler-icon-Home\" viewBox=\"0 0 24 24\">{TestIcons.HomeInner}</symbol>", cut.Markup);
        Assert.Contains($"<symbol id=\"tabler-icon-HomeFilled\" viewBox=\"0 0 24 24\">{TestIcons.HomeFilledInner}</symbol>", cut.Markup);
    }

    [Fact]
    public void Host_RendersTheSymbolsInAHiddenZeroSizeSvg()
    {
        // Act
        var cut = Render<TablerIconSprite>();

        // Assert
        Assert.StartsWith(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"0\" height=\"0\" aria-hidden=\"true\" style=\"position:absolute;width:0;height:0;overflow:hidden\"><symbol ",
            cut.Markup);
    }

    [Fact]
    public void Host_TurnsOnSpriteMode_ForIconsInItsChildContent()
    {
        // Act
        var cut = Render<TablerIconSprite>(parameters => parameters
            .AddChildContent<TablerIcon>(icon => icon
                .Add(x => x.Type, TablerIconType.Home)));

        // Assert
        Assert.Equal($"{OutlineSvgOpenTag}{HomeUse}</svg>", cut.FindComponent<TablerIcon>().Markup);
    }

    [Fact]
    public void PerInstanceInline_WinsOverTheHost()
    {
        // Act
        var cut = Render<TablerIconSprite>(parameters => parameters
            .AddChildContent<TablerIcon>(icon => icon
                .Add(x => x.Type, TablerIconType.Home)
                .Add(x => x.SvgMode, TablerSvgMode.Inline)));

        // Assert
        Assert.Equal($"{OutlineSvgOpenTag}{TestIcons.HomeInner}</svg>", cut.FindComponent<TablerIcon>().Markup);
    }

    [Fact]
    public void CascadedInline_InsideTheHost_RendersThatSubtreeInline()
    {
        // Act - e.g. a card that html-to-image captures, inside an app that uses the sprite
        var cut = Render<TablerIconSprite>(parameters => parameters
            .AddChildContent<CascadingValue<TablerSvgMode>>(cascade => cascade
                .Add(x => x.Value, TablerSvgMode.Inline)
                .Add(x => x.IsFixed, true)
                .AddChildContent<TablerIcon>(icon => icon
                    .Add(x => x.Type, TablerIconType.Home))));

        // Assert
        Assert.Equal($"{OutlineSvgOpenTag}{TestIcons.HomeInner}</svg>", cut.FindComponent<TablerIcon>().Markup);
    }

    [Fact]
    public void PerInstanceSprite_WinsOverACascadedInline()
    {
        // Act
        var cut = Render<CascadingValue<TablerSvgMode>>(parameters => parameters
            .Add(x => x.Value, TablerSvgMode.Inline)
            .Add(x => x.IsFixed, true)
            .AddChildContent<TablerIcon>(icon => icon
                .Add(x => x.Type, TablerIconType.Home)
                .Add(x => x.SvgMode, TablerSvgMode.Sprite)));

        // Assert
        Assert.Equal($"{OutlineSvgOpenTag}{HomeUse}</svg>", cut.FindComponent<TablerIcon>().Markup);
    }

    [Fact]
    public void RootLevelCascadingValue_TurnsOnSpriteMode()
    {
        // Arrange
        Services.AddCascadingValue(_ => TablerSvgMode.Sprite);

        // Act
        var cut = Render<TablerIcon>(parameters => parameters
            .Add(x => x.Type, TablerIconType.Home));

        // Assert
        Assert.Equal($"{OutlineSvgOpenTag}{HomeUse}</svg>", cut.Markup);
    }

    [Fact]
    public void Host_AddsTheSymbol_ForAnIconRegisteredAfterItRendered()
    {
        // Arrange - an assembly whose module initializer only runs once one of its components is first used
        var cut = Render<TablerIconSprite>();
        Assert.Empty(cut.FindAll("symbol#tabler-icon-Bell"));

        // Act
        TablerSvgRegistry.Register(TablerIconType.Bell, isFilled: false, inner: BellInner);

        // Assert
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("symbol#tabler-icon-Bell")));
    }

    [Fact]
    public async Task Host_StopsListeningForRegistrations_WhenDisposed()
    {
        // Arrange
        var cut = Render<TablerIconSprite>();
        var spriteSheet = cut.FindComponent<TablerSpriteSheet>().Instance;
        Assert.Contains(spriteSheet, GetIconRegisteredSubscribers());

        // Act
        await DisposeComponentsAsync();
        TablerSvgRegistry.Register(TablerIconType.BellOff, isFilled: false, inner: BellInner);

        // Assert - a handler left behind would keep the disposed sheet alive and keep trying to render it
        Assert.DoesNotContain(spriteSheet, GetIconRegisteredSubscribers());
    }

    [Fact]
    public async Task Prerendered_HostAndIcon_EmitTheSymbolAndItsReference()
    {
        // Arrange
        await using var htmlRenderer = new HtmlRenderer(new ServiceCollection().BuildServiceProvider(), NullLoggerFactory.Instance);
        RenderFragment childContent = builder =>
        {
            builder.OpenComponent<TablerIcon>(0);
            builder.AddComponentParameter(1, nameof(TablerIcon.Type), TablerIconType.Home);
            builder.CloseComponent();
        };
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(TablerIconSprite.ChildContent)] = childContent,
        });

        // Act
        var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await htmlRenderer.RenderComponentAsync<TablerIconSprite>(parameters);
            return output.ToHtmlString();
        });

        // Assert
        Assert.Contains($"<symbol id=\"tabler-icon-Home\" viewBox=\"0 0 24 24\">{TestIcons.HomeInner}</symbol>", html);
        Assert.Contains($"{OutlineSvgOpenTag}{HomeUse}</svg>", html);
    }

    private static object?[] GetIconRegisteredSubscribers()
    {
        var eventField = typeof(TablerSvgRegistry).GetField(nameof(TablerSvgRegistry.IconRegistered), BindingFlags.NonPublic | BindingFlags.Static)!;
        var handlers = (Delegate?)eventField.GetValue(null);
        return handlers?.GetInvocationList()
            .Select(x => x.Target)
            .ToArray() ?? [];
    }
}
