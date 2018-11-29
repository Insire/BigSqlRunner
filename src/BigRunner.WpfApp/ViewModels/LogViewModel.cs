using Serilog;
using Serilog.Events;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BigRunner.WpfApp
{
    public sealed class LogViewModel : ObservableObject
    {
        private ObservableCollection<LogEvent> _items;
        public ObservableCollection<LogEvent> Items
        {
            get { return _items; }
            private set { SetValue(ref _items, value); }
        }

        private IAsyncCommand _clearCommand;
        public IAsyncCommand ClearCommand
        {
            get { return _clearCommand; }
            private set { SetValue(ref _clearCommand, value); }
        }

        private IAsyncCommand _logVerboseCommand;
        public IAsyncCommand LogVerboseCommand
        {
            get { return _logVerboseCommand; }
            private set { SetValue(ref _logVerboseCommand, value); }
        }

        private IAsyncCommand _logDebugCommand;
        public IAsyncCommand LogDebugCommand
        {
            get { return _logDebugCommand; }
            private set { SetValue(ref _logDebugCommand, value); }
        }

        private IAsyncCommand _logInformationCommand;
        public IAsyncCommand LogInformationCommand
        {
            get { return _logInformationCommand; }
            private set { SetValue(ref _logInformationCommand, value); }
        }

        private IAsyncCommand _logWarningCommand;
        public IAsyncCommand LogWarningCommand
        {
            get { return _logWarningCommand; }
            private set { SetValue(ref _logWarningCommand, value); }
        }

        private IAsyncCommand _logErrorCommand;
        public IAsyncCommand LogErrorCommand
        {
            get { return _logErrorCommand; }
            private set { SetValue(ref _logErrorCommand, value); }
        }

        private IAsyncCommand _logFatalCommand;
        public IAsyncCommand LogFatalCommand
        {
            get { return _logFatalCommand; }
            private set { SetValue(ref _logFatalCommand, value); }
        }

        private LogEventLevel _logEventLevel;
        public LogEventLevel LogEventLevel
        {
            get { return _logEventLevel; }
            private set { SetValue(ref _logEventLevel, value); }
        }

        private LogViewModel()
        {
            _items = new ObservableCollection<LogEvent>();
            LogEventLevel = LogEventLevel.Verbose;
            LogVerboseCommand = AsyncCommand.Create(() => SetLogLevelInternal(LogEventLevel.Verbose), () => true);
            LogDebugCommand = AsyncCommand.Create(() => SetLogLevelInternal(LogEventLevel.Debug), () => true);
            LogInformationCommand = AsyncCommand.Create(() => SetLogLevelInternal(LogEventLevel.Information), () => true);
            LogWarningCommand = AsyncCommand.Create(() => SetLogLevelInternal(LogEventLevel.Warning), () => true);
            LogErrorCommand = AsyncCommand.Create(() => SetLogLevelInternal(LogEventLevel.Error), () => true);
            LogFatalCommand = AsyncCommand.Create(() => SetLogLevelInternal(LogEventLevel.Fatal), () => true);
            ClearCommand = AsyncCommand.Create(ClearInternal, () => _items.Count > 0);
        }

        public LogViewModel(LoggerConfiguration configuration)
            : this()
        {
            configuration
                .WriteTo.Observers(events => events
                .Where(evt => evt.Level.Equals(LogEventLevel))
                .Sample(TimeSpan.FromSeconds(1))
                .Do(evt => Application.Current?.Dispatcher?.InvokeAsync(() =>
                {
                    if (_items.Count > 100)
                        _items.RemoveAt(100);

                    _items.Insert(0, evt);
                }))
                .Subscribe());
        }

        private async Task SetLogLevelInternal(LogEventLevel level)
        {
            await Application.Current?.Dispatcher?.InvokeAsync(() => LogEventLevel = level);
        }

        private Task ClearInternal(CancellationToken token)
        {
            _items.Clear();

            return Task.CompletedTask;
        }
    }
}