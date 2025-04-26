using Microsoft.Extensions.Logging;
using Moq;
using System;

namespace FolioMonitor.Tests.Helpers;

// Helper extension for verifying logs (optional but useful)
public static class LoggerVerifyExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> mockLogger, LogLevel level, Times times, string message)
    {
        mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            times);
    }
     public static void VerifyLogContains<T>(this Mock<ILogger<T>> mockLogger, LogLevel level, Times times, string messageSubstring)
    {
        mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messageSubstring)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            times);
    }
} 