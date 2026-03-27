using EquipTrack.Domain.Entities;

namespace EquipTrack.Domain.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int id);
    Task<IEnumerable<Employee>> GetAllAsync();
    Task AddAsync(Employee employee);
}