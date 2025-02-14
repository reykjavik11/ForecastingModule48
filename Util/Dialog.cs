using ForecastingModule.Helper;
using ForecastingModule.Service.Impl;
using System.Collections.Generic;
using System;
using ForecastingModule.OtherForm;
using ForecastingModule.Controller;

namespace ForecastingModule.Util
{
    internal sealed class Dialog
    {
        public class Tables
        {
            private Tables(string value)
            {
                this.Value = value;
            }

            public string Value { get; private set; }

            public static Tables OPERATIONS_SETTINGS { get { return new Tables("OperationsSettings"); } }
            public static Tables SALES_CODES { get { return new Tables("SalesCodes"); } }
            public static Tables USERS { get { return new Tables("Users"); } }
            public static Tables SUB_TABS { get { return new Tables("SubTabs"); } }
            public static Tables FORECAST_TABS { get { return new Tables("ParentTabs"); } }
            public override string ToString()
            {
                return Value;
            }

        };

        public static readonly Dictionary<String, String> MANAGE_TABLES_AND_SQL = new Dictionary<String, String> {
            {Tables.OPERATIONS_SETTINGS.Value, $"SELECT * FROM {Tables.OPERATIONS_SETTINGS.Value} order by OPS_DisplayOrder"},
            {Tables.SALES_CODES.Value, $"SELECT * FROM {Tables.SALES_CODES.Value} order by SC_ForecastTab, SC_SalesCode"},
            {Tables.USERS.Value, $"SELECT * FROM {Tables.USERS.Value} order by USR_UserName"},
            {Tables.SUB_TABS.Value, $"SELECT * FROM {Tables.SUB_TABS.Value} order by SUB_ParentTab, SUB_DisplayOrder"},
            {Tables.FORECAST_TABS.Value, $"SELECT * FROM {Tables.FORECAST_TABS.Value} order by TAB_DisplayOrder"}
        };

        public static void showManageTableDialog(string table)
        {
            string sql;
            MANAGE_TABLES_AND_SQL.TryGetValue(table, out sql);
            if (sql == null)
            {
                throw new KeyNotFoundException($"showManageTableDialog -> Table {table} was not find.");
            }

            Optional<Tuple<string, List<Tuple<string, string>>>> optional = InformationSchemaServiceImpl.Instance.getColumsMetaByTable(table);
            if (!optional.HasValue)
            {
                throw new InvalidOperationException($"No meta's information by {table}.");
            }
            Tuple<string, List<Tuple<string, string>>> model = optional.Get();
            SettingsView settingsForm = new SettingsView(model);

            SettingsController settingsController = new SettingsController(model, settingsForm, sql);
            settingsForm.ShowDialog();
        }
    }
}
