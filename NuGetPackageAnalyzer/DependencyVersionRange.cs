using NuGet.Versioning;

namespace NuGetPackageAnalyzer
{
    public class DependencyVersionRange
    {
        public DependencyVersionRange(NuGetVersion version)
        {
            Min = version;
            Max = version;
        }

        public NuGetVersion Min { get; private set; }
        public NuGetVersion Max { get; private set; }

        public void Include(NuGetVersion version)
        {
            if (version < Min)
                Min = version;
            if (version > Max)
                Max = version;
        }

        public override string ToString()
        {
            return Min == Max ? Min.ToString() : $"{Min}-{Max}";
        }
    }
}