using EquipTrack.Domain.Entities;

namespace EquipTrack.Domain.Interfaces;

public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(int id);
    Task<IEnumerable<Asset>> GetAllAsync();
    Task AddAsync(Asset asset);
    void Update(Asset asset);
}