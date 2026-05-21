using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.API.Contracts.Notifications;
using Notifications.API.Entities.Enums;
using Notifications.API.Persistence;
using Notifications.API.Services.Notifications;

namespace Notifications.API.Consumers;

public class NotificationConsumer(
    AppDbContext db,
    ILogger<NotificationConsumer> logger,
    INotificationSender sender)
    : IConsumer<SendNotificationMessage>
{
    private const int MaxRetryCount = 5;

    public async Task Consume(ConsumeContext<SendNotificationMessage> context)
    {
        var recipients = await db.MessageRecipients
            .Include(recipient => recipient.Message)
            .Where(recipient => context.Message.RecipientIds.Contains(recipient.Id))
            .ToListAsync(context.CancellationToken);

        var statuses = await db.DeliveryStatuses
            .Where(status =>
                status.Code == DeliveryStatusCode.Delivered ||
                status.Code == DeliveryStatusCode.Failed ||
                status.Code == DeliveryStatusCode.Queued)
            .ToDictionaryAsync(
                status => status.Code,
                status => status.Id,
                context.CancellationToken);
        
        if (!statuses.TryGetValue(DeliveryStatusCode.Delivered, out var deliveredStatusId))
            throw new InvalidOperationException("Статус Delivered не найден");

        if (!statuses.TryGetValue(DeliveryStatusCode.Failed, out var failedStatusId))
            throw new InvalidOperationException("Статус Failed не найден");

        if (!statuses.TryGetValue(DeliveryStatusCode.Queued, out var queuedStatusId))
            throw new InvalidOperationException("Статус Queued не найден");
        
        foreach (var recipient in recipients)
        {
            try
            {
                await sender.SendAsync(
                    recipient,
                    recipient.Message.Subject,
                    recipient.Message.MessageBody,
                    context.CancellationToken);

                recipient.DeliveryStatusId = deliveredStatusId;
            }
            catch (Exception ex)
            {
                recipient.RetryCount++;

                if (recipient.RetryCount >= MaxRetryCount)
                {
                    recipient.DeliveryStatusId = failedStatusId;
                }
                else
                {
                    recipient.DeliveryStatusId = queuedStatusId;

                    recipient.NextRetry = DateTime.UtcNow.AddMinutes(
                        recipient.RetryCount * 5);
                }

                logger.LogError(
                    ex,
                    "Ошибка отправки уведомления {Recipient}",
                    recipient.ContactData);
            }

            recipient.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }
}