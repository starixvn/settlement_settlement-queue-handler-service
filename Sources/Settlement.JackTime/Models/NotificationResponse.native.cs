namespace Settlement.JackTime.Models;
public record NotificationResponse(Notification Data, bool IsSuccessful, string ErrMsg);

public record Notification(int OffSet, IEnumerable<NotificationDetail> Notifications);