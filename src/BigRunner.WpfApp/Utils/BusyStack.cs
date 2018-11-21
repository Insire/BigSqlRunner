using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows;

namespace BigRunner.WpfApp
{
    public sealed class BusyStack
    {
        private readonly ConcurrentBag<BusyToken> _items;

        private readonly Action<bool> _onChanged;

        private BusyStack()
        {
            _items = new ConcurrentBag<BusyToken>();
        }

        public BusyStack(Action<bool> onChanged)
            : this()
        {
            _onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
        }

        public async Task<bool> Pull()
        {
            var result = _items.TryTake(out var token);

            if (result)
                await InvokeOnChanged().ConfigureAwait(false);

            return result;
        }

        public async Task Push(BusyToken token)
        {
            _items.Add(token);

            await InvokeOnChanged().ConfigureAwait(false);
        }

        public bool HasItems()
        {
            return _items?.TryPeek(out var token) ?? false;
        }

        public BusyToken GetToken()
        {
            return new BusyToken(this);
        }

        private async Task InvokeOnChanged()
        {
            await Application.Current.Dispatcher.InvokeAsync(() => _onChanged(HasItems()));
        }
    }
}
