using System.Net;
using System.Net.Http.Json;
using Planoria.Api.Dtos;
using Planoria.Models;
using Xunit;

namespace Planoria.Api.Tests;

public class TaskOwnershipTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _api;
    public TaskOwnershipTests(ApiFixture api) => _api = api;

    private static CreateTaskRequest NewTask() => new()
    {
        Title = "Alice's task",
        Description = "private",
        RecurrenceType = RecurrenceType.Daily,
        DueDate = DateTime.UtcNow.AddDays(1)
    };

    private async Task<int> AliceCreatesTaskAsync()
    {
        var res = await _api.ClientFor(_api.TokenA).PostAsJsonAsync("/api/tasks", NewTask());
        res.EnsureSuccessStatusCode();
        var task = await res.Content.ReadFromJsonAsync<TaskResponse>();
        return task!.Id;
    }

    [Fact]
    public async Task Request_without_token_returns_401()
    {
        var res = await _api.Anonymous().GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Owner_can_read_own_task()
    {
        var id = await AliceCreatesTaskAsync();
        var res = await _api.ClientFor(_api.TokenA).GetAsync($"/api/tasks/{id}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Other_user_cannot_read_task()
    {
        var id = await AliceCreatesTaskAsync();
        var res = await _api.ClientFor(_api.TokenB).GetAsync($"/api/tasks/{id}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Other_user_does_not_see_task_in_list()
    {
        await AliceCreatesTaskAsync();
        var list = await _api.ClientFor(_api.TokenB).GetFromJsonAsync<List<TaskResponse>>("/api/tasks");
        Assert.Empty(list!);
    }

    [Fact]
    public async Task Other_user_cannot_update_task()
    {
        var id = await AliceCreatesTaskAsync();
        var res = await _api.ClientFor(_api.TokenB).PutAsJsonAsync($"/api/tasks/{id}",
            new UpdateTaskRequest { Title = "Hacked", RecurrenceType = RecurrenceType.Daily, DueDate = DateTime.UtcNow });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Other_user_cannot_delete_task()
    {
        var id = await AliceCreatesTaskAsync();
        var res = await _api.ClientFor(_api.TokenB).DeleteAsync($"/api/tasks/{id}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        // görev gerçekten silinmemiş olmalı
        var still = await _api.ClientFor(_api.TokenA).GetAsync($"/api/tasks/{id}");
        Assert.Equal(HttpStatusCode.OK, still.StatusCode);
    }

    [Fact]
    public async Task Other_user_cannot_complete_task()
    {
        var id = await AliceCreatesTaskAsync();
        var res = await _api.ClientFor(_api.TokenB).PostAsync($"/api/tasks/{id}/complete", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Completing_twice_keeps_the_first_completion_time()
    {
        var id = await AliceCreatesTaskAsync();
        var alice = _api.ClientFor(_api.TokenA);

        var first = await (await alice.PostAsync($"/api/tasks/{id}/complete", null))
            .Content.ReadFromJsonAsync<TaskResponse>();
        await Task.Delay(50);
        var second = await (await alice.PostAsync($"/api/tasks/{id}/complete", null))
            .Content.ReadFromJsonAsync<TaskResponse>();

        Assert.True(first!.IsCompleted);
        Assert.Equal(first.CompletedAt, second!.CompletedAt);
    }

    [Fact]
    public async Task Update_with_blank_title_returns_400()
    {
        var id = await AliceCreatesTaskAsync();
        var res = await _api.ClientFor(_api.TokenA).PutAsJsonAsync($"/api/tasks/{id}",
            new UpdateTaskRequest { Title = "   ", RecurrenceType = RecurrenceType.Daily, DueDate = DateTime.UtcNow });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}