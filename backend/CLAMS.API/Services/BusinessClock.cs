namespace CLAMS.API.Services;

/// <summary>
/// Single source of truth for "today" in business rules.
/// CLAMS operates on Nairobi time (UTC+3), so date checks must not use UTC.
/// </summary>
public static class BusinessClock
{
    private static readonly TimeZoneInfo NairobiTimeZone = ResolveTimeZone();

    public static DateOnly Today =>
        DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                NairobiTimeZone));

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Nairobi");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. Africa Standard Time");
        }
    }
}
