using System.IO;
using System.Linq;
using System.Numerics;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using StereoKit;
using StereoKitApp.Painting;
using StereoKitApp.Utils.Colors;

namespace StereoKitApp.Features.Gltf
{
    using VERTEXDATA = VertexPositionNormal;
    public static class GltfExportBox3DModel
    {
        /// <summary>
        /// Exports the model to a GLB Stream.
        /// </summary>
        /// <param name="modelData"></param>
        /// <param name="stream"></param>
        public static void ExportModelAsGlbToStream(Box3DModelData modelData, Stream stream)
        {
            var sceneBuilder = new SharpGLTF.Scenes.SceneBuilder();
            var meshBuilder = new MeshBuilder<VERTEXDATA>();
            var materialLookup = modelData.ActivePixelDatas
                .GroupBy(x => x.Color)
                .ToDictionary(
                    k =>
                    {
                        var c = k.Key;
                        return c;
                    },
                    v =>
                    {
                        var c = v.Key;
                        var vecColor = c.ToVec4();
                        return new MaterialBuilder()
                            .WithMetallicRoughnessShader()
                            .WithChannelParam("BaseColor", vecColor);
                    }
                );

            foreach (var pixelData in modelData.ActivePixelDatas)
            {
                var material = materialLookup[pixelData.Color];
                var positionOffset = (Matrix4x4)Matrix.TR(
                    pixelData.Position,
                    Quaternion.Concatenate(
                        // Readjust to gltf orientation (Inverse of the StereoKit gltf correction: matrix gltf_orientation_correction = matrix_trs(vec3_zero, quat_from_angles(0, 180, 0));)
                        Quat.FromAngles(0, -180, 0),
                        pixelData.CubeRotation.q
                    )
                );

                meshBuilder.AddMesh(
                    CreateMesh(material, pixelData.VoxelKind),
                    m => m.WithBaseColor(pixelData.Color.ToVec4()),
                    input =>
                        VertexBuilder<VERTEXDATA, VertexEmpty, VertexEmpty>
                            .CreateFrom(input)
                            .TransformedBy(positionOffset)
                );
            }

            sceneBuilder.AddRigidMesh(meshBuilder, Matrix4x4.Identity);

            var gltfScene = sceneBuilder.ToGltf2();
            gltfScene.WriteGLB(stream);
        }

        // This can be very much optimized bu sharing meshes, and using a instanced material for coloring. But we export tiny-tiny meshes so its not a priority...
        private static MeshBuilder<VERTEXDATA> CreateMesh(
            MaterialBuilder material,
            Box3DModelData.VoxelKind meshType
        )
        {
            var mesh = new MeshBuilder<VERTEXDATA>();
            var cubePrim = mesh.UsePrimitive(material);

            Mesh cube = AssetLookup.Models.GetCubeModel(meshType).Visuals[0].Mesh;
            // var cube = Mesh.GenerateRoundedCube(Vec3.One, 0.1f);

            var indices = cube.GetInds();
            var vertices = cube.GetVerts();
            for (int i = 0; i < indices.Length;)
            {
                var vertA = vertices[indices[i++]];
                var vertB = vertices[indices[i++]];
                var vertC = vertices[indices[i++]];
                cubePrim.AddTriangle(
                    new VERTEXDATA(vertA.pos, vertA.norm),
                    new VERTEXDATA(vertB.pos, vertB.norm),
                    new VERTEXDATA(vertC.pos, vertC.norm)
                );
            }

            return mesh;
        }
    }
}
