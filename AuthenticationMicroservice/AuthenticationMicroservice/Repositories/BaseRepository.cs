namespace AuthenticationMicroservice.Repositories;

using System.Net;
using Exceptions;
using Microsoft.EntityFrameworkCore;
using Models;

public interface IBaseRepository<TEntity> where TEntity : class
{
    TEntity? GetById(int id);
    TEntity GetByIdThrowIfNull(int id);
    TEntity? GetById(Guid id);
    TEntity GetByIdThrowIfNull(Guid id);
    IQueryable<TEntity> GetAll();
    Task<TEntity> CreateAndSaveAsync(TEntity entity);
    Task<TEntity> UpdateAndSaveAsync(TEntity entity);
    Task DeleteAndSaveAsync(TEntity entity);
    void PrepareToAdd(TEntity toAdd);
    void PrepareToAdd(IEnumerable<TEntity> toAdd);
    void PrepareToUpdate(TEntity toUpdate);
    void PrepareToUpdate(IEnumerable<TEntity> toUpdate);
    void PrepareToDelete(TEntity toDelete);
    void PrepareToDelete(IEnumerable<TEntity> toDelete);
    Task CommitPendingChanges();
}

public sealed class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
    private readonly AuthenticationDbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public BaseRepository(AuthenticationDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<TEntity>();
    }

    public TEntity? GetById(int id)
    {
        return _dbSet.Find(id);
    }

    public TEntity? GetById(Guid id)
    {
        return _dbSet.Find(id);
    }

    public TEntity GetByIdThrowIfNull(int id)
    {
        return GetById(id) ??
               throw new AuthenticationException($"Item with ID {id} does not exist.", HttpStatusCode.NotFound);
    }

    public TEntity GetByIdThrowIfNull(Guid id)
    {
        return GetById(id) ??
               throw new AuthenticationException($"Item with ID {id} does not exist.", HttpStatusCode.NotFound);
    }

    public IQueryable<TEntity> GetAll()
    {
        return _dbSet.AsQueryable();
    }

    public async Task<TEntity> CreateAndSaveAsync(TEntity entity)
    {
        var created = await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();

        return created.Entity;
    }

    public async Task<TEntity> UpdateAndSaveAsync(TEntity entity)
    {
        var updated = _dbSet.Update(entity);
        await _context.SaveChangesAsync();

        return updated.Entity;
    }

    public async Task DeleteAndSaveAsync(TEntity entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public void PrepareToAdd(TEntity toAdd)
    {
        _dbSet.Add(toAdd);
    }

    public void PrepareToAdd(IEnumerable<TEntity> toAdd)
    {
        _dbSet.AddRange(toAdd);
    }

    public void PrepareToUpdate(TEntity toUpdate)
    {
        _dbSet.Update(toUpdate);
    }

    public void PrepareToUpdate(IEnumerable<TEntity> toUpdate)
    {
        _dbSet.UpdateRange(toUpdate);
    }

    public void PrepareToDelete(TEntity toDelete)
    {
        _dbSet.Remove(toDelete);
    }

    public void PrepareToDelete(IEnumerable<TEntity> toDelete)
    {
        _dbSet.RemoveRange(toDelete);
    }

    public async Task CommitPendingChanges()
    {
        await _context.SaveChangesAsync();
    }
}