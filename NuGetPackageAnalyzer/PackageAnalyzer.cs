using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json.Linq;

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
                _ => throw new ArgumentOutOfRangeException(nameof(issue), issue, null)
            };
        }
        private static NuGetPackageDependencies AnalyzePackageDependencies(string directory, string framework)
        {
            var packageDependencies = new NuGetPackageDependencies();
            var projects = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories);
            foreach (var project in projects)
                if (IsSdkProject(project))
                    GetSdkDependencies(project, framework, packageDependencies);
                else
                    GetNonSdkDependencies(project, packageDependencies);

            return packageDependencies;
        }
        public static void GetBindingRedirects(string directory, string framework, IConsole console)
        {
            var packageDependencies = AnalyzePackageDependencies(directory, framework);
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
        private static void GetNonSdkDependencies(string project, NuGetPackageDependencies dependencies)
        {
            var packagesConfig = Path.Combine(Path.GetDirectoryName(project) ?? string.Empty, "packages.config");
            if (File.Exists(packagesConfig))
            {
                var document = new XmlDocument();
                document.Load(packagesConfig);
                var packages = document.SelectNodes("//package")?.OfType<XmlElement>() ??
                               Enumerable.Empty<XmlElement>();
                foreach (var package in packages)
                    dependencies.AddDependency(project, package.GetAttribute("id"),
                        Version.Parse(package.GetAttribute("version")));
            }
            else
            {
                dependencies.AddIssue(project, AnalysisIssue.MissingPackagesConfig);
            }
        }
        public static void GetPackageUpgrades(string directory, string framework, IConsole console)
        {
            var packageDependencies = AnalyzePackageDependencies(directory, framework);
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
        private static void GetSdkDependencies(string project, string frameworkVersion,
            NuGetPackageDependencies dependencies)
        {
            var assetsJson = Path.Combine(Path.GetDirectoryName(project) ?? string.Empty, @"obj\project.assets.json");
            if (File.Exists(assetsJson))
            {
                var jObject = JObject.Parse(File.ReadAllText(assetsJson));
                var references = jObject["targets"]?[frameworkVersion]?.OfType<JProperty>() ??
                                 Enumerable.Empty<JProperty>();
                foreach (var r in references)
                {
                    var (name, version, _) = r.Name.Split("/");
                    dependencies.AddDependency(project, name, Version.Parse(version));
                    var referenceDependencies =
                        r.Value["dependencies"]?.OfType<JProperty>() ?? Enumerable.Empty<JProperty>();
                    foreach (var referenceDependency in referenceDependencies)
                        dependencies.AddDependency(project, referenceDependency.Name,
                            Version.Parse(referenceDependency.Value.ToString()));
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
        private static void WriteProjectIssues(IConsole console, IList<ProjectIssue> projectIssues)
        {
            if (projectIssues.Any())
            {
                foreach (var issuesByType in projectIssues.GroupBy(r => r.Issue))
                {
                    console.Out.WriteLine(AnalysisIssueDescription(issuesByType.Key));
                    foreach (var project in issuesByType.OrderBy(r => r.Name))
                        console.Out.WriteLine($"  {project.Name}");
                }

                console.Out.WriteLine();
            }
        }
    }
}