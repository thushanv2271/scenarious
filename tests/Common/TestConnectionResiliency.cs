using Npgsql;
using Polly;
using Polly.Retry;

namespace IntegrationTests.Common;

public static class TestConnectionResiliency
{
    private static readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<NpgsqlException>()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (exception, timespan, retryCount, context) => Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s due to: {exception.Message}"));

    public static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
    {
        return await _retryPolicy.ExecuteAsync(operation);
    }

    public static async Task ExecuteWithRetryAsync(Func<Task> operation)
    {
        await _retryPolicy.ExecuteAsync(operation);
    }
}
