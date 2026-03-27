using EquipTrack.Application.DTOs;
using EquipTrack.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquipTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll()
    {
        return Ok(await _employeeService.GetAllAsync());
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateEmployeeDto dto)
    {
        var id = await _employeeService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id }, id);
    }
}