using EquipTrack.Application.DTOs;
using EquipTrack.Domain.Entities;
using EquipTrack.Domain.Interfaces;

namespace EquipTrack.Application.Services;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAllocationRepository _allocationRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Inject all required repositories and the Unit of Work
    public AssetService(
        IAssetRepository assetRepository,
        IEmployeeRepository employeeRepository,
        IAllocationRepository allocationRepository,
        IUnitOfWork unitOfWork)
    {
        _assetRepository = assetRepository;
        _employeeRepository = employeeRepository;
        _allocationRepository = allocationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<AssetDto>> GetAllAssetsAsync()
    {
        var assets = await _assetRepository.GetAllAsync();
        return assets.Select(MapToDto);
    }

    public async Task<AssetDto?> GetByIdAsync(int id)
    {
        var asset = await _assetRepository.GetByIdAsync(id);
        return asset == null ? null : MapToDto(asset);
    }

    public async Task<int> CreateAssetAsync(CreateAssetDto dto)
    {
        var asset = new Asset { Name = dto.Name, SerialNumber = dto.SerialNumber, Status = AssetStatus.Available };
        await _assetRepository.AddAsync(asset);
        await _unitOfWork.SaveChangesAsync();
        return asset.Id;
    }

    public async Task<bool> AssignAssetAsync(int assetId, int employeeId)
    {
        var asset = await _assetRepository.GetByIdAsync(assetId);
        var employee = await _employeeRepository.GetByIdAsync(employeeId);

        if (asset == null || employee == null) return false;

        asset.Assign();

        await _allocationRepository.AddAsync(new Allocation
        {
            AssetId = assetId,
            EmployeeId = employeeId,
            AssignedDate = DateTime.UtcNow
        });
        _assetRepository.Update(asset);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReturnAssetAsync(int assetId)
    {
        var asset = await _assetRepository.GetByIdAsync(assetId);
        if (asset == null) return false;

        var allocation = await _allocationRepository.GetActiveByAssetIdAsync(assetId);
        if (allocation == null) return false;

        asset.Return();
        allocation.ReturnDate = DateTime.UtcNow;
        _assetRepository.Update(asset);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static AssetDto MapToDto(Asset a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        SerialNumber = a.SerialNumber,
        Status = a.Status.ToString()
    };
}