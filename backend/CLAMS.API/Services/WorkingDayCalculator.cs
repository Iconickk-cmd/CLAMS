
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

    /// <summary>
    /// For each date, returns the next working day after it
    /// (skipping weekends and active holidays). Used to show the
    /// return-to-work date for a leave request.
    /// </summary>
    public async Task<Dictionary<DateOnly, DateOnly>> GetNextWorkingDatesAsync(
        IEnumerable<DateOnly> afterDates,
        CancellationToken cancellationToken = default)
    {
        var dates = afterDates.Distinct().ToList();
        var result = new Dictionary<DateOnly, DateOnly>();

        if (dates.Count == 0)
            return result;

        var from = dates.Min().AddDays(1);
        var to = dates.Max().AddDays(60);

        var holidayDates = (await _context.Holidays
            .AsNoTracking()
            .Where(h =>
                h.IsActive &&
                h.HolidayDate >= from &&
                h.HolidayDate <= to)
            .Select(h => h.HolidayDate)
            .Distinct()
            .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var after in dates)
        {
            var next = after.AddDays(1);

            for (var i = 0; i < 60; i++)
            {
                var isWeekend =
                    next.DayOfWeek == DayOfWeek.Saturday ||
                    next.DayOfWeek == DayOfWeek.Sunday;

                if (!isWeekend && !holidayDates.Contains(next))
                    break;

                next = next.AddDays(1);
            }

            result[after] = next;
        }

        return result;
    }
}
