using EquipTrack.Application.DTOs;

namespace EquipTrack.Application.Services;

public interface IAssetService
{
    Task<IEnumerable<AssetDto>> GetAllAssetsAsync();
    Task<AssetDto?> GetByIdAsync(int id);
    Task<int> CreateAssetAsync(CreateAssetDto dto);
    Task<bool> AssignAssetAsync(int assetId, int employeeId);
    Task<bool> ReturnAssetAsync(int assetId);
}