# CLAUDE.md

Guidance for working in this repo (Tabler Icons for Blazor).

## What this is

Tabler Icons for Blazor, shipped as **two NuGet packages that share one API** (`TablerIconType` enum + `<TablerIcon Type="..." />`):

| Package | Rendering | Size model |
|---------|-----------|------------|
| `Kebechet.Blazor.Tabler.Icons` | Icon **font** (`<i class>`) | One woff2 with every glyph (no tree-shaking) |
| `Kebechet.Blazor.Tabler.Icons.Svg` | Inline **SVG** (`<svg>`) | A source generator inlines only referenced icons (tree-shaking) |

An app uses **one** backend; switching is a package swap.

## Repository structure

| Path | Purpose |
|------|---------|
| `src/Core` | Shared project (`.shproj` + `.projitems`) holding the generated `TablerIconType` enum. Linked into both packages via `<Import Project="..\Core\Core.projitems" Label="Shared" />` - **no separate NuGet**. |
| `src/Blazor.Tabler.Icons` | Font package: `TablerIconConstants` (CSS classes), `TablerIconTypeExtensions.ToCssClass()`, `<TablerIcon>`, `wwwroot` woff2 + CSS. |
| `src/Blazor.Tabler.Icons.Svg` | SVG package: `<TablerIcon>` (inline svg), `TablerSvgRegistry`, `IncludeTablerIconsAttribute`. |
| `src/Blazor.Tabler.Icons.Svg.Generator` | Roslyn source generator (`netstandard2.0`) + embedded `TablerSvgData.txt` dataset (build-time only, never shipped to the browser). |
| `tests/` | xUnit. `Blazor.Tabler.Icons.Tests` (font mapping) and `Blazor.Tabler.Icons.Svg.Tests` (generator: tree-shaking, aliases, diagnostics; registry). |
| `scripts/` | `.cs` generation scripts (see below). |
| `src/Blazor.Tabler.Icons.slnx` | Solution. |

## Generating icons

Generation uses **.NET 10 file-based C# scripts** (`dotnet run file.cs`), not PowerShell. Run from the repo root:

```bash
dotnet run scripts/Generate-TablerIcons.cs       # webfont -> enum (Core) + constants/extensions (font) + woff2/CSS (wwwroot)
dotnet run scripts/Generate-TablerSvgData.cs     # @tabler/icons SVGs -> embedded dataset (+ alias resolution)
```

Both take an optional version arg, e.g. `dotnet run scripts/Generate-TablerSvgData.cs -- 3.45.0`.

- The enum is generated into **Core** (`src/Core/TablerIconType.cs`); constants/extensions stay in the font package.
- The SVG dataset resolves Tabler **font aliases** (e.g. `discount-2` -> `rosette-discount`) from `aliases.json` so an alias renders the canonical icon's SVG. Goal: full coverage, so `TABLERSVG002` (icon has no SVG data) effectively never fires.
- **Generated artifacts are committed** (enum, constants, extensions, woff2, CSS, `TablerSvgData.txt`) - same convention as the font assets. Don't gitignore them; the manual publish workflow builds without running the scripts.

## How the SVG generator works

Runs in the **consumer's** build, scans `.razor`/`.cs` for `TablerIconType.X` references (+ `[assembly: IncludeTablerIcons(...)]` for runtime-chosen icons), and emits a `[ModuleInitializer]` registering only those icons into `TablerSvgRegistry`. Unused icons are never generated -> nothing ships. Diagnostics: `TABLERSVG001` (non-constant `Type`), `TABLERSVG002` (referenced icon missing from dataset).

## Build & test

```bash
dotnet build src/Blazor.Tabler.Icons.slnx -c Release
dotnet test  src/Blazor.Tabler.Icons.slnx -c Release
```

## Conventions & gotchas

- **Sizing the SVG `<TablerIcon>`** is CSS-only (defaults to `width/height: 1em`): size via a font-size class (`text-2xl`) or a width/height class (`size-6`, `size-full`); color via `currentColor` (`text-red-500`); outline thickness via `stroke-*`. There is no `Size`/`SizePx` parameter.
- **Two distinct enum types.** Because Core is compiled into each package (shared source, not a shared DLL), `TablerIconType` has a separate identity in each. Fine for one-backend apps; a single project referencing **both** packages hits CS0433 (the test projects are split per backend for this reason).
- **Packaging the generator:** the analyzer DLL is packed into `analyzers/dotnet/cs` by the `_IncludeGeneratorInPackage` target. It references the built generator DLL **by path** - do NOT invoke `<MSBuild Targets="Build">` during pack, because CI packs with `--no-build` and that throws "NoBuild was set but the Build target was invoked."
- **MSBuild/XML:** XML comments cannot contain `--` (e.g. don't write `--no-build` in a `<!-- -->` comment).
- **CS1591 warnings** ("missing XML comment") on the ~6200 enum members are expected (names are self-documenting); `GenerateDocumentationFile` is on so per-member component docs still ship.
- **Versioning:** `<tabler_version>(.<fix_number>)` - e.g. `3.44.0` matches Tabler 3.44.0; `3.44.0.1` is a package-level patch. The auto-update check compares versions with `sort -V`, so a 4th segment does not trigger a spurious update.
- **CI:** `tests.yml` runs on push/PR. `check-tabler-update.yml` (daily) regenerates both, runs tests, then builds/packs/publishes **both** packages; `publish-nuget.yml` is the manual publish. Both publish via `dotnet nuget push *.nupkg` so both packages go out together. Core is never published.
- **READMEs:** each package has its own focused `README.md` (set as `PackageReadmeFile`); the root `README.md` is the GitHub landing page (overview + comparison) and is not packed.
