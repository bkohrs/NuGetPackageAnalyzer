namespace NuGetPackageAnalyzer
{
    public class ProjectIssue
    {
        public ProjectIssue(string name, AnalysisIssue issue)
        {
            Name = name;
            Issue = issue;
        }

        public string Name { get; }
        public AnalysisIssue Issue { get; }
    }
}