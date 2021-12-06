using JetBrains.Annotations;
using StereoKit;

namespace StereoKitApp.Utils
{
    [PublicAPI]
    public enum ClientBuild
    {
        Unknown,
        Debug,
        Release,
        Debug2,
        Debug3
    }

    public static class MultiplayerDevUtils
    {
        [PublicAPI]
        public static ClientBuild GetClientBuildType()
        {
#if DEBUG
            return ClientBuild.Debug;
#elif DEBUG2
            return ClientBuild.Debug2;
#elif DEBUG3
            return ClientBuild.Debug3;
#elif RELEASE
            return ClientBuild.Release;
#else
            return ClientBuild.Unknown;
#endif
        }

        /// <summary>
        /// Some multiplayer testing is only possible while in flatscreen mode as
        /// OpenXR does not allow multiple instances
        /// </summary>
        /// <returns></returns>
        [PublicAPI]
        public static bool IsInDesktopFlatMonitorMode()
        {
            return SK.ActiveDisplayMode == DisplayMode.Flatscreen;
        }
    }
}
