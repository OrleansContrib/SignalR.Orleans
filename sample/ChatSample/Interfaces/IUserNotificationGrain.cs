using Orleans;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IUserNotificationGrain : IGrainWithStringKey
    {
        Task SendMessageAsync(string name, string message);
    }
}
