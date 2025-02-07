using System;
using System.Collections.Generic;
using ForecastingModule.Helper;

namespace ForecastingModule.Service
{
    internal interface InformationSchemaService
    {
        Optional<Tuple<string, List<Tuple<string, string>>>> getColumsMetaByTable(string tableName);
    }
}
