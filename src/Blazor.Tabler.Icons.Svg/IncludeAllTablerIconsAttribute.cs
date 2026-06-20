namespace Blazor.Tabler.Icons.Svg;

/// <summary>
/// Includes <b>every</b> Tabler icon (~6000) and disables tree-shaking, so any icon can be
/// rendered when the icon is chosen at runtime. Intended for galleries, showcases, and runtime
/// icon-pickers only - do not use in apps shipped to end users, as it bloats the bundle. Wrap
/// the attribute in <c>#if DEBUG</c> to keep it out of Release builds.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class IncludeAllTablerIconsAttribute : Attribute
{
}
