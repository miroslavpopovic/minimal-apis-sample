using System.Net;
using System.Net.Http.Json;
using MinimalApis.MinimalSample.Models;

namespace MinimalApis.MinimalSample.Tests;

public class UserApiTests : IAsyncLifetime
{
    private MinimalWebApplicationFactory? _application;
    private HttpClient _client = new();

    public Task InitializeAsync()
    {
        _application = new MinimalWebApplicationFactory();
        _client = _application.CreateClient();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _application!.DisposeAsync();
    }

    [Fact]
    public async Task Get_users_returns_a_page_of_users()
    {
        var users = await _client.GetFromJsonAsync<PagedList<UserModel>>("/api/v1/users?page=2&size=10");

        Assert.Equal(2, users!.Page);
        Assert.Equal(10, users.PageSize);
        Assert.Equal(10, users.Items.Count());
    }

    [Fact]
    public async Task Get_user_returns_a_single_user_for_existing_user_id()
    {
        var user = await _client.GetFromJsonAsync<UserModel>("/api/v1/users/1");

        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.NotEmpty(user.Name);
    }

    [Fact]
    public async Task Get_user_returns_not_found_for_wrong_id()
    {
        var result = await _client.GetAsync("/api/v1/users/0");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task Delete_user_returns_not_found_for_wrong_id()
    {
        var result = await _client.DeleteAsync("/api/v1/users/0");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task Delete_user_returns_ok_for_existing_user_id()
    {
        var result = await _client.DeleteAsync($"/api/v1/users/1");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task Create_user_returns_created_user_with_url()
    {
        var model = new UserInputModel
        {
            Name = "Test user 123",
            HourRate = 100
        };

        var result = await _client.PostAsJsonAsync("/api/v1/users", model);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);

        var user = await result.Content.ReadFromJsonAsync<UserModel>();

        Assert.Equal($"http://localhost/api/v1/users?id={user!.Id}", result.Headers.First().Value.First());
        Assert.Equal(model.Name, user.Name);
        Assert.Equal(model.HourRate, user.HourRate);
    }

    [Fact]
    public async Task Update_user_returns_not_found_for_wrong_id()
    {
        var model = new UserInputModel
        {
            Name = "Test name changed",
            HourRate = 123
        };

        var result = await _client.PutAsJsonAsync("/api/v1/users/0", model);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task Update_user_returns_ok_for_existing_user_id()
    {
        var model = new UserInputModel
        {
            Name = "Test name changed",
            HourRate = 123
        };

        var result = await _client.PutAsJsonAsync("/api/v1/users/1", model);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        var user = await result.Content.ReadFromJsonAsync<UserModel>();

        Assert.Equal(model.Name, user!.Name);
        Assert.Equal(model.HourRate, user.HourRate);
    }
}
