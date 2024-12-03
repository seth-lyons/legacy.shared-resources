using SharedResources;
using Xunit;
using Xunit.Abstractions;

namespace xUnitxTests.CommonToolsTests
{
    public class AzureAPIsTests //: IClassFixture<>
    {
        private readonly ITestOutputHelper _output;
        public AzureAPIsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [ClassData(typeof(ExtentionsTestData_Includes))]
        public void ExtentionsTests_Includes(bool expected, string value, string compareTo)
        {
            Assert.Equal(expected, value.Includes(compareTo));
        }
    }
}
