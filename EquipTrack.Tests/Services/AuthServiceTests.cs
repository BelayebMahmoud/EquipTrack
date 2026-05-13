using EquipTrack.Application.DTOs;
using EquipTrack.Application.Services;
using EquipTrack.Domain.Entities;
using EquipTrack.Domain.Interfaces;
using Moq;
using Xunit;

namespace EquipTrack.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _sut = new AuthService(
            _userRepo.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            _uow.Object);
    }

    [Fact]
    public async Task RegisterAsync_WhenUsernameIsAvailable_ReturnsToken()
    {
        var dto = new RegisterDto { Username = "alice", Email = "alice@example.com", Password = "Password1" };
        var expectedResponse = new AuthResponseDto { Token = "jwt-token", ExpiresAt = DateTime.UtcNow.AddHours(1) };

        _userRepo.Setup(r => r.GetByUsernameAsync("alice")).ReturnsAsync((User?)null);
        _passwordHasher.Setup(h => h.Hash("Password1")).Returns("hashed-pw");
        _tokenService.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns(expectedResponse);

        var result = await _sut.RegisterAsync(dto);

        Assert.Equal("jwt-token", result.Token);
        _userRepo.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Username == "alice" &&
            u.Email == "alice@example.com" &&
            u.PasswordHash == "hashed-pw" &&
            u.Role == UserRole.User)), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenUsernameTaken_ThrowsInvalidOperationException()
    {
        var dto = new RegisterDto { Username = "alice", Email = "alice@example.com", Password = "Password1" };

        _userRepo.Setup(r => r.GetByUsernameAsync("alice"))
            .ReturnsAsync(new User { Id = 1, Username = "alice" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RegisterAsync(dto));
        _uow.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsToken()
    {
        var storedUser = new User
        {
            Id = 1, Username = "alice", Email = "alice@example.com",
            PasswordHash = "hashed-pw", Role = UserRole.User
        };
        var expectedResponse = new AuthResponseDto { Token = "jwt-token", ExpiresAt = DateTime.UtcNow.AddHours(1) };

        _userRepo.Setup(r => r.GetByUsernameAsync("alice")).ReturnsAsync(storedUser);
        _passwordHasher.Setup(h => h.Verify("Password1", "hashed-pw")).Returns(true);
        _tokenService.Setup(t => t.GenerateToken(storedUser)).Returns(expectedResponse);

        var result = await _sut.LoginAsync(new LoginDto { Username = "alice", Password = "Password1" });

        Assert.Equal("jwt-token", result.Token);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        var storedUser = new User { Id = 1, Username = "alice", PasswordHash = "hashed-pw" };

        _userRepo.Setup(r => r.GetByUsernameAsync("alice")).ReturnsAsync(storedUser);
        _passwordHasher.Setup(h => h.Verify("WrongPassword", "hashed-pw")).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(new LoginDto { Username = "alice", Password = "WrongPassword" }));
    }
}
