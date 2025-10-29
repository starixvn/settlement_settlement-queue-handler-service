namespace Settlement.JackTime.Models;
public record NotificationResponse(Data Data, bool IsSuccessful, string ErrMsg);

public record Data(int offSet, IEnumerable<Notification> Notifications);