using DummyApp.Analytics.Functions.Models;

namespace DummyApp.Analytics.Functions.Services;

public interface ICosmosAnalyticsRepository
{
    Task SaveAsync(AnalyticsEventRequest analyticsEvent, CancellationToken cancellationToken);
}
