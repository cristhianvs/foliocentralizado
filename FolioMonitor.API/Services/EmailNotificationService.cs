using FolioMonitor.Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FolioMonitor.API.Services;

public class EmailNotificationService : INotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(IConfiguration configuration, ILogger<EmailNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAlertAsync(AlertInfo alertInfo)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        
        // Read config using indexer and parse manually
        var host = smtpSettings["Host"];
        _ = int.TryParse(smtpSettings["Port"], out var port);
        _ = bool.TryParse(smtpSettings["UseSsl"], out var useSsl);
        var username = smtpSettings["Username"];
        var password = smtpSettings["Password"]; // Consider Key Vault!
        var senderEmail = smtpSettings["SenderEmail"];
        var senderName = smtpSettings["SenderName"];
        var recipientEmailsRaw = smtpSettings["RecipientEmails"];

        if (string.IsNullOrWhiteSpace(host) || port == 0 || string.IsNullOrWhiteSpace(username) || 
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(senderEmail) || 
            string.IsNullOrWhiteSpace(recipientEmailsRaw))
        {
            _logger.LogError("SMTP settings are not fully configured. Cannot send email alert.");
            return;
        }

        var recipientEmails = recipientEmailsRaw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!recipientEmails.Any())
        {
             _logger.LogError("No valid recipient emails configured in SmtpSettings:RecipientEmails.");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName ?? senderEmail, senderEmail));
            foreach (var recipient in recipientEmails)
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }

            message.Subject = $"Folio Alert: {alertInfo.Level} - {alertInfo.Module}";
            message.Body = new TextPart(TextFormat.Html) // Or TextFormat.Plain
            {
                Text = $"<html><body>"
                     + $"<h1>Folio Monitoring Alert</h1>"
                     + $"<p><strong>Level:</strong> {alertInfo.Level}</p>"
                     + $"<p><strong>Module:</strong> {alertInfo.Module}</p>"
                     + $"<p><strong>Series:</strong> {alertInfo.FolioInicio} - {alertInfo.FolioFin}</p>"
                     + $"<p><strong>Folio Actual:</strong> {alertInfo.FolioActual?.ToString() ?? "N/A"}</p>"
                     + $"<p style=\"color: {(alertInfo.Level == AlertLevel.Critical ? "red" : "orange")};\">"
                     + $"<strong>Available Folios:</strong> {alertInfo.FoliosDisponibles} (Threshold: {alertInfo.Threshold})</p>"
                     + $"<p>Alert Message: {alertInfo.Message}</p>"
                     + $"<p>Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>"
                     + "</body></html>"
            };

            using var client = new SmtpClient();
            // Use SecureSocketOptions based on UseSsl and Port common practices
            var secureSocketOptions = useSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.Auto;
            if (port == 465) 
            {
                // Port 465 usually implies SslOnConnect
                secureSocketOptions = SecureSocketOptions.SslOnConnect;
                useSsl = true; // Ensure SSL is used if port is 465
            } 
            
            _logger.LogInformation("Connecting to SMTP host {Host} on port {Port} using SSL: {UseSsl}", host, port, useSsl);
            await client.ConnectAsync(host, port, secureSocketOptions); 

            _logger.LogInformation("Authenticating SMTP user {Username}", username);
            await client.AuthenticateAsync(username, password);

            _logger.LogInformation("Sending email alert to {Recipients}", string.Join(", ", recipientEmails));
            await client.SendAsync(message);
            _logger.LogInformation("Email alert sent successfully.");

            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email alert.");
            // Depending on requirements, might need retry logic or alternative notifications
        }
    }
} 