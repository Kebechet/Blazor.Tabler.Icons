using Blazor.Tabler.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Blazor.Tabler.Icons.Svg;

/// <summary>
/// The hidden <c>&lt;svg&gt;</c> holding one <c>&lt;symbol&gt;</c> per registered icon, rendered by
/// <see cref="TablerIconSprite"/>. A separate component with no parameters, so the symbols are not
/// diffed again every time the host re-renders with new child content.
/// </summary>
/// <remarks>
/// Streaming server rendering sends later updates only for components that opted in. A page that
/// streams and first uses a class library's icons after an await registers them after the first chunk
/// has gone out, so without this the page's icons arrive as <c>&lt;use&gt;</c> references to symbols
/// that are never sent. The attribute does not exist before .NET 8, which has no streaming rendering.
/// </remarks>
#if NET8_0_OR_GREATER
[StreamRendering(true)]
#endif
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
            // Sized in the paths' own coordinates: without it a <use> stretches the symbol to whatever
            // viewBox a caller sets on the icon's <svg>, which inline paths do not. On the symbol rather
            // than on each <use>, which would add the two attributes to every icon in every render batch.
            builder.AddAttribute(9, "width", "24");
            builder.AddAttribute(10, "height", "24");
            builder.AddMarkupContent(11, entry.Inner);
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
