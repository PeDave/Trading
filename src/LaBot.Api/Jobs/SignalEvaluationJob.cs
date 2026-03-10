using LaBot.Api.Hubs;
using LaBot.Api.Models.Domain;
using LaBot.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace LaBot.Api.Jobs;

public class SignalEvaluationJob
{
    private readonly ISignalGeneratorService _signalService;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<SignalHub> _hubContext;
    private readonly ILogger<SignalEvaluationJob> _logger;

    public SignalEvaluationJob(
        ISignalGeneratorService signalService,
        INotificationService notificationService,
        IHubContext<SignalHub> hubContext,
        ILogger<SignalEvaluationJob> logger)
    {
        _signalService = signalService;
        _notificationService = notificationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogDebug("SignalEvaluationJob executing...");

        try
        {
            var updatedSignals = await _signalService.EvaluateSignalsAsync();

            foreach (var signal in updatedSignals)
            {
                // Broadcast to appropriate tier group via SignalR
                var groupName = SignalHub.GetTierGroupName(signal.MinTier);
                var update = new
                {
                    signalId = signal.Id,
                    symbol = signal.Symbol,
                    direction = signal.Direction.ToString(),
                    status = signal.Status.ToString(),
                    entryPrice = signal.EntryPrice,
                    currentStatus = signal.Status.ToString(),
                    updatedAt = signal.UpdatedAt
                };

                await _hubContext.Clients.Group(groupName).SendAsync("SignalUpdate", update);

                // Admin group always gets updates
                await _hubContext.Clients.Group("admin").SendAsync("SignalUpdate", update);

                // Send notification for significant events
                if (signal.Status is SignalStatus.TP1Hit or SignalStatus.TP2Hit
                    or SignalStatus.TP3Hit or SignalStatus.StopLoss)
                {
                    var emoji = signal.Status switch
                    {
                        SignalStatus.TP1Hit => "✅",
                        SignalStatus.TP2Hit => "🎯",
                        SignalStatus.TP3Hit => "🚀",
                        SignalStatus.StopLoss => "🛑",
                        _ => "📊"
                    };

                    var webhookPayload = new
                    {
                        event_type = "signal_update",
                        signal_id = signal.Id,
                        symbol = signal.Symbol,
                        direction = signal.Direction.ToString(),
                        status = signal.Status.ToString(),
                        entry_price = signal.EntryPrice,
                        timeframe = signal.Timeframe,
                        emoji,
                        timestamp = DateTime.UtcNow
                    };

                    await _notificationService.TriggerN8nWebhookAsync(webhookPayload);
                    _logger.LogInformation("Signal {Id} {Symbol} status: {Status} {Emoji}",
                        signal.Id, signal.Symbol, signal.Status, emoji);
                }
            }

            if (updatedSignals.Count > 0)
                _logger.LogInformation("SignalEvaluationJob processed {Count} signal updates", updatedSignals.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SignalEvaluationJob");
        }
    }
}
