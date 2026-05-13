using EquipTrack.Application.DTOs;
using EquipTrack.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquipTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;

    public AssetsController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAll()
        => Ok(await _assetService.GetAllAssetsAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<AssetDto>> GetById(int id)
    {
        var asset = await _assetService.GetByIdAsync(id);
        return asset == null ? NotFound() : Ok(asset);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateAssetDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var assetId = await _assetService.CreateAssetAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = assetId }, assetId);
    }

    [HttpPost("{id}/assign/{employeeId}")]
    public async Task<IActionResult> AssignToEmployee(int id, int employeeId)
    {
        var result = await _assetService.AssignAssetAsync(id, employeeId);
        return result ? Ok("Asset assigned successfully.") : NotFound("Asset or Employee not found.");
    }

    [HttpPost("{id}/return")]
    public async Task<IActionResult> Return(int id)
    {
        var result = await _assetService.ReturnAssetAsync(id);
        return result ? Ok("Asset returned successfully.") : NotFound("Asset or active allocation not found.");
    }
}