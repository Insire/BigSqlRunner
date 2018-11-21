using MahApps.Metro.Controls.Dialogs;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace BigRunner.WpfApp
{
    public sealed class ShellViewModel : ObservableObject
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly BusyStack _busyStack;
        private readonly Func<string, LoggerConfiguration> _loggerFactory;

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

        public IAsyncCommand AddCommand { get; }
        public IAsyncCommand RemoveCommand { get; }

        public IAsyncCommand EditRunnerNameCommand { get; }
        public IAsyncCommand EditSelectedRunnerNameCommand { get; }

        public ShellViewModel()
        {
            _dialogCoordinator = DialogCoordinator.Instance;
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
                Runners.Add(new SqlRunnerViewModel(_dialogCoordinator, _loggerFactory, "Runner " + Runners.Count));

                if (SelectedRunner is null)
                    SelectedRunner = Runners[0];

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

        private static LoggerConfiguration LoggerFactory(string databaseName)
        {
            return new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .WriteTo.Console()
                        .WriteTo.File($"logs\\{databaseName}.log", rollingInterval: RollingInterval.Day);
        }
    }
}
