using Blazor.Tabler.Icons;
using Blazor.Tabler.Icons.Svg;
using Xunit;

namespace Blazor.Tabler.Icons.Svg.Tests;

/// <summary>
/// Real Tabler markup for the icons the component tests render. <see cref="TablerSvgRegistry"/> is
/// process-wide, so every test registers the same body for a given icon and the classes that touch
/// it share the <see cref="RegistryCollection"/> to stay off each other's toes.
/// </summary>
internal static class TestIcons
{
    public const string HomeInner = "<path stroke=\"none\" d=\"M0 0h24v24H0z\" fill=\"none\" /> <path d=\"M5 12l-2 0l9 -9l9 9l-2 0\" /> <path d=\"M5 12v7a2 2 0 0 0 2 2h10a2 2 0 0 0 2 -2v-7\" /> <path d=\"M9 21v-6a2 2 0 0 1 2 -2h2a2 2 0 0 1 2 2v6\" />";

    public const string HomeFilledInner = "<path stroke=\"none\" d=\"M0 0h24v24H0z\" fill=\"none\" /> <path d=\"M12.707 2.293l9 9c.63 .63 .184 1.707 -.707 1.707h-1v6a3 3 0 0 1 -3 3h-1v-7a3 3 0 0 0 -2.824 -2.995l-.176 -.005h-2a3 3 0 0 0 -3 3v7h-1a3 3 0 0 1 -3 -3v-6h-1c-.89 0 -1.337 -1.077 -.707 -1.707l9 -9a1 1 0 0 1 1.414 0m.293 11.707a1 1 0 0 1 1 1v7h-4v-7a1 1 0 0 1 .883 -.993l.117 -.007z\" />";

    public const string StarInner = "<path stroke=\"none\" d=\"M0 0h24v24H0z\" fill=\"none\" /> <path d=\"M12 17.75l-6.172 3.245l1.179 -6.873l-5 -4.867l6.9 -1l3.086 -6.253l3.086 6.253l6.9 1l-5 4.867l1.179 6.873l-6.158 -3.245\" />";

    public static void Register()
    {
        TablerSvgRegistry.Register(TablerIconType.Home, isFilled: false, inner: HomeInner);
        TablerSvgRegistry.Register(TablerIconType.HomeFilled, isFilled: true, inner: HomeFilledInner);
        TablerSvgRegistry.Register(TablerIconType.Star, isFilled: false, inner: StarInner);
    }
}

[CollectionDefinition(Name)]
public sealed class RegistryCollection
{
    public const string Name = "TablerSvgRegistry";
}
