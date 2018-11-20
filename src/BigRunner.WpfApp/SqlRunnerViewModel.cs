using BigRunner.Core;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BigRunner.WpfApp
{
    public sealed class SqlRunnerViewModel : ObservableObject
    {
        private readonly BusyStack _busyStack;
        private readonly Func<string, ILogger> _loggerFactory;

        public IAsyncCommand ExecuteScript { get; }

        public SqlRunnerOptionsViewModel OptionsViewModel { get; }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            private set { SetValue(ref _isBusy, value); }
        }

        public SqlRunnerViewModel(Func<string, ILogger> loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _busyStack = new BusyStack(hasItems => IsBusy = hasItems);

            OptionsViewModel = new SqlRunnerOptionsViewModel();
            ExecuteScript = AsyncCommand.Create(ExecuteScriptInternalAsync, CanExecuteScript);
        }

        private async Task ExecuteScriptInternalAsync(CancellationToken token)
        {
            using (_busyStack.GetToken())
            {
                var options = new SqlRunnerOptions()
                {
                    ConnectionString = OptionsViewModel.ConnectionString,
                    SqlFilePath = OptionsViewModel.SqlFilePath,
                    Terminator = OptionsViewModel.Terminator,
                };
                var runner = new SqlRunner(_loggerFactory("TODO"), options);

                await runner.Run(token).ConfigureAwait(false);
            }
        }

        private bool CanExecuteScript()
        {
            return !_isBusy
                && !string.IsNullOrEmpty(OptionsViewModel.SqlFilePath)
                && File.Exists(OptionsViewModel.SqlFilePath)
                && !string.IsNullOrEmpty(OptionsViewModel.ConnectionString);
        }
    }
}
