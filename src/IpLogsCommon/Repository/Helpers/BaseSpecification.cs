using System.Linq.Expressions;
using IpLogsCommon.Repository.Interfaces;

namespace IpLogsCommon.Repository.Helpers;

public abstract class BaseSpecification<T> : ISpecification<T>
{
    public IEnumerable<Expression<Func<T, bool>>> Criterias { get; } = new List<Expression<Func<T, bool>>>();
    public bool AsNoTracking { get; protected init; }

    protected virtual void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        ((List<Expression<Func<T, bool>>>)Criterias).Add(criteria);
    }
}

public abstract class BaseSpecification<T, TResult> : BaseSpecification<T>, ISpecification<T, TResult>
{
    public Expression<Func<T, TResult>> Selector { get; protected init; } = x => default!;
    public bool Distinct { get; protected init; }
}