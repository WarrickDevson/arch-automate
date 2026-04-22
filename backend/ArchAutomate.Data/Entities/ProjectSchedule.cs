namespace ArchAutomate.Data.Entities;

/// <summary>
/// Stores the IFC-extracted door and window schedules for a project.
/// One row per project (upserted on each extraction).
/// JSON columns hold a serialised array of ScheduleItem records.
/// </summary>
public class ProjectSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

    /// <summary>JSON array of door schedule items (serialised ScheduleItemDto[]).</summary>
    public string DoorScheduleJson { get; set; } = "[]";

    /// <summary>JSON array of window schedule items (serialised ScheduleItemDto[]).</summary>
    public string WindowScheduleJson { get; set; } = "[]";

    public int DoorCount { get; set; }
    public int WindowCount { get; set; }

    // Navigation
    public Project Project { get; set; } = null!;
}
