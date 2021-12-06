using JetBrains.Annotations;
using StereoKit;
using System;

namespace StereoKitApp.Utils
{
    public static class HierarchyUtils
    {
        /// <summary>Dispose the returned value when you want to close the Item scope</summary>
        /// <inheritdoc cref="Hierarchy.Push"/>
        [MustUseReturnValue]
        public static IDisposable HierarchyItemScope(in Matrix transform)
        {
            Hierarchy.Push(in transform);

            return new ActionOnDispose(Hierarchy.Pop);
        }
        // Not sure how well the BoundsToWorld code below worked. I did not actually need it, so I wont try to fix it right now.

        // /// <summary>Convert the bounds from Local to World space</summary>
        // /// <param name="bounds"></param>
        // /// <param name="worldScale">I have not found a way to figure out the world scale at this point in the hierarchy. Figure it out yourself! :)</param>
        // [MustUseReturnValue]
        // public static Bounds ToWorld(in Bounds bounds, Vec3 worldScale)
        // {
        //     return new Bounds(Hierarchy.ToWorld(bounds.center), bounds.dimensions * worldScale);
        // }
        // /// <summary>Convert the bounds from Local to World space</summary>
        // /// <param name="bounds"></param>
        // /// <param name="worldScale">I have not found a way to figure out the world scale at this point in the hierarchy. Figure it out yourself! :)</param>
        // [MustUseReturnValue]
        // public static Bounds ToWorld(in Bounds bounds, float worldScale)
        // {
        //     return new Bounds(Hierarchy.ToWorld(bounds.center), bounds.dimensions * worldScale);
        // }
    }
}
