[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/kebechet)

# Blazor.Icons.Tabler
[![NuGet Version](https://img.shields.io/nuget/v/Kebechet.Blazor.Icons.Tabler)](https://www.nuget.org/packages/Kebechet.Blazor.Icons.Tabler/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Kebechet.Blazor.Icons.Tabler)](https://www.nuget.org/packages/Kebechet.Blazor.Icons.Tabler/)
[![CI](https://github.com/Kebechet/Blazor.Icons.Tabler/actions/workflows/ci.yml/badge.svg)](https://github.com/Kebechet/Blazor.Icons.Tabler/actions/workflows/ci.yml)
![Last updated](https://img.shields.io/github/last-commit/Kebechet/Blazor.Icons.Tabler/main?label=last%20updated)
[![Twitter](https://img.shields.io/twitter/url/https/twitter.com/samuel_sidor.svg?style=social&label=Follow%20samuel_sidor)](https://x.com/samuel_sidor)

A Blazor component library providing [Tabler Icons](https://tabler.io/icons) (5000+ icons) with strongly-typed enums and constant-time lookups.

## Installation

```bash
dotnet add package Kebechet.Blazor.Icons.Tabler
```

The CSS is **automatically registered** via Blazor's JavaScript initializers - no manual setup required.

## Usage

### Component

```razor
<TablerIcon Type="TablerIconType.Home" />
<TablerIcon Type="TablerIconType.Settings" Class="text-red" Style="font-size: 2rem" />
<TablerIcon Type="TablerIconType.User" @onclick="HandleClick" title="Profile" />
```

### Direct CSS class

```csharp
// Using the extension method
var css = TablerIconType.Activity.ToCssClass(); // "ti ti-activity"

// Using constants directly
var css = TablerIconConstants.Activity; // "ti ti-activity"
```

```razor
<i class="@TablerIconType.Home.ToCssClass()"></i>
<i class="@TablerIconConstants.Settings"></i>
```

## API

| Type | Description |
|------|-------------|
| `TablerIcon` | Blazor component for rendering icons |
| `TablerIconType` | Enum with all 5000+ icon names |
| `TablerIconConstants` | Static class with CSS class string constants |
| `TablerIconTypeExtensions.ToCssClass()` | Extension method to convert enum to CSS class |

### TablerIcon Component Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `Type` | `TablerIconType` | Required. The icon to display |
| `Class` | `string?` | Additional CSS classes |
| `Style` | `string?` | Inline styles |
| `Attributes` | `Dictionary<string, object>?` | Captures unmatched attributes (`@onclick`, `title`, `aria-*`, etc.) |

## Updating Icons

Run the PowerShell script to regenerate from the latest Tabler Icons:

```powershell
.\scripts\Generate-TablerIcons.ps1
```

The script downloads the latest `@tabler/icons-webfont` npm package and generates:
- `TablerIconType.cs` - enum
- `TablerIconConstants.cs` - constants
- `TablerIconTypeExtensions.cs` - extension method
- `wwwroot/tabler-icons.min.css` - stylesheet
- `wwwroot/fonts/*.woff2` - font files

## Design Decisions

### Enum + Constants + Switch Expression

We use three separate constructs for maximum flexibility and performance:

- **`TablerIconType` enum**: Provides IntelliSense, compile-time safety, and easy discovery of available icons
- **`TablerIconConstants`**: Direct string constants for scenarios where you need the raw CSS class
- **`ToCssClass()` extension**: Uses a switch expression that maps enum to constants

The switch expression compiles to a jump table, providing **O(1) constant-time** lookup. No reflection, no dictionary lookups, no string interpolation at runtime.

### Why not reflection or `nameof()`?

Reflection-based approaches (attributes, `nameof()`) have runtime overhead and require string manipulation. Our approach:
- Zero allocations for enum-to-string conversion
- Compile-time verification that all enum values are handled
- The compiler optimizes the switch expression into efficient IL

### Font format: woff2 only

We include only `.woff2` files, not `.woff`, `.ttf`, or `.eot`:
- **woff2** has the best compression (~30% smaller than woff)
- Supported by all modern browsers (Chrome 36+, Firefox 39+, Safari 10+, Edge 14+)
- Legacy formats add ~10MB of unnecessary bloat

### Font weights: regular + filled only

Tabler provides multiple font weights (200, 300, 400, filled), but we include only:
- **Regular (400)**: The default weight
- **Filled**: Solid icon variants

The 200/300 weights were excluded because:
- The default CSS doesn't wire them up (no `@font-face` rules for those weights)
- They would require custom CSS to use
- Removing them saves ~850KB

### Naming conventions

- Enum values use PascalCase: `TablerIconType.AddressBook`
- Names starting with numbers get underscore prefix: `TablerIconType._24Hours`
- No underscores between words: `Ad2` not `Ad_2`

## Versioning

`<tabler_version>(.<fix_number>)`

Examples:
- `3.36.1` - matches Tabler Icons v3.36.1
- `3.36.1.1` - patch release for package-level fixes

## License

MIT - Same as Tabler Icons
