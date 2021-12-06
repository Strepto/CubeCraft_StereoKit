using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StereoKitApp.Utils.WordSequenceGenerator
{
    public static class WordSequenceGenerator
    {
        private static readonly IReadOnlyList<string> WordList = WordListData.Words;

        private static readonly Random Rng = new Random();
        public static string GenerateRandomWordSequence(uint numWords, string separator = "-")
        {
            var result = new StringBuilder();
            for (int i = 0; i < numWords; i++)
            {
                result.Append(GetRandomListItemFromNonEmptyList(WordList));
                if (i + 1 < numWords)
                    result.Append(separator);
            }

            return result.ToString();
        }

        private static T GetRandomListItemFromNonEmptyList<T>(IReadOnlyList<T> list)
        {
            var index = Rng.Next(0, list.Count - 1);
            return list[index];
        }
    }
}
