using System;
using System.Collections.Generic;
using StereoKit;

namespace StereoKitApp.Utils
{
    public static class ColorSpriteUtils
    {
        private static readonly Dictionary<Color, Sprite> ColorTexLookup = new Dictionary<
            Color,
            Sprite
        >();

        /// <summary>
        /// Creates a 1x1 Texture with the given color. Can be used for simple color visualization.
        /// </summary>
        /// <remarks>
        /// The Tex is cached, so all lookups for the given color is always the same Tex.
        /// </remarks>
        /// <param name="color"></param>
        /// <returns>A 1x1 Tex with the given Color</returns>
        public static Sprite LookupColorSprite(Color color)
        {
            // TODO: THIS CODE LEAKS!!!
            var gotValue = ColorTexLookup.TryGetValue(color, out var foundColor);

            if (!gotValue)
            {
                foundColor = ColorTexLookup[color] = Sprite.FromTex(
                    Tex.FromColors(new[] { color }, 1, 1),
                    SpriteType.Single
                );
            }

            if (ColorTexLookup.Count % 10000 == 0)
                Console.WriteLine(
                    "You have created "
                        + ColorTexLookup.Count
                        + " sprites. These are never garbage collected. This is a major memory leak. Consider fixing this!"
                );

            return foundColor;
        }
    }
}
