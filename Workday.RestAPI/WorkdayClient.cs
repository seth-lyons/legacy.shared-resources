using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedResources;

namespace SharedResources
{
    public class WorkdayClient : IDisposable
    {
        private RestClient _client { get; set; }
        private WorkdaySettings _settings { get; set; }

        private string _token;
        public WorkdayClient(WorkdaySettings settings)
        {
            _settings = settings;
            _client = new RestClient();
        }

        public async Task<string> GetToken()
        {
            var result = await _client.Post($"{_settings.BaseAddress}/ccx/oauth2/{_settings.Tenant}/token",
                new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string>("refresh_token", _settings.RefreshToken)
                }),
               new[] {
                       new KeyValuePair<string, string>(HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded"),
                       new KeyValuePair<string, string>(HttpRequestHeader.Authorization.ToString(), $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.ClientID}:{_settings.ClientSecret}"))}"),
               }
            );

            if (result.IsError)
                throw new Exception($"Failed to retrieve access token. {result.StatusCode}, {result.Reason}: {result.ResponseBody}");
            return (string)JObject.Parse(result.ResponseBody)?["access_token"];
        }

        public async Task<bool> UpdateCustomField(string employeeId, string customObjectName, Dictionary<string, object> values, bool contingentWorker = false, bool newTokenOnFail = true)
        {
            if (_token.IsEmpty())
                _token = await GetToken();

            var body = new JObject
            {
                ["worker"] = new JObject { ["id"] = $"{(contingentWorker ? "Contingent_Worker_ID" : "Employee_ID")}={employeeId}" }
            };
            foreach (var value in values)
                body[value.Key] = new JValue(value.Value);

            var result = await _client.Post($"{_settings.BaseAddress}/ccx/api/v1/{_settings.Tenant}/customObjects/{customObjectName}?updateIfExists=true", body, _token);

            if (result.IsError && result.StatusCode == HttpStatusCode.Unauthorized && newTokenOnFail)
            {
                _token = await GetToken();
                return await UpdateCustomField(employeeId, customObjectName, values, contingentWorker, false);
            }
            return !result.IsError;
        }

        public async Task<JArray> GetReport(string reportOwner, string reportName, string[] arrayLabels = null, Dictionary<string, object> queryValues = null, bool jsonDirect = false)
        {
            if (string.IsNullOrWhiteSpace(_settings.Username) || string.IsNullOrWhiteSpace(_settings.Password))
                throw new Exception($"The reporting API uses basic authentication and requires a username and password.");

            reportOwner = reportOwner.Replace(" ", "_");
            reportName = Regex.Replace(reportName, @"[ &]", "_");
            var filter = queryValues.IsEmpty() ? ""
                : $"?{string.Join("&", queryValues.Select(v => $"{v.Key}={(v.Value is DateTime ? ((DateTime)v.Value).ToString("yyyy-MM-dd-HH:mm") : v.Value)}"))}"; // add URL encoding if this becomes a problem

            if (jsonDirect)
                filter = filter.IsEmpty() ? "?format=json" : filter + "&format=json";

            var response = await _client.Get(
                $"{(_settings.BaseAddress ?? "https://services1.myworkday.com")}/ccx/service/customreport2/{(_settings.Tenant ?? "nlcloans")}/{reportOwner}/{reportName}{filter}",
                new[] { new KeyValuePair<string, string>("Authorization", $"Basic {Convert.ToBase64String(Encoding.Default.GetBytes($"{_settings.Username}:{_settings.Password}"))}") }
            );

            if (response.IsError)
                throw new Exception($"Failed to retrieve report. {response.StatusCode}, {response.Reason}: {response.ResponseBody}");

            if (jsonDirect)
                return JObject.Parse(response.ResponseBody)?["Report_Entry"] as JArray;

            var xml = response.ResponseBody.Insert(response.ResponseBody.IndexOf("xmlns:wd"), "xmlns:json=\"http://james.newtonking.com/projects/json\" ");
            xml = Regex.Replace(xml, $@"(<[^</]*?wd:(?:{string.Join("|", arrayLabels?.Any() == true ? arrayLabels.Union(new[] { "Report_Entry" }) : new[] { "Report_Entry" })}).*?)>", "$1 json:Array=\"true\">");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return (JArray)JObject.Parse(JsonConvert.SerializeXmlNode(doc))?["wd:Report_Data"]?["wd:Report_Entry"];
        }

        public async Task<JArray> GetWorkerReport(bool activeOnly = false, bool excludeContingent = false)
          => await GetReport("slyons", "RPT_Workers", new[] { "ID", "Managers", "Assigned_Orgs_at_Hire" },
              new Dictionary<string, object> {
                    { "Active_Only", activeOnly ? "1" : "0" },
                    { "Exclude_Contingent", excludeContingent ? "1" : "0"}
              }
          );

        public async Task<JArray> GetBranchManagerReport()
            => await GetReport("clieber", "Branch Manager List");

        public async Task<JArray> GetBranchReport()
            => await GetReport("clieber", "Location Listing");

        public async Task<JArray> GetWorkerImage()
            => await GetReport("slyons", "RPT_Worker_Photos");

        public async Task<JObject> GetWorkerImage(string employeeID)
            => (JObject)(await GetReport("slyons", "RPT_Worker_Photos", queryValues: new Dictionary<string, object> { { "Employee_ID", employeeID } }))?.FirstOrDefault();

        public async Task<byte[]> GetWorkerImageContent(string employeeID)
        {
            var imageDetails = await GetWorkerImage(employeeID);
            if ((string)imageDetails?["wd:Worker_has_Photo"] != "1")
                return null;
            return Convert.FromBase64String((string)imageDetails?["wd:Photo"]?["wd:attachmentContent"]);
        }

        public void Dispose()
        {
            ((IDisposable)_client).Dispose();
        }
    }
}
