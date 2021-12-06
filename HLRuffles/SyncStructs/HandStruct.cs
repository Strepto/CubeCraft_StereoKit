using System;
using StereoKit;
using StereoKitApp.Utils.EqualityComparers;

namespace StereoKitApp.HLRuffles.SyncStructs
{
    public struct HandStruct : IEquatable<HandStruct>
    {
        public Vec3 ThumbTip;
        public Vec3 IndexTip;
        public Vec3 MiddleTip;
        public Vec3 RingTip;
        public Vec3 LittleTip;

        public Vec3 ThumbMiddle;
        public Vec3 IndexMiddle;
        public Vec3 MiddleMiddle;
        public Vec3 RingMiddle;
        public Vec3 LittleMiddle;

        public Vec3 ThumbProximal;
        public Vec3 IndexProximal;
        public Vec3 MiddleProximal;
        public Vec3 RingProximal;
        public Vec3 LittleProximal;

        public bool Equals(HandStruct other)
        {
            var vec3Comp = Vec3EqualityComparer.Instance;
            return vec3Comp.Equals(ThumbTip, other.ThumbTip)
                && vec3Comp.Equals(IndexTip, other.IndexTip)
                && vec3Comp.Equals(MiddleTip, other.MiddleTip)
                && vec3Comp.Equals(RingTip, other.RingTip)
                && vec3Comp.Equals(LittleTip, other.LittleTip)
                && vec3Comp.Equals(ThumbMiddle, other.ThumbMiddle)
                && vec3Comp.Equals(IndexMiddle, other.IndexMiddle)
                && vec3Comp.Equals(MiddleMiddle, other.MiddleMiddle)
                && vec3Comp.Equals(RingMiddle, other.RingMiddle)
                && vec3Comp.Equals(LittleMiddle, other.LittleMiddle)
                && vec3Comp.Equals(ThumbProximal, other.ThumbProximal)
                && vec3Comp.Equals(IndexProximal, other.IndexProximal)
                && vec3Comp.Equals(MiddleProximal, other.MiddleProximal)
                && vec3Comp.Equals(RingProximal, other.RingProximal)
                && vec3Comp.Equals(LittleProximal, other.LittleProximal);
        }

        public override bool Equals(object? obj)
        {
            return obj is HandStruct other && Equals(other);
        }

        public override int GetHashCode()
        {
            var vec3Comp = Vec3EqualityComparer.Instance;
            unchecked
            {
                var hashCode = vec3Comp.GetHashCode(ThumbTip);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(IndexTip);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(MiddleTip);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(RingTip);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(LittleTip);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(ThumbMiddle);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(IndexMiddle);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(MiddleMiddle);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(RingMiddle);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(LittleMiddle);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(ThumbProximal);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(IndexProximal);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(MiddleProximal);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(RingProximal);
                hashCode = (hashCode * 397) ^ vec3Comp.GetHashCode(LittleProximal);
                return hashCode;
            }
        }
    }
}
