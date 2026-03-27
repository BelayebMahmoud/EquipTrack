using EquipTrack.Domain.Entities;
using EquipTrack.Domain.Interfaces;
using EquipTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquipTrack.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;
    public EmployeeRepository(AppDbContext context) => _context = context;

    public async Task<Employee?> GetByIdAsync(int id) => await _context.Employees.FindAsync(id);
    public async Task<IEnumerable<Employee>> GetAllAsync() => await _context.Employees.ToListAsync();
    public async Task AddAsync(Employee employee) => await _context.Employees.AddAsync(employee);
}