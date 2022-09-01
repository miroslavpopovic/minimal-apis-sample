using System.ComponentModel.DataAnnotations;

namespace MinimalApis.MinimalSample.Refactored.Features.Clients;

public class Client
{
    public long Id { get; init; }

    [Required]
    public string Name { get; set; } = string.Empty;
}
