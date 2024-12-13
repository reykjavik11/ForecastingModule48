using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }
}
