using System.Collections.Generic;
using ForecastingModule.Helper;

namespace ForecastingModule.Repository
{
    internal interface OperationsPlanningRepository
    {
        /*
            select op.OPS_NbrDays, op.OPS_NbrDays, op.OPS_Comments
            FROM [WeilerForecasting].[dbo].[OperationsSettings] op
            where op.OPS_Tab ='{equipmentName}' and op.OPS_ActiveFlag=1
        */
        Dictionary<string, object> getOperationsSetting(string equipmentName);

        SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> retrieveExistedOperationsPlanning(string equipmentName);

        int saveOperationsPlanning(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data);
    }
}
