using EquipTrack.Application.DTOs;
using EquipTrack.Domain.Entities;
using EquipTrack.Domain.Interfaces;

namespace EquipTrack.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EmployeeService(IEmployeeRepository employeeRepository, IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
    {
        var employees = await _employeeRepository.GetAllAsync();
        return employees.Select(e => new EmployeeDto { Id = e.Id, Name = e.Name, Department = e.Department, Email = e.Email });
    }

    public async Task<int> CreateAsync(CreateEmployeeDto dto)
    {
        var employee = new Employee { Name = dto.Name, Department = dto.Department, Email = dto.Email };
        await _employeeRepository.AddAsync(employee);
        await _unitOfWork.SaveChangesAsync();
        return employee.Id;
    }
}