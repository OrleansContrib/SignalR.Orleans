namespace SignalR.Orleans.ConnectionGroups;

internal readonly record struct ConnectionGroupKey
{
    public required ConnectionGroupType GroupType { get; init; }
    public required string HubType { get; init; }
    public required string GroupId { get; init; }

    public string ToPrimaryGrainKey() => $"{GroupType}:{HubType}:{GroupId}";

    public static ConnectionGroupKey FromPrimaryGrainKey(string primaryGrainKey)
    {
        var parts = primaryGrainKey.Split(':', 3);
        return new()
        {
            GroupType = Enum.Parse<ConnectionGroupType>(parts[0]),
            HubType = parts[1],
            GroupId = parts[2],
        };
    }
}
