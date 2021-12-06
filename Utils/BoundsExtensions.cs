using System;
using JetBrains.Annotations;
using StereoKit;

namespace StereoKitApp.Utils
{
    public static class BoundsExtensions
    {
        /// <summary>
        /// Copy the bounds, and return a new Bounds with position and extents scaled.
        /// </summary>
        [Pure]
        public static Bounds Scaled(this Bounds bounds, float scaleUniform)
        {
            return Scaled(bounds, Vec3.One * scaleUniform);
        }

        /// <summary>
        /// Copy the bounds, and return a new Bounds with position and extents scaled.
        /// </summary>
        [Pure]
        public static Bounds Scaled(this Bounds bounds, Vec3 scale)
        {
            var outBounds = bounds;
            outBounds.center *= scale;
            outBounds.dimensions *= scale;
            return outBounds;
        }

        /// <summary>
        /// Copy the bounds, and Convert the result to the Local Hierarchy.
        /// </summary>
        [Obsolete("Not actually obsolete, but I have not tested that this works yet...")]
        [Pure]
        public static Bounds ToLocal(this Bounds bounds)
        {
            var scale = Hierarchy.ToLocal(Vec3.One);
            var outBounds = bounds;
            outBounds.center = Hierarchy.ToLocal(outBounds.center) * scale;
            outBounds.dimensions = Hierarchy.ToLocal(outBounds.dimensions) * scale;
            return outBounds;
        }
    }
}
