using System.Collections.Concurrent;
using Blazor.Tabler.Icons;

namespace Blazor.Tabler.Icons.Svg;

/// <summary>
/// Holds the SVG body for the icons a consumer actually uses. Populated by a module
/// initializer that the source generator emits into the consuming assembly, so only
/// referenced icons are ever present at runtime.
/// </summary>
public static class TablerSvgRegistry
{
    /// <summary>
    /// Concurrent because every assembly that references the package registers from its own module
    /// initializer, which the runtime runs on first use of that assembly - on any thread, possibly
    /// while a <see cref="TablerIconSprite"/> is enumerating the entries.
    /// </summary>
    private static readonly ConcurrentDictionary<TablerIconType, TablerSvgEntry> _entries = new();

    /// <summary>
    /// Raised when an icon is added or its markup changes. A <see cref="TablerIconSprite"/> that has
    /// already rendered re-renders on it, since an assembly's module initializer can run after the
    /// sprite was first rendered (e.g. a class library whose page is opened later).
    /// </summary>
    internal static event Action? IconRegistered;

    /// <summary>Registers the inner SVG markup for an icon. Called by generated code.</summary>
    public static void Register(TablerIconType type, bool isFilled, string inner)
    {
        var entry = new TablerSvgEntry(isFilled, inner);
        if (_entries.TryGetValue(type, out var registeredEntry) && registeredEntry == entry)
        {
            return;
        }

        _entries[type] = entry;
        IconRegistered?.Invoke();
    }

    /// <summary>Returns the registered SVG body for an icon, if it was included in the build.</summary>
    public static bool TryGet(TablerIconType type, out TablerSvgEntry entry)
    {
        return _entries.TryGetValue(type, out entry);
    }

    /// <summary>Every registered icon, ordered by type so a sprite renders its symbols in a stable order.</summary>
    internal static KeyValuePair<TablerIconType, TablerSvgEntry>[] GetEntries()
    {
        return _entries
            .OrderBy(x => x.Key)
            .ToArray();
    }
}

/// <summary>Inner SVG markup of a single icon plus whether it is a filled (vs outline) variant.</summary>
public readonly record struct TablerSvgEntry(bool IsFilled, string Inner);
