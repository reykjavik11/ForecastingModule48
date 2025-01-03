using System;

namespace ForecastingModule.Util
{
    public sealed class DateUtil
    {
        private DateUtil() { }

        public static DateTime calculateForecastDay(DateTime operationDate)
        {
            if(operationDate == null)
            {
                throw new ArgumentException("DateUtil.calculateForecastDay - operationDate is null");
            }
            // Find the first day of the operation month
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
    }
}
