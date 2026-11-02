using Blazor.Tabler.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazor.Tabler.Icons.Svg;

/// <summary>
/// The hidden <c>&lt;svg&gt;</c> holding one <c>&lt;symbol&gt;</c> per registered icon, rendered by
/// <see cref="TablerIconSprite"/>. A separate component with no parameters, so the symbols are not
/// diffed again every time the host re-renders with new child content.
/// </summary>
internal sealed class TablerSpriteSheet : ComponentBase, IDisposable
{
    private const string SymbolIdPrefix = "tabler-icon-";

    /// <summary>
    /// The id of an icon's <c>&lt;symbol&gt;</c>. Prefixed so it cannot clash with the app's own ids;
    /// built from the member name, which, unlike the numeric value, does not shift between Tabler versions.
    /// </summary>
    internal static string GetSymbolId(TablerIconType type)
    {
        return $"{SymbolIdPrefix}{type}";
    }

    protected override void OnInitialized()
    {
        TablerSvgRegistry.IconRegistered += OnIconRegistered;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "xmlns", "http://www.w3.org/2000/svg");
        builder.AddAttribute(2, "width", "0");
        builder.AddAttribute(3, "height", "0");
        builder.AddAttribute(4, "aria-hidden", "true");
        // Zero-size and out of flow rather than display:none, which stops gradients and clip paths
        // inside a sprite from painting through <use> in Chromium and Firefox.
        builder.AddAttribute(5, "style", "position:absolute;width:0;height:0;overflow:hidden");
        foreach (var (type, entry) in TablerSvgRegistry.GetEntries())
        {
            builder.OpenElement(6, "symbol");
            builder.SetKey(type);
            builder.AddAttribute(7, "id", GetSymbolId(type));
            builder.AddAttribute(8, "viewBox", "0 0 24 24");
            builder.AddMarkupContent(9, entry.Inner);
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    private void OnIconRegistered()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        TablerSvgRegistry.IconRegistered -= OnIconRegistered;
    }
}
