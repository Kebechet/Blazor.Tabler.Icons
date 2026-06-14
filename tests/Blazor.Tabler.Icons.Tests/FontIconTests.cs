using Blazor.Tabler.Icons;
using Xunit;

namespace Blazor.Tabler.Icons.Tests;

public class FontIconTests
{
    [Fact]
    public void ToCssClass_ForOutlineIcon_ReturnsTiPrefixedClass()
    {
        // Arrange
        var icon = TablerIconType.Home;

        // Act
        var css = icon.ToCssClass();

        // Assert
        Assert.Equal("ti ti-home", css);
    }

    [Fact]
    public void ToCssClass_ForFilledIcon_ReturnsTifPrefixedClass()
    {
        // Arrange
        var icon = TablerIconType.HomeFilled;

        // Act
        var css = icon.ToCssClass();

        // Assert
        Assert.StartsWith("tif tif-", css);
    }

    [Fact]
    public void Constant_MatchesExtensionResult()
    {
        // Assert
        Assert.Equal(TablerIconConstants.Home, TablerIconType.Home.ToCssClass());
    }

    [Fact]
    public void EveryIcon_HasValidCssClass()
    {
        // Arrange
        var invalid = new List<string>();

        // Act
        foreach (var icon in Enum.GetValues<TablerIconType>())
        {
            var css = icon.ToCssClass();
            var isValid =
                !string.IsNullOrWhiteSpace(css) &&
                (css.StartsWith("ti ti-") || css.StartsWith("tif tif-"));
            if (!isValid)
            {
                invalid.Add($"{icon} => '{css}'");
            }
        }

        // Assert
        Assert.Empty(invalid);
    }
}
