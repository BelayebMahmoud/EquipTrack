using EquipTrack.Application.DTOs;
using EquipTrack.Domain.Entities;
using EquipTrack.Domain.Interfaces;

namespace EquipTrack.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (dto.Password.Length < 8)
            throw new InvalidOperationException("Password must be at least 8 characters.");

        var existing = await _userRepository.GetByUsernameAsync(dto.Username);
        if (existing is not null)
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = UserRole.User
        };

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return _tokenService.GenerateToken(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username);

        if (user is null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        return _tokenService.GenerateToken(user);
    }
}
