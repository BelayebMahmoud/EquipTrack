using EquipTrack.Application.DTOs;

namespace EquipTrack.Application.Services;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllAsync();
    Task<int> CreateAsync(CreateEmployeeDto dto);
}