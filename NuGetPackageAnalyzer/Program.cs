using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace NuGetPackageAnalyzer
{
    internal class Program
    {
        private static Command CreateBindingRedirectsCommand()
        {
            var command = new Command("redirects", "Get a list of needed binding redirects.")
            {
                new Argument<string>("directory", "Directory containing the projects"),
                new Argument<string>("framework", ".NET Framework version to analyze")
            };
            command.Handler = CommandHandler.Create<string, string, IConsole>(PackageAnalyzer.GetBindingRedirects);
            return command;
        }
        private static Command CreateDependenciesCommand()
        {
            var command = new Command("dependencies", "Get a list of project dependencies.")
            {
                new Argument<string>("directory", "Directory containing the projects"),
                new Argument<string>("framework", ".NET Framework version to analyze")
            };
            command.Handler = CommandHandler.Create<string, string, IConsole>(PackageAnalyzer.GetPackageDependencies);
            return command;
        }
        private static Command CreatePackagesCommand()
        {
            var command = new Command("packages", "Get a list of packages that need upgrading.")
            {
                new Argument<string>("directory", "Directory containing the projects"),
                new Argument<string>("framework", ".NET Framework version to analyze")
            };
            command.Handler = CommandHandler.Create<string, string, IConsole>(PackageAnalyzer.GetPackageUpgrades);
            return command;
        }
        private static async Task Main(string[] args)
        {
            var command = new RootCommand
            {
                CreateDependenciesCommand(),
                CreatePackagesCommand(),
                CreateBindingRedirectsCommand(),
            };
            await command.InvokeAsync(args).ConfigureAwait(false);
        }
    }
}