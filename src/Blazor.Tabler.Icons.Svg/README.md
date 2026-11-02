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
| `SvgMode` | `TablerSvgMode?` | `Inline` or `Sprite` for this icon; `null` inherits it (see [Sprite mode](#sprite-mode-opt-in)) |

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

## Sprite mode (opt-in)

By default every `<TablerIcon>` carries its own paths. In **sprite mode** each icon's paths are rendered once, as a `<symbol>` in one hidden `<svg>`, and every `<TablerIcon>` renders only a reference to it:

```html
<svg ... class="tabler-icon"><use href="#tabler-icon-Home"></use></svg>
```

It pays off when the same icons are rendered many times and the render output crosses a wire. Blazor Hybrid (MAUI) copies every render batch through the WebView bridge and Blazor Server sends it over the circuit, and a render batch does not deduplicate markup: a list of 200 rows with the same icon sends its paths 200 times. An icon's paths average ~300 characters; the reference is ~25.

Wrap the content whose icons should use the sprite, usually the layout body:

```razor
@inherits LayoutComponentBase

<TablerIconSprite>
    @Body
</TablerIconSprite>
```

`<TablerIconSprite>` renders the sprite - one `<symbol id="tabler-icon-{Name}">` per icon in your build, the same tree-shaken set - and switches every `<TablerIcon>` inside it to sprite mode. Icons outside it render inline, and an app that does not use it renders exactly what it rendered before.

The `<svg>` itself is unchanged: `fill`, `stroke`, `stroke-width`, `stroke-linecap`/`stroke-linejoin`, `class`, `style` and captured attributes stay on it and inherit into the symbol, so sizing, `currentColor`, `stroke-*` classes and filled vs outline icons behave as in inline mode. What no longer works is CSS that targets the paths themselves (`.tabler-icon path { ... }`): `<use>` renders the symbol in a shadow tree that page selectors do not reach.

### Keeping some icons inline

The nearest setting wins: an icon's own `SvgMode`, then the closest cascaded `TablerSvgMode`, then inline.

```razor
@* One icon *@
<TablerIcon Type="TablerIconType.Share" SvgMode="TablerSvgMode.Inline" />

@* A whole subtree, including icons inside other components *@
<CascadingValue Value="TablerSvgMode.Inline" IsFixed="true">
    <ShareCard />
</CascadingValue>
```

> Keep anything that is copied out of the page inline - an element captured with html-to-image, cloned, or serialized. The copy does not carry the sprite, so a `<use>` in it points at nothing and the icon comes out blank. html-to-image up to v1.11.13 (its latest release) only copies symbols referenced through `xlink:href`; this package emits the SVG 2 `href`. Its unreleased master resolves both ([3e59251](https://github.com/bubkoo/html-to-image/commit/3e592510c6)).

### Where the sprite must be

- **Once per page, in the tree that renders the icons.** A cascading value does not cross a component that has its own `@rendermode`, so in a Blazor Web App with per-page or per-component interactivity, icons inside an interactive component render inline unless that component's content is wrapped in its own `<TablerIconSprite>`. Two sprites on one page are harmless: their symbols carry the same markup.
- **Without child content**, `<TablerIconSprite />` renders only the sprite, for icons opted in through `SvgMode="TablerSvgMode.Sprite"` or a cascade of your own, e.g. .NET 8+'s root-level `builder.Services.AddCascadingValue(_ => TablerSvgMode.Sprite);`.
- **Prerendering** (static SSR, interactive server, prerendered WebAssembly) works: the sprite and the references are plain HTML.
- **Class libraries** register their icons when the runtime first uses them, which can be after the sprite rendered; the sprite re-renders to add them.

> Sprite mode with no `<TablerIconSprite>` in the document renders the icon blank, with no error. It can only happen when you opt in yourself (`SvgMode="TablerSvgMode.Sprite"` or your own cascade) - icons inside `<TablerIconSprite>` always have it. If an icon is blank, check that the page contains `<symbol id="tabler-icon-{Name}">`.

### Browser support

`<use href="#id">` (SVG 2, without the `xlink:` prefix) needs **Chrome / Android WebView 50+** and **Safari 12.1+ / iOS 12.2+**. Blazor Hybrid renders in the device's own engine (the Android System WebView, WKWebView on iOS), so in a MAUI app those are the device's WebView and iOS versions. Source: [MDN browser compatibility for `<use>` `href`](https://developer.mozilla.org/en-US/docs/Web/SVG/Reference/Element/use#browser_compatibility). Inline mode has no such floor.

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

Full docs and the font variant: [github.com/Kebechet/Blazor.Tabler.Icons](https://github.com/Kebechet/Blazor.Tabler.Icons)

## License

MIT - same as Tabler Icons.
