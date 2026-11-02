using Blazor.Tabler.Icons;
using Blazor.Tabler.Icons.Svg;
using Bunit;
using Xunit;

namespace Blazor.Tabler.Icons.Svg.Tests;

/// <summary>
/// Pins the inline markup <see cref="TablerIcon"/> rendered before sprite mode existed. Each
/// expected string was captured from that component, so an app that does not opt in must keep
/// producing exactly these bytes.
/// </summary>
[Collection(RegistryCollection.Name)]
public class TablerIconInlineTests : BunitContext
{
    public TablerIconInlineTests()
    {
        TestIcons.Register();
    }

    [Fact]
    public void OutlineIcon_RendersStrokeAttributesAndInnerMarkup()
    {
        // Act
        var cut = Render<TablerIcon>(parameters => parameters
            .Add(x => x.Type, TablerIconType.Home));

        // Assert
        Assert.Equal(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1em\" height=\"1em\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" class=\"tabler-icon\">" +
            TestIcons.HomeInner +
            "</svg>",
            cut.Markup);
    }

    [Fact]
    public void FilledIcon_RendersFillAttributesWithoutStrokeWidth()
    {
        // Act
        var cut = Render<TablerIcon>(parameters => parameters
            .Add(x => x.Type, TablerIconType.HomeFilled));

        // Assert
        Assert.Equal(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1em\" height=\"1em\" viewBox=\"0 0 24 24\" fill=\"currentColor\" stroke=\"none\" stroke-linecap=\"round\" stroke-linejoin=\"round\" class=\"tabler-icon\">" +
            TestIcons.HomeFilledInner +
            "</svg>",
            cut.Markup);
    }

    [Fact]
    public void ClassStyleAndCapturedAttributes_AreForwardedToTheSvg()
    {
        // Act
        var cut = Render<TablerIcon>(parameters => parameters
            .Add(x => x.Type, TablerIconType.Star)
            .Add(x => x.Class, "size-6 text-red-500")
            .Add(x => x.Style, "stroke-width:1.5")
            .AddUnmatched("title", "Favourite")
            .AddUnmatched("aria-label", "Add to favourites"));

        // Assert
        Assert.Equal(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1em\" height=\"1em\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" class=\"tabler-icon size-6 text-red-500\" style=\"stroke-width:1.5\" title=\"Favourite\" aria-label=\"Add to favourites\">" +
            TestIcons.StarInner +
            "</svg>",
            cut.Markup);
    }

    [Fact]
    public void UnregisteredIcon_RendersNothing()
    {
        // Act
        var cut = Render<TablerIcon>(parameters => parameters
            .Add(x => x.Type, TablerIconType.ZzzOff));

        // Assert
        Assert.Equal(string.Empty, cut.Markup);
    }
}
