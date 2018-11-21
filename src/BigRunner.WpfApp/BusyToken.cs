using System;

namespace BigRunner.WpfApp
{
    public sealed class BusyToken : IDisposable
    {
        private readonly BusyStack _stack;

        public BusyToken(BusyStack stack)
        {
            _stack = stack ?? throw new ArgumentNullException(nameof(stack));
            _ = stack.Push(this);
        }

        public void Dispose()
        {
            _ = _stack.Pull();
        }
    }
}
