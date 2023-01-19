namespace SignalR.Orleans.Core
{
    public class HubContext<THub>
    {
        private readonly IGrainFactory _grainFactory;
        private readonly string _hubName;

        public HubContext(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
            var hubType = typeof(THub);
            _hubName = hubType.IsInterface && hubType.Name.StartsWith("I")
                ? hubType.Name[1..]
                : hubType.Name;
        }

        /// <summary>
        /// Gets an <see cref="IHubMessageInvoker"/> that allows you to invoke methods on a single connection.
        /// </summary>
        /// <param name="connectionId">The id of the connection that the method will be invoked on.</param>
        public IHubMessageInvoker Client(string connectionId) => _grainFactory.GetClientGrain(_hubName, connectionId);

        /// <summary>
        /// Gets an <see cref="IHubGroupMessageInvoker"/> that allows you to invoke methods on a group of connections in the named group.
        /// </summary>
        /// <param name="groupName">The name of the group of connections that the method will be invoked on.</param>
        public IHubGroupMessageInvoker Group(string groupName) => _grainFactory.GetGroupGrain(_hubName, groupName);

        /// <summary>
        /// Gets an <see cref="IHubGroupMessageInvoker"/> that allows you to invoke methods on a group of connections belonging to the given authenticated user.
        /// </summary>
        /// <param name="userId">The id of the authenticated user whose connections the method will be invoked on.</param>
        public IHubGroupMessageInvoker User(string userId) => _grainFactory.GetUserGrain(_hubName, userId);
    }
}