using EquipTrack.Application.DTOs;

namespace EquipTrack.Application.Services;

public interface IAllocationService
{
    Task<IEnumerable<AllocationDto>> GetAllAsync();
    Task<IEnumerable<AllocationDto>> GetByAssetIdAsync(int assetId);
    Task<IEnumerable<AllocationDto>> GetByEmployeeIdAsync(int employeeId);
}
