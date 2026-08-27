using System.Net;
using System.Text.Json;
using DummyApp.Analytics.Functions.Models;
using DummyApp.Analytics.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DummyApp.Analytics.Functions;

public sealed class AnalyticsFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ICosmosAnalyticsRepository _repository;
    private readonly ILogger<AnalyticsFunction> _logger;

    public AnalyticsFunction(ICosmosAnalyticsRepository repository, ILogger<AnalyticsFunction> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [Function("AnalyticsFunction")]
    public async Task<HttpResponseData> Run(
#if DEBUG
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "analytics/event")]
#else
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "analytics/event")]
#endif
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Analytics event received. Method: {Method}, Url: {Url}", req.Method, req.Url);

        var body = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            _logger.LogWarning("Analytics request body is empty.");
            return CreateBadRequest(req, "Request body is required.");
        }

        AnalyticsEventRequest? analyticsEvent;
        try
        {
            analyticsEvent = JsonSerializer.Deserialize<AnalyticsEventRequest>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in analytics request.");
            return CreateBadRequest(req, "Invalid JSON in request body.");
        }

        if (analyticsEvent is null)
        {
            _logger.LogWarning("Analytics request deserialized to null.");
            return CreateBadRequest(req, "Request body is required.");
        }

        if (analyticsEvent.OrderId == Guid.Empty)
            return CreateBadRequest(req, "OrderId is required.");

        if (string.IsNullOrWhiteSpace(analyticsEvent.Status))
            return CreateBadRequest(req, "Status is required.");

        if (string.IsNullOrWhiteSpace(analyticsEvent.Email))
            return CreateBadRequest(req, "Email is required.");

        try
        {
            await _repository.SaveAsync(analyticsEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save analytics event for order {OrderId}.", analyticsEvent.OrderId);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Failed to save analytics event.", cancellationToken);
            return response;
        }

        var successResponse = req.CreateResponse(HttpStatusCode.OK);
        await successResponse.WriteAsJsonAsync(new { message = "Analytics event accepted." }, cancellationToken);
        return successResponse;
    }

    private static HttpResponseData CreateBadRequest(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.WriteString(message);
        return response;
    }
}
