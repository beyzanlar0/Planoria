using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Planoria.Api.Tests;

public class ApiFixture : IAsyncLifetime
{
    public ApiFactory Factory { get; } = new();
    public string TokenA { get; private set; } = "";   // Alice
    public string TokenB { get; private set; } = "";   // Bob

    public async Task InitializeAsync()
    {
        TokenA = await RegisterAsync("alice");
        TokenB = await RegisterAsync("bob");
    }

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }

    private HttpClient NewClient() => Factory.CreateClient(
        new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    public HttpClient Anonymous() => NewClient();

    public HttpClient ClientFor(string token)
    {
        var client = NewClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<string> RegisterAsync(string name)
    {
        var res = await NewClient().PostAsJsonAsync("/api/auth/register",
            new { username = name, email = $"{name}@test.com", password = "Test12345678!" });
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("token").GetString()!;
    }
}