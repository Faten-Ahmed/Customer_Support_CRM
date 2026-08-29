using CRM.Domain.Sla;

namespace CRM.Application.Sla;

public static class BusinessTimeCalculator
{
    public static DateTime AddBusinessMinutes(DateTime startUtc, int minutes, BusinessHours hours)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(hours.TimeZone);
        var current = TimeZoneInfo.ConvertTimeFromUtc(startUtc, tz);
        var remaining = minutes;

        while (remaining > 0)
        {
            if (IsBusinessTime(current, hours))
                remaining--;
            current = current.AddMinutes(1);
        }

        // Advance to next business minute if we ended outside business hours
        while (!IsBusinessTime(current, hours))
            current = current.AddMinutes(1);

        return TimeZoneInfo.ConvertTimeToUtc(current, tz);
    }

    public static int ElapsedBusinessMinutes(DateTime startUtc, DateTime endUtc, BusinessHours hours)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(hours.TimeZone);
        var current = TimeZoneInfo.ConvertTimeFromUtc(startUtc, tz);
        var end = TimeZoneInfo.ConvertTimeFromUtc(endUtc, tz);
        var elapsed = 0;

        while (current < end)
        {
            if (IsBusinessTime(current, hours))
                elapsed++;
            current = current.AddMinutes(1);
        }

        return elapsed;
    }

    private static bool IsBusinessTime(DateTime localDt, BusinessHours hours)
    {
        if (!hours.WorkDays.Contains(localDt.DayOfWeek.ToString())) return false;
        var date = DateOnly.FromDateTime(localDt);
        if (hours.Holidays.Any(h => h.Date == date)) return false;
        var time = TimeOnly.FromDateTime(localDt);
        return time >= hours.StartTime && time < hours.EndTime;
    }
}
