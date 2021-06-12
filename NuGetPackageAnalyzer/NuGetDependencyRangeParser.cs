using System;

namespace NuGetPackageAnalyzer
{
    public static class NuGetDependencyRangeParser
    {
        public static bool TryParse(string input, out Version version)
        {
            version = null;
            if (input.StartsWith("("))
                return false;
            var parts = input.Trim('(', ')', '[', ']').Split(",");
            if (Version.TryParse(parts[0], out version))
                return true;
            return false;
        }
    }
}