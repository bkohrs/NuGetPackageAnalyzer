using NUnit.Framework;

namespace NuGetPackageAnalyzer.Tests
{
    [TestFixture]
    public class NuGetDependencyRangeParserTests
    {
        [TestCase("1.0", ExpectedResult = "1.0")]
        [TestCase("(1.0,)", ExpectedResult = null)]
        [TestCase("[1.0]", ExpectedResult = "1.0")]
        [TestCase("(,1.0]", ExpectedResult = null)]
        [TestCase("(,1.0)", ExpectedResult = null)]
        [TestCase("[1.0,2.0]", ExpectedResult = "1.0")]
        [TestCase("(1.0,2.0)", ExpectedResult = null)]
        [TestCase("[1.0,2.0)", ExpectedResult = "1.0")]
        [TestCase("(1.0)", ExpectedResult = null)]
        public string Parse(string input)
        {
            if (NuGetDependencyRangeParser.TryParse(input, out var result))
                return result.ToString();
            return null;
        }
    }
}