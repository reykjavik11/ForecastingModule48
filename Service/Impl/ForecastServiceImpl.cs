using System;
using ForecastingModule.Helper;
using ForecastingModule.Repository.Impl;
using ForecastingModule.Util;
using ForecastingModule.Utilities;

namespace ForecastingModule.Service.Impl
{
    public class ForecastServiceImpl : ForecastService
    {
        private static readonly Lazy<ForecastServiceImpl> _instance = new Lazy<ForecastServiceImpl>(() => new ForecastServiceImpl());
        public static ForecastServiceImpl Instance => _instance.Value;
        private readonly ForecastRepositoryImpl forecastRepositoryImpl;
        public static readonly string TOTAL = "TOTAL";
        private ForecastServiceImpl()
        {
            forecastRepositoryImpl = ForecastRepositoryImpl.Instance;
        }
        public SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> retrieveForecastData(string modelName)
        {
            SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> syncLinkedDictionary;
            try
            {
                syncLinkedDictionary = forecastRepositoryImpl.retrieveForecast(modelName);
                Calculation.addTotal(syncLinkedDictionary);
            } catch(Exception ex)
            {
                Logger.Instance.LogError(ex.StackTrace);
                throw ex;
            } 
            return syncLinkedDictionary;
        }
    }
}
