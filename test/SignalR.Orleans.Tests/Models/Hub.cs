using Microsoft.AspNetCore.SignalR;

namespace SignalR.Orleans.Tests.Models
{
    public class MyHub : Hub
    {

    }

    public class DifferentHub : Hub
    {

    }

    // matching interface naming
    public class DaHub : Hub<IDaHub>
    {
    }

    public interface IDaHub { }

    // non matching interface and class
    public class DaHubx : Hub<IDaHub>
    {
    }

    // using base
    public class DaHubUsingBase : DaGenericHubBase<IDaHub>
    {
    }

    public class DaGenericHubBase<THub> : Hub<THub> where THub : class
    {
    }
}
