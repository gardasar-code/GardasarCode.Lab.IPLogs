using System.Linq.Expressions;

namespace IpLogsCommon.Repository.Interfaces;

public interface ISpecification<T>
{
    IEnumerable<Expression<Func<T, bool>>> Criterias { get; }
    bool AsNoTracking { get; }
}

public interface ISpecification<T, TResult> : ISpecification<T>
{
    public Expression<Func<T, TResult>> Selector { get; }
    public bool Distinct { get; }
}