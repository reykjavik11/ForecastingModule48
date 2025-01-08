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
        SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> retrieveForecast(string modelName);
    }
}
