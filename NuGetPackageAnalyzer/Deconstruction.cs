using System.Collections.Generic;
using System.Linq;

namespace NuGetPackageAnalyzer
{
    public static class Deconstruction
    {
        public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out IList<T> rest)
        {
            first = list.Count > 0 ? list[0] : default;
            second = list.Count > 1 ? list[1] : default;
            rest = list.Skip(2).ToList();
        }
    }
}