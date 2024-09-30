using System.Runtime.CompilerServices;

namespace IpLogsCommon.Repository.Interfaces;

public interface IRepository<out TK> : IDisposable
{
    Task<T> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
    void SetValues<T>(T entity, T newEntity, CancellationToken cancellationToken = default) where T : class;

    Task<T?> FirstOrDefaultAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default)
        where T : class;

    ConfiguredCancelableAsyncEnumerable<TResult> AsAsyncEnumerableStream<T, TResult>(ISpecification<T, TResult> spec,
        CancellationToken cancellationToken = default) where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}