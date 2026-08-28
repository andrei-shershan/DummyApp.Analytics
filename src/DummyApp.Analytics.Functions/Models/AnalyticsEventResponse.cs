namespace DummyApp.Analytics.Functions.Models;

public sealed record AnalyticsEventResponse(
    string Id,
    Guid OrderId,
    string Status,
    string Email,
    string SiteId,
    AnalyticsOrderAddress? Address,
    IEnumerable<AnalyticsOrderItem> Items,
    IEnumerable<string> Tags,
    DateTimeOffset EventTimestamp);
