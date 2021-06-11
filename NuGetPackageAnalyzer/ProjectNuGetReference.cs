using System;

namespace NuGetPackageAnalyzer
{
    public class ProjectNuGetReference
    {
        public ProjectNuGetReference(string projectName, string nuGetPackageName, Version nuGetPackageVersion)
        {
            ProjectName = projectName;
            NuGetPackageName = nuGetPackageName;
            NuGetPackageVersion = nuGetPackageVersion;
        }

        public string ProjectName { get; }
        public string NuGetPackageName { get; }
        public Version NuGetPackageVersion { get; }
    }
}