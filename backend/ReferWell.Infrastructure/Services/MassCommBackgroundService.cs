using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReferWell.Domain.Entities;
using ReferWell.Infrastructure.Data;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;

namespace ReferWell.Infrastructure.Services;

public record MassCommJob(Guid CampaignId, List<MassCommMessage> Messages);

public class MassCommChannel
{
    private readonly Channel<MassCommJob> _channel = Channel.CreateUnbounded<MassCommJob>();
    public ChannelWriter<MassCommJob> Writer => _channel.Writer;
    public ChannelReader<MassCommJob> Reader => _channel.Reader;
}

public class MassCommBackgroundService : BackgroundService
{
    private readonly MassCommChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MassCommBackgroundService> _logger;

    // Throttle: 2 messages per second
    private static readonly TimeSpan ThrottleDelay = TimeSpan.FromMilliseconds(500);

    public MassCommBackgroundService(
        MassCommChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<MassCommBackgroundService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            foreach (var message in job.Messages)
            {
                if (stoppingToken.IsCancellationRequested) break;
                try
                {
                    // Simulate sending (replace with real SMTP/Email service)
                    await Task.Delay(100, stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var msg = await db.MassCommMessages.FindAsync(new object[] { message.Id }, stoppingToken);
                    if (msg != null)
                    {
                        msg.Status = "Sent";
                        msg.SentAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    _logger.LogInformation("Message sent to {Email}", message.RecipientEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send message to {Email}", message.RecipientEmail);
                }

                await Task.Delay(ThrottleDelay, stoppingToken);
            }
        }
    }
}
