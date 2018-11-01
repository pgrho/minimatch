using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Shipwreck.Minimatch
{
    public sealed class MatcherFactory
    {
        private static readonly Regex _IsNumericRange = new Regex(@"^([0-9]+)\.\.([0-9]+)$");

        public bool AllowBackslash { get; set; }

        public bool IgnoreCase { get; set; }

        public Matcher Create(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)
                || pattern[0] == '#')
            {
                return new Matcher();
            }

            var result = true;

            var sb = new StringBuilder(Math.Max(16, pattern.Length * 2));
            sb.Append('^');

            if (pattern[0] == '!')
            {
                result = false;
                AppendPattern(pattern, 1, pattern.Length - 1, sb);
            }
            else if (pattern.StartsWith("\\#"))
            {
                AppendPattern(pattern, 1, pattern.Length - 1, sb);
            }
            else
            {
                AppendPattern(pattern, 0, pattern.Length, sb);
            }
            sb.Append('$');

            return new Matcher(
                result,
                new Regex(sb.ToString(), IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None));
        }

        private void AppendPattern(string pattern, int start, int length, StringBuilder sb)
        {
            var next = start + length;
            for (var i = start; i < next; i++)
            {
                if (ProcessBrace(pattern, start, length, i, sb)
                    || ProcessParenthesis(pattern, start, length, i, sb))
                {
                    return;
                }
            }

            var last = start;

            // ?
            // *
            // **
            // **/
            // **/*

            var mr = new Regex(AllowBackslash ? @"(?:\?|(?:\*\*[\/\\])+|\*\*?|(?:\*\*[\/\\])+\*?)" : @"(?:\?|(?:\*\*\/)+|\*\*?|(?:\*\*\/)+\*?)");

            foreach (Match m in mr.Matches(pattern, start))
            {
                if (m.Index + m.Length > start + length)
                {
                    break;
                }
                AppendLiteralPattern(pattern, sb, last, m.Index);

                if (m.Length == 1)
                {
                    AppendNonPathSeparatorChar(sb);
                    if (m.Value[0] == '*')
                    {
                        sb.Append(
                            (m.Index == start
                            || IsDirectorySeparator(pattern[m.Index - 1]))
                            && (m.Index == start + length - 1
                            || IsDirectorySeparator(pattern[m.Index + 1])) ? '+' : '*');
                    }
                }
                else
                {
                    sb.Append("(?:");
                    AppendNonPathSeparatorChar(sb);
                    sb.Append('+');
                    AppendPathSeparatorChar(sb);
                    sb.Append(")*");

                    if (m.Value[m.Length - 1] == '*')
                    {
                        AppendNonPathSeparatorChar(sb);
                        sb.Append('+');
                    }
                }

                last = m.Index + m.Length;
            }

            AppendLiteralPattern(pattern, sb, last, start + length);
        }

        private bool ProcessBrace(string pattern, int start, int length, int i, StringBuilder sb)
        {
            if (pattern[i] != '{')
            {
                return false;
            }
            AppendPattern(pattern, start, i - start, sb);

            var patternFound = false;

            var n = 1;
            var next = start + length;
            for (var j = i + 1; j < next; j++)
            {
                var jc = pattern[j];

                if (jc == '{' || IsOpeningParenthesis(pattern, j, i))
                {
                    n++;
                }
                else if (jc == '}' || (jc == ')' && n > 1))
                {
                    if (--n == 0)
                    {
                        var nrm = patternFound ? null : _IsNumericRange.Match(pattern, i + 1, j - i - 1);
                        if (nrm?.Success == true)
                        {
                            AppendNumberRange(sb, nrm.Groups[1].Value, nrm.Groups[2].Value);
                        }
                        else
                        {
                            if (patternFound)
                            {
                                sb.Append('|');
                                AppendPattern(pattern, i + 1, j - i - 1, sb);
                                sb.Append(')');
                            }
                            else
                            {
                                AppendPattern(pattern, i + 1, j - i - 1, sb);
                            }
                        }

                        AppendPattern(pattern, j + 1, length - j + start - 1, sb);

                        return true;
                    }
                }
                else if (jc == ',' && n == 1)
                {
                    if (patternFound)
                    {
                        sb.Append('|');
                    }
                    else
                    {
                        sb.Append("(?:");
                        patternFound = true;
                    }
                    AppendPattern(pattern, i + 1, j - i - 1, sb);
                    i = j;
                }
            }
            throw new FormatException();
        }

        private bool ProcessParenthesis(string pattern, int start, int length, int i, StringBuilder sb)
        {
            if (!IsOpeningParenthesis(pattern, i, start))
            {
                return false;
            }

            AppendPattern(pattern, start, i - start, sb);

            var pc = pattern[i - 1];
            sb.Append(pc == '!' ? "(?<!.*(?:" : "(?:");

            var patternFound = false;

            var n = 1;
            var next = start + length;
            for (var j = i + 1; j < next; j++)
            {
                var jc = pattern[j];

                if (jc == '{' || IsOpeningParenthesis(pattern, j, i))
                {
                    n++;
                }
                else if (jc == '}' && n > 1 || jc == ')')
                {
                    if (--n == 0)
                    {
                        if (patternFound)
                        {
                            sb.Append('|');
                        }
                        AppendPattern(pattern, i + 1, j - i - 1, sb);

                        switch (pc)
                        {
                            case '!':
                                sb.Append(")).*");
                                break;

                            case '*':
                            case '+':
                            case '?':
                                sb.Append(')');
                                sb.Append(pc);
                                break;

                            default:
                                sb.Append(')');
                                break;
                        }

                        AppendPattern(pattern, j + 1, length - j + start - 1, sb);

                        return true;
                    }
                }
                else if (jc == ',' && n == 1)
                {
                    if (patternFound)
                    {
                        sb.Append('|');
                    }
                    else
                    {
                        patternFound = true;
                    }
                    AppendPattern(pattern, i + 1, j - i - 1, sb);
                    i = j;
                }
            }
            throw new FormatException();
        }

        private bool IsDirectorySeparator(char c)
            => c == '/' || (AllowBackslash && c == '\\');

        private static bool IsOpeningParenthesis(string pattern, int index, int start)
        {
            if (index > start && pattern[index] == '(')
            {
                var p = pattern[index - 1];
                return p == '+'
                    || p == '*'
                    || p == '?'
                    || p == '@'
                    || p == '!';
            }
            return false;
        }

        private void AppendLiteralPattern(string pattern, StringBuilder sb, int last, int m)
        {
            for (var i = last; i < m; i++)
            {
                var c = pattern[i];

                switch (c)
                {
                    case '/':
                        AppendPathSeparatorChar(sb);
                        break;

                    case '\\':
                        if (AllowBackslash)
                        {
                            AppendPathSeparatorChar(sb);
                        }
                        else
                        {
                            sb.Append(@"\\");
                        }
                        break;

                    case '[':
                    case '^':
                    case '$':
                    case '.':
                    case '|':
                    case '+':
                    case '(':
                    case ')':
                    case '{':
                    case '}':
                        sb.Append('\\');
                        sb.Append(c);
                        break;

                    default:
                        sb.Append(c);
                        break;
                }
            }
        }

        private void AppendPathSeparatorChar(StringBuilder sb)
            => sb.Append(AllowBackslash ? @"[\/\\]" : @"\/");

        private void AppendNonPathSeparatorChar(StringBuilder sb)
            => sb.Append(AllowBackslash ? @"[^\/\\]" : "[^/]");

        #region AppendNumberRange

        private static void AppendNumberRange(StringBuilder sb, string min, string max)
        {
            var length = Math.Max(min.Length, max.Length);
            min = min.PadLeft(length, '0');
            max = max.PadLeft(length, '0');

            var isZero = true;

            if (min.CompareTo(max) > 0)
            {
                var t = max;
                max = min;
                min = t;
            }

            sb.Append("0*");

            for (var i = 0; i < length; i++)
            {
                var l = min[i];
                var u = max[i];
                isZero &= l == '0';
                if (l == u)
                {
                    if (isZero)
                    {
                        continue;
                    }
                    sb.Append(l);
                }
                else if (i == length - 1)
                {
                    sb.Append('[');
                    sb.Append(l);
                    if (l + 1 < u)
                    {
                        sb.Append('-');
                    }
                    sb.Append(u);
                    sb.Append(']');

                    return;
                }
                else
                {
                    sb.Append("(?:");
                    if (!isZero)
                    {
                        sb.Append(l);
                    }

                    AppendLowerbound(sb, min, i + 1, isZero);
                    sb.Append('|');

                    if (u > l + 1)
                    {
                        sb.Append('[');
                        sb.Append((char)(l + 1));
                        sb.Append('-');
                        sb.Append((char)(u - 1));
                        sb.Append("][0-9]");

                        if (i < length - 2)
                        {
                            sb.Append('{').Append(length - 1 - i).Append('}');
                        }
                        sb.Append('|');
                    }

                    sb.Append(u);
                    AppendUpperbound(sb, max, i + 1);

                    sb.Append(')');

                    return;
                }
            }
        }

        private static void AppendUpperbound(StringBuilder sb, string v, int j)
        {
            var c = v[j];
            if (c == '0')
            {
                sb.Append('0');
                if (j + 1 < v.Length)
                {
                    AppendUpperbound(sb, v, j + 1);
                }
            }
            else if (j == v.Length - 1)
            {
                sb.Append("[0-");
                sb.Append(c);
                sb.Append(']');
            }
            else
            {
                sb.Append("(?:[0-");
                sb.Append((char)(c - 1));
                sb.Append("][0-9]");
                if (j < v.Length - 2)
                {
                    sb.Append('{').Append(v.Length - 1 - j).Append('}');
                }
                sb.Append('|');

                sb.Append(c);
                AppendUpperbound(sb, v, j + 1);

                sb.Append(')');
            }
        }

        private static void AppendLowerbound(StringBuilder sb, string v, int j, bool isZero)
        {
            var c = v[j];
            if (c == '9')
            {
                sb.Append('9');
                if (j + 1 < v.Length)
                {
                    AppendLowerbound(sb, v, j + 1, false);
                }
            }
            else if (j == v.Length - 1)
            {
                sb.Append('[');
                sb.Append(c);
                sb.Append("-9]");
            }
            else
            {
                sb.Append("(?:");
                if (isZero && c == '0')
                {
                    AppendLowerbound(sb, v, j + 1, true);
                }
                else
                {
                    sb.Append(c);
                    AppendLowerbound(sb, v, j + 1, false);
                }
                sb.Append("|[");
                sb.Append((char)(c + 1));
                sb.Append('-');
                sb.Append("9][0-9]");
                if (j < v.Length - 2)
                {
                    sb.Append('{').Append(v.Length - 1 - j).Append('}');
                }

                sb.Append(')');
            }
        }

        #endregion AppendNumberRange

        public Func<string, bool?> Compile(string pattern)
            => Create(pattern).IsMatch;

        public Func<string, bool> Compile(IEnumerable<string> patterns)
        {
            var mms = patterns.Select(Create).ToArray();

            return s =>
            {
                bool? r = null;
                foreach (var mm in mms)
                {
                    r = mm.IsMatch(s) ?? r;
                }
                return r ?? false;
            };
        }
    }
}