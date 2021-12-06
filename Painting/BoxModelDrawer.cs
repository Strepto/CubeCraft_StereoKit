using System.Collections.Generic;
using StereoKit;
using StereoKitApp.Utils;

namespace StereoKitApp.Painting
{
    public static class BoxModelDrawer
    {
        public static void Draw(Box3DModelData data, Vec3 localOffset, float localScaleUniform)
        {
            Draw(data.ActivePixelDatas, localOffset, Vec3.One * localScaleUniform);
        }
        public static void Draw(Box3DModelData data, Vec3 localOffset, Vec3 localScale)
        {
            Draw(data.ActivePixelDatas, localOffset, localScale);
        }

        public static void Draw(
            IReadOnlyList<Box3DModelData.PixelData> data,
            Vec3 localOffset,
            Vec3 localScale
        )
        {
            using (HierarchyUtils.HierarchyItemScope(Matrix.TS(localOffset, localScale)))
            {
                foreach (var activePixel in data)
                {
                    DrawSingle(activePixel);
                }
            }
        }

        /// <summary>
        /// Draw a single PixelData at its position.
        /// You will need to wrap this with a transform for it to be useful
        /// </summary>
        /// <param name="pixelData"></param>
        /// <param name="cubeColorOverride"></param>
        public static void DrawSingle(
            Box3DModelData.PixelData pixelData,
            Color? cubeColorOverride = null
        )
        {
            DrawSingle(
                pixelData.VoxelKind,
                pixelData.Position,
                pixelData.CubeRotation,
                cubeColorOverride ?? pixelData.Color
            );
        }

        public static void DrawSingle(
            Box3DModelData.VoxelKind cubeType,
            Vec3 pos,
            Quat rot,
            Color color
        )
        {
            AssetLookup.Models.GetCubeModel(cubeType).Draw(Matrix.TR(pos, rot), color);
        }
    }
}
