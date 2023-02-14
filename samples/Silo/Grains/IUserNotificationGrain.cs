namespace Silo;

public interface IUserNotificationGrain : IGrainWithStringKey
{
    Task SendMessageAsync(string name, string message);
}