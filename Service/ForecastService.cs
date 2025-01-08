using ForecastingModule.Helper;

namespace ForecastingModule.Service
{
    public interface ForecastService
    {
        SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> retrieveForecastData(string modelName);
    }
}
