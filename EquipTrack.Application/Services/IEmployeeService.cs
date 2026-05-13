using EquipTrack.Application.DTOs;

namespace EquipTrack.Application.Services;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllAsync();
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(CreateEmployeeDto dto);
}