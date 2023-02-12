namespace SignalR.Orleans.ConnectionGroups;

internal enum ConnectionGroupType
{
    /// <summary>
    /// All the connections made by a single authenticated user.
    /// </summary>
    AuthenticatedUser,

    /// <summary>
    /// All the connections that have been added to a named group.
    /// </summary>
    NamedGroup,
}

