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
    {
        var assets = await _assetService.GetAllAssetsAsync();
        return Ok(assets);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateAssetDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var assetId = await _assetService.CreateAssetAsync(dto);

        // Return 201 Created
        return CreatedAtAction(nameof(GetAll), new { id = assetId }, assetId);
    }
    // Add this method to existing AssetsController
    [HttpPost("{id}/assign/{employeeId}")]
    public async Task<IActionResult> AssignToEmployee(int id, int employeeId)
    {
        try
        {
            var result = await _assetService.AssignAssetAsync(id, employeeId);
            if (!result) return NotFound("Asset or Employee not found.");

            return Ok("Asset assigned successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message); // Handles Domain Exceptions (e.g., Asset not available)
        }
    }
}