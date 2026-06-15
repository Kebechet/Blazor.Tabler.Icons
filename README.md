[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/kebechet)

# Blazor.Icons.Tabler

[![NuGet Version](https://img.shields.io/nuget/v/Kebechet.Blazor.Tabler.Icons?label=nuget%20%28font%29)](https://www.nuget.org/packages/Kebechet.Blazor.Tabler.Icons/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Kebechet.Blazor.Tabler.Icons?label=downloads%20%28font%29)](https://www.nuget.org/packages/Kebechet.Blazor.Tabler.Icons/)

[![NuGet Version (SVG)](https://img.shields.io/nuget/v/Kebechet.Blazor.Tabler.Icons.Svg?label=nuget%20%28svg%29)](https://www.nuget.org/packages/Kebechet.Blazor.Tabler.Icons.Svg/)
[![NuGet Downloads (SVG)](https://img.shields.io/nuget/dt/Kebechet.Blazor.Tabler.Icons.Svg?label=downloads%20%28svg%29)](https://www.nuget.org/packages/Kebechet.Blazor.Tabler.Icons.Svg/)

[![Auto Update](https://github.com/Kebechet/Blazor.Icons.Tabler/actions/workflows/check-tabler-update.yml/badge.svg)](https://github.com/Kebechet/Blazor.Icons.Tabler/actions/workflows/check-tabler-update.yml)
![Last updated](https://img.shields.io/github/last-commit/Kebechet/Blazor.Icons.Tabler/main?label=last%20updated)
[![Twitter](https://img.shields.io/twitter/url/https/twitter.com/samuel_sidor.svg?style=social&label=Follow%20samuel_sidor)](https://x.com/samuel_sidor)

[Tabler Icons](https://tabler.io/icons) (6000+) for Blazor with a strongly-typed `TablerIconType` enum and no runtime reflection. **Automatically updated** - a daily pipeline publishes new Tabler releases.

## Two packages, one API

Both expose the same `TablerIconType` enum and the same `<TablerIcon Type="..." />` component. Pick the backend that fits; switching is a package swap.

| Package | Rendering | Size model | Docs |
|---------|-----------|------------|------|
| [`Kebechet.Blazor.Tabler.Icons`](https://www.nuget.org/packages/Kebechet.Blazor.Tabler.Icons/) | Icon **font** (`<i class>`) | One woff2 with every glyph (no tree-shaking) | [font README](src/Blazor.Tabler.Icons/README.md) |
| [`Kebechet.Blazor.Tabler.Icons.Svg`](https://www.nuget.org/packages/Kebechet.Blazor.Tabler.Icons.Svg/) | Inline **SVG** (`<svg>`) | A source generator inlines **only the icons you reference** | [svg README](src/Blazor.Tabler.Icons.Svg/README.md) |

- Pick **font** for the simplest drop-in: one cached file, no generator, maximum compatibility.
- Pick **SVG** when bundle size matters or you want crisp, box-aligned icons that color via `currentColor`.

```bash
dotnet add package Kebechet.Blazor.Tabler.Icons        # font
dotnet add package Kebechet.Blazor.Tabler.Icons.Svg    # svg (tree-shaking)
```

```razor
<TablerIcon Type="TablerIconType.Home" />
```

## Repository structure

| Project | Purpose |
|---------|---------|
| `src/Core` | Shared project holding the generated `TablerIconType` enum (the single source of truth). Linked into both packages - no separate NuGet. |
| `src/Blazor.Tabler.Icons` | Font package: CSS-class constants, `<TablerIcon>`, woff2 + CSS. |
| `src/Blazor.Tabler.Icons.Svg` | SVG package: `<TablerIcon>` (inline svg), registry, `[IncludeTablerIcons]`. |
| `src/Blazor.Tabler.Icons.Svg.Generator` | Roslyn source generator + embedded SVG dataset (build-time only, never shipped). |
| `tests/` | xUnit tests for both backends (font mapping; svg generation, tree-shaking, diagnostics). |

## Updating icons

```bash
# .NET 10 file-based apps (no project needed); run from the repo root
dotnet run scripts/Generate-TablerIcons.cs       # enum (-> Core), font constants/extensions, woff2 + CSS
dotnet run scripts/Generate-TablerSvgData.cs     # SVG dataset embedded in the generator
```

Both accept an optional Tabler version argument, e.g. `dotnet run scripts/Generate-TablerSvgData.cs -- 3.45.0`.

The daily `check-tabler-update` workflow runs both, runs the tests, and publishes both packages when a new Tabler version is released.

## Design decisions

### No runtime reflection
The enum maps to data via compile-time switch expressions, not `[Description]` + reflection. The font package uses a switch over CSS-class constants (O(1), zero allocations); the SVG package's generator emits a switch containing only the icons you used, which is what makes tree-shaking possible (see the [svg README](src/Blazor.Tabler.Icons.Svg/README.md#how-tree-shaking-works)).

### Single shared enum
`TablerIconType` lives in `src/Core` and is compiled into each package from shared source - one definition, no drift, no extra dependency. Because it is compiled into each assembly, the two packages' enum types are distinct; that is fine since an app uses one backend.

### Font: woff2 only, regular + filled weights
Only `.woff2` (best compression, all modern browsers) and only the regular + filled weights the default CSS actually wires up - excluding `.woff`/`.ttf` and the 200/300 weights saves ~11MB.

### Naming conventions
- PascalCase enum values: `TablerIconType.AddressBook`
- Names starting with a digit get an underscore prefix: `TablerIconType._24Hours`
- No underscores between words

## Versioning

`<tabler_version>(.<fix_number>)` - e.g. `3.44.0` matches Tabler Icons v3.44.0; `3.44.0.1` is a package-level patch.

## License

MIT - same as Tabler Icons.
