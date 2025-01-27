using System;
using System.Collections.Generic;
using System.Linq;
using ForecastingModule.Helper;
using ForecastingModule.Repository.Impl;
using ForecastingModule.Utilities;

namespace ForecastingModule.Util
{
    public sealed class Calculation
    {
        public static readonly string TOTAL = "TOTAL";
        private static Logger logger = Logger.Instance;
        private Calculation() { }

        public static void addTotal(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> syncLinkedDictionary)
        {
            IEnumerator<string> saleCodes = syncLinkedDictionary.Keys.GetEnumerator();
            while (saleCodes.MoveNext())
            {
                string saleCode = saleCodes.Current;
                int rowTotal = getSumBySalesCode(saleCode, syncLinkedDictionary);

                SyncLinkedDictionary<object, object> saleCodeContent = syncLinkedDictionary.Get(saleCode);
                if (saleCodeContent != null)
                {
                    saleCodeContent.Add(TOTAL, rowTotal);
                }
            }
        }

        public static int getSumBySalesCode(string SALECODE, SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> synchronizedLinkedMap)
        {
            int sum = 0;
            SyncLinkedDictionary<object, object> synchronizedLinkedDictionary = synchronizedLinkedMap.Get(SALECODE);
            foreach (var item in synchronizedLinkedDictionary.Keys)
            {
                if (item is DateTime && synchronizedLinkedDictionary.Get(((DateTime)item)) is int)
                {
                    object value = synchronizedLinkedDictionary.Get(((DateTime)item));
                    if (value is int)
                    {
                        sum += (int)value;
                    }
                }
            }

            return sum;
        }

        public static bool populateBase0Forecasting(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> operPlanningMap, 
                                                 SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> forecastMap)
        {
            bool anyUpdate = false;
            foreach (string key in forecastMap.Keys.ToList())
            {
                SyncLinkedDictionary<object, object> forecastParams = forecastMap.Get(key);

                object objectForecastPercent = forecastParams.Get("SC_FCPercent");
                object objectBase = forecastParams.Get(ForecastRepositoryImpl.BASE_FLAG);

                if (forecastParams != null && objectBase is bool base0Flag && !base0Flag
                    && objectForecastPercent != null && objectForecastPercent is int forecastPercentage)
                {
                    anyUpdate = populateBySaleCodeAndDate(operPlanningMap, anyUpdate, forecastParams, forecastPercentage);
                }
            }
            return anyUpdate;
        }

        private static bool populateBySaleCodeAndDate(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> operPlanningMap, 
                                                      bool anyUpdate, 
                                                      SyncLinkedDictionary<object, object> forecastParams, 
                                                      int forecastPercentage)
        {
            float carryOverFromPrevMonth = 0;
            foreach (object paramKey in forecastParams.Keys.ToList())
            {
                if (paramKey is DateTime operDate)
                {

                    int base1Sum = Calculation.getBaseSumByDate(operPlanningMap, operDate);//calculate sum from base =1 OP data by specific date
                    float countFromBase0Percentage = base1Sum * ((float)forecastPercentage / 100);//calculate specific percantage value from base OP data

                    float floatresult = countFromBase0Percentage + carryOverFromPrevMonth;//calculate float result 
                    floatresult = (floatresult - (int)floatresult >= 0.9f) ? (float)Math.Ceiling(floatresult) : floatresult;// rounded if partial part >= 0.9 

                    int result = (int)floatresult;//cast float result to int

                    carryOverFromPrevMonth = floatresult - result;//calculate fraction on cuurent month taht will be carred out on next month 

                    object prevCount = forecastParams.Get(operDate);
                    bool updated = forecastParams.Update(operDate, result, prevCount);//TODO is better to change if 0 to null?
                    if (updated && !anyUpdate)
                    {
                        anyUpdate = true;
                    }
                }
            }
            return anyUpdate;
        }

        public static int getBaseSumByDate(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> operationModel, DateTime operationsDate)
        {
            int baseSum = 0;
            foreach (string saleCode in operationModel.Keys.ToList())
            {
                SyncLinkedDictionary<object, object> operationParams = operationModel.Get(saleCode);
                if (operationParams != null)
                {
                    foreach (object key in operationParams.Keys.ToList())
                    {
                        if (key is DateTime keyOperationDate && keyOperationDate.Equals(operationsDate)
                            && operationParams.Get(ForecastRepositoryImpl.BASE_FLAG) is bool flag && flag)//search same input date and base flag = 1
                        {
                            object objectCount = operationParams.Get(keyOperationDate);
                            if (objectCount != null && objectCount is int count)
                            {
                                baseSum += count;
                            }
                            else
                            {
                                logger.LogError($"getBaseSumByDate() -> calculation base sum by date {operationsDate} - illegal type of {objectCount} (type should be int)");
                            }
                        }
                    }
                }
            }
            return baseSum;
        }
    }
}
