using EquipTrack.Domain.Entities;

namespace EquipTrack.Domain.Interfaces;

public interface IAllocationRepository
{
    Task AddAsync(Allocation allocation);
    Task<IEnumerable<Allocation>> GetAllAsync();
    Task<IEnumerable<Allocation>> GetByAssetIdAsync(int assetId);
    Task<IEnumerable<Allocation>> GetByEmployeeIdAsync(int employeeId);
    Task<Allocation?> GetActiveByAssetIdAsync(int assetId);
}