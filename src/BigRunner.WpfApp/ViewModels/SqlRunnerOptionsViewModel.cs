using BigRunner.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BigRunner.WpfApp
{
    public sealed class SqlRunnerOptionsViewModel : ObservableObject
    {
        private static readonly IEnumerable<BatchTerminator> _cache = Enum.GetValues(typeof(BatchTerminator)).Cast<BatchTerminator>().ToArray();

        public IEnumerable<BatchTerminator> Terminators { get; }

        private BatchTerminator _terminator;
        public BatchTerminator Terminator
        {
            get { return _terminator; }
            set { SetValue(ref _terminator, value); }
        }

        private string _customTerminator;
        public string CustomTerminator
        {
            get { return _customTerminator; }
            set { SetValue(ref _customTerminator, value); }
        }

        private string _sqlFilePath;
        public string SqlFilePath
        {
            get { return _sqlFilePath; }
            set { SetValue(ref _sqlFilePath, value); }
        }

        private string _connectionString;
        public string ConnectionString
        {
            get { return _connectionString; }
            set { SetValue(ref _connectionString, value); }
        }

        public SqlRunnerOptionsViewModel()
        {
            Terminators = _cache;
        }

        public SqlRunnerOptionsModel GetModel()
        {
            return new SqlRunnerOptionsModel()
            {
                ConnectionString = ConnectionString,
                CustomTerminator = CustomTerminator,
                SqlFilePath = SqlFilePath,
                Terminator = Terminator,
            };
        }
    }
}