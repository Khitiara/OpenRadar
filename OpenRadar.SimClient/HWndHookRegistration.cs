using System;
using System.Windows.Interop;

namespace OpenRadar.SimClient
{
    /// <summary>
    /// A disposable registration of a win32 message pump handler. Used to process SimConnect messages on the UI event loop
    /// </summary>
    public class HWndHookRegistration : IDisposable
    {
        private HwndSourceHook _hook;
        private HwndSource     _source;
        /// <summary>
        /// Whether this instance has been disposed.
        /// </summary>
        public bool Disposed { get; private set; }

        public HWndHookRegistration(HwndSourceHook hook, HwndSource source) {
            _hook = hook;
            _source = source;
            source.AddHook(_hook);
        }
        
        /// <inheritdoc />
        public void Dispose() {
            if (Disposed) return;
            GC.SuppressFinalize(this);
            Disposed = true;
            _source.RemoveHook(_hook);
        }
    }

    /// <summary>
    /// Extensions to <see cref="HwndSource"/> to support <see cref="HWndHookRegistration"/>
    /// </summary>
    public static class HwndEx
    {
        /// <summary>
        /// Adds an <see cref="HwndSourceHook"/> to the event loop of the given <see cref="HwndSource"/>
        /// </summary>
        /// <param name="source">The HWND source</param>
        /// <param name="hook">The delegate to register to the event loop</param>
        /// <returns>An object that will remove the event loop handler when disposed</returns>
        public static HWndHookRegistration Register(this HwndSource source, HwndSourceHook hook) {
            return new HWndHookRegistration(hook, source);
        }
    }
}