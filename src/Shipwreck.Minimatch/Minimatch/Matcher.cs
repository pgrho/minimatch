using System.Text.RegularExpressions;

namespace Shipwreck.Minimatch
{
#if MINIMATCH_PUBLIC
    public
#else
    internal
#endif
    struct Matcher
    {
        internal readonly bool Result;
        internal readonly Regex Regex;

        internal Matcher(bool result, Regex regex)
        {
            Result = result;
            Regex = regex;
        }

        public bool? IsMatch(string s)
            => s != null && Regex?.IsMatch(s) == true ? Result : (bool?)null;
    }
}