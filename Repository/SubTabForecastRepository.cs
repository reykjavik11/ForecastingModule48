using System.Collections.Generic;

namespace ForecastingModule.Repository.Impl
{
    internal interface SubTabForecastRepository
    {
        List<string> getActiveSubTabs(string parentTab);
    }
}