using CoreTests.Objects;
using CoreTests.Tests;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using SharedResources;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Collections.Concurrent;

using System.Linq;

namespace CoreTests
{
    public class Start
    {
        private readonly Settings _settings;
        public Start(IOptions<Settings> settings)
        {
            _settings = settings.Value;
        }

        public int solution(string S, string[] L)
        {
            var availableLetters = S
                ?.GroupBy(s => s)
                ?.ToDictionary(g => g.Key, g => g.Count());

            int currentMax = 0;
            foreach (var name in L)
            {
                int? canMake = (int?)name
                    ?.GroupBy(s => s)
                    ?.Select(g =>
                    {
                        var needed = g.Count();
                        if (!availableLetters.TryGetValue(g.Key, out int availableOfLetter) || needed > availableOfLetter)
                            return 0;
                        return Math.Floor((double)availableOfLetter / needed);
                    })?.Min();
                if (canMake != null && canMake > currentMax)
                    currentMax = (int)canMake;
            }

            Console.WriteLine();

            return currentMax;
        }

        public async Task Main(string[] args)
        {
            try
            {
                await EncompassTests.Run(_settings.Encompass);
                #region Previous Test
                /*
                //  var s = new PipelineRequest(new Filter("Fields.Log.MS.Date.Cond. Approval", "20211010", precision: Precision.Day, SharedResources.MatchType.Equals), new[] { "Loan.LoanNumber" }).ToString();
                //using (var api = new RingCentralClient(_settings.RingCentral))
                //{
                //    var s = await api.GetDirectoryEntries();
                //    var c = s.Count(a => (string)a?["type"] == "User");
                //}
                //JObject q = new JObject
                //{
                //    ["First"] = new JArray {
                //        new JObject {
                //            ["Yes"] = "Yep"
                //        }
                //    },
                //    ["Second"] = new JArray {
                //        new JObject {
                //            ["No"] = "Nope"
                //        }
                //    }
                //};


                // var a = (q.First as JProperty).Value;


                //var values = JObject.Parse(@"{
                //    ""Vendor"": [
                //    ""NMLS""
                //    ],
                //    ""Invoice #"": [
                //    ""CP091721-ADAMS""
                //    ],
                //    ""Invoice Amount"": [
                //    110.00
                //    ],
                //    ""Payment Method"": [
                //    ""ACHA""
                //    ],
                //    ""LX Type"": [
                //    ""A - General""
                //    ],
                //    ""Posting Date"": [
                //    ""2021-09-17T00:00:00-04:00""
                //    ],
                //    ""Company Code"": [
                //    ""NLC""
                //    ]
                //    }");
                //var iterations = values
                //    ?.Children<JProperty>()
                //    .Max(a => (a.Value as JArray)?.Count ?? 0);

                //using (var credit = new CreditClient(_settings.CreditAPI_PROD))
                //{
                //    var report = await credit.RetrieveExisting(
                //        "64670276",
                //        new BorrowerInformation
                //        {
                //            FirstName = "Wanna",
                //            LastName = "House",
                //            SSN = "000-11-2222"
                //        },
                //        new BorrowerInformation
                //        {
                //            FirstName = "Needa",
                //            LastName = "House",
                //            SSN = "999-44-5555"
                //        });

                //    var processedReport = CreditProcessor.ProcessXML(report);
                //}


                ////Encrypt and Sign
                //using (var pgp = new PGPCoreClient(new EncryptionSettings
                //{
                //    Base64PrivateKey = _settings.PGPKeysSigner.Base64PrivateKey,
                //    Base64PublicKey = _settings.PGPKeysEncryptor.Base64PublicKey                    
                //}))
                //{
                //    var file = @"C:\app_files\PGPTESTS\e3onlytusers.csv";

                //    var encryptedBytes = await pgp.Encrypt(File.ReadAllBytes(file), true, name: "encrypted.pgp");

                //    File.WriteAllBytes($"{file}.pgp", encryptedBytes);

                //   // var decryptedBytes = await pgp.Decrypt(File.ReadAllBytes($"{file}.pgp"));
                //  //  File.WriteAllBytes(file.Insert(file.LastIndexOf('.'), "_DECRYPTED"), decryptedBytes);
                //}

                //using (var client = new AzureRestClient(_settings.AzureManagement.ClientID, _settings.AzureManagement.ClientSecret))
                //{
                //    var cases = await client.GetOpenSecurityInsightCases();
                //}

                //var sw = new Stopwatch();
                //sw.Start();
                ////Parallel.ForEach(
                ////    new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                ////    new ParallelOptions { MaxDegreeOfParallelism = 4 },
                ////    i => { Task.Delay(1000).Wait(); Console.WriteLine($"{i}, {sw.Elapsed}"); }
                ////);
                //var t = 1;
                //var additionTracker = new ConcurrentBag<long>();
                //var responses = await Task.WhenAll(Enumerable.Range(1, 10000).Select(async i =>
                //{
                //    loopstart:
                //    if (additionTracker.Count > 900 && (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - additionTracker.OrderByDescending(a => a).Take(900).Last()) < 60)
                //    {
                //        goto loopstart;
                //    }

                //    additionTracker.Add(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                //    await Task.Delay(1000);
                //    Console.WriteLine($"{t++}) {i}, {sw.Elapsed}");
                //    return i;
                //}));

                //using (var te = new TotalExpertClient(_settings.AzureAD.ClientID, _settings.AzureAD.ClientSecret, TotalExpertEnvironment.Production))
                //{
                //    var lead = await te.GetContact(109515830);
                //    //var user = await te.GetUsers();
                //    var testLead = new Contact
                //    {
                //        Source = "GoHighLevel",
                //        FirstName = "Tester",
                //        LastName = $"McTesterson",
                //        Email = $"Seth.Lyons+Test@nationslending.com",
                //        Owner = new Owner
                //        {
                //            Username = "seth-lyons-nationslending"
                //        }
                //    };

                //    var leadRes = await te.CreateContact(testLead);
                //}
                //      var users = await te.GetUsers();
                //  var groups = await te.GetContactGroups();
                //var users = Operations.CsvToJson(File.ReadAllBytes(@"C:\Users\slyons\Downloads\Refiroster (2).csv"));
                //var teUsers = await te.GetUsers();
                //foreach (var user in users)
                //{
                //    var email = ((string)user?["refiemail"])?.Trim();
                //    DateTime.TryParse((string)user?["DateEnd"], out DateTime expiration);

                //    if (email.IsEmpty())
                //    {
                //        Console.WriteLine("Email Is Empty");
                //        continue;
                //    }

                //    if (expiration == default)
                //    {
                //        Console.WriteLine($"Expiration could not be determined for email {email}");
                //        continue;
                //    }

                //    var teUser = teUsers.FirstOrDefault(te => email.Is((string)te?["email"]));
                //    if (teUser == null)
                //    {
                //        Console.WriteLine($"TE User not found for email {email}");
                //        continue;
                //    }

                //    if (expiration > DateTime.Today)
                //    {
                //        var isInMaverick = teUser?["teams"]?.Any(t => (string)t?["team_name"] == "Maverick Subscribers");
                //        Console.WriteLine($"email: In Maverick? {isInMaverick}");

                //        //var userID = (int)teUser?["id"];
                //        //if (userID != default)
                //        //{
                //        //    var userUpdate = await te.UpdateUser(userID, new { teams = new[] { new { team_name = "Maverick Subscribers" } } });
                //        //    var userValidation = await te.GetUser(userID);
                //        //}
                //    }

                //Console.WriteLine($"FOUND{(expiration <= DateTime.Today ? $" - Expired: {expiration}" : "")}");
                // }
                // var teams = await te.CreateTeam("Test Team");
                // var user = await te.UpdateUser(102062, new { teams = new[] { new { team_name = "Maverick Subscribers" } } });
                //   var users = await te.GetUsers();
                // var teams = await te.GetTeams();
                // var contacts = await te.GetContacts();
                //var contact = await te.CreateContacts(new[] { 
                //    new Contact
                //    {
                //        FirstName = "John",
                //        LastName = "Smith",
                //        Source = "API",
                //        Email = "Test2@testemail.com"
                //    },
                //    new Contact
                //    {
                //        FirstName = "Jane",
                //        LastName = "Smith",
                //        Source = "API",
                //        Email = "Test3@testemail.com",
                //        Owner = new Owner
                //        {
                //            Email = "NTL-test-lo2@example.com"
                //        }
                //    }
                //});
                //  }

                // await TotalExpertTests.Run(_settings.TotalExpert_Dev, _settings.AzureAD);
                //await EncompassTests.Run(_settings.Encompass);
                //await VelocifyTests.Run(_settings.Velocify);

                //using (var credit = new CreditClient(_settings.CreditAPI))
                //{
                //    var report = await credit.RetrieveExisting("1128530", new BorrowerInformation
                //    {
                //        FirstName = "Ima",
                //        LastName = ocessXML(report);
                //}
                */
                #endregion
                
            }
            catch (Exception e)
            {
                e.PrintFormattedMessage();
            }
        }
    }
}
