using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace LaBot.Api.Services;

public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task TriggerN8nWebhookAsync(object payload);
}

public class NotificationService : INotificationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public NotificationService(IConfiguration config, ILogger<NotificationService> logger, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var smtpHost = _config["Smtp:Host"];
            var smtpPortStr = _config["Smtp:Port"];
            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];
            var fromEmail = _config["Smtp:FromEmail"] ?? "noreply@labotkripto.com";
            var fromName = _config["Smtp:FromName"] ?? "LaBot Kripto";

            if (string.IsNullOrWhiteSpace(smtpHost))
            {
                _logger.LogWarning("SMTP not configured, skipping email to {To}", to);
                return;
            }

            var port = int.TryParse(smtpPortStr, out var p) ? p : 587;

            using var client = new SmtpClient(smtpHost, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = body.TrimStart().StartsWith("<")
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
        }
    }

    public async Task TriggerN8nWebhookAsync(object payload)
    {
        try
        {
            var webhookUrl = _config["N8n:WebhookUrl"];
            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                _logger.LogDebug("N8N webhook URL not configured, skipping webhook");
                return;
            }

            var client = _httpClientFactory.CreateClient("n8n");
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(webhookUrl, content);
            if (response.IsSuccessStatusCode)
                _logger.LogInformation("N8N webhook triggered successfully");
            else
                _logger.LogWarning("N8N webhook returned {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger N8N webhook");
        }
    }
}
