using System.ComponentModel.DataAnnotations;
using MinimalApis.MinimalSample.Refactored.Features.Clients;

namespace MinimalApis.MinimalSample.Refactored.Features.Projects;

public class Project
{
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Client? Client { get; set; }
}
