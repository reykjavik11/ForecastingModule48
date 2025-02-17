## Programs sql

> Forecast tabs (Left button's menu):
> 
> `SELECT [TAB_TabName] FROM [WeilerForecasting].[dbo].[ParentTabs] where [TAB_ActiveFlag] = 1 order by [TAB_DisplayOrder]`

> Operations Planning (Top tab list):
>
> `select distinct sc.SC_OperationsTab, t1.OPS_DisplayOrder  from [WeilerForecasting].[dbo].[SalesCodes] as sc join (select os.OPS_Tab, os.OPS_DisplayOrder  from OperationsSettings os where os.OPS_ActiveFlag = 1) as t1  on (t1.OPS_Tab = sc.SC_ForecastTab or t1.OPS_Tab = sc.SC_OperationsTab) and sc.SC_ActiveFlag =1  and sc.SC_BaseFlag = 1 join [WeilerForecasting].[dbo].OperationsSettings os on os.OPS_Tab = sc.SC_OperationsTab and os.OPS_ActiveFlag =1 order by t1.OPS_DisplayOrder`