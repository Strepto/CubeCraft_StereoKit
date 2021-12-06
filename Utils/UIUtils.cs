using System;
using JetBrains.Annotations;
using StereoKit;

namespace StereoKitApp.Utils
{
    public static class UIUtils
    {
        /// <inheritdoc cref="UI.WindowBegin(string,ref StereoKit.Pose,StereoKit.Vec2,StereoKit.UIWin,StereoKit.UIMove)"/>
        [MustUseReturnValue]
        public static IDisposable UIWindowScope(
            string text,
            ref Pose pose,
            Vec2 size,
            UIWin windowType,
            UIMove moveType
        )
        {
            UI.WindowBegin(text, ref pose, size, windowType, moveType);
            return new ActionOnDispose(UI.WindowEnd);
        }

        /// <inheritdoc cref="UI.EnableFarInteract"/>
        /// <summary>
        /// Enable or disable the Far interaction within this scope. Returns to the previous <see cref="UI.EnableFarInteract"/> state when disposed.
        /// </summary>
        /// <param name="enable">Set this to True, or False for the Enable of Far Interact in this scope.</param>
        [MustUseReturnValue]
        public static IDisposable EnableFarInteractScope(bool enable)
        {
            var prevValue = UI.EnableFarInteract;
            UI.EnableFarInteract = enable;
            return new ActionOnDispose(
                () =>
                {
                    UI.EnableFarInteract = prevValue;
                }
            );
        }

        /// <inheritdoc cref="UI.HandleBegin(string, ref Pose, Bounds, bool, UIMove)"/>
        [MustUseReturnValue]
        public static ActionOnDispose UIHandleScope(
            string id,
            ref Pose pose,
            Bounds bounds,
            bool drawBounds,
            UIMove moveType,
            out bool wasInteractedWithThisFrame
        )
        {
            wasInteractedWithThisFrame = UI.HandleBegin(id, ref pose, bounds, drawBounds, moveType);
            return new ActionOnDispose(UI.HandleEnd);
        }
    }
}
