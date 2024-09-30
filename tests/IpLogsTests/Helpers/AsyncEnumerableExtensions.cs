using System.Runtime.CompilerServices;

namespace IpLogsTests.Helpers;

public static class AsyncEnumerableExtensions
{
    public static ConfiguredCancelableAsyncEnumerable<T> ToConfiguredCancelableAsyncEnumerable<T>(
        this IEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        return GetAsyncEnumerable(source).WithCancellation(cancellationToken);
    }

    private static async IAsyncEnumerable<T> GetAsyncEnumerable<T>(IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
            await Task.Yield(); // Обеспечивает асинхронность
        }
    }
}