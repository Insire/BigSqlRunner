namespace BigRunner.Core
{
    public sealed class SqlRunnerOptions
    {
        public BatchTerminator Terminator { get; set; }
        public string SqlFilePath { get; set; }
        public string ConnectionString { get; set; }
    }
}
