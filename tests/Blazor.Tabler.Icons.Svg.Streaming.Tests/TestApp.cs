using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;

namespace Blazor.Tabler.Icons.Svg.Streaming.Tests;

/// <summary>The smallest server-rendered app: a router whose pages share <see cref="SpriteTestLayout"/>.</summary>
public sealed class StreamingTestApp : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "html");
        builder.OpenElement(1, "body");
        builder.OpenComponent<Router>(2);
        builder.AddAttribute(3, nameof(Router.AppAssembly), typeof(StreamingTestApp).Assembly);
        builder.AddAttribute(4, nameof(Router.Found), (RenderFragment<RouteData>)(routeData => found =>
        {
            found.OpenComponent<RouteView>(0);
            found.AddAttribute(1, nameof(RouteView.RouteData), routeData);
            found.CloseComponent();
        }));
        builder.AddAttribute(5, nameof(Router.NotFound), (RenderFragment)(notFound => notFound.AddContent(0, "not found")));
        builder.CloseComponent();
        builder.CloseElement();
        builder.CloseElement();
    }
}

/// <summary>A layout that is not itself streaming and puts its pages inside a <see cref="TablerIconSprite"/>.</summary>
public sealed class SpriteTestLayout : LayoutComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TablerIconSprite>(0);
        builder.AddAttribute(1, nameof(TablerIconSprite.ChildContent), (RenderFragment)(content => content.AddContent(0, Body)));
        builder.CloseComponent();
    }
}

/// <summary>
/// The icons the pages register, picked from the enum so no generator is involved. Each is registered
/// by one page only, so no test finds it already in the process-wide registry.
/// </summary>
public static class TestIcons
{
    public const string Markup = "<path d=\"M5 12l-2 0l9 -9l9 9l-2 0\" />";

    private static readonly TablerIconType[] _brandIcons = Enum.GetValues<TablerIconType>()
        .Where(x => x.ToString().StartsWith("Brand", StringComparison.Ordinal))
        .ToArray();

    public static TablerIconType LateStreamingIcon { get; } = _brandIcons[0];

    public static TablerIconType BeforeFirstRenderIcon { get; } = _brandIcons[1];

    public static string GetSymbolId(TablerIconType type)
    {
        return $"tabler-icon-{type}";
    }
}

/// <summary>
/// Registers its icon after its first await, which is after a streaming response has flushed its first
/// chunk: the situation of a library whose module initializer only runs once the page uses it.
/// </summary>
[Route(Path)]
[Layout(typeof(SpriteTestLayout))]
[StreamRendering(true)]
public sealed class LateStreamingPage : ComponentBase
{
    public const string Path = "/late-streaming";

    private bool _isLoaded;

    protected override async Task OnInitializedAsync()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        TablerSvgRegistry.Register(TestIcons.LateStreamingIcon, isFilled: false, inner: TestIcons.Markup);
        _isLoaded = true;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!_isLoaded)
        {
            builder.AddContent(0, "loading");
            return;
        }

        builder.OpenComponent<TablerIcon>(1);
        builder.AddAttribute(2, nameof(TablerIcon.Type), TestIcons.LateStreamingIcon);
        builder.CloseComponent();
    }
}

/// <summary>Registers its icon before the icon's first render, in a page that does not stream.</summary>
[Route(Path)]
[Layout(typeof(SpriteTestLayout))]
public sealed class RegisteringPage : ComponentBase
{
    public const string Path = "/registering";

    protected override void OnInitialized()
    {
        TablerSvgRegistry.Register(TestIcons.BeforeFirstRenderIcon, isFilled: false, inner: TestIcons.Markup);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TablerIcon>(0);
        builder.AddAttribute(1, nameof(TablerIcon.Type), TestIcons.BeforeFirstRenderIcon);
        builder.CloseComponent();
    }
}
