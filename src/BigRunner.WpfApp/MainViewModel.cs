using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BigRunner.WpfApp
{
    public sealed class MainViewModel : ObservableObject
    {
        private readonly BusyStack _busyStack;
        private readonly Func<string, ILogger> _loggerFactory;

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            private set { SetValue(ref _isBusy, value); }
        }

        public ObservableCollection<SqlRunnerViewModel> Runners { get; }

        private SqlRunnerViewModel _selectedRunner;
        public SqlRunnerViewModel SelectedRunner
        {
            get { return _selectedRunner; }
            set { SetValue(ref _selectedRunner, value); }
        }

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }

        public MainViewModel()
        {
            _loggerFactory = LoggerFactory;
            _busyStack = new BusyStack(hasItems => IsBusy = hasItems);

            Runners = new ObservableCollection<SqlRunnerViewModel>();
            AddCommand = AsyncCommand.Create(AddInternal, () => true);
            RemoveCommand = AsyncCommand.Create(RemoveInternal, CanRemove);
        }

        private Task AddInternal(CancellationToken token)
        {
            using (_busyStack.GetToken())
            {
                Runners.Add(new SqlRunnerViewModel(_loggerFactory));

                return Task.CompletedTask;
            }
        }

        private Task RemoveInternal(CancellationToken token)
        {
            using (_busyStack.GetToken())
            {
                Runners.Remove(SelectedRunner);

                return Task.CompletedTask;
            }
        }

        private bool CanRemove()
        {
            return !(SelectedRunner is null)
                && !(Runners is null)
                && Runners.Contains(SelectedRunner);
        }

        private static ILogger LoggerFactory(string databaseName)
        {
            return new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Console()
                        .WriteTo.File("logs\\databaseName.log", rollingInterval: RollingInterval.Day)
                        .CreateLogger();
        }
    }
}
