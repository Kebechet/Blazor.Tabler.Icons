using System.Collections.Concurrent;
using Blazor.Tabler.Icons;

namespace Blazor.Tabler.Icons.Svg;

/// <summary>
/// Holds the SVG body for the icons a consumer actually uses. Populated by a module
/// initializer that the source generator emits into every consuming assembly, so only
/// referenced icons are ever present at runtime.
/// </summary>
/// <remarks>
/// Safe for concurrent registration and lookup. The module initializers of several assemblies
/// can run at the same time on different threads (for example a test host that loads assemblies
/// from parallel test threads), and a lookup can happen while a registration is still running.
/// </remarks>
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

    /// <summary>
    /// Registers the inner SVG markup for an icon. Assemblies built with an earlier version of the
    /// source generator call this once per icon; the generator now calls <see cref="RegisterRange"/>.
    /// </summary>
    public static void Register(TablerIconType type, bool isFilled, string inner)
    {
        if (Store(type, new TablerSvgEntry(isFilled, inner)))
        {
            IconRegistered?.Invoke();
        }
    }

    /// <summary>
    /// Registers the inner SVG markup for a batch of icons and announces it once, however many icons
    /// it holds. Called by generated code: a sprite re-renders for every announcement, and in Blazor
    /// Server every open circuit has one, so announcing each icon would send each of them a render
    /// batch per icon the first time an assembly is used.
    /// </summary>
    public static void RegisterRange(IEnumerable<TablerSvgRegistration> registrations)
    {
        var isChanged = false;
        foreach (var registration in registrations)
        {
            isChanged |= Store(registration.Type, new TablerSvgEntry(registration.IsFilled, registration.Inner));
        }

        if (isChanged)
        {
            IconRegistered?.Invoke();
        }
    }

    /// <summary>Returns the registered SVG body for an icon, if it was included in the build.</summary>
    public static bool TryGet(TablerIconType type, out TablerSvgEntry entry)
    {
        return _entries.TryGetValue(type, out entry);
    }

    /// <summary>Every registered icon, ordered by type so a sprite renders its symbols in a stable order.</summary>
    internal static KeyValuePair<TablerIconType, TablerSvgEntry>[] GetEntries()
    {
        // The dictionary's own ToArray is a locked snapshot. LINQ would read Count and then CopyTo,
        // which throws when another thread registers an icon in between.
        return _entries
            .ToArray()
            .OrderBy(x => x.Key)
            .ToArray();
    }

    /// <summary>Stores the entry, and returns whether that added the icon or changed its markup.</summary>
    private static bool Store(TablerIconType type, TablerSvgEntry entry)
    {
        if (_entries.TryGetValue(type, out var registeredEntry) && registeredEntry == entry)
        {
            return false;
        }

        _entries[type] = entry;
        return true;
    }
}

/// <summary>Inner SVG markup of a single icon plus whether it is a filled (vs outline) variant.</summary>
public readonly record struct TablerSvgEntry(bool IsFilled, string Inner);

/// <summary>One icon of a batch passed to <see cref="TablerSvgRegistry.RegisterRange"/>.</summary>
public readonly record struct TablerSvgRegistration(TablerIconType Type, bool IsFilled, string Inner);
