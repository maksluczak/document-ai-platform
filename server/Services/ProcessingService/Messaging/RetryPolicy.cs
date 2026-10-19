using Azure;
using MassTransit;

namespace ProcessingService.Messaging;

public static class RetryPolicy
{
    public const int RetryLimit = 5;

    public static void Configure(IRetryConfigurator retry, TimeSpan minInterval, TimeSpan maxInterval, TimeSpan intervalDelta)
    {
        retry.Ignore<RequestFailedException>(exception => exception.Status is >= 400 and < 500 and not 429);
        retry.Exponential(RetryLimit, minInterval, maxInterval, intervalDelta);
    }
}