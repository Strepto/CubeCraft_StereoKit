using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StereoKit;
using StereoKitApp.Painting;

namespace StereoKitApp
{
    public static class AssetLookup
    {
        static string GetFullPathForAssetFile(string relativeToAssetFolderPath)
        {
            // FIXME: THIS DOES NOT WORK FOR ANDROID
            return Path.GetFullPath(
                Path.Combine(SK.Settings.assetsFolder, relativeToAssetFolderPath)
            );
        }
        public static class Sprites
        {
            public const string RocketId = "rocket_1f680.png";
            public static Sprite LoadRocketSprite => Sprite.FromFile(RocketId, SpriteType.Single);
        }

        public static class Models
        {
            private static List<Model>? _cubeModels;

            public static Model LoadX => Model.FromFile("Models/X.glb");
            public static Model LoadColorPickerShell => Model.FromFile("Models/ColorPicker.glb");
            public static Model LoadColorPickerInside =>
                Model.FromFile("Models/ColorPickerInside.glb");

            public static Model ColorPaletteBoard => Model.FromFile("Models/MenuBoard.glb");
            public static Model ColorPickerBoard => Model.FromFile("Models/ColorPickerBoard.glb");
            public static Model RoundedButton => Model.FromFile("Models/RoundedButton.glb");
            public static Model PaintButtonNormal => Model.FromFile("Models/PaintButtonNormal.glb");
            public static Model PaintButtonPushed => Model.FromFile("Models/PaintButtonPushed.glb");
            public static Model EraseButtonNormal => Model.FromFile("Models/EraseButtonNormal.glb");
            public static Model EraseButtonPushed => Model.FromFile("Models/EraseButtonPushed.glb");
            public static Model ColorPickerButtonNormal =>
                Model.FromFile("Models/ColorPickerButtonNormal.glb");
            public static Model ColorPickerButtonPushed =>
                Model.FromFile("Models/ColorPickerButtonPushed.glb");
            public static Model NukeButton1 => Model.FromFile("Models/NukeButton1.glb");
            public static Model NukeButton2 => Model.FromFile("Models/NukeButton2.glb");
            public static Model NukeButton3 => Model.FromFile("Models/NukeButton3.glb");
            public static Model SaveButton => Model.FromFile("Models/SaveButton.glb");
            public static Model LoadButton => Model.FromFile("Models/LoadButton.glb");

            public static Model GetCubeModel(Box3DModelData.VoxelKind voxelKind)
            {
                _cubeModels ??= LoadCubeModels().ToList();

                return _cubeModels[(byte)voxelKind];
            }

            private static IReadOnlyList<Model> LoadCubeModels()
            {
                var materialWithTransparency = Material.PBR.Copy();
                materialWithTransparency.Transparency = Transparency.Blend;

                var cubeModels = new List<Model>
                {
                    Model.FromFile("Models/CubeModel.glb"),
                    Model.FromFile("Models/RoundedCubeModel.glb"),
                    Model.FromFile("Models/RoundedEdgeCubeModel.glb"),
                    Model.FromFile("Models/CutCubeModel.glb"),
                    Model.FromFile("Models/RoundedTopCubeModel.glb"),
                    Model.FromFile("Models/HalfCubeModel.glb"),
                    Model.FromFile("Models/ChippedCubeModel.glb"),
                    Model.FromFile("Models/CutEdgeCubeModel.glb"),
                    Model.FromFile("Models/CutTopCubeModel.glb")
                };

                foreach (Model model in cubeModels)
                {
                    foreach (var modelVisual in model.Visuals)
                    {
                        modelVisual.Material = materialWithTransparency;
                    }
                }

                return cubeModels;
            }
        }

        // ReSharper disable once UnusedType.Global <-- Should be used later.
        public static class Data
        {
            // TODO: Loading file assets on android is weird. If adding non SK data to assets, you need to find a way for them to load on Android.
        }

        public static class Sounds
        {
            private static Sound[] LoadSoundSequence(string folder, string fileBaseName)
            {
                var path = Path.Combine(SK.Settings.assetsFolder ?? "Assets", folder);

                if (!Directory.Exists(path))
                {
                    Console.WriteLine(
                        $"Could not find the path: {path}. NOTE: Some platforms(ANDROID) really hate listing content files. A method to fis this can be found here: https://docs.microsoft.com/nb-no/xamarin/xamarin-forms/data-cloud/data/files?tabs=windows but I did not fix it as it took too long."
                    );
                    return Array.Empty<Sound>();
                }
                var tones = new List<Sound>();

                foreach (var file in Directory.GetFiles(path, fileBaseName + "*"))
                {
                    var assetFolderRelativePath = file.Split(
                            new[] { SK.Settings.assetsFolder },
                            StringSplitOptions.None
                        )
                        .Last();
                    tones.Add(Sound.FromFile(assetFolderRelativePath));
                }

                return tones.ToArray();
            }

            public static class UI
            {
                public static readonly Sound[] Pips = new[]
                {
                    Sound.FromFile("Sounds/UI/pip_2.wav"),
                    Sound.FromFile("Sounds/UI/pip_3.wav"),
                    Sound.FromFile("Sounds/UI/pip_4.wav"),
                    Sound.FromFile("Sounds/UI/pip_5.wav"),
                };

                public static readonly Sound Pop1 = Sound.FromFile("Sounds/UI/Pop1.wav");
                public static readonly Sound Explode1 = Sound.FromFile("Sounds/UI/Explode1.wav");
            }
        }
    }
}
