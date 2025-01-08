using System;
using System.Collections.Generic;
using System.Dynamic;

namespace ForecastingModule.Util
{
    public sealed class DataGridHelper
    {
        private DataGridHelper() { }

        public static List<DateTime> GenerateDateList(DateTime startDate, int nDays, int nMonths, bool operationPlanning = true)
        {
            List<DateTime> dates = new List<DateTime>();

            DateTime currentDate = startDate.AddDays(nDays);

            currentDate = new DateTime(currentDate.Year, currentDate.Month, 1);

            dates.Add(currentDate);
            for (int i = 1; i < nMonths; ++i)
            {
                currentDate = currentDate.AddMonths(1);
                dates.Add(currentDate);
            }

            return dates;
        }
    }
}
