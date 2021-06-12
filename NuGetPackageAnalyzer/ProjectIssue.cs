namespace NuGetPackageAnalyzer
{
    public class ProjectIssue
    {
        public ProjectIssue(string name, AnalysisIssue issue, string additionalDetails = null)
        {
            Name = name;
            Issue = issue;
            AdditionalDetails = additionalDetails;
        }

        public string Name { get; }
        public AnalysisIssue Issue { get; }
        public string AdditionalDetails { get; }
    }
}