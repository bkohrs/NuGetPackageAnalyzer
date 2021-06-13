using NuGet.Versioning;

namespace NuGetPackageAnalyzer
{
    public class ProjectNuGetReference
    {
        public ProjectNuGetReference(string projectName, string nuGetPackageName, NuGetVersion nuGetPackageVersion)
        {
            ProjectName = projectName;
            NuGetPackageName = nuGetPackageName;
            NuGetPackageVersion = nuGetPackageVersion;
        }

        public string ProjectName { get; }
        public string NuGetPackageName { get; }
        public NuGetVersion NuGetPackageVersion { get; }
    }
}