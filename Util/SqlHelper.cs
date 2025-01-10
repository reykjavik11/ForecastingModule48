using System.Collections.Generic;
using System.Text;
using ForecastingModule.Helper;
using ForecastingModule.Repository.Impl;

namespace ForecastingModule.Util
{
    internal static class SqlHelper
    {
        public static StringBuilder generateDeleteQuery(List<string> opModelList, string table, string column)
        {
            StringBuilder deleteQuery = new StringBuilder($"DELETE FROM [dbo].[{table}] WHERE [{column}] in (");
            for (int i = 0; i < opModelList.Count; i++)
            {
                deleteQuery.Append($"@DeleteId{i}");
                if (i < opModelList.Count - 1)
                {
                    deleteQuery.Append(", ");
                }
            }
            deleteQuery.Append(")");
            return deleteQuery;
        }

        public static HashSet<string> getModelSet(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data, List<string> salesCodesKeys)
        {
            HashSet<string> opModelSet = new HashSet<string>();

            foreach (string saleCode in salesCodesKeys)
            {
                SyncLinkedDictionary<object, object> valuesDictionaryBySaleCode = data.Get(saleCode);
                if (valuesDictionaryBySaleCode != null)
                {
                    string scModel = (string)valuesDictionaryBySaleCode.Get(OperationsPlanningRepositoryImpl.SC_MODEL);
                    if (scModel != null)
                    {
                        opModelSet.Add(scModel);
                    }
                }
            }

            return opModelSet;
        }
    }
}
