namespace Settlement.JackTime.Models;
public record NotificationResponse(Notification Data, bool IsSuccessful, string ErrMsg);

public record Notification(int offSet, IEnumerable<NotificationDetail> Notifications);