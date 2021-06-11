using System;

namespace NuGetPackageAnalyzer
{
    public class NuGetDependency
    {
        public NuGetDependency(string name, Version version)
        {
            Name = name;
            VersionRange = new VersionRange(version);
        }

        public string Name { get; }
        public VersionRange VersionRange { get; }
    }
}