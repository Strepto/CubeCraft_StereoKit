using System.Collections.Generic;
using StereoKit;

namespace StereoKitApp.Utils.EqualityComparers
{
    public class Vec3EqualityComparer : IEqualityComparer<Vec3>
    {
        public static readonly Vec3EqualityComparer Instance = new Vec3EqualityComparer();

        public bool Equals(Vec3 x, Vec3 y)
        {
            return x.x.Equals(y.x) && x.y.Equals(y.y) && x.z.Equals(y.z);
        }

        public int GetHashCode(Vec3 obj)
        {
            unchecked
            {
                var hashCode = obj.x.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.y.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.z.GetHashCode();
                return hashCode;
            }
        }
    }
}
