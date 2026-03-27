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

        // Map Domain Entities to DTOs to prevent exposing database models to the API
        return assets.Select(a => new AssetDto
        {
            Id = a.Id,
            Name = a.Name,
            SerialNumber = a.SerialNumber,
            Status = a.Status.ToString()
        });
    }

    public async Task<int> CreateAssetAsync(CreateAssetDto dto)
    {
        // Map DTO to Domain Entity
        var asset = new Asset
        {
            Name = dto.Name,
            SerialNumber = dto.SerialNumber,
            Status = AssetStatus.Available // New assets are always available by default
        };

        await _assetRepository.AddAsync(asset);
        await _unitOfWork.SaveChangesAsync(); // Commit the transaction to the database

        return asset.Id;
    }

    public async Task<bool> AssignAssetAsync(int assetId, int employeeId)
    {
        // 1. Fetch the required entities
        var asset = await _assetRepository.GetByIdAsync(assetId);
        var employee = await _employeeRepository.GetByIdAsync(employeeId);

        // 2. Validate existence
        if (asset == null || employee == null)
        {
            return false;
        }

        // 3. Execute Domain Logic
        // This will change the status to 'InUse' or throw an InvalidOperationException if already assigned
        asset.Assign();

        // 4. Create the Allocation tracking record
        var allocation = new Allocation
        {
            AssetId = assetId,
            EmployeeId = employeeId,
            AssignedDate = DateTime.UtcNow
        };

        // 5. Update repositories
        await _allocationRepository.AddAsync(allocation);
        _assetRepository.Update(asset);

        // 6. Commit the transaction
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}