using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryStudio.Forms
{
    public static class FuzzyMatch
    {
        public static List<FuzzyMatchResult> MatchList(List<string> texts, string pattern)
        {
            return texts.Select(text => Match(text, pattern))
                        .OrderByDescending(result => result.Score)
                        .ToList();
        }

        public static FuzzyMatchResult Match(string text, string pattern)
        {
            // 简单的模糊匹配算法，计算匹配得分
            int score = 0;
            int patternIndex = 0;
            for (int i = 0; i < text.Length && patternIndex < pattern.Length; i++)
            {
                if (char.ToLower(text[i]) == char.ToLower(pattern[patternIndex]))
                {
                    score++;
                    patternIndex++;
                }
            }
            return new FuzzyMatchResult
            {
                Text = text,
                Score = score
            };
        }
    }

    public class FuzzyMatchResult
    {
        public string Text { get; set; }
        public int Score { get; set; }
    }
}
