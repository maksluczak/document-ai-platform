using Azure;
using Contracts.Events;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProcessingService.Messaging;

namespace ProcessingService.Tests.Messaging;

public class RetryPolicyTests
{
    public class FailingScenario
    {
        private int _deliveries;

        public FailingScenario(Exception exceptionToThrow)
        {
            ExceptionToThrow = exceptionToThrow;
        }

        public Exception ExceptionToThrow { get; }

        public int Deliveries => _deliveries;

        public void RecordDelivery() => Interlocked.Increment(ref _deliveries);
    }

    public class AlwaysFailingConsumer : IConsumer<DocumentUploaded>
    {
        private readonly FailingScenario _scenario;

        public AlwaysFailingConsumer(FailingScenario scenario)
        {
            _scenario = scenario;
        }

        public Task Consume(ConsumeContext<DocumentUploaded> context)
        {
            _scenario.RecordDelivery();
            throw _scenario.ExceptionToThrow;
        }
    }

    private static async Task<int> CountDeliveriesUntilFault(Exception exceptionToThrow)
    {
        var failingScenario = new FailingScenario(exceptionToThrow);

        await using var provider = new ServiceCollection()
            .AddSingleton(failingScenario)
            .AddMassTransitTestHarness(registration =>
            {
                registration.AddConsumer<AlwaysFailingConsumer>();
                registration.UsingInMemory((context, inMemory) =>
                {
                    inMemory.UseMessageRetry(retry => RetryPolicy.Configure(
                        retry,
                        minInterval: TimeSpan.FromMilliseconds(1),
                        maxInterval: TimeSpan.FromMilliseconds(10),
                        intervalDelta: TimeSpan.FromMilliseconds(1)));
                    inMemory.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        await harness.Bus.Publish(new DocumentUploaded(Guid.NewGuid(), "invoice.pdf", "https://blob.local/invoice.pdf", DateTime.UtcNow));

        Assert.True(await harness.Published.Any<Fault<DocumentUploaded>>());
        return failingScenario.Deliveries;
    }

    [Fact]
    public async Task RetryPolicy_RetriesServiceUnavailable_ThenPublishesFault()
    {
        var deliveries = await CountDeliveriesUntilFault(new RequestFailedException(503, "Service unavailable"));

        Assert.Equal(1 + RetryPolicy.RetryLimit, deliveries);
    }

    [Fact]
    public async Task RetryPolicy_RetriesTooManyRequests()
    {
        var deliveries = await CountDeliveriesUntilFault(new RequestFailedException(429, "Too many requests"));

        Assert.Equal(1 + RetryPolicy.RetryLimit, deliveries);
    }

    [Fact]
    public async Task RetryPolicy_DoesNotRetryUnauthorized()
    {
        var deliveries = await CountDeliveriesUntilFault(new RequestFailedException(401, "Unauthorized"));

        Assert.Equal(1, deliveries);
    }

    [Fact]
    public async Task RetryPolicy_RetriesClassifierConnectionErrors()
    {
        var deliveries = await CountDeliveriesUntilFault(new HttpRequestException("Connection refused"));

        Assert.Equal(1 + RetryPolicy.RetryLimit, deliveries);
    }
}