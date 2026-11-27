namespace Blazor.Tabler.Icons.Svg;

/// <summary>
/// How a <see cref="TablerIcon"/> puts its paths into the page.
/// </summary>
public enum TablerSvgMode
{
    /// <summary>
    /// The icon's paths are rendered inside its own <c>&lt;svg&gt;</c>. The default, and the only
    /// output a copied or cloned element (e.g. an html-to-image capture) can rely on.
    /// </summary>
    Inline,

    /// <summary>
    /// The <c>&lt;svg&gt;</c> holds a <c>&lt;use&gt;</c> that references the icon's symbol in a
    /// <see cref="TablerIconSprite"/>, so the paths are sent to the page once instead of per icon.
    /// </summary>
    Sprite
}
