using EquipTrack.Domain.Entities;
using EquipTrack.Domain.Interfaces;
using EquipTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquipTrack.Infrastructure.Repositories;

public class AllocationRepository : IAllocationRepository
{
    private readonly AppDbContext _context;
    public AllocationRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(Allocation allocation) => await _context.Allocations.AddAsync(allocation);

    public async Task<IEnumerable<Allocation>> GetByAssetIdAsync(int assetId) =>
        await _context.Allocations.Where(a => a.AssetId == assetId).ToListAsync();

    public async Task<IEnumerable<Allocation>> GetByEmployeeIdAsync(int employeeId) =>
        await _context.Allocations.Where(a => a.EmployeeId == employeeId).ToListAsync();

    public async Task<Allocation?> GetActiveByAssetIdAsync(int assetId) =>
        await _context.Allocations.FirstOrDefaultAsync(a => a.AssetId == assetId && a.ReturnDate == null);
}