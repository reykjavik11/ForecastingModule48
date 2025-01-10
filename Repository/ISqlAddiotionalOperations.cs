using ForecastingModule.Helper;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace ForecastingModule.Repository
{
    internal interface ISqlAddiotionalOperations
    {
        void insertCommands(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data, List<string> salesCodesKeys, SqlCommand insertCommand);
        string generateInsertQueries(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data, List<string> salesCodesKeys);
    }
}
