using System.Collections;
using System.Collections.Generic;

namespace xUnitxTests.CommonToolsTests
{
    public class ExtentionsTestData_Includes : IEnumerable<object[]>
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { true, "test", "ES" };
            yield return new object[] { true, "test", "es" };
            yield return new object[] { true, "test", "test" };
            yield return new object[] { true, "test", "" };
            yield return new object[] { true, "TEST", "es" };
            yield return new object[] { false, "TEST", "et" };
            yield return new object[] { false, "TEST", "Et" };
            yield return new object[] { false, "TEST", "TESTS" };
            yield return new object[] { false, "TEST", "JD" };
            yield return new object[] { false, "TEST", "OT" };
        }
    }
}
