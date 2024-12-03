using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharedResources
{
    public class TotalExpertClient : IDisposable
    {
        private TotalExpertTokenProvider _tokenProvider { get; set; }
        private RestClient _client;

        public TotalExpertClient(ClientSettings clientSettings, bool useTokenBackup = true, string azureClientID = null, string azureClientSecret = null)
        {
            _tokenProvider = new TotalExpertTokenProvider(clientSettings, useTokenBackup, azureClientID, azureClientSecret);
            _client = new RestClient(clientSettings.BaseAddress);
        }

        public TotalExpertClient(string azureClientID, string azureClientSecret, TotalExpertEnvironment environment = TotalExpertEnvironment.Production)
        {
            var settings = new ClientSettings
            {
                Environment = environment.ToString(),
                BaseAddress = environment == TotalExpertEnvironment.Production ? "https://public.totalexpert.net/v1" : "https://public.ct.totalexpert.net/v1"
            };
            _tokenProvider = new TotalExpertTokenProvider(settings, true, azureClientID, azureClientSecret);
            _client = new RestClient(settings.BaseAddress);
        }

        public async Task<TokenAuthentication> GetTokenDetails() => await _tokenProvider.GetTokenDetails();
        public async Task<JObject> GetUser(int id) => await GetResponse($"/users/{id}") as JObject;
        public async Task<JArray> GetUsers() => await GetAllObjects("/users");
        public async Task<JObject> UpdateUser(long id, object update) => (await GetResponse($"/users/{id}", update, new HttpMethod("PATCH"))) as JObject;

        public async Task<JArray> GetContactGroups() => await GetAllObjects("/contact-groups");
        public async Task<JObject> GetContactGroup(long id) => (await GetResponse($"/contact-groups/{id}")) as JObject;
        public async Task<JArray> GetContacts() => await GetAllObjects("/contacts");
        public async Task<JArray> GetTeams() => await GetAllObjects("/teams");
        public async Task<JObject> CreateTeam(string teamName, string[] managers = null)
            => (await GetResponse("/teams", new { team_name = teamName, managers = managers?.Select(m => new { username = m }) })) as JObject;
        public async Task<JObject> GetContact(long id) => (await GetResponse($"/contacts/{id}")) as JObject;
        public async Task<JObject> CreateContact(Contact contact)
        {
            JObject response;
            try
            {
                response = (await GetResponse("/contacts", contact)) as JObject;
            }
            catch (Exception e)
            {
                response = new JObject { ["error"] = e.Message };
            }
            //Serializing to filter out nulls
            var originObject = JObject.Parse(JsonConvert.SerializeObject(contact, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            originObject.Merge(response);
            return originObject;
        }

        //TE has an endpoint call limit of 1000 per minute. Split these batches and make sure to wait until 1 minute is up before starting the next
        //in testing - 900 p/m caused gateway errors and service unavailable. Switching to 450 per 30 seconds to eleviate the issues
        //450 per 30 seconds also caused issues. Switching to 500 p/60sec
        public async Task<JArray> CreateContacts(IEnumerable<Contact> contacts, bool outputProgress = false, bool _1x1 = false)
        {
            var allResponses = new JArray();
            DateTime? startTime = null;

            int batchEnd = 0;
            var sw = new Stopwatch();

            foreach (var batch in contacts.Batch(500))
            {
                int batchStart = batchEnd + 1;
                batchEnd += batch.Length;
                string batchID = $"{batchStart}-{batchEnd}";

                var waitTime = startTime == null ? 0 : (60000 - (DateTime.UtcNow - (DateTime)startTime).TotalMilliseconds);
                if (waitTime > 0)
                    await Task.Delay(TimeSpan.FromMilliseconds(waitTime));
                startTime = DateTime.UtcNow;

                sw.Restart();
                if (_1x1)
                {
                    foreach (var l in batch)
                        allResponses.Add(await CreateContact(l));
                }
                else
                    allResponses.Merge(await Task.WhenAll(batch.Select(l => CreateContact(l))));

                if (outputProgress)
                    Console.WriteLine($"Batch '{batchID}', started: {startTime}, completed in: " + sw.Elapsed);
            }
            return allResponses;
        }

        public async Task<JArray> CreateContactsTimeTracking(IEnumerable<Contact> contacts)
        {
            var sw = new Stopwatch();
            var additionTracker = new ConcurrentBag<long>();
            var responses = await Task.WhenAll(contacts.Select(async contact =>
            {
                loopstart:
                if (additionTracker.Count >= 900 && (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - additionTracker.OrderByDescending(a => a).Take(900).Last()) < 61)
                    goto loopstart;

                additionTracker.Add(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                return await CreateContact(contact);
            }));

            return new JArray(responses);
        }

        //UNTESTED
        public async Task<JArray> CreateContactsAsync(IEnumerable<Contact> contacts, bool outputProgress = false, int degreeOfParallelism = 8)
        {
            var allResponses = new JArray();
            DateTime? startTime = null;

            int batchEnd = 0;
            var sw = new Stopwatch();

            foreach (var batch in contacts.Batch(900))
            {
                int batchStart = batchEnd + 1;
                batchEnd += batch.Length;
                string batchID = $"{batchStart}-{batchEnd}";

                var waitTime = startTime == null ? 0 : (60000 - (DateTime.UtcNow - (DateTime)startTime).TotalMilliseconds);
                if (waitTime > 0)
                    await Task.Delay(TimeSpan.FromMilliseconds(waitTime));
                startTime = DateTime.UtcNow;

                sw.Restart();
                var bag = new ConcurrentBag<JObject>();

                Parallel
                    .ForEach(
                        batch,
                        new ParallelOptions { MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, degreeOfParallelism) },
                        l => bag.Add(CreateContact(l).Result));
                allResponses.Merge(bag);

                if (outputProgress)
                    Console.WriteLine($"Batch '{batchID}', started: {startTime}, completed in: " + sw.Elapsed);
            }
            return allResponses;
        }

        protected async Task<JArray> GetAllObjects(string uri, string rootObjectName = "items", int pageSize = 100)
        {
            var objects = new JArray();
            int? pageNumber = 1;
            do
            {
                var response = await SendRequest($"{uri}{(uri.Contains("?") ? "&" : "?")}page[number]={pageNumber}&page[size]={pageSize}");
                if (response.IsError || response.ResponseBody == null)
                    throw new Exception($"Response did not indicate success. {response.StatusCode}, {response.Reason}: {response.ResponseBody}");

                var jObj = JObject.Parse(response.ResponseBody);
                pageNumber = (int?)jObj?["links"]?["next"];
                objects.Merge((JArray)jObj?[rootObjectName]);
            } while (pageNumber != null);

            return objects;
        }

        protected async Task<JToken> GetResponse(string uri, object body = null, HttpMethod method = null)
        {
            var response = await SendRequest(uri, httpMethod: method ?? (body == null ? HttpMethod.Get : HttpMethod.Post), body: body);
            if (response.IsError)
                throw new Exception($"Response did not indicate success. {response.StatusCode}, {response.Reason}: {response.ResponseBody}");
            return string.IsNullOrWhiteSpace(response.ResponseBody) ? null : JToken.Parse(response.ResponseBody);
        }

        private async Task<WebRequestResponse> SendRequest(string requestUri, bool encodeReponse = false, string accept = "application/json", HttpMethod httpMethod = null, object body = null)
        {
            return await _client.Send(requestUri, body, httpMethod ?? HttpMethod.Get, new Dictionary<string, string> { { "Accept", accept }, { "Authorization", $"Bearer {await _tokenProvider.GetToken()}" } }, encodeReponse);
        }

        public void Dispose()
        {
            ((IDisposable)_client).Dispose();
        }
    }
}
