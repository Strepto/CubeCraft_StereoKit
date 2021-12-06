using System.Collections.Generic;
using StereoKit;

namespace StereoKitApp.Utils.EqualityComparers
{
    public class QuatEqualityComparer : IEqualityComparer<Quat>
    {
        public bool Equals(Quat x, Quat y)
        {
            return x.x.Equals(y.x) && x.y.Equals(y.y) && x.z.Equals(y.z) && x.w.Equals(y.w);
        }

        public int GetHashCode(Quat obj)
        {
            unchecked
            {
                var hashCode = obj.x.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.y.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.z.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.w.GetHashCode();
                return hashCode;
            }
        }
    }
}
