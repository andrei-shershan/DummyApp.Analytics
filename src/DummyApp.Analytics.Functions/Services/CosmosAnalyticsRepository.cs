using Microsoft.Azure.Cosmos;
using DummyApp.Analytics.Functions.Models;
using DummyApp.Analytics.Functions.Options;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DummyApp.Analytics.Functions.Services;

public sealed class CosmosAnalyticsRepository : ICosmosAnalyticsRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbOptions _options;
    private readonly Container _container;

    public CosmosAnalyticsRepository(IOptions<CosmosDbOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException($"{nameof(CosmosDbOptions.ConnectionString)} is not configured.");
        }

        _cosmosClient = new CosmosClient(_options.ConnectionString);
        var database = _cosmosClient.CreateDatabaseIfNotExistsAsync(_options.AnalyticsDatabaseName).GetAwaiter().GetResult();
        _container = database.Database.CreateContainerIfNotExistsAsync(_options.EventsContainerName, "/OrderId").GetAwaiter().GetResult().Container;
    }

    public async Task SaveAsync(AnalyticsEventRequest analyticsEvent, CancellationToken cancellationToken)
    {
        if (analyticsEvent is null)
        {
            throw new ArgumentNullException(nameof(analyticsEvent));
        }

        var document = new AnalyticsEventDocument(analyticsEvent);
        await _container.UpsertItemAsync(document, new PartitionKey(document.OrderId.ToString()), cancellationToken: cancellationToken);
    }

    private sealed class AnalyticsEventDocument
    {
        [JsonProperty("id")]
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public Guid OrderId { get; init; }
        public string Status { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string SiteId { get; init; } = string.Empty;
        public AnalyticsOrderAddress? Address { get; init; }
        public IEnumerable<AnalyticsOrderItem> Items { get; init; } = Array.Empty<AnalyticsOrderItem>();
        public IEnumerable<string> Tags { get; init; } = Array.Empty<string>();
        public DateTimeOffset EventTimestamp { get; init; }

        public AnalyticsEventDocument(AnalyticsEventRequest request)
        {
            OrderId = request.OrderId;
            Status = request.Status;
            Email = request.Email;
            SiteId = request.SiteId;
            Address = request.Address;
            Items = request.Items;
            Tags = request.Tags;
            EventTimestamp = request.EventTimestamp;
        }
    }
}
