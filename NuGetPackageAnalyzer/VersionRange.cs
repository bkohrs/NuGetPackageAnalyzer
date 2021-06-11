using System;

namespace NuGetPackageAnalyzer
{
    public class VersionRange
    {
        public VersionRange(Version version)
        {
            Min = version;
            Max = version;
        }

        public Version Min { get; private set; }
        public Version Max { get; private set; }

        public void Include(Version version)
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