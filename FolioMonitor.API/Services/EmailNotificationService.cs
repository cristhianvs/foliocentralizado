using FolioMonitor.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using System.Threading.Tasks;

namespace FolioMonitor.API.Services;

public class EmailNotificationService : INotificationService
{
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly IConfiguration _configuration;

    public EmailNotificationService(ILogger<EmailNotificationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendAlertAsync(string subject, string message)
    {
        try
        {
            var smtpConfig = _configuration.GetSection("SmtpSettings");
            var host = smtpConfig["Host"];
            var port = int.Parse(smtpConfig["Port"] ?? "587");
            var useSsl = bool.Parse(smtpConfig["UseSsl"] ?? "true");
            var username = smtpConfig["Username"];
            var password = smtpConfig["Password"];
            var senderEmail = smtpConfig["SenderEmail"];
            var senderName = smtpConfig["SenderName"];
            var recipientEmails = smtpConfig["RecipientEmails"]?.Split(',');

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(senderEmail) || 
                recipientEmails == null || recipientEmails.Length == 0)
            {
                _logger.LogError("Missing SMTP configuration");
                return;
            }

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            foreach (var recipient in recipientEmails)
            {
                email.To.Add(new MailboxAddress("", recipient.Trim()));
            }
            email.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = message
            };
            email.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, useSsl);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(email);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Alert email sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending alert email");
        }
    }
} 