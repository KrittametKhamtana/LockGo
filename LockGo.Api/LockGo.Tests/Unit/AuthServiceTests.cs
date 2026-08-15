using FluentAssertions;
using LockGo.Application.Common;
using LockGo.Application.Common.Exceptions;
using LockGo.Application.DTOs;
using LockGo.Application.Interfaces;
using LockGo.Application.Services;
using LockGo.Domain.Entities;
using Moq;

namespace LockGo.Tests.Unit;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<bool>> op, CancellationToken ct) => op(ct));

        _jwtTokenGenerator.Setup(g => g.GenerateToken(It.IsAny<User>())).Returns("fake-jwt-token");

        _sut = new AuthService(_userRepository.Object, _unitOfWork.Object, _jwtTokenGenerator.Object);
    }

    private static SignUpRequest ValidSignUp() => new(
        FirstName: "Jane",
        LastName: "Doe",
        Email: "jane@example.com",
        Username: "janedoe",
        Password: "password123",
        ConfirmPassword: "password123");

    [Fact]
    public async Task SignUpAsync_WhenEmailAndUsernameAreFree_CreatesAccountAndReturnsToken()
    {
        var request = ValidSignUp();

        var result = await _sut.SignUpAsync(request, CancellationToken.None);

        result.Token.Should().Be("fake-jwt-token");
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be("jane@example.com");
        result.Username.Should().Be("janedoe");
        _userRepository.Verify(
            r => r.Add(It.Is<User>(u =>
                u.Email == "jane@example.com" &&
                u.Username == "janedoe" &&
                u.PasswordHash != null &&
                u.PasswordHash != "password123")),
            Times.Once);
    }

    [Fact]
    public async Task SignUpAsync_HashesThePassword_NeverStoresItInPlainText()
    {
        var request = ValidSignUp();
        User? added = null;
        _userRepository.Setup(r => r.Add(It.IsAny<User>())).Callback<User>(u => added = u);

        await _sut.SignUpAsync(request, CancellationToken.None);

        added.Should().NotBeNull();
        PasswordHasher.Verify("password123", added!.PasswordHash!).Should().BeTrue();
    }

    [Fact]
    public async Task SignUpAsync_WhenEmailAlreadyRegistered_ThrowsConflict()
    {
        _userRepository.Setup(r => r.EmailExistsAsync("jane@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = ValidSignUp();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _sut.SignUpAsync(request, CancellationToken.None));

        ex.Code.Should().Be("EMAIL_TAKEN");
        _userRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task SignUpAsync_WhenUsernameAlreadyTaken_ThrowsConflict()
    {
        _userRepository.Setup(r => r.UsernameExistsAsync("janedoe", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = ValidSignUp();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _sut.SignUpAsync(request, CancellationToken.None));

        ex.Code.Should().Be("USERNAME_TAKEN");
        _userRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task SignUpAsync_WhenPasswordsDoNotMatch_ThrowsValidation()
    {
        var request = ValidSignUp() with { ConfirmPassword = "somethingElse" };

        await Assert.ThrowsAsync<ValidationAppException>(() => _sut.SignUpAsync(request, CancellationToken.None));

        _userRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("1234567")]
    public async Task SignUpAsync_WhenPasswordTooShort_ThrowsValidation(string shortPassword)
    {
        var request = ValidSignUp() with { Password = shortPassword, ConfirmPassword = shortPassword };

        await Assert.ThrowsAsync<ValidationAppException>(() => _sut.SignUpAsync(request, CancellationToken.None));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("")]
    public async Task SignUpAsync_WhenEmailIsInvalid_ThrowsValidation(string badEmail)
    {
        var request = ValidSignUp() with { Email = badEmail };

        await Assert.ThrowsAsync<ValidationAppException>(() => _sut.SignUpAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task SignInAsync_WhenUsernameAndPasswordMatch_ReturnsToken()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Jane Doe",
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            Username = "janedoe",
            PasswordHash = PasswordHasher.Hash("password123"),
        };
        _userRepository.Setup(r => r.GetByUsernameAsync("janedoe", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _sut.SignInAsync(new SignInRequest("janedoe", "password123"), CancellationToken.None);

        result.Token.Should().Be("fake-jwt-token");
        result.Username.Should().Be("janedoe");
    }

    [Fact]
    public async Task SignInAsync_WhenPasswordIsWrong_ThrowsUnauthorized()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Jane Doe",
            Username = "janedoe",
            PasswordHash = PasswordHasher.Hash("password123"),
        };
        _userRepository.Setup(r => r.GetByUsernameAsync("janedoe", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => _sut.SignInAsync(new SignInRequest("janedoe", "wrong-password"), CancellationToken.None));

        ex.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task SignInAsync_WhenUsernameDoesNotExist_ThrowsUnauthorized_NotNotFound()
    {
        _userRepository.Setup(r => r.GetByUsernameAsync("ghost", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Same error as a wrong password — must not reveal whether the username exists.
        var ex = await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => _sut.SignInAsync(new SignInRequest("ghost", "whatever1"), CancellationToken.None));

        ex.Code.Should().Be("INVALID_CREDENTIALS");
    }
}
