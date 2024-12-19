using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ForecastingModule.Helper;

namespace ForecastingModule.Repository
{
    internal interface ForecastRepository
    {
        /*
         select distinct sc.SC_SalesCode, sc.SC_ItemName, sc.SC_FCPercent, DATEADD(DAY, 
        -((DATEPART(WEEKDAY, EOMONTH(fr.FC_Date)) + 5) % 7), 
        EOMONTH(fr.FC_Date)
    ) AS FC_Date, DATEFROMPARTS(YEAR(DATEADD(month, 1, fr.FC_Date)), MONTH(DATEADD(month, 1, fr.FC_Date)), 1) as FC_OperationsDate,
	IsNUll(fr.FC_Quantity, 0)as FC_Quantity, sc.SC_Comments, sc.SC_Model
	from [WeilerForecasting].[dbo].[SalesCodes] sc
	left join [WeilerForecasting].[dbo].[OperationsSettings] os on os.OPS_Tab = sc.SC_OperationsTab 
	left join [WeilerForecasting].[dbo].Forecast fr on fr.FC_Model = sc.SC_Model and fr.FC_SalesCode = sc.SC_SalesCode
		  and DATEFROMPARTS(YEAR(DATEADD(month, 1, fr.FC_Date)), MONTH(DATEADD(month, 1, fr.FC_Date)), 1) between DATEFROMPARTS(YEAR(DATEADD(day, os.OPS_NbrDays, GETDATE())), MONTH(DATEADD(day, os.OPS_NbrDays, GETDATE())), 1) and EOMONTH(DATEADD(month, os.OPS_NbrMonths, GETDATE()))
	where sc.SC_OperationsTab = '{equipmentName}'
	order by sc.SC_SalesCode
        */
        SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> retrieveForecast(string modelName);
    }
}
