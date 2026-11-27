using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Blazor.Tabler.Icons.Svg.Streaming.Tests;

public class StreamingRenderingTests
{
    [Fact]
    public async Task StreamingPage_RegisteringAnIconAfterTheFirstFlush_SendsItsSymbolInTheStreamedUpdate()
    {
        // Arrange
        await using var app = await StartApp();
        var symbolId = TestIcons.GetSymbolId(TestIcons.LateStreamingIcon);

        // Act
        var html = await app.CreateClient().GetStringAsync(LateStreamingPage.Path);

        // Assert
        Assert.Contains("<blazor-ssr>", html);
        Assert.Contains($"href=\"#{symbolId}\"", html);
        Assert.Contains($"<symbol id=\"{symbolId}\"", html);
    }

    [Fact]
    public async Task Page_RegisteringAnIconBeforeItsFirstRender_HasTheSymbolInTheDocument()
    {
        // Arrange
        await using var app = await StartApp();
        var symbolId = TestIcons.GetSymbolId(TestIcons.BeforeFirstRenderIcon);

        // Act
        var html = await app.CreateClient().GetStringAsync(RegisteringPage.Path);

        // Assert
        Assert.DoesNotContain("<blazor-ssr>", html);
        Assert.Contains($"href=\"#{symbolId}\"", html);
        Assert.Contains($"<symbol id=\"{symbolId}\"", html);
    }

    private static async Task<WebApplication> StartApp()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Services.AddRazorComponents();

        var app = builder.Build();
        app.UseAntiforgery();
        app.MapRazorComponents<StreamingTestApp>();
        await app.StartAsync();

        return app;
    }
}

internal static class WebApplicationTestExtensions
{
    public static HttpClient CreateClient(this WebApplication app)
    {
        return app.GetTestServer().CreateClient();
    }
}
