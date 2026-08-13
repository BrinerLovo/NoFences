using System;
using System.Windows.Forms;

namespace NoFences.Util
{
    /// <summary>
    /// Coalesces rapid UI-thread requests and runs only the most recent action
    /// after the configured quiet period.
    /// </summary>
    public sealed class ThrottledExecution : IDisposable
    {
        private readonly Timer timer;
        private Action pendingAction;
        private bool disposed;

        public ThrottledExecution(TimeSpan delay)
        {
            int interval = Math.Max(1, (int)Math.Ceiling(delay.TotalMilliseconds));
            timer = new Timer { Interval = interval };
            timer.Tick += Timer_Tick;
        }

        public void Run(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            ThrowIfDisposed();

            pendingAction = action;
            timer.Stop();
            timer.Start();
        }

        public void Flush()
        {
            if (disposed || pendingAction == null)
                return;

            timer.Stop();
            Action action = pendingAction;
            pendingAction = null;
            action();
        }

        public void Cancel()
        {
            if (disposed)
                return;

            timer.Stop();
            pendingAction = null;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Flush();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ThrottledExecution));
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            pendingAction = null;
            timer.Stop();
            timer.Tick -= Timer_Tick;
            timer.Dispose();
        }
    }
}
