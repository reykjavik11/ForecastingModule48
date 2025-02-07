namespace ForecastingModule.Controller
{
    internal interface ISettingConntroller
    {
        void Insert();
        void Delete(int rowIndex);
        void Save();
    }
}