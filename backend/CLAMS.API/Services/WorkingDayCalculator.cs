
using CLAMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CLAMS.API.Services;

public class WorkingDayCalculator
{
    private readonly CLAMSHRDbContext _context;

    public WorkingDayCalculator(CLAMSHRDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns the actual working dates between two dates,
    /// including both the start and end dates.
    /// Weekends and active holidays are excluded.
    /// </summary>
    public async Task<IReadOnlyList<DateOnly>> GetWorkingDatesAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException(
                "The end date cannot be earlier than the start date.");
        }

        var holidays = await _context.Holidays
            .AsNoTracking()
            .Where(h =>
                h.IsActive &&
                h.HolidayDate >= startDate &&
                h.HolidayDate <= endDate)
            .Select(h => h.HolidayDate)
            .Distinct()
            .ToListAsync(cancellationToken);

        var holidayDates = holidays.ToHashSet();

        var workingDates = new List<DateOnly>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isWeekend =
                date.DayOfWeek == DayOfWeek.Saturday ||
                date.DayOfWeek == DayOfWeek.Sunday;

            var isHoliday = holidayDates.Contains(date);

            if (!isWeekend && !isHoliday)
            {
                workingDates.Add(date);
            }
        }

        return workingDates;
    }

    /// <summary>
    /// Counts working days inclusively between two dates.
    /// </summary>
    public async Task<int> CountWorkingDaysAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var workingDates = await GetWorkingDatesAsync(
            startDate,
            endDate,
            cancellationToken);

        return workingDates.Count;
    }
}
