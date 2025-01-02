namespace ForecastingModule.Helper
{
    public sealed class CellUpdateInfo
    {
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public object NewValue { get; set; }
    }
}
