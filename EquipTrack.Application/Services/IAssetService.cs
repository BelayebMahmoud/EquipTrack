using EquipTrack.Application.DTOs;

namespace EquipTrack.Application.Services;

public interface IAssetService
{
    Task<IEnumerable<AssetDto>> GetAllAssetsAsync();
    Task<int> CreateAssetAsync(CreateAssetDto dto);
    Task<bool> AssignAssetAsync(int assetId, int employeeId);
}