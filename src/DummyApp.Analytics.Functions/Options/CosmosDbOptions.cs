namespace DummyApp.Analytics.Functions.Options;

public sealed record CosmosDbOptions
{
    public const string SectionName = "CosmosDb";

    public string? ConnectionString { get; init; }
    public string AnalyticsDatabaseName { get; init; } = "Analytics";
    public string EventsContainerName { get; init; } = "Events";
}
