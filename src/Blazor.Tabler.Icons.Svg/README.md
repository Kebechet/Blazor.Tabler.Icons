# Kebechet.Blazor.Tabler.Icons.Svg

[![NuGet Version](https://img.shields.io/nuget/v/Kebechet.Blazor.Tabler.Icons.Svg)](https://www.nuget.org/packages/Kebechet.Blazor.Tabler.Icons.Svg/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Kebechet.Blazor.Tabler.Icons.Svg)](https://www.nuget.org/packages/Kebechet.Blazor.Tabler.Icons.Svg/)

[Tabler Icons](https://tabler.io/icons) for Blazor, rendered as **inline SVG** with built-in **tree-shaking**: a source generator inlines only the icons you actually reference, so unused icons never ship. Same strongly-typed `TablerIconType` enum and `<TablerIcon Type="..." />` API as the font package.

> Prefer the simplest drop-in (one cached font file, no generator)? Use [`Kebechet.Blazor.Tabler.Icons`](https://www.nuget.org/packages/Kebechet.Blazor.Tabler.Icons/).

## Why SVG

- **Tree-shaking** - only referenced icons are emitted into your assembly (a typical app inlines a few dozen of ~6000).
- **Crisp + box-aligned** - real `<svg>` boxes, no font baseline quirks.
- **`currentColor`** - icons inherit text color automatically.

## Installation

```bash
dotnet add package Kebechet.Blazor.Tabler.Icons.Svg
```

No CSS, no JS, no font file - the SVG is inlined.

## Usage

```razor
@using Blazor.Tabler.Icons.Svg

<TablerIcon Type="TablerIconType.Home" />
<TablerIcon Type="TablerIconType.User" Class="text-2xl text-red-500" />
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `Type` | `TablerIconType` | Required. The icon to display |
| `Class` | `string?` | Additional CSS classes - the way to size and color the icon |
| `Style` | `string?` | Inline styles |
| `Attributes` | `Dictionary<string, object>?` | Captures unmatched attributes |

### Sizing and color

The `<svg>` defaults to `width`/`height` of `1em` and uses `currentColor`, so you control it with CSS:

- **By font size** - a `font-size` class scales it, like the font backend: `Class="text-2xl"`.
- **By width/height** - a CSS width/height overrides the `1em` default: `Class="size-6"`, `Class="w-5 h-5"`, or `Class="size-full"` to fill a sized parent.
- **Color** - follows the text color: `Class="text-red-500"`.
- **Stroke thickness** (outline icons) - `Class="stroke-1"` or `Style="stroke-width:1.5"`.

```razor
<div class="w-40 h-24">
    <TablerIcon Type="TablerIconType.Home" Class="size-full" />
</div>
```

## How tree-shaking works

A Roslyn source generator runs in **your** build, scans your `.razor`/`.cs` for `TablerIconType.X` references, and emits a module initializer that registers only those icons. Unused icons are never generated, so they never ship - no trimmer configuration required. The full ~6000-icon dataset lives inside the generator (a build-time-only dependency) and never reaches the browser.

References through an **enum alias** (any name, e.g. `global using IconType = TablerIconType;` then `IconType.Plus`) are detected automatically - in C# semantically, and in markup by discovering the alias from your `using` directives. No configuration needed.

## Icons selected at runtime

Icons chosen dynamically (e.g. `Enum.Parse` from config) can't be seen statically. Force-include them:

```csharp
[assembly: IncludeTablerIcons(TablerIconType.Home, TablerIconType.User)]
```

Build warnings keep this honest:

- **TABLERSVG001** - a `<TablerIcon>` uses a non-constant `Type` (add the attribute above).
- **TABLERSVG002** - a referenced icon has no SVG data in this build (rare - every current Tabler icon is covered, including font aliases; see below).

### Aliases

Tabler keeps legacy/renamed names as **aliases** (e.g. `discount-2` -> `rosette-discount`, `hexagon-0` -> `hexagon-number-0`). The dataset resolves these from `aliases.json`, so an alias renders the canonical icon's SVG - `TablerIconType.Discount2` works under the SVG backend just like its canonical `TablerIconType.RosetteDiscount`.

## Include every icon (galleries / dev tools)

For an icon **gallery**, **showcase**, or runtime **icon-picker** - where the requirement is "any of the ~6000 icons, selectable at runtime" - listing them all is impractical. Opt out of tree-shaking entirely:

```csharp
#if DEBUG
[assembly: IncludeAllTablerIcons]
#endif
```

This registers every icon and disables tree-shaking, so any icon renders no matter how it is chosen (no `TABLERSVG001` warnings). The build prints **TABLERSVG003** (informational) as a reminder that all ~6000 icons are included.

> Do not ship this in an app delivered to end users - it bloats the bundle, which is exactly what tree-shaking exists to prevent. The `#if DEBUG` guard keeps it out of Release builds; a gallery/Storybook is dev-only, so the bundle-size cost never reaches anyone.

## More

Full docs and the font variant: [github.com/Kebechet/Blazor.Icons.Tabler](https://github.com/Kebechet/Blazor.Icons.Tabler)

## License

MIT - same as Tabler Icons.
