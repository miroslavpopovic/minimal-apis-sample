namespace MinimalApis.MinimalSample.Refactored.Features.Clients;

/// <summary>
/// Represents a single client to add or modify.
/// </summary>
public class ClientInputModel
{
    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Maps the current model into a <see cref="Client"/> instance.
    /// </summary>
    /// <param name="client">A client instance to modify.</param>
    public void MapTo(Client client)
    {
        client.Name = Name;
    }
}
