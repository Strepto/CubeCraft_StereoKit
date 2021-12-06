using System.Collections.Generic;
using StereoKit;

namespace StereoKitApp.Painting
{
    internal static class GridUtils
    {
        public static Vec3[] GetGridLayout(uint dimensions = 8)
        {
            var gridPositions = new List<Vec3>(); //[(int)Math.Pow(dimensions, 3)];
            for (int x = 0; x < dimensions; x++)
            {
                for (int y = 0; y < dimensions; y++)
                {
                    for (int z = 0; z < dimensions; z++)
                    {
                        gridPositions.Add(new Vec3(x, y, z));
                    }
                }
            }

            return gridPositions.ToArray();
        }
    }
}
