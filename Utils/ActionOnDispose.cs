using System;

namespace StereoKitApp.Utils
{
    /// <summary>
    /// Dispose this to trigger an action on dispose.
    ///
    /// Consider wrapping in a using scope.
    /// </summary>
    /// <remarks>
    /// This is often used to trigger "end" of a scope.
    /// UI.BeginWindow
    /// ----
    /// UI.EndWindow {-- Triggered by an <see cref="ActionOnDispose"/>
    /// </remarks>
    public class ActionOnDispose : IDisposable
    {
        private readonly Action _actionToTriggerOnDispose;

        public ActionOnDispose(Action actionToTriggerOnDispose)
        {
            _actionToTriggerOnDispose = actionToTriggerOnDispose;
        }

        public void Dispose()
        {
            _actionToTriggerOnDispose.Invoke();
        }
    }
}
