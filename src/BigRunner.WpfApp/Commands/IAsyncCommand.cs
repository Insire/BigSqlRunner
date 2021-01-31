using System.Threading.Tasks;
using System.Windows.Input;

namespace BigRunner.WpfApp
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync(object parameter);
    }
}
