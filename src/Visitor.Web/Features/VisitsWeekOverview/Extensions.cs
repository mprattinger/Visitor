namespace Visitor.Web.Features.VisitsWeekOverview;

public static class Extensions
{

    public static (DateTime start, DateTime end) GetWeek(DateTime? startDateUTC = null)
    {
        var today = startDateUTC?.Date ?? DateTime.UtcNow.Date;
        var currentDayOfWeek = (int)today.DayOfWeek;
        if (currentDayOfWeek == 0) // Sunday
        {
            today.AddDays(1);
        }
        else if (currentDayOfWeek == 6) // Monday
        {
            today.AddDays(2);
        }

        var end = today.AddDays(8);

        return (today, end);
    }
}
