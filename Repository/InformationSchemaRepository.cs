using System;
using System.Collections.Generic;

namespace ForecastingModule.Repository
{
    internal interface InformationSchemaRepository
    {
        List<Tuple<String, String>> getColumsMetaByTable(String tableName);
    }
}
