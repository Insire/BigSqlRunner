using System;

namespace BigRunner.WpfApp
{
    public sealed class BusyToken : IDisposable
    {
        private readonly BusyStack _stack;

        public BusyToken(BusyStack stack)
        {
            _stack = stack ?? throw new ArgumentNullException(nameof(stack));
            stack.Push(this).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            _stack.Pull().GetAwaiter().GetResult();
        }
    }
}
