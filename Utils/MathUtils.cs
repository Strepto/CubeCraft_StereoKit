using System;
using StereoKit;

namespace StereoKitApp.Utils
{
    public static class MathUtils
    {
        public static float DegreesToRadians(float degrees)
        {
            float rad = degrees * SKMath.Pi / 180.0f;
            return rad;
        }

        public static float RadiansToDegrees(float radians)
        {
            float degrees = (radians * SKMath.Pi) / 180f;
            return degrees;
        }

        public static float Clamp(float value, float min, float max)
        {
            if (min > max)
            {
                throw new ArgumentException($"The min ({min}) was larger than the max {max}");
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        /// <summary>
        /// Normalize an angle in Degrees, to be within 0-360 range.
        /// </summary>
        /// <param name="angleDegrees"></param>
        /// <returns></returns>
        public static float NormalizeDegreesAngle360(float angleDegrees)
        {
            angleDegrees = angleDegrees % 360;

            // force it to be the positive remainder, so that 0 <= angle < 360
            angleDegrees = (angleDegrees + 360) % 360;

            return angleDegrees;
        }

        /// <summary>
        /// Normalize an angle in Degrees, to be within -180 -> 180 range.
        /// </summary>
        /// <param name="angleDegrees"></param>
        /// <returns></returns>
        public static float NormalizeDegreesAngle180(float angleDegrees)
        {
            angleDegrees = NormalizeDegreesAngle360(angleDegrees);

            // force into the minimum absolute value residue class, so that -180 < angle <= 180
            if (angleDegrees > 180)
                angleDegrees -= 360;
            return angleDegrees;
        }
    }
}
