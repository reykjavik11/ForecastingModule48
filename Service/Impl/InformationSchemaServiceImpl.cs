using System;
using System.Collections.Generic;
using ForecastingModule.Helper;
using ForecastingModule.Repository.Impl;

namespace ForecastingModule.Service.Impl
{
    internal class InformationSchemaServiceImpl : InformationSchemaService
    {
        private static readonly Lazy<InformationSchemaServiceImpl> _instance = new Lazy<InformationSchemaServiceImpl>(() => new InformationSchemaServiceImpl());
        public static InformationSchemaServiceImpl Instance => _instance.Value;

        private readonly InformationSchemaRepositoryImpl schemaRepositoryImpl;
        public static readonly string TOTAL = "TOTAL";
        private InformationSchemaServiceImpl()
        {
            schemaRepositoryImpl = InformationSchemaRepositoryImpl.Instance;
        }
        public Optional<Tuple<string, List<Tuple<string, string>>>> getColumsMetaByTable(string tableName)
        {
            List<Tuple<string, string>> metaList = schemaRepositoryImpl.getColumsMetaByTable(tableName);

            return metaList.Count == 0 ? Optional<Tuple<string, List<Tuple<string, string>>>>.Empty() :
                Optional<Tuple<string, List<Tuple<string, string>>>>.Of(Tuple.Create(tableName, metaList));
        }
    }
}
