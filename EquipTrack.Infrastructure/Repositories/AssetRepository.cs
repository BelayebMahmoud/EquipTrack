using EquipTrack.Domain.Entities;
using EquipTrack.Domain.Interfaces;
using EquipTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquipTrack.Infrastructure.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly AppDbContext _context;

    public AssetRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Asset?> GetByIdAsync(int id) => await _context.Assets.FindAsync(id);
    public async Task<IEnumerable<Asset>> GetAllAsync() => await _context.Assets.ToListAsync();
    public async Task AddAsync(Asset asset) => await _context.Assets.AddAsync(asset);
    public void Update(Asset asset) => _context.Assets.Update(asset);
}