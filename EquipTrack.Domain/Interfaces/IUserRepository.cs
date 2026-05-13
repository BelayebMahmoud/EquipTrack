using EquipTrack.Domain.Entities;

namespace EquipTrack.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task AddAsync(User user);
    Task<bool> AnyAsync();
}
