using System;
using System.Collections.Generic;
using ForecastingModule.Helper;
using ForecastingModule.Repository.Impl;
using ForecastingModule.Utilities;

namespace ForecastingModule.Service.Impl
{
    internal sealed class OperationsPlanningServiceImpl : OperationsPlanningService
    {
        private static readonly Lazy<OperationsPlanningServiceImpl> _instance = new Lazy<OperationsPlanningServiceImpl>(() => new OperationsPlanningServiceImpl());
        //private readonly OperationsPlanningServiceImpl operationService = OperationsPlanningServiceImpl.Instance;
        public static OperationsPlanningServiceImpl Instance => _instance.Value;
        private readonly OperationsPlanningRepositoryImpl repositoryImpl;
        public static readonly string TOTAL = "TOTAL";
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

                IEnumerator<string> saleCodes = syncLinkedDictionary.Keys.GetEnumerator();
                while (saleCodes.MoveNext())
                {
                    string saleCode = saleCodes.Current;
                    int rowTotal = getTotal(saleCode, syncLinkedDictionary);

                    SyncLinkedDictionary<object, object> saleCodeContent = syncLinkedDictionary.Get(saleCode);
                    if (saleCodeContent != null)
                    {
                        saleCodeContent.Add(TOTAL, rowTotal);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError(ex.StackTrace);
                throw ex;
            }
            return syncLinkedDictionary;
        }

        public static int getTotal(string SALECODE, SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> synchronizedLinkedMap)
        {
            int sum = 0;
            SyncLinkedDictionary<object, object> synchronizedLinkedDictionary = synchronizedLinkedMap.Get(SALECODE);
            foreach (var item in synchronizedLinkedDictionary.Keys)
            {
                if (item is DateTime && synchronizedLinkedDictionary.Get(((DateTime)item)) is int)
                {
                    object value = synchronizedLinkedDictionary.Get(((DateTime)item));
                    if(value is int) { 
                        sum += (int)value; 
                    } 
                }
            }

            return sum;
        }

        public int save(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data)
        {
            try
            {
                return repositoryImpl.saveOperationsPlanning(data);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError(ex.StackTrace); 
                throw ex;
            }
        }
    }
}
