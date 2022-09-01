using System.ComponentModel.DataAnnotations;
using MinimalApis.MinimalSample.Refactored.Features.Projects;
using MinimalApis.MinimalSample.Refactored.Features.Users;

namespace MinimalApis.MinimalSample.Refactored.Features.TimeEntries;

public class TimeEntry
{
    public long Id { get; set; }

    [Required]
    public User User { get; set; } = new();

    [Required]
    public Project Project { get; set; } = new();

    public DateTime EntryDate { get; set; }

    public int Hours { get; set; }

    public decimal HourRate { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;
}
