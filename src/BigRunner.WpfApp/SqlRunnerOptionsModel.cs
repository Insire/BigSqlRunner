using BigRunner.Core;

namespace BigRunner.WpfApp
{
    public sealed class SqlRunnerOptionsModel
    {
        public BatchTerminator Terminator { get; set; }
        public string CustomTerminator { get; set; }
        public string SqlFilePath { get; set; }
        public string ConnectionString { get; set; }
    }
}