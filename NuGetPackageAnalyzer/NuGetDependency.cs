using NuGet.Versioning;

namespace NuGetPackageAnalyzer
{
    public class NuGetDependency
    {
        public NuGetDependency(string name, NuGetVersion version)
        {
            Name = name;
            VersionRange = new DependencyVersionRange(version);
        }

        public string Name { get; }
        public DependencyVersionRange VersionRange { get; }
    }
}