using System.Net;
using System.Text.Json;
using DummyApp.Analytics.Functions.Models;
using DummyApp.Analytics.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DummyApp.Analytics.Functions;

public sealed class AnalyticsQueryFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ICosmosAnalyticsRepository _repository;
    private readonly ILogger<AnalyticsQueryFunction> _logger;

    public AnalyticsQueryFunction(ICosmosAnalyticsRepository repository, ILogger<AnalyticsQueryFunction> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [Function("AnalyticsQueryFunction")]
    public async Task<HttpResponseData> Run(
#if DEBUG
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "analytics")]
#else
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "analytics")]
#endif
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Analytics query received. Url: {Url}", req.Url);

        var queryString = req.Url.Query?.TrimStart('?') ?? string.Empty;
        var queryParameters = queryString.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split(new[] { '=' }, 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => Uri.UnescapeDataString(parts[0]), parts => Uri.UnescapeDataString(parts[1]), StringComparer.OrdinalIgnoreCase);

        if (!queryParameters.TryGetValue("periodDays", out var periodDaysValue) || !int.TryParse(periodDaysValue, out var periodDays) || periodDays <= 0)
        {
            return CreateBadRequest(req, "periodDays must be a positive integer.");
        }

        try
        {
            var analytics = await _repository.GetAnalyticsAsync(periodDays, cancellationToken);
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(analytics, JsonOptions), cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve analytics data for periodDays {PeriodDays}.", periodDays);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Failed to retrieve analytics data.", cancellationToken);
            return response;
        }
    }

    private static HttpResponseData CreateBadRequest(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.WriteString(message);
        return response;
    }
}
