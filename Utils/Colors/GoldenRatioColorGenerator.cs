using System;

namespace StereoKitApp.Utils.Colors
{
    /// <summary>
    /// Inspired by https://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/
    /// </summary>
    public class GoldenRatioColorGenerator
    {
        private const double GoldenRatioConjugate = 0.618033988749895;
        private double _currentHue;
        public GoldenRatioColorGenerator(double? seedHue = null)
        {
            var seed = seedHue ?? new Random().NextDouble();
            _currentHue = seed;
        }

        public StereoKit.Color GenerateNextColorInSequence()
        {
            // This ensures the first color we return is the seed color
            var color = StereoKit.Color.HSV((float)_currentHue, 0.8f, 0.8f, opacity: 1f);
            // use golden ratio
            _currentHue += GoldenRatioConjugate;
            _currentHue %= 1;
            return color;
        }

        public static StereoKit.Color GenerateColorByIndex(int index)
        {
            var hue = (GoldenRatioConjugate * index) % 1;
            return StereoKit.Color.HSV((float)hue, 0.8f, 0.8f, opacity: 1f);
        }
    }
}
