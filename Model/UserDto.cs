using System;

namespace ForecastingModule.Model
{
    internal class UserDto
    {
        public string userName { get; set; }
        public bool accessOperationsPlanning { get; set; }
        public bool accessForecast { get; set; }
        public bool accessOperationsSettings { get; set; }
        public bool accessForecastSettings { get; set; }
        public bool accessManage { get; set; }
        public bool activeFlag { get; set; }

        public override string ToString()
        {
            return "{" 
                + $"userName: {userName},"
                + $"accessOperationsPlanning: {accessOperationsPlanning},"
                + $"accessForecast: {accessForecast},"
                + $"accessOperationsSettings: {accessOperationsSettings},"
                + $"accessForecastSettings: {accessForecastSettings},"
                + $"accessManage: {accessManage},"
                + $"activeFlag: {activeFlag}"
                + "}";
        }
    }
}
