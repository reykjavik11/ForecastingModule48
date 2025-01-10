using System;
using System.Collections.Generic;
using ForecastingModule.Helper;
using ForecastingModule.Repository.Impl;
using ForecastingModule.Util;
using ForecastingModule.Utilities;

namespace ForecastingModule.Service.Impl
{
    internal sealed class OperationsPlanningServiceImpl : OperationsPlanningService
    {
        private static readonly Lazy<OperationsPlanningServiceImpl> _instance = new Lazy<OperationsPlanningServiceImpl>(() => new OperationsPlanningServiceImpl());
        public static OperationsPlanningServiceImpl Instance => _instance.Value;
        private readonly OperationsPlanningRepositoryImpl repositoryImpl;
        private OperationsPlanningServiceImpl()
        {
            repositoryImpl = OperationsPlanningRepositoryImpl.Instance;
        }

        public Dictionary<string, object> getOperationsSetting(string equipmentName)
        {
            return repositoryImpl.getOperationsSetting(equipmentName);
        }

        public SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> retrieveExistedOperationsPlanning(string equipmentName)
        {
            SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> syncLinkedDictionary;
            try
            {
                syncLinkedDictionary = repositoryImpl.retrieveExistedOperationsPlanning(equipmentName);

                Calculation.addTotal(syncLinkedDictionary);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError(ex.StackTrace);
                throw ex;
            }
            return syncLinkedDictionary;
        }

        public int save(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data)
        {
            try
            {
                return repositoryImpl.save(data);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError(ex.StackTrace); 
                throw ex;
            }
        }
    }
}
