using System.Numerics;
using StereoKit;

namespace StereoKitApp.Utils.Colors
{
    /// <summary>
    /// Predefined colors for easier coding.
    /// </summary>
    public static class Colors
    {
        public static readonly Color Magenta = new Color(1, 0, 1);

        /// <summary>
        /// Convert a color to a Vec4. XYZW -> RGBA
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Vector4 ToVec4(this Color color)
        {
            return new Vec4(color.r, color.g, color.b, color.a);
        }
    }
}
