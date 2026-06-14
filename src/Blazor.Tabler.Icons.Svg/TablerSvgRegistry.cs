using Blazor.Tabler.Icons;

namespace Blazor.Tabler.Icons.Svg;

/// <summary>
/// Holds the SVG body for the icons a consumer actually uses. Populated by a module
/// initializer that the source generator emits into the consuming assembly, so only
/// referenced icons are ever present at runtime.
/// </summary>
public static class TablerSvgRegistry
{
    private static readonly Dictionary<TablerIconType, TablerSvgEntry> _entries = new();

    /// <summary>Registers the inner SVG markup for an icon. Called by generated code.</summary>
    public static void Register(TablerIconType type, bool isFilled, string inner)
    {
        _entries[type] = new TablerSvgEntry(isFilled, inner);
    }

    /// <summary>Returns the registered SVG body for an icon, if it was included in the build.</summary>
    public static bool TryGet(TablerIconType type, out TablerSvgEntry entry)
    {
        return _entries.TryGetValue(type, out entry);
    }
}

/// <summary>Inner SVG markup of a single icon plus whether it is a filled (vs outline) variant.</summary>
public readonly record struct TablerSvgEntry(bool IsFilled, string Inner);
