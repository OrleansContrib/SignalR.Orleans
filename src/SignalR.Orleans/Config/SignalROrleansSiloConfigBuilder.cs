namespace SignalR.Orleans;

public class SignalROrleansSiloConfigBuilder : SignalROrleansConfigBaseBuilder
{
    internal Action<ISiloBuilder> ConfigureBuilder { get; set; } = default!;

    /// <summary>
    /// Configure builder, such as providers.
    /// </summary>
    /// <param name="configure">Configure action. This may be called multiple times.</param>
    public SignalROrleansSiloConfigBuilder Configure(Action<ISiloBuilder> configure)
    {
        ConfigureBuilder += configure;
        return this;
    }
}
