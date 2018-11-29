using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BigRunner.WpfApp
{
    public partial class Shell : MetroWindow
    {
        private readonly ShellViewModel _shellViewModel;

        public IAsyncCommand OpenNavigationCommand { get; }
        public IAsyncCommand OpenDashboardCommand { get; }

        public bool IsDashboardOpen
        {
            get { return (bool)GetValue(IsDashboardOpenProperty); }
            set { SetValue(IsDashboardOpenProperty, value); }
        }

        public static readonly DependencyProperty IsDashboardOpenProperty = DependencyProperty.Register(
            nameof(IsDashboardOpen),
            typeof(bool),
            typeof(Shell),
            new PropertyMetadata(true));

        public Shell()
        {
            OpenNavigationCommand = AsyncCommand.Create(OpenNavigationInternal, CanOpenNavigation);
            OpenDashboardCommand = AsyncCommand.Create(OpenDashboardInternal, CanOpenDashboard);
            InitializeComponent();

            _shellViewModel = (ShellViewModel)FindResource("ShellViewModel");
            DialogParticipation.SetRegister(this, _shellViewModel);
        }

        private Task OpenNavigationInternal(CancellationToken token)
        {
            NavigationFlyout.IsOpen = true;
            IsDashboardOpen = false;

            return Task.CompletedTask;
        }

        private bool CanOpenNavigation()
        {
            return !NavigationFlyout.IsOpen;
        }

        private Task OpenDashboardInternal(CancellationToken token)
        {
            IsDashboardOpen = true;
            NavigationFlyout.IsOpen = false;

            return Task.CompletedTask;
        }

        private bool CanOpenDashboard()
        {
            return !IsDashboardOpen;
        }
    }
}