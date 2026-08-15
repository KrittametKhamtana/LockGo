using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LockGo.Application.DTOs;

namespace LockGo.Tests.Integration;

public class AuthApiTests : IClassFixture<LockGoWebApplicationFactory>, IAsyncLifetime
{
    private readonly LockGoWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthApiTests(LockGoWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.SeedAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static SignUpRequest ValidSignUp(string suffix) => new(
        FirstName: "Jane",
        LastName: "Doe",
        Email: $"jane-{suffix}@example.com",
        Username: $"janedoe-{suffix}",
        Password: "password123",
        ConfirmPassword: "password123");

    [Fact]
    public async Task SignUp_WithNewEmailAndUsername_Returns201WithToken()
    {
        var request = ValidSignUp(Guid.NewGuid().ToString("N")[..8]);

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.Username.Should().Be(request.Username);
        body.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task SignUp_WithAlreadyRegisteredEmail_Returns409()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var first = ValidSignUp(suffix);
        await _client.PostAsJsonAsync("/api/auth/signup", first);

        // Same email, different username.
        var second = first with { Username = $"different-{suffix}" };
        var response = await _client.PostAsJsonAsync("/api/auth/signup", second);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error.Code.Should().Be("EMAIL_TAKEN");
    }

    [Fact]
    public async Task SignUp_WithAlreadyTakenUsername_Returns409()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var first = ValidSignUp(suffix);
        await _client.PostAsJsonAsync("/api/auth/signup", first);

        // Same username, different email.
        var second = first with { Email = $"different-{suffix}@example.com" };
        var response = await _client.PostAsJsonAsync("/api/auth/signup", second);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error.Code.Should().Be("USERNAME_TAKEN");
    }

    [Fact]
    public async Task SignUp_WithMismatchedPasswords_Returns400()
    {
        var request = ValidSignUp(Guid.NewGuid().ToString("N")[..8]) with { ConfirmPassword = "somethingElse1" };

        var response = await _client.PostAsJsonAsync("/api/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignIn_WithCorrectCredentials_Returns200WithToken()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var signUp = ValidSignUp(suffix);
        await _client.PostAsJsonAsync("/api/auth/signup", signUp);

        var response = await _client.PostAsJsonAsync("/api/auth/signin", new SignInRequest(signUp.Username, signUp.Password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.Username.Should().Be(signUp.Username);
    }

    [Fact]
    public async Task SignIn_WithWrongPassword_Returns401()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var signUp = ValidSignUp(suffix);
        await _client.PostAsJsonAsync("/api/auth/signup", signUp);

        var response = await _client.PostAsJsonAsync("/api/auth/signin", new SignInRequest(signUp.Username, "wrong-password"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task SignIn_WithUnknownUsername_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/signin",
            new SignInRequest($"no-such-user-{Guid.NewGuid():N}", "whatever1"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record ErrorEnvelope(ErrorDetail Error);
    private record ErrorDetail(string Code, string Message);
}
