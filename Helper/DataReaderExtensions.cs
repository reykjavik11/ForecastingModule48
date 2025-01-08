using System.Data;
using System;

namespace ForecastingModule.Helper
{
    public static class DataReaderExtensions
    {
        public static Nullable<T> GetNullableValue<T>(this IDataReader reader, string columnName) where T : struct
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? (Nullable<T>)null : (T)reader.GetValue(ordinal);
        }
    }
}
