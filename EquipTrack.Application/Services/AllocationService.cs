using EquipTrack.Application.DTOs;
using EquipTrack.Domain.Entities;
using EquipTrack.Domain.Interfaces;

namespace EquipTrack.Application.Services;

public class AllocationService : IAllocationService
{
    private readonly IAllocationRepository _allocationRepository;

    public AllocationService(IAllocationRepository allocationRepository)
    {
        _allocationRepository = allocationRepository;
    }

    public async Task<IEnumerable<AllocationDto>> GetAllAsync()
    {
        var allocations = await _allocationRepository.GetAllAsync();
        return allocations.Select(MapToDto);
    }

    public async Task<IEnumerable<AllocationDto>> GetByAssetIdAsync(int assetId)
    {
        var allocations = await _allocationRepository.GetByAssetIdAsync(assetId);
        return allocations.Select(MapToDto);
    }

    public async Task<IEnumerable<AllocationDto>> GetByEmployeeIdAsync(int employeeId)
    {
        var allocations = await _allocationRepository.GetByEmployeeIdAsync(employeeId);
        return allocations.Select(MapToDto);
    }

    private static AllocationDto MapToDto(Allocation a) => new()
    {
        Id = a.Id,
        AssetId = a.AssetId,
        AssetName = a.Asset?.Name ?? string.Empty,
        EmployeeId = a.EmployeeId,
        EmployeeName = a.Employee?.Name ?? string.Empty,
        AssignedDate = a.AssignedDate,
        ReturnDate = a.ReturnDate
    };
}
