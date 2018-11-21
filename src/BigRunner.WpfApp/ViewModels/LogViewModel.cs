using Serilog;
using Serilog.Events;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace BigRunner.WpfApp
{
    // TODO add filtering per commands
    public sealed class LogViewModel : ObservableObject
    {
        private ObservableCollection<LogEvent> _items;
        public ObservableCollection<LogEvent> Items
        {
            get { return _items; }
            private set { SetValue(ref _items, value); }
        }

        public LogViewModel()
        {
            _items = new ObservableCollection<LogEvent>();
        }

        public LogViewModel(LoggerConfiguration configuration)
            : this()
        {
            configuration
                .WriteTo.Observers(events => events
                .Do(evt => _items.Insert(0, evt))
                .Subscribe());
        }
    }
}
