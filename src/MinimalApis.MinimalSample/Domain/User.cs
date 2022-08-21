using System.ComponentModel.DataAnnotations;

namespace MinimalApis.MinimalSample.Domain;

public class User
{
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public decimal HourRate { get; set; }
}
