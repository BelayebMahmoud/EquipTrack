using EquipTrack.Application.DTOs;
using EquipTrack.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquipTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AllocationsController : ControllerBase
{
    private readonly IAllocationService _allocationService;

    public AllocationsController(IAllocationService allocationService)
    {
        _allocationService = allocationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AllocationDto>>> GetAll()
        => Ok(await _allocationService.GetAllAsync());

    [HttpGet("by-asset/{assetId}")]
    public async Task<ActionResult<IEnumerable<AllocationDto>>> GetByAsset(int assetId)
        => Ok(await _allocationService.GetByAssetIdAsync(assetId));

    [HttpGet("by-employee/{employeeId}")]
    public async Task<ActionResult<IEnumerable<AllocationDto>>> GetByEmployee(int employeeId)
        => Ok(await _allocationService.GetByEmployeeIdAsync(employeeId));
}
