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
        => Ok(await _employeeService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var employee = await _employeeService.GetByIdAsync(id);
        return employee == null ? NotFound() : Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateEmployeeDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var id = await _employeeService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }
}