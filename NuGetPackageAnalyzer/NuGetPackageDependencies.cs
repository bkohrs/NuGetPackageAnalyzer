using System;
using System.Collections.Generic;
using NuGet.Versioning;

namespace NuGetPackageAnalyzer
{
    public class NuGetPackageDependencies
    {
        private readonly Dictionary<string, NuGetDependency> _overallDependencies =
            new(StringComparer.CurrentCultureIgnoreCase);
        private readonly Dictionary<string, Dictionary<string, NuGetDependency>> _projectDependencies =
            new(StringComparer.CurrentCultureIgnoreCase);
        private readonly List<ProjectIssue> _projectIssues = new();

        public void AddDependency(string projectName, string nuGetPackageName, NuGetVersion nuGetPackageVersion)
        {
            AddProjectDependency(_overallDependencies, nuGetPackageName, nuGetPackageVersion);
            if (!_projectDependencies.TryGetValue(projectName, out var dependencies))
            {
                dependencies = new Dictionary<string, NuGetDependency>(StringComparer.CurrentCultureIgnoreCase);
                _projectDependencies.Add(projectName, dependencies);
            }

            AddProjectDependency(dependencies, nuGetPackageName, nuGetPackageVersion);
        }
        public void AddIssue(string project, AnalysisIssue issue, string additionalDetails = null)
        {
            _projectIssues.Add(new ProjectIssue(project, issue, additionalDetails));
        }
        private void AddProjectDependency(Dictionary<string, NuGetDependency> dependencies, string nuGetPackageName,
            NuGetVersion nuGetPackageVersion)
        {
            if (!dependencies.TryGetValue(nuGetPackageName, out var dependency))
                dependencies[nuGetPackageName] = new NuGetDependency(nuGetPackageName, nuGetPackageVersion);
            else
                dependency.VersionRange.Include(nuGetPackageVersion);
        }
        public void AddProjectReference(string project, string referencedProject)
        {
            if (_projectDependencies.TryGetValue(referencedProject, out var referencedDependencies))
            {
                foreach (var dependency in referencedDependencies.Values)
                {
                    AddDependency(project, dependency.Name, dependency.VersionRange.Min);
                    AddDependency(project, dependency.Name, dependency.VersionRange.Max);
                }
            }
        }
        public IEnumerable<ProjectNuGetReference> GetBindingRedirects()
        {
            foreach (var projectDependency in _projectDependencies)
            {
                foreach (var dependency in projectDependency.Value.Values)
                {
                    if (dependency.VersionRange.Min < dependency.VersionRange.Max)
                    {
                        yield return new ProjectNuGetReference(projectDependency.Key, dependency.Name,
                            dependency.VersionRange.Max);
                    }
                }
            }
        }
        public IEnumerable<ProjectNuGetReference> GetPackageDependencies()
        {
            foreach (var projectDependency in _projectDependencies)
            {
                foreach (var dependency in projectDependency.Value.Values)
                {
                    yield return new ProjectNuGetReference(projectDependency.Key, dependency.Name,
                        dependency.VersionRange.Max);
                }
            }
        }
        public IEnumerable<ProjectNuGetReference> GetPackageUpgrades()
        {
            foreach (var projectDependency in _projectDependencies)
            {
                foreach (var dependency in projectDependency.Value.Values)
                {
                    var overallMax = _overallDependencies[dependency.Name].VersionRange.Max;
                    if (overallMax > dependency.VersionRange.Max)
                        yield return new ProjectNuGetReference(projectDependency.Key, dependency.Name, overallMax);
                }
            }
        }
        public IEnumerable<ProjectIssue> GetProjectIssues()
        {
            return _projectIssues;
        }
    }
}