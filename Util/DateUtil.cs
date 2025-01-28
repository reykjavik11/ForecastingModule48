using System;

namespace ForecastingModule.Util
{
    public sealed class DateUtil
    {
        private DateUtil() { }

        public static DateTime toForecastDay(DateTime operationDate)
        {
            //Find the first day of the operation month
            DateTime firstDayOfCurrentMonth = new DateTime(operationDate.Year, operationDate.Month, 1);

            // Get the last day of the previous month
            DateTime lastDayOfPreviousMonth = firstDayOfCurrentMonth.AddDays(-1);

            // Calculate the last Monday of the previous month
            int daysSinceMonday = (int)lastDayOfPreviousMonth.DayOfWeek - (int)DayOfWeek.Monday;

            // Adjust if the day is before Monday
            if (daysSinceMonday < 0)
            {
                daysSinceMonday += 7;
            }

            DateTime lastMondayOfPreviousMonth = lastDayOfPreviousMonth.AddDays(-daysSinceMonday);
            return lastMondayOfPreviousMonth;
        }

        public static DateTime toOperationPlanningDay(DateTime forecastDate)
        {
            forecastDate = forecastDate.AddMonths(1);

            forecastDate = new DateTime(forecastDate.Year, forecastDate.Month, 1);

            return forecastDate;
        }
    }
}
