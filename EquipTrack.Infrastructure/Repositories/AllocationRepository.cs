using EquipTrack.Domain.Entities;
using EquipTrack.Domain.Interfaces;
using EquipTrack.Infrastructure.Data;

namespace EquipTrack.Infrastructure.Repositories;

public class AllocationRepository : IAllocationRepository
{
    private readonly AppDbContext _context;
    public AllocationRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(Allocation allocation) => await _context.Allocations.AddAsync(allocation);
}