using MahApps.Metro.Controls;
using System.Threading;
using System.Threading.Tasks;

namespace BigRunner.WpfApp
{
    public partial class Shell : MetroWindow
    {
        public IAsyncCommand OpenNavigationCommand { get; }

        public Shell()
        {
            OpenNavigationCommand = AsyncCommand.Create(OpenNavigationInternal, CanOpenNavigation);
            InitializeComponent();
        }

        private Task OpenNavigationInternal(CancellationToken token)
        {
            NavigationFlyout.IsOpen = true;
            return Task.CompletedTask;
        }

        private bool CanOpenNavigation()
        {
            return !NavigationFlyout.IsOpen;
        }
    }
}
