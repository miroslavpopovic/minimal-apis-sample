using System.ComponentModel.DataAnnotations;

namespace MinimalApis.MinimalSample.Domain;

public class Project
{
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Client? Client { get; set; }
}
