using Blazor.Tabler.Icons;
using Blazor.Tabler.Icons.Svg;
using Xunit;

namespace Blazor.Tabler.Icons.Svg.Tests;

public class RegistryTests
{
    [Fact]
    public void Register_ThenTryGet_ReturnsStoredEntry()
    {
        // Arrange
        TablerSvgRegistry.Register(TablerIconType.Activity, isFilled: false, inner: "<path d=\"M1 1\" />");

        // Act
        var found = TablerSvgRegistry.TryGet(TablerIconType.Activity, out var entry);

        // Assert
        Assert.True(found);
        Assert.False(entry.IsFilled);
        Assert.Equal("<path d=\"M1 1\" />", entry.Inner);
    }

    [Fact]
    public void TryGet_ForUnregisteredIcon_ReturnsFalse()
    {
        // Act
        var found = TablerSvgRegistry.TryGet(TablerIconType.ZzzOff, out _);

        // Assert
        Assert.False(found);
    }
}
