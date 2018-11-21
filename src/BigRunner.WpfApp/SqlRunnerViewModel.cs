using BigRunner.Core;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BigRunner.WpfApp
{
    public sealed class SqlRunnerViewModel : ObservableObject
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly BusyStack _busyStack;
        private readonly Func<string, LoggerConfiguration> _loggerFactory;

        private ILogger _logger;

        public IAsyncCommand ExecuteScriptCommand { get; }
        public IAsyncCommand SelectScriptFileCommand { get; }
        public IAsyncCommand OpenConnectionStringResourceCommand { get; }
        public IAsyncCommand EditNameCommand { get; }
        public IAsyncCommand EditCustomTerminatorCommand { get; }

        public SqlRunnerOptionsViewModel OptionsViewModel { get; }

        private LogViewModel _log;
        public LogViewModel Log
        {
            get { return _log; }
            private set { SetValue(ref _log, value); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            private set { SetValue(ref _isBusy, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetValue(ref _name, value); }
        }

        public SqlRunnerViewModel(IDialogCoordinator dialogCoordinator, Func<string, LoggerConfiguration> loggerFactory, string name)
        {
            _dialogCoordinator = dialogCoordinator ?? throw new ArgumentNullException(nameof(dialogCoordinator));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _busyStack = new BusyStack(hasItems => IsBusy = hasItems);

            Name = name;
            OptionsViewModel = new SqlRunnerOptionsViewModel();
            ExecuteScriptCommand = AsyncCommand.Create(ExecuteScriptInternalAsync, CanExecuteScript);
            SelectScriptFileCommand = AsyncCommand.Create(SelectScriptFileInternal, () => true);
            OpenConnectionStringResourceCommand = AsyncCommand.Create(OpenConnectioNStringResourceInternal, () => true);
            EditNameCommand = AsyncCommand.Create(EditNameInternal, () => true);
            EditCustomTerminatorCommand = AsyncCommand.Create(EditCustomTerminatorInternal, () => true);
        }

        private Task ExecuteScriptInternalAsync(CancellationToken token)
        {
            using (_busyStack.GetToken())
            {
                var options = new SqlRunnerOptions()
                {
                    ConnectionString = OptionsViewModel.ConnectionString,
                    SqlFilePath = OptionsViewModel.SqlFilePath,
                    Terminator = OptionsViewModel.Terminator,
                };

                if (_logger is null)
                {
                    var config = _loggerFactory("TODO");
                    Log = new LogViewModel(config);
                    _logger = config.CreateLogger();// fill with database name
                }

                var runner = new SqlRunner(_logger, options);

                _logger.Verbose("Starting");
                _logger.Debug("Starting");
                _logger.Information("Starting");
                _logger.Warning("Starting");
                _logger.Error("Starting");
                _logger.Fatal("Starting");

                return Task.CompletedTask;
                //await runner.Run(token).ConfigureAwait(false);
            }
        }

        private bool CanExecuteScript()
        {
            return true;
            return !_isBusy
                && !string.IsNullOrEmpty(OptionsViewModel.SqlFilePath)
                && File.Exists(OptionsViewModel.SqlFilePath)
                && !string.IsNullOrEmpty(OptionsViewModel.ConnectionString);
        }

        private Task OpenConnectioNStringResourceInternal(CancellationToken token)
        {
            Process.Start("https://www.connectionstrings.com/");

            return Task.CompletedTask;
        }

        private async Task EditNameInternal(CancellationToken token)
        {
            var result = await _dialogCoordinator.ShowInputAsync(this, "test", "message");
            await System.Windows.Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => Name = result));
        }

        private async Task EditCustomTerminatorInternal(CancellationToken token)
        {
            var result = await _dialogCoordinator.ShowInputAsync(this, "test", "message");
            await System.Windows.Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => OptionsViewModel.CustomTerminator = result));
        }

        private Task SelectScriptFileInternal(CancellationToken token)
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DereferenceLinks = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                ShowReadOnly = true,
                ValidateNames = true,
                Multiselect = false,
                Filter = "SQL files (*.sql)|*.txt|All files (*.*)|*.*",
                Title = "Select a SQL script file",
            };

            if (dialog.ShowDialog() is true)
                OptionsViewModel.SqlFilePath = dialog.SafeFileName;

            return Task.CompletedTask;
        }
    }
}
