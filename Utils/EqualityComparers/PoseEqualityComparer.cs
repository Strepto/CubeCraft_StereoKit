using System.Collections.Generic;
using StereoKit;

namespace StereoKitApp.Utils.EqualityComparers
{
    public class PoseEqualityComparer : IEqualityComparer<Pose>
    {
        public static readonly PoseEqualityComparer Instance = new PoseEqualityComparer();

        private readonly Vec3EqualityComparer _vec3EqualityComparer = new Vec3EqualityComparer();
        private readonly QuatEqualityComparer _quatEqualityComparer = new QuatEqualityComparer();
        public bool Equals(Pose x, Pose y)
        {
            return _vec3EqualityComparer.Equals(x.position, y.position)
                && _quatEqualityComparer.Equals(x.orientation, y.orientation);
        }

        public int GetHashCode(Pose obj)
        {
            unchecked
            {
                var hashCode = _vec3EqualityComparer.GetHashCode(obj.position);
                hashCode = (hashCode * 397) ^ _quatEqualityComparer.GetHashCode(obj.orientation);
                return hashCode;
            }
        }
    }
}
