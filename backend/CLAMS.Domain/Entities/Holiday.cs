using CLAMS.Domain.Enums;

namespace CLAMS.Domain.Entities;

public class Holiday
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly HolidayDate { get; set; }

    public HolidayCategory Category { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
