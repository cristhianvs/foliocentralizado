using FolioMonitor.API.Services;
using FolioMonitor.Core.Interfaces;
using FolioMonitor.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System;
using MimeKit;
using System.Linq;

namespace FolioMonitor.Tests.API.Services;

// NOTE: These tests only verify the MimeMessage construction.
// Testing actual email sending would require a mock SMTP server or integration setup.
public class EmailNotificationServiceTests
{
    private readonly Mock<ILogger<EmailNotificationService>> _mockLogger;
    private Mock<IConfiguration> _mockConfiguration;
    private Mock<IConfigurationSection> _mockSmtpSection;

    public EmailNotificationServiceTests()
    {
        _mockLogger = new Mock<ILogger<EmailNotificationService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockSmtpSection = new Mock<IConfigurationSection>();

        // Setup default mock configuration
        _mockConfiguration.Setup(c => c.GetSection("SmtpSettings")).Returns(_mockSmtpSection.Object);
        SetupSmtpConfig(
            host: "smtp.test.com", 
            port: "587", 
            useSsl: "true", 
            username: "test@test.com", 
            password: "password", 
            senderEmail: "noreply@test.com", 
            senderName: "Test Sender", 
            recipientEmails: "recipient1@test.com;recipient2@test.com"
        );
    }

    // Helper to mock specific config values using the indexer
    private void SetupSmtpConfig(string? host, string? port, string? useSsl, string? username, 
                                 string? password, string? senderEmail, string? senderName, string? recipientEmails)
    {
        _mockSmtpSection.SetupGet(s => s["Host"]).Returns(host);
        _mockSmtpSection.SetupGet(s => s["Port"]).Returns(port); // Return as string, service will parse
        _mockSmtpSection.SetupGet(s => s["UseSsl"]).Returns(useSsl); // Return as string, service will parse
        _mockSmtpSection.SetupGet(s => s["Username"]).Returns(username);
        _mockSmtpSection.SetupGet(s => s["Password"]).Returns(password);
        _mockSmtpSection.SetupGet(s => s["SenderEmail"]).Returns(senderEmail);
        _mockSmtpSection.SetupGet(s => s["SenderName"]).Returns(senderName);
        _mockSmtpSection.SetupGet(s => s["RecipientEmails"]).Returns(recipientEmails);
    }
    
    // We cannot easily mock SmtpClient SendAsync, so we focus on message creation
    // and verify the service attempts to proceed when config is valid.
    [Fact]
    public async Task SendAlertAsync_ShouldAttemptToSend_WhenConfigIsValid()
    {
        // Arrange
        var service = new EmailNotificationService(_mockConfiguration.Object, _mockLogger.Object);
        var alertInfo = new AlertInfo { Level = AlertLevel.Warning, Message = "Test Warning" };
        
        // Act
        // Expect ConnectAsync or AuthenticateAsync to fail later, but check logs before that
        try { await service.SendAlertAsync(alertInfo); } catch { /* Ignore expected exceptions from SmtpClient */ }

        // Assert
        // Verify it logged trying to connect, indicating config was read okay.
         _mockLogger.VerifyLogContains(LogLevel.Information, Times.AtLeastOnce(), "Connecting to SMTP host");
    }

    [Fact]
    public async Task SendAlertAsync_ShouldLogErrorAndNotSend_WhenConfigIsMissing()
    {
        // Arrange
        // Invalidate some config
        SetupSmtpConfig(null, "587", "true", "user", "pass", "sender", "Sender", "recip"); 
        var service = new EmailNotificationService(_mockConfiguration.Object, _mockLogger.Object);
        var alertInfo = new AlertInfo { Level = AlertLevel.Critical, Message = "Test Critical" };

        // Act
        await service.SendAlertAsync(alertInfo);

        // Assert
        _mockLogger.VerifyLogContains(LogLevel.Error, Times.Once(), "SMTP settings are not fully configured");
    }
    
     [Fact]
    public async Task SendAlertAsync_ShouldLogErrorAndNotSend_WhenRecipientsMissing()
    {
        // Arrange
        SetupSmtpConfig("host", "587", "true", "user", "pass", "sender", "Sender", ""); // Empty recipients 
        var service = new EmailNotificationService(_mockConfiguration.Object, _mockLogger.Object);
        var alertInfo = new AlertInfo { Level = AlertLevel.Critical, Message = "Test Critical" };

        // Act
        await service.SendAlertAsync(alertInfo);

        // Assert
        // The initial check for whitespace catches this before the specific recipient check
        _mockLogger.VerifyLogContains(LogLevel.Error, Times.Once(), "SMTP settings are not fully configured");
    }
} 