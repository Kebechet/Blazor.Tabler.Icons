# Kebechet.Blazor.Tabler.Icons

[![NuGet Version](https://img.shields.io/nuget/v/Kebechet.Blazor.Tabler.Icons)](https://www.nuget.org/packages/Kebechet.Blazor.Tabler.Icons/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Kebechet.Blazor.Tabler.Icons)](https://www.nuget.org/packages/Kebechet.Blazor.Tabler.Icons/)

[Tabler Icons](https://tabler.io/icons) for Blazor, rendered as an **icon font**, with a strongly-typed `TablerIconType` enum and constant-time CSS-class lookups. 6000+ icons, automatically updated when Tabler releases.

> Want only the icons you use shipped to the browser? See the tree-shaking SVG variant: [`Kebechet.Blazor.Tabler.Icons.Svg`](https://www.nuget.org/packages/Kebechet.Blazor.Tabler.Icons.Svg/).

## Installation

```bash
dotnet add package Kebechet.Blazor.Tabler.Icons
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
var css = TablerIconType.Activity.ToCssClass(); // "ti ti-activity"
var css = TablerIconConstants.Activity;         // "ti ti-activity"
```

```razor
<i class="@TablerIconType.Home.ToCssClass()"></i>
<i class="@TablerIconConstants.Settings"></i>
```

## API

| Member | Description |
|--------|-------------|
| `TablerIcon` | Blazor component for rendering an icon |
| `TablerIconType` | Enum of all icon names |
| `TablerIconConstants` | CSS-class string constants |
| `TablerIconType.ToCssClass()` | Extension converting an enum value to its CSS class |

### `TablerIcon` parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `Type` | `TablerIconType` | Required. The icon to display |
| `Class` | `string?` | Additional CSS classes |
| `Style` | `string?` | Inline styles |
| `Attributes` | `Dictionary<string, object>?` | Captures unmatched attributes (`@onclick`, `title`, `aria-*`, ...) |

Sizing/coloring use `font-size` and `color` (it is a font), e.g. `Style="font-size: 2rem; color: red"`.

## More

Full docs, the SVG variant, and design notes: [github.com/Kebechet/Blazor.Icons.Tabler](https://github.com/Kebechet/Blazor.Icons.Tabler)

## License

MIT - same as Tabler Icons.
