using SharedResources;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CoreTests.Tests
{
    public class EncompassTests
    {
        internal static async Task Run(EncompassSettings settings)
        {
            using (var client = new EncompassClient(settings))
            {
                var sw = new Stopwatch();
                sw.Start();



                sw.Stop();
                Console.WriteLine(sw.Elapsed);
            }
        }

        static async Task PipelineRequestTest(EncompassClient client)
        {

            var result = await client.GetLoansWithCursor(new Filter("Loan.LoanNumber", "00000000", MatchType.Equals));
        }
    }
}
