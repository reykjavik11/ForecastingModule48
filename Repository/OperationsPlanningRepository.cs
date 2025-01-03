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

        /*
         * select top 1 op.OP_RecordID
            FROM [WeilerForecasting].[dbo].[OperationsPlanning] op
	        join [WeilerForecasting].[dbo].[SalesCodes] sc on op.OP_Model = sc.SC_Model and sc.SC_BaseFlag=1
	        where sc.SC_OperationsTab='{equipmentName}' and op.OP_Date between DATEFROMPARTS(YEAR(DATEADD(day, {plusNumDays}, GETDATE())), MONTH(DATEADD(day, {plusNumDays}, GETDATE())), 1) and  EOMONTH(DATEADD(day, {plusNumDays}, GETDATE()))
        */
        //bool anyForecastExistInPeriod(string equipmentName);//Probably, redundant

        /*
            select sc.SC_SalesCode, op.OP_Date, op.OP_Model, op.OP_Quantity, sc.SC_Model
	   from (select SC_SalesCode, SC_Model, SC_OperationsTab from [WeilerForecasting].[dbo].[SalesCodes] where SC_BaseFlag=1) sc
    left join [WeilerForecasting].[dbo].[OperationsPlanning] op on op.OP_Model = sc.SC_Model 
	left join [WeilerForecasting].[dbo].[OperationsSettings] os on os.OPS_Tab = sc.SC_OperationsTab 
	where sc.SC_OperationsTab='{equipmentName}' and op.OP_Date between DATEFROMPARTS(YEAR(DATEADD(day, os.OPS_NbrDays, GETDATE())), MONTH(DATEADD(day, os.OPS_NbrDays, GETDATE())), 1) and EOMONTH(DATEADD(month, os.OPS_NbrMonths, GETDATE()))
	order by sc.SC_SalesCode   
        */
        SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> retrieveExistedOperationsPlanning(string equipmentName);

        int saveOperationsPlanning(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data);
    }
}
