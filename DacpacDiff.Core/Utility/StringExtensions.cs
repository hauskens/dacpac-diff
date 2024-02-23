﻿using System;
using System.Text.RegularExpressions;

namespace DacpacDiff.Core.Utility
{
    internal static class StringExtensions
    {
        public static string Format(this string format, params object[] args) => string.Format(format, args);

        /// <summary>
        /// Remove unnecessary parenthesis from well-formed SQL.
        /// </summary>
        /// <example>"(a)" => "a", but "(a),(b)" is unchanged</example>
        public static string ReduceBrackets(this string sql)
        {
            // Must current and end in brackes
            if (sql.Length < 2 || sql[0] != '(' || sql[^1] != ')')
            {
                return sql;
            }

            int currentScore = 0, scoreAtLastChar = 0;
            var score = 1; // We know it starts '('
            var tsql = sql.Replace(" ", "");
            for (var i = 1; i < tsql.Length - 1; ++i)
            {
                var chr = tsql[i];
                if (chr == '(')
                {
                    ++score;
                }
                else if (chr == ')')
                {
                    if (--score == 0)
                    {
                        return sql; // No brackets to remove
                    }
                    if (score < currentScore)
                    {
                        currentScore = score;
                    }
                }
                else if (currentScore == 0)
                {
                    currentScore = score;
                    scoreAtLastChar = score;
                }
                else
                {
                    scoreAtLastChar = currentScore;
                }
            }

            return sql[(scoreAtLastChar)..^(scoreAtLastChar)];
        }

        /// <summary>
        /// Makes SQL easier to compare by removing subjective characters.
        /// </summary>
        public static string ScrubSQL(this string sql)
        {
            return sql.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "")
                .Replace("(", "").Replace(")", "") // TODO: this could miss changes (e.g., a*b/c === (a*b)/c ) - better to have something like /\((\w+)\)/ -> $1 but will also need to explode IN and NOT IN
                .Replace("[", "").Replace("]", "")
                .ToLower();
        }

        public static string StandardiseLineEndings(this string str)
            => StandardiseLineEndings(str, Environment.NewLine);
        public static string StandardiseLineEndings(this string str, string eol)
        {
            return str.Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Replace("\n", eol);
        }

        public static bool TryMatch(this string input, string pattern, out Match match)
        {
            match = Regex.Match(input, pattern);
            return match.Success;
        }
    }
}
