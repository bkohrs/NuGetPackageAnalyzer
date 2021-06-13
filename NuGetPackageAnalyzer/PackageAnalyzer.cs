using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using Newtonsoft.Json.Linq;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace NuGetPackageAnalyzer
{
    public class PackageAnalyzer
    {
        private static string AnalysisIssueDescription(AnalysisIssue issue)
        {
            return issue switch
            {
                AnalysisIssue.MissingPackagesConfig => "The following projects do not have a packages.config file and were skipped",
                AnalysisIssue.MissingAssetsJson => "The following projects do not have a project.assets.json file and were skipped",
                AnalysisIssue.InvalidVersion => "The following projects had an invalid version and that dependency was skipped",
                AnalysisIssue.MissingNuGetPackage => "The following projects had missing NuGet package and its dependencies were skipped",
                AnalysisIssue.MissingNuSpecFile => "The following projects did not have a .nuspec file in the NuGet package and its dependencies were skipped",
                _ => throw new ArgumentOutOfRangeException(nameof(issue), issue, null)
            };
        }
        private static NuGetPackageDependencies AnalyzePackageDependencies(string directory, NuGetFramework framework)
        {
            var packageDependencies = new NuGetPackageDependencies();
            var projects = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories);
            foreach (var project in projects)
            {
                if (IsSdkProject(project))
                    GetSdkDependencies(project, framework, packageDependencies);
                else
                    GetNonSdkDependencies(directory, project, framework, packageDependencies);
            }
            foreach (var project in projects)
                LinkProjectReferences(project, packageDependencies);

            return packageDependencies;
        }
        public static void GetBindingRedirects(string directory, string framework, IConsole console)
        {
            var packageDependencies = AnalyzePackageDependencies(directory, NuGetFramework.Parse(framework));
            WriteProjectIssues(console, packageDependencies.GetProjectIssues().ToList());
            var packageUpgrades = packageDependencies.GetBindingRedirects().ToList();
            if (packageUpgrades.Count > 0)
            {
                console.Out.WriteLine("The following projects need binding redirects:");
                foreach (var byProject in packageUpgrades.GroupBy(r => r.ProjectName).OrderBy(r=> r.Key))
                {
                    console.Out.WriteLine($"  {Path.GetFileNameWithoutExtension(byProject.Key)}");
                    foreach (var packageUpgrade in byProject.OrderBy(r => r.NuGetPackageName))
                    {
                        console.Out.WriteLine(
                            $"    {packageUpgrade.NuGetPackageName} => {packageUpgrade.NuGetPackageVersion}");
                    }
                }
            }
            else
                console.Out.WriteLine("No needed binding redirects found.");
        }
        private static void GetNonSdkDependencies(string directory, string project, NuGetFramework framework, NuGetPackageDependencies dependencies)
        {
            var frameworkReducer = new FrameworkReducer();
            var packagesConfig = Path.Combine(Path.GetDirectoryName(project) ?? string.Empty, "packages.config");
            if (File.Exists(packagesConfig))
            {
                var document = new XmlDocument();
                document.Load(packagesConfig);
                var packages = document.SelectNodes("//package")?.OfType<XmlElement>() ??
                               Enumerable.Empty<XmlElement>();
                foreach (var package in packages)
                {
                    var name = package.GetAttribute("id");
                    var versionText = package.GetAttribute("version");
                    if (NuGetVersion.TryParse(versionText, out var version))
                        dependencies.AddDependency(project, name, version);
                    else
                        dependencies.AddIssue(project, AnalysisIssue.InvalidVersion, $"{name}:{versionText}");
                    var nuGetPackage = Path.Combine(directory, @$"packages\{name}.{versionText}\{name}.{versionText}.nupkg");
                    if (File.Exists(nuGetPackage))
                    {
                        using var zipFile = ZipFile.OpenRead(nuGetPackage);
                        var nuspec = zipFile.GetEntry($"{name}.nuspec");
                        if (nuspec != null)
                        {
                            using var reader = new StreamReader(nuspec.Open());
                            var content = reader.ReadToEnd();
                            var xmlDocument = new XmlDocument();
                            xmlDocument.LoadXml(content);
                            var groups =
                                (xmlDocument.SelectNodes("//*[local-name()='dependencies']/*[local-name()='group']")
                                    ?.OfType<XmlElement>() ?? Enumerable.Empty<XmlElement>()).ToList();
                            var frameworks = groups
                                .Select(r => r.Attributes["targetFramework"]?.Value)
                                .Where(r => !string.IsNullOrWhiteSpace(r))
                                .Select(NuGetFramework.Parse)
                                .ToList();
                            var effectiveFramework = frameworkReducer.GetNearest(framework, frameworks);
                            var effectiveGroup = groups.FirstOrDefault(r =>
                            {
                                var groupFramework = r.Attributes["targetFramework"]?.Value;
                                return !string.IsNullOrWhiteSpace(groupFramework) &&
                                       NuGetFramework.Parse(groupFramework) == effectiveFramework;
                            }) ?? groups.FirstOrDefault(r =>
                                string.IsNullOrWhiteSpace(r.Attributes["targetFramework"]?.Value));
                            var referenceDependencies =
                                effectiveGroup?.SelectNodes("*[local-name()='dependency']")?.OfType<XmlElement>() ??
                                Enumerable.Empty<XmlElement>();
                            foreach (var referenceDependency in referenceDependencies)
                            {
                                var referenceDependencyName = referenceDependency.GetAttribute("id");
                                var referenceDependencyVersionText = referenceDependency.GetAttribute("version");
                                if (VersionRange.TryParse(referenceDependencyVersionText, out var versionRange) && versionRange.IsMinInclusive)
                                    dependencies.AddDependency(project, referenceDependencyName, versionRange.MinVersion);
                                else
                                    dependencies.AddIssue(project, AnalysisIssue.InvalidVersion,
                                        $"{referenceDependencyName}:{referenceDependencyVersionText}");
                            }
                        }
                        else
                            dependencies.AddIssue(project, AnalysisIssue.MissingNuSpecFile, $"{name}:{versionText}");
                    }
                    else
                        dependencies.AddIssue(project, AnalysisIssue.MissingNuGetPackage, $"{name}:{versionText}");
                }
            }
            else
            {
                dependencies.AddIssue(project, AnalysisIssue.MissingPackagesConfig);
            }
        }
        public static void GetPackageDependencies(string directory, string framework, IConsole console)
        {
            var packageDependencies = AnalyzePackageDependencies(directory, NuGetFramework.Parse(framework));
            WriteProjectIssues(console, packageDependencies.GetProjectIssues().ToList());
            var packageUpgrades = packageDependencies.GetPackageDependencies().ToList();
            if (packageUpgrades.Count > 0)
            {
                console.Out.WriteLine("The following project dependencies were identified:");
                foreach (var byProject in packageUpgrades.GroupBy(r => r.ProjectName).OrderBy(r=> r.Key))
                {
                    console.Out.WriteLine($"  {Path.GetFileNameWithoutExtension(byProject.Key)}");
                    foreach (var packageUpgrade in byProject.OrderBy(r => r.NuGetPackageName))
                    {
                        console.Out.WriteLine(
                            $"    {packageUpgrade.NuGetPackageName} => {packageUpgrade.NuGetPackageVersion}");
                    }
                }
            }
            else
                console.Out.WriteLine("No needed upgrades found.");
        }
        public static void GetPackageUpgrades(string directory, string framework, IConsole console)
        {
            var packageDependencies = AnalyzePackageDependencies(directory, NuGetFramework.Parse(framework));
            WriteProjectIssues(console, packageDependencies.GetProjectIssues().ToList());
            var packageUpgrades = packageDependencies.GetPackageUpgrades().ToList();
            if (packageUpgrades.Count > 0)
            {
                console.Out.WriteLine("The following projects need to upgrade nuget packages:");
                foreach (var byProject in packageUpgrades.GroupBy(r => r.ProjectName).OrderBy(r=> r.Key))
                {
                    console.Out.WriteLine($"  {Path.GetFileNameWithoutExtension(byProject.Key)}");
                    foreach (var packageUpgrade in byProject.OrderBy(r => r.NuGetPackageName))
                    {
                        console.Out.WriteLine(
                            $"    {packageUpgrade.NuGetPackageName} => {packageUpgrade.NuGetPackageVersion}");
                    }
                }
            }
            else
                console.Out.WriteLine("No needed upgrades found.");
        }
        private static void GetSdkDependencies(string project, NuGetFramework framework,
            NuGetPackageDependencies dependencies)
        {
            var assetsJson = Path.Combine(Path.GetDirectoryName(project) ?? string.Empty, @"obj\project.assets.json");
            if (File.Exists(assetsJson))
            {
                var jObject = JObject.Parse(File.ReadAllText(assetsJson));
                var targets = jObject["targets"]?.OfType<JProperty>();
                var effectiveTarget = targets?.FirstOrDefault(r =>
                {
                    try
                    {
                        return NuGetFramework.Parse(r.Name) == framework;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
                var references = effectiveTarget?.Value?.OfType<JProperty>() ?? Enumerable.Empty<JProperty>();
                foreach (var r in references)
                {
                    var (name, versionText, _) = r.Name.Split("/");
                    if (NuGetVersion.TryParse(versionText, out var version))
                        dependencies.AddDependency(project, name, version);
                    else
                        dependencies.AddIssue(project, AnalysisIssue.InvalidVersion, $"{name}:{versionText}");
                    var referenceDependencies =
                        r.Value["dependencies"]?.OfType<JProperty>() ?? Enumerable.Empty<JProperty>();
                    foreach (var referenceDependency in referenceDependencies)
                    {
                        var referenceVersionText = referenceDependency.Value.ToString();
                        if (VersionRange.TryParse(referenceVersionText, out var referenceVersion) && referenceVersion.IsMinInclusive)
                            dependencies.AddDependency(project, referenceDependency.Name, referenceVersion.MinVersion);
                        else
                        {
                            dependencies.AddIssue(project, AnalysisIssue.InvalidVersion,
                                $"{referenceDependency.Name}:{referenceVersionText}");
                        }
                    }
                }
            }
            else
            {
                dependencies.AddIssue(project, AnalysisIssue.MissingAssetsJson);
            }
        }
        private static bool IsSdkProject(string project)
        {
            var document = new XmlDocument();
            document.Load(project);
            var projectNode = document.DocumentElement;
            var sdk = projectNode?.Attributes["Sdk"];
            return sdk != null;
        }
        private static void LinkProjectReferences(string project, NuGetPackageDependencies packageDependencies)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(project);
            var references = xmlDocument.SelectNodes("//*[local-name()='ProjectReference']")?.OfType<XmlElement>() ??
                             Enumerable.Empty<XmlElement>();
            foreach (var reference in references)
            {
                var referencedProject =
                    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project) ?? string.Empty, reference.GetAttribute("Include")));
                packageDependencies.AddProjectReference(project, referencedProject);
            }
        }
        private static void WriteProjectIssues(IConsole console, IList<ProjectIssue> projectIssues)
        {
            if (projectIssues.Any())
            {
                foreach (var issuesByType in projectIssues.GroupBy(r => r.Issue))
                {
                    console.Out.WriteLine(AnalysisIssueDescription(issuesByType.Key));
                    foreach (var project in issuesByType.OrderBy(r => r.Name))
                        console.Out.WriteLine(
                            $"  {project.Name}{(!string.IsNullOrWhiteSpace(project.AdditionalDetails) ? $" ({project.AdditionalDetails})" : "")}");
                }

                console.Out.WriteLine();
            }
        }
    }
}