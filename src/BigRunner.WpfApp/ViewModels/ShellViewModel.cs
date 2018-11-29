using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BigRunner.WpfApp
{
    // TODO serialize/save runners to file
    public sealed class ShellViewModel : ObservableObject
    {
        private const string RunnerCacheFileName = "BigRunnerStore.json";

        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly BusyStack _busyStack;
        private readonly Func<string, LoggerConfiguration> _loggerFactory;

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            private set { SetValue(ref _isBusy, value); }
        }

        private SqlRunnerViewModel _selectedRunner;
        public SqlRunnerViewModel SelectedRunner
        {
            get { return _selectedRunner; }
            set { SetValue(ref _selectedRunner, value); }
        }

        public ObservableCollection<SqlRunnerViewModel> Runners { get; }

        public IAsyncCommand AddCommand { get; }
        public IAsyncCommand RemoveCommand { get; }
        public IAsyncCommand EditRunnerNameCommand { get; }
        public IAsyncCommand EditSelectedRunnerNameCommand { get; }
        public IAsyncCommand SaveCommand { get; }
        public IAsyncCommand LoadCommand { get; }

        public ShellViewModel()
        {
            _dialogCoordinator = DialogCoordinator.Instance;
            _loggerFactory = LoggerFactory;
            _busyStack = new BusyStack(hasItems => IsBusy = hasItems);

            Runners = new ObservableCollection<SqlRunnerViewModel>();
            AddCommand = AsyncCommand.Create(AddInternal, () => true);
            RemoveCommand = AsyncCommand.Create(RemoveInternal, CanRemove);
            SaveCommand = AsyncCommand.Create(SaveInternalAsync, CanSave);
            LoadCommand = AsyncCommand.Create(LoadInternal, CanLoad);
        }

        private async Task SaveInternalAsync(CancellationToken token)
        {
            var runnerModels = Runners.Select(p => p.GetModel()).ToArray();
            var json = JsonConvert.SerializeObject(runnerModels);

            await Task.Run(() => File.WriteAllText(RunnerCacheFileName, json, Encoding.Default)).ConfigureAwait(false);
        }

        private bool CanSave()
        {
            return true;
        }

        private async Task LoadInternal(CancellationToken token)
        {
            var json = await Task.Run(() => File.ReadAllText(RunnerCacheFileName, Encoding.Default));
            var runnerModels = JsonConvert.DeserializeObject<SqlRunnerModel[]>(json);

            if (runnerModels is null)
                return;

            for (var i = 0; i < runnerModels.Length; i++)
            {
                var model = runnerModels[i];
                var viewModel = new SqlRunnerViewModel(_dialogCoordinator, this, _loggerFactory, model.Name);
                viewModel.OptionsViewModel.ConnectionString = model.OptionsModel.ConnectionString;
                viewModel.OptionsViewModel.CustomTerminator = model.OptionsModel.CustomTerminator;
                viewModel.OptionsViewModel.SqlFilePath = model.OptionsModel.SqlFilePath;
                viewModel.OptionsViewModel.Terminator = model.OptionsModel.Terminator;

                Runners.Add(viewModel);
            }
        }

        private bool CanLoad()
        {
            return File.Exists(RunnerCacheFileName);
        }

        private Task AddInternal(CancellationToken token)
        {
            using (_busyStack.GetToken())
            {
                Runners.Add(new SqlRunnerViewModel(_dialogCoordinator, this, _loggerFactory, "Runner " + Runners.Count));

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