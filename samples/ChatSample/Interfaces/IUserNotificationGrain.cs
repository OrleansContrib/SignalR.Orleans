using System.Threading.Tasks;
using Orleans;

namespace Interfaces
{
    public interface IUserNotificationGrain : IGrainWithStringKey
    {
        Task SendMessageAsync(string name, string message);
    }
}
