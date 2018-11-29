using BigRunner.Core;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Serilog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BigRunner.WpfApp
{
    // TODO add dialog states
    public sealed class SqlRunnerViewModel : ObservableObject
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly BusyStack _busyStack;
        private readonly BusyStack _fileCountBusyStack;
        private readonly Func<string, LoggerConfiguration> _loggerFactory;
        private readonly Progress<int> _progress;
        private readonly ShellViewModel _shell;

        private ILogger _logger;
        private DateTime _scriptStartTime;
        private System.Timers.Timer _timer;
        private long _readIndex;
        private CancellationTokenSource _pauseCancellationTokenSource;

        public IAsyncCommand PauseScriptCommand { get; }
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

        private bool _isBusyAnalyzingFile;
        public bool IsBusyAnalyzingFile
        {
            get { return _isBusyAnalyzingFile; }
            private set { SetValue(ref _isBusyAnalyzingFile, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetValue(ref _name, value); }
        }

        private long _scriptLineCount;
        public long ScriptLineCount
        {
            get { return _scriptLineCount; }
            private set { SetValue(ref _scriptLineCount, value); }
        }

        private long _value;
        public long Value
        {
            get { return _value; }
            private set { SetValue(ref _value, value); }
        }

        private long _maximum;
        public long Maximum
        {
            get { return _maximum; }
            private set { SetValue(ref _maximum, value); }
        }

        private TimeSpan _scriptRunTime;
        public TimeSpan ScriptRunTime
        {
            get { return _scriptRunTime; }
            private set { SetValue(ref _scriptRunTime, value); }
        }

        private int _commandsExecuted;
        public int CommandsExecuted
        {
            get { return _commandsExecuted; }
            private set { SetValue(ref _commandsExecuted, value); }
        }

        public SqlRunnerViewModel(IDialogCoordinator dialogCoordinator, ShellViewModel shell, Func<string, LoggerConfiguration> loggerFactory, string name)
        {
            _dialogCoordinator = dialogCoordinator ?? throw new ArgumentNullException(nameof(dialogCoordinator));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));

            _busyStack = new BusyStack(hasItems => IsBusy = hasItems);
            _progress = new Progress<int>();
            _fileCountBusyStack = new BusyStack(hasItems => IsBusyAnalyzingFile = hasItems);
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += _timer_Elapsed;

            Name = name;
            OptionsViewModel = new SqlRunnerOptionsViewModel();
            OptionsViewModel.PropertyChanged += OptionsViewModel_PropertyChanged;

            ExecuteScriptCommand = AsyncCommand.Create(ExecuteScriptInternal, CanExecuteScript);
            SelectScriptFileCommand = AsyncCommand.Create(SelectScriptFileInternal, () => true);
            OpenConnectionStringResourceCommand = AsyncCommand.Create(OpenConnectioNStringResourceInternal, () => true);
            EditNameCommand = AsyncCommand.Create(EditNameInternal, () => true);
            EditCustomTerminatorCommand = AsyncCommand.Create(EditCustomTerminatorInternal, () => true);
            PauseScriptCommand = AsyncCommand.Create(PauseInternal, CanPause);
        }

        public SqlRunnerModel GetModel()
        {
            return new SqlRunnerModel()
            {
                Name = Name,
                OptionsModel = OptionsViewModel.GetModel(),
                ReadIndex = _readIndex,
            };
        }

        private Task PauseInternal(CancellationToken token)
        {
            _pauseCancellationTokenSource.Cancel();
            _pauseCancellationTokenSource.Dispose();

            return Task.CompletedTask;
        }

        private bool CanPause()
        {
            return IsBusy && !(_pauseCancellationTokenSource is null);
        }

        private async void OptionsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OptionsViewModel.SqlFilePath))
            {
                if (!File.Exists(OptionsViewModel.SqlFilePath))
                    return;

                var max = await GetLineCount(OptionsViewModel.SqlFilePath).ConfigureAwait(false);
                await Application.Current?.Dispatcher?.InvokeAsync(() => Maximum = max);
            }
        }

        private async void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            await Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                OnPropertyChanged(nameof(CommandsExecuted));
                OnPropertyChanged(nameof(Value));

                ScriptRunTime = DateTime.UtcNow - _scriptStartTime;
            });

            Interlocked.Exchange(ref _commandsExecuted, 0);
        }

        private void HandleCommandProgress(int progress)
        {
            Interlocked.Add(ref _commandsExecuted, progress);
            Interlocked.Add(ref _value, progress);
            Interlocked.Add(ref _readIndex, progress);
        }

        private async Task ExecuteScriptInternal(CancellationToken token)
        {
            using (_busyStack.GetToken())
            {
                _pauseCancellationTokenSource = new CancellationTokenSource();
                token.Register(() =>
                {
                    _progress.ProgressChanged -= _progress_ProgressChanged;
                    _readIndex = 0;
                });

                using (var tempCTS = CancellationTokenSource.CreateLinkedTokenSource(_pauseCancellationTokenSource.Token, token))
                {
                    var options = new SqlRunnerOptions()
                    {
                        ConnectionString = OptionsViewModel.ConnectionString,
                        SqlFilePath = OptionsViewModel.SqlFilePath,
                        Terminator = OptionsViewModel.Terminator,
                    };

                    if (_logger is null)
                    {
                        var config = _loggerFactory(Path.GetFileNameWithoutExtension(OptionsViewModel.SqlFilePath));
                        Log = new LogViewModel(config);
                        _logger = config.CreateLogger();
                    }

                    var runner = new SqlRunner(_logger, options);
                    _progress.ProgressChanged += _progress_ProgressChanged;
                    _scriptStartTime = DateTime.UtcNow;
                    _timer.Start();

                    try
                    {
                        await Task.Run(async () => await runner.Run(_progress, _readIndex, tempCTS.Token).ConfigureAwait(false)).ConfigureAwait(false);
                    }
                    finally
                    {
                        _timer.Stop();
                    }
                }
            }
        }

        private void _progress_ProgressChanged(object sender, int e)
        {
            HandleCommandProgress(e);
        }

        private async Task<long> GetLineCount(string path)
        {
            long count = 0;

            await Task.Run(async () =>
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    count = await CountLinesMaybe(stream).ConfigureAwait(false);
            }).ConfigureAwait(false);

            return count;
        }

        // source: http://www.nimaara.com/2018/03/20/counting-lines-of-a-text-file/
        private async Task<long> CountLinesMaybe(Stream stream)
        {
            const char CR = '\r';
            const char LF = '\n';
            const char NULL = (char)0;

            const int BytesAtTheTime = 4;

            var lineCount = 0L;
            var byteBuffer = new byte[1024 * 1024];
            var detectedEOL = NULL;
            var currentChar = NULL;

            var timeStamp = DateTime.UtcNow;
            int bytesRead;
            while ((bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
            {
                var i = 0;
                for (; i <= bytesRead - BytesAtTheTime; i += BytesAtTheTime)
                {
                    currentChar = (char)byteBuffer[i];

                    if (detectedEOL != NULL)
                    {
                        if (currentChar == detectedEOL) { lineCount++; }

                        currentChar = (char)byteBuffer[i + 1];
                        if (currentChar == detectedEOL) { lineCount++; }

                        currentChar = (char)byteBuffer[i + 2];
                        if (currentChar == detectedEOL) { lineCount++; }

                        currentChar = (char)byteBuffer[i + 3];
                        if (currentChar == detectedEOL) { lineCount++; }
                    }
                    else
                    {
                        if (currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                        }
                        i -= BytesAtTheTime - 1;
                    }
                }

                for (; i < bytesRead; i++)
                {
                    currentChar = (char)byteBuffer[i];

                    if (detectedEOL != NULL)
                    {
                        if (currentChar == detectedEOL) { lineCount++; }
                    }
                    else
                    {
                        if (currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                        }
                    }
                }

                if (DateTime.UtcNow > timeStamp)
                {
                    await Application.Current?.Dispatcher?.InvokeAsync(() => ScriptLineCount = lineCount);
                    timeStamp = DateTime.UtcNow.AddSeconds(3);
                }
            }

            if (currentChar != LF && currentChar != CR && currentChar != NULL)
                lineCount++;

            await Application.Current?.Dispatcher?.InvokeAsync(() => ScriptLineCount = lineCount);

            return lineCount;
        }

        private bool CanExecuteScript()
        {
            return !_isBusy
                && !IsBusyAnalyzingFile
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
            var result = await _dialogCoordinator.ShowInputAsync(_shell, "Edit Runner name", "Enter the new name:");
            await Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Normal, new Action(() => Name = result));
        }

        private async Task EditCustomTerminatorInternal(CancellationToken token)
        {
            var result = await _dialogCoordinator.ShowInputAsync(_shell, "Provide custom Terminator", "Enter the terminator to use:");
            await Application.Current?.Dispatcher?.BeginInvoke(DispatcherPriority.Normal, new Action(() => OptionsViewModel.CustomTerminator = result));
        }

        private Task SelectScriptFileInternal(CancellationToken token)
        {
            using (_fileCountBusyStack.GetToken())
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
                    Filter = "SQL files (*.sql)|*.sql|All files (*.*)|*.*",
                    Title = "Select a SQL script file",
                };

                if (dialog.ShowDialog() is true)
                    OptionsViewModel.SqlFilePath = dialog.FileName;
            }

            return Task.CompletedTask;
        }
    }
}