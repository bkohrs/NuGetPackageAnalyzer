namespace NuGetPackageAnalyzer
{
    public enum AnalysisIssue
    {
        MissingPackagesConfig,
        MissingAssetsJson,
        InvalidVersion,
        MissingNuGetPackage,
        MissingNuSpecFile,
    }
}