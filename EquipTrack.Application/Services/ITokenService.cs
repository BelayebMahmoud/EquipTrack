using EquipTrack.Application.DTOs;
using EquipTrack.Domain.Entities;

namespace EquipTrack.Application.Services;

public interface ITokenService
{
    AuthResponseDto GenerateToken(User user);
}
