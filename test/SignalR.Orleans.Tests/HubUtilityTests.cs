using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace SignalR.Orleans.Tests
{
    public class HubUtilityTests
    {
        [Fact]
        public void GivenAWeaklyTypedHub_WhenGetHubName_ThenReturnsHubName()
        {
            var result = HubUtility.GetHubName<MyWeaklyTypedHub>();

            Assert.Equal("MyWeaklyTypedHub", result);
        }

        [Fact]
        public void GivenAStronglyTypedHub_WhenGetHubName_ThenReturnsHubName()
        {
            var result = HubUtility.GetHubName<MyStronglyTypedHub>();

            Assert.Equal("MyStronglyTypedHub", result);
        }

        private class MyWeaklyTypedHub : Hub
        {
        }

        private interface IHubClient
        {
        }

        private class MyStronglyTypedHub : Hub<IHubClient>
        {
        }
    }
}
