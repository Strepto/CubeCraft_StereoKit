using System;
using System.Collections.Generic;
using System.Linq;

namespace StereoKitApp.Utils
{
    public static class ListExtensions
    {
        /// <summary>
        /// Take N items from the list, starting at an index. If the range is larger than the list it will loop back to the start!
        /// </summary>
        /// <param name="input"></param>
        /// <param name="startIndex">This is "inclusive" and will be the first returned.</param>
        /// <param name="take">Number of items to take</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> TakeLoop<T>(
            this IReadOnlyList<T> input,
            int startIndex,
            int take
        )
        {
            if (input.Count == 0)
                throw new ArgumentException("input list had zero items");

            var maxIndex = input.Count - 1;

            for (int i = 0; i < take; i++)
            {
                var index = (startIndex + i) % maxIndex;
                if (index < 0)
                {
                    index = maxIndex + index;
                }
                yield return input[index];
            }
        }
    }
}
