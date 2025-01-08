using System;
using System.Collections.Generic;
using ForecastingModule.Helper;

namespace ForecastingModule.Util
{
    public sealed class Calculation
    {
        public static readonly string TOTAL = "TOTAL";
        private Calculation() { }

        public static void addTotal(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>>  syncLinkedDictionary)
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
    }
}
