namespace EquipTrack.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
}