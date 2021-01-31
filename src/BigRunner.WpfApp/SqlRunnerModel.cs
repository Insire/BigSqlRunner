namespace BigRunner.WpfApp
{
    public sealed class SqlRunnerModel
    {
        public long ReadIndex { get; set; }
        public string Name { get; set; }
        public SqlRunnerOptionsModel OptionsModel { get; set; }
    }
}