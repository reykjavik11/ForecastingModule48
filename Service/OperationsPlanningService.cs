using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ForecastingModule.Helper;

namespace ForecastingModule.Service
{
    internal interface OperationsPlanningService
    {
        Dictionary<string, object> getOperationsSetting(string equipmentName);

        /// <summary>
        /// Generate SyncLinkedDictionary with already calculated TOTAL
        /// </summary>
        /// <param name="equipmentName"></param>
        /// <returns></returns>
        SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> retrieveExistedOperationsPlanning(string equipmentName);
        SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> retrieveOperationsByModel(string model);
        int save(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data);

    }
}
