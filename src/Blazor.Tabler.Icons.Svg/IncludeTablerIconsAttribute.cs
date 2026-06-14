using Blazor.Tabler.Icons;

namespace Blazor.Tabler.Icons.Svg;

/// <summary>
/// Forces the generator to include icons that are selected dynamically at runtime (e.g. parsed
/// from config), which it cannot detect from static <c>TablerIconType.X</c> references.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class IncludeTablerIconsAttribute : Attribute
{
    public IncludeTablerIconsAttribute(params TablerIconType[] icons)
    {
        Icons = icons;
    }

    public TablerIconType[] Icons { get; }
}
