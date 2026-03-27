using EquipTrack.Domain.Entities;

namespace EquipTrack.Domain.Interfaces;

public interface IAllocationRepository
{
    Task AddAsync(Allocation allocation);
}