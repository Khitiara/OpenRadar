using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace OpenRadar.SimClient
{
    public class HWndHookRegistration : IDisposable
    {
        private HwndSourceHook _hook;
        private HwndSource     _source;
        public bool Disposed { get; private set; }

        public HWndHookRegistration(HwndSourceHook hook, HwndSource source) {
            _hook = hook;
            _source = source;
            source.AddHook(_hook);
        }

        public void Dispose() {
            if (Disposed) return;
            Disposed = true;
            _source.RemoveHook(_hook);
        }
    }

    public static class HwndEx
    {
        public static HWndHookRegistration RegisterHWndHook(this Visual visual, HwndSourceHook hook) {
            return (PresentationSource.FromVisual(visual) as HwndSource)!.Register(hook);
        }

        public static HWndHookRegistration Register(this HwndSource source, HwndSourceHook hook) {
            return new HWndHookRegistration(hook, source);
        }
    }
}