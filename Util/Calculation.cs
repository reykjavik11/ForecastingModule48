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
        public static readonly string SC_MODAL = "SC_Model";

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
            SyncLinkedDictionary<object, object> synchronizedLinkedDictionary = synchronizedLinkedMap.Get(SALECODE);
            return getSum(synchronizedLinkedDictionary);
        }

        public static int getSum(SyncLinkedDictionary<object, object> synchronizedLinkedDictionary)
        {
            int sum = 0;
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

        public static bool populateForecasting(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> operPlanningMap,
                                                 SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> forecastMap)
        {
            bool anyUpdate = false;
            foreach (string key in forecastMap.Keys.ToList())
            {
                SyncLinkedDictionary<object, object> forecastParams = forecastMap.Get(key);

                object objectForecastPercent = forecastParams.Get("SC_FCPercent");
                object objectBase = forecastParams.Get(ForecastRepositoryImpl.BASE_FLAG);
                object objectModel = forecastParams.Get(SC_MODAL);

                if (forecastParams != null && objectBase is bool flagBaseOrNot //&& !flagBaseOrNot
                    && objectForecastPercent != null && objectForecastPercent is int forecastPercentage
                    && objectModel != null && objectModel is string model)
                {
                    anyUpdate = populateForecastBySaleCode(operPlanningMap, anyUpdate, forecastParams, forecastPercentage, key, model, flagBaseOrNot);
                }

                addTotals(forecastParams);
            }
            return anyUpdate;
        }

        public static int getBaseSumByDate(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> operationModel, DateTime operationsDate, string modelFromForecast)
        {
            int baseSum = 0;
            foreach (string saleCode in operationModel.Keys.ToList())
            {
                SyncLinkedDictionary<object, object> operationParams = operationModel.Get(saleCode);

                if (operationParams != null)
                {
                    object objectModel = operationParams.Get(SC_MODAL);
                    if (objectModel != null && objectModel is string modelFromOperatrion
                            && !modelFromOperatrion.Equals(modelFromForecast)) //OP model contain all model - and we are searching particular one. If model does not equal - continue throught the iteration
                    {
                        continue;
                    }
                    foreach (object key in operationParams.Keys.ToList())
                    {
                        if (key is DateTime keyOperationDate && keyOperationDate.Equals(operationsDate)
                            && operationParams.Get(ForecastRepositoryImpl.BASE_FLAG) is bool flag && flag //search same input date and base flag = 1
                            /*&& objectModel != null && objectModel is string modelFromOperatrion
                            && modelFromOperatrion.Equals(modelFromForecast)*/)
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

        private static void addTotals(SyncLinkedDictionary<object, object> forecastParams)
        {
            int sum = getSum(forecastParams);
            object objectTotal = forecastParams.Get(TOTAL);
            if (objectTotal == null)
            {
                forecastParams.Add(TOTAL, sum);
            }
            else
            {
                forecastParams.Update(TOTAL, sum, objectTotal);
            }
        }

        private static bool populateForecastBySaleCode(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> operPlanningMap,
                                                      bool anyUpdate,
                                                      SyncLinkedDictionary<object, object> forecastParams,
                                                      int forecastPercentage,
                                                      string forecastSaleCode,
                                                      string model,
                                                      bool forecastFlagBase)
        {
            //bool anyUpdate = false;
            float carryOverFromPrevMonth = 0;
            foreach (string operKey in operPlanningMap.Keys.ToList())
            {
                SyncLinkedDictionary<object, object> operParams = operPlanningMap.Get(operKey);

                List<DateTime> operationsDates = getSortedOperationsDates(operParams);

                foreach (DateTime operDate in operationsDates)
                {
                    DateTime forecastDate = DateUtil.toForecastDay(operDate);
                    bool updated = false;
                    if (forecastFlagBase)//update forecast base flag = 1 if it not compare with OperPlanning (base flag = 1)
                    {
                        updated = updateBase1(forecastParams, forecastSaleCode, operKey, operParams, operDate, forecastDate, updated);
                    }
                    else
                    {
                        updated = updateBase0(operPlanningMap, forecastParams, forecastPercentage, model, ref carryOverFromPrevMonth, operDate, forecastDate);
                    }
                    if (updated && !anyUpdate)
                    {
                        anyUpdate = true;
                    }
                }
            }
            return anyUpdate;
        }

        private static bool updateBase0(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> operPlanningMap, 
                                        SyncLinkedDictionary<object, object> forecastParams, 
                                        int forecastPercentage, 
                                        string model, 
                                        ref float carryOverFromPrevMonth, 
                                        DateTime operDate, 
                                        DateTime forecastDate)
        {
            bool updated;
            int base1ColumnSum = Calculation.getBaseSumByDate(operPlanningMap, operDate, model);//calculate sum from base =1 OP data by specific date
            float countFromBase0Percentage = base1ColumnSum * ((float)forecastPercentage / 100);//calculate specific percantage value from base OP data

            float floatresult = countFromBase0Percentage + carryOverFromPrevMonth;//calculate float result 
            floatresult = (floatresult - (int)floatresult >= 0.9f) ? (float)Math.Ceiling(floatresult) : floatresult;// rounded if partial part >= 0.9 

            int result = (int)floatresult;//cast float result to int

            carryOverFromPrevMonth = floatresult - result;//calculate fraction on cuurent month taht will be carred out on next month 

            object prevCount = forecastParams.Get(operDate);

            if (forecastParams.Keys.Contains(forecastDate))
            {
                updated = forecastParams.Update(forecastDate, result, prevCount);//TODO is better to change if 0 to null?
            }
            else
            {
                updated = true;
                forecastParams.Add(forecastDate, result);
            }

            return updated;
        }

        private static bool updateBase1(SyncLinkedDictionary<object, object> forecastParams, string forecastSaleCode, string operKey, SyncLinkedDictionary<object, object> operParams, DateTime operDate, DateTime forecastDate, bool updated)
        {
            object operObject = operParams.Get(operDate);
            object forecastCount = forecastParams.Get(forecastDate);

            bool sameSaleCodeAndBaseCode = forecastSaleCode.Equals(operKey);
            bool baseFromOPNotNull = operObject is int operPlanCount && !operObject.Equals(forecastCount);
            bool baseFromForecastingNotNull = forecastCount is int && !forecastCount.Equals(operObject);
            if ((baseFromOPNotNull || baseFromForecastingNotNull) && sameSaleCodeAndBaseCode)
            {
                if (forecastParams.Keys.Contains(forecastDate))
                {
                    updated = forecastParams.Update(forecastDate, operObject, forecastCount);//TODO is better to change if 0 to null?
                }
                else
                {
                    updated = true;
                    forecastParams.Add(forecastDate, operObject);
                }
            }

            return updated;
        }

        private static List<DateTime> getSortedOperationsDates(SyncLinkedDictionary<object, object> operParams)
        {
            List<DateTime> operationDates = new List<DateTime>();
            foreach (object paramKey in operParams.Keys.ToList())
            {
                if (paramKey is DateTime operDate)
                {
                    operationDates.Add(operDate);
                }
            }
            operationDates.Sort();
            return operationDates;
        }
    }
}
