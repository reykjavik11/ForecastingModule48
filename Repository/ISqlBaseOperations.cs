using System.Collections.Generic;
using System.Data.SqlClient;
using ForecastingModule.Helper;

namespace ForecastingModule.Repository
{
    internal interface ISqlBaseOperations
    {
        int save(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data);
    }
}
