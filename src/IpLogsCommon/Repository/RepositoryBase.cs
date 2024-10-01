using System.Runtime.CompilerServices;
using IpLogsCommon.Repository.Helpers;
using IpLogsCommon.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IpLogsCommon.Repository;

public sealed class RepositoryBase<TK>(TK dbContext) : IRepository where TK : DbContext
{
    private DbContext DbContext { get; } = dbContext;

    public async Task<T> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        await DbContext.AddAsync(entity, cancellationToken);
        return entity;
    }

    public void SetValues<T>(T entity, T newEntity)
        where T : class
    {
        DbContext.Entry(entity).CurrentValues.SetValues(newEntity);
    }

    public Task<T?> FirstOrDefaultAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default)
        where T : class
    {
        return ApplySpecification(spec).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return DbContext.SaveChangesAsync(cancellationToken);
    }

    public ConfiguredCancelableAsyncEnumerable<TResult> AsAsyncEnumerableStream<T, TResult>(
        ISpecification<T, TResult> spec,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = SpecificationEvaluator<T, TResult>.GetQuery(DbContext.Set<T>().AsQueryable(), spec);
        return query.AsAsyncEnumerable().WithCancellation(cancellationToken);
    }

    private IQueryable<T> ApplySpecification<T>(ISpecification<T> spec) where T : class
    {
        return SpecificationEvaluator<T>.GetQuery(DbContext.Set<T>().AsQueryable(), spec);
    }

    #region Dispose

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~RepositoryBase()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) DbContext.Dispose();
        _disposed = true;
    }

    #endregion
}