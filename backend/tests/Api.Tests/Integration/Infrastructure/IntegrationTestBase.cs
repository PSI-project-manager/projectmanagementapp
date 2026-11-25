using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Tests.Integration.Infrastructure;

[Collection(IntegrationCollection.Name)]
public abstract class IntegrationTestBase(ApiFactory factory) : IAsyncLifetime
{
    private readonly List<IServiceScope> scopes = [];

    protected ApiFactory Factory { get; } = factory;

    protected TestData Data { get; } = new(factory);

    public virtual Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public virtual Task DisposeAsync()
    {
        foreach (var scope in scopes)
        {
            scope.Dispose();
        }
        return Task.CompletedTask;
    }

    protected HttpClient AnonymousClient() => Factory.CreateClient();

    protected HttpClient ClientFor(TestUser user) => ClientWithToken(TestTokens.For(user));

    protected HttpClient ClientWithToken(string token)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // resolves a service the same way a request would: from its own DI scope
    protected T Resolve<T>()
        where T : notnull
    {
        var scope = Factory.Services.CreateScope();
        scopes.Add(scope);
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    protected static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
}
