using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedResources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SharedResources
{
    public class EncompassClient : IDisposable
    {
        private RestClient _client;
        private string _baseAddress = "https://api.elliemae.com/encompass/v1";
        private string _baseAddress_v3 = "https://api.elliemae.com/encompass/v3";
        private EncompassTokenProvider _tokenProvider;

        private static string[] _approvedLicenseStatuses = new[] { "Approved", "Approved - Conditional", "Approved - Deficient", "Approved - On Appeal" };
        public EncompassClient(EncompassSettings settings)
        {
            _client = new RestClient(_baseAddress);
            _tokenProvider = new EncompassTokenProvider(settings);
        }

        public EncompassClient(string username, string password, string clientID, string clientSecret, string instanceID)
        {
            _client = new RestClient(_baseAddress);
            _tokenProvider = new EncompassTokenProvider(
                new EncompassSettings { ClientID = clientID, ClientSecret = clientSecret, Username = username, Password = password, InstanceID = instanceID });
        }

        public async Task<JObject> GetLoanFields(string loanId, string[] fields = null)
        {
            var jObj = new JObject()
            {
                ["GUID"] = loanId
            };

            fields = (fields ?? new[] {
                "GUID",
                "364", // Loan Number
                "2",
                "3",
                "3238",
                "1823",
                "11",
                "12",
                "14",
                "15",
                "315",
                "326",
                "324",
                "3237",
                "3629",
                "319",
                "313",
                "321",
                "323",
                "LoanTeamMember.Email.Loan Officer"
            })
            ?.OrderBy(a => a)
            ?.ToArray();

            var response = await SendRequest($"/loans/{loanId}/fieldReader?includeMetadata=true", httpMethod: HttpMethod.Post, body: fields);
            if (response.IsError)
            {
                var responseObj = !string.IsNullOrWhiteSpace(response.ResponseBody) ? JObject.Parse(response.ResponseBody) : new JObject();
                responseObj["reason"] = response.Reason;
                responseObj["code"] = response.StatusCode.ToString();
                throw new Exception(responseObj.ToString());
            }

            var responseJson = JArray.Parse(response.ResponseBody);
            foreach (var field in responseJson)
            {
                string id = (string)field["fieldId"];
                if (id == "364") id = "LoanNumber";
                if (id != "GUID" && jObj.ContainsKey(id))
                    id = fields
                        ?.Where(a => a.StartsWith($"{id}#"))
                        ?.OrderBy(a => a) //redundant but just in case
                        ?.First(a => !jObj.ContainsKey(a));

                jObj[id] = (string)field?["value"];
            }
            return jObj;
        }

        public async Task<JArray> GetUserLicenses(string userID, bool excludeNonActiveLicenses = true, bool excludeNonApprovedLicenses = true, bool excludeExpiredLicenses = true)
        {
            var response = await SendRequest($"/company/users/{userID}/licenses");
            if (response.IsError && response.StatusCode != HttpStatusCode.NotFound)
                throw new Exception(response.Reason);

            IEnumerable<JToken> licenses = string.IsNullOrWhiteSpace(response.ResponseBody) || response.StatusCode == HttpStatusCode.NotFound
                ? new JArray()
                : JArray.Parse(response.ResponseBody);

            if (excludeNonActiveLicenses) licenses = licenses.Where(l => (bool)l["enabled"] == true);
            if (excludeNonApprovedLicenses) licenses = licenses.Where(l => _approvedLicenseStatuses.Contains((string)l["status"], StringComparer.InvariantCultureIgnoreCase));
            if (excludeExpiredLicenses) licenses = licenses.Where(l => !DateTime.TryParse((string)l?["expirationDate"], out DateTime expiration) || expiration >= DateTime.Today);

            return JArray.FromObject(licenses);
        }

        public async Task<JArray> GetUsers()
            => await GetAllItems("/company/users?limit=1000");

        public async Task<JArray> GetOrganizations()
             => await GetAllItems("/organizations?limit=1000");

        public async Task<JArray> GetLoanBorrowers(string loanId)
        {
            var response = await SendRequest($"/loans/{loanId}/applications");
            var responseJson = JArray.Parse(response.ResponseBody);

            return responseJson;
        }

        public async Task<JArray> GetUsersIncludingLicenses(
            bool licensesForActiveOnly = true,
            bool excludeNonActiveLicenses = true,
            bool excludeNonApprovedLicenses = true,
            bool excludeExpiredLicenses = true,
            bool condensed = true)
        {
            var users = await GetUsers();
            foreach (var batch in (licensesForActiveOnly
                ? users.Where(u => ((JArray)u?["userIndicators"])?.Select(i => (string)i)?.Contains("Enabled") == true)
                : users)?.Batch(20)) //batches of 20 to avoid 429 errors
            {
                await Task.WhenAll(batch.Select(async user =>
                {
                    int wait = 1;
                    loopstart:
                    try
                    {
                        var licenses = await GetUserLicenses((string)user["id"], excludeNonActiveLicenses, excludeNonApprovedLicenses, excludeExpiredLicenses);
                        user["licenses"] = licenses?.Count > 0
                            ? (condensed ? new JArray(licenses?.Select(l => (string)l?["state"])?.ToArray()) : licenses)
                            : new JArray();
                    }
                    catch
                    {
                        if (wait > 64)
                            throw;
                        await Task.Delay(TimeSpan.FromSeconds(wait));
                        wait *= 2;
                        goto loopstart;
                    }
                }));
            }
            return users;
        }

        public async Task<JArray> GetLoanAttachments(string loanId, string[] attachmentIds)
        {
            var response = await SendRequest($"{_baseAddress_v3}/loans/{loanId}/attachmentDownloadUrl", body: new { attachments = attachmentIds });
            var responseJson = JObject.Parse(response.ResponseBody)?["attachments"] as JArray;
            return responseJson;
        }

        public async Task<Dictionary<string, byte[]>> DownloadLoanAttachments(string loanId, string[] attachmentIds)
        {
            var dict = new Dictionary<string, byte[]>();
            var attachments = await GetLoanAttachments(loanId, attachmentIds);

            foreach (var attachment in attachments)
            {
                var url = (string)attachment["originalUrls"].First();
                dict.Add((string)attachment?["id"], (await SendRequest(url, encodeResponse: true)).ResponseBytes);
            }
            return dict;
        }

        public async Task<JArray> GetLoanDocuments(string loanId, string[] documentNames = null, string borrowerPairId = null)
        {
            var response = await SendRequest($"/loans/{loanId}/documents");
            if (response.IsError)
                throw new WebException($"{response.StatusCode}:{response.Reason}, {response.ResponseBody}");

            IEnumerable<JToken> responseJson = JArray.Parse(response.ResponseBody)
                ?.OrderByDescending(a => (DateTime?)a["dateCreated"]);

            if (documentNames?.Any() == true)
                responseJson = responseJson?.Where(a => documentNames.Contains((string)a?["title"], StringComparer.InvariantCultureIgnoreCase));

            if (borrowerPairId != null)
                responseJson = responseJson?.Where(a => ((string)a?["applicationId"]).Is(borrowerPairId) == true);

            return new JArray(responseJson?.ToArray());
        }

        public async Task<JArray> GetLoanDocuments(string loanId, string documentName, string borrowerPairId = null)
            => await GetLoanDocuments(loanId, new[] { documentName }, borrowerPairId);

        public async Task<string> GetCreditReferenceNumberFromDocuments(string loanId, string borrowerPairId = null)
        {
            var latestComment = (await GetLoanDocuments(loanId, "Credit Report", borrowerPairId))
                ?.Where(a =>
                {
                    var comments = a?["comments"];
                    if (comments != null
                        && comments.Type == JTokenType.Array
                        && ((JArray)comments).Count > 0
                        && ((JArray)comments)?.Any(b => ((string)b?["comments"])?.Contains("Reference Number:") == true) == true
                    )
                        return true;
                    return false;
                })
                ?.SelectMany(a => (JArray)a["comments"])
                ?.OrderByDescending(a => (DateTime?)a["dateCreated"])
                ?.FirstOrDefault();

            if (latestComment?.Type == JTokenType.Object)
            {
                var comments = latestComment?["comments"];
                var refID = Regex.Match((string)latestComment["comments"], @"\(Reference Number:(.*?)\)")?.Groups?[1]?.Value;
                return refID;
            }
            return null;
        }

        public async Task DeleteExistingDocuments(string loanId, string[] documentNames, bool invalidateAttachments = true, DateTime? createdDate = null)
        {
            var documents = (await GetLoanDocuments(loanId))
                ?.Where(a => documentNames.Contains((string)a?["title"], StringComparer.InvariantCultureIgnoreCase));

            if (createdDate != null)
                documents = documents.Where(d => DateTime.TryParse((string)d?["dateCreated"], out DateTime created)
                    ? createdDate?.Date.Equals(created.ToLocalTime().Date) == true
                    : false
                );

            if (documents?.Any() == true)
            {
                foreach (var document in documents)
                {
                    if (invalidateAttachments)
                    {
                        var attachments = (JArray)document?["attachments"];
                        if (attachments?.Any() == true)
                        {
                            foreach (var attachment in attachments)
                                await UpdateAttachment(loanId, (string)attachment?["entityId"], $"{(string)attachment?["entityName"]} - REMOVED", false);
                        }
                    }
                    await DeleteDocument(loanId, (string)document?["documentId"]);
                }
            }
        }

        public async Task<JArray> GetLoans(IEnumerable<string> numbers, IEnumerable<Sort> sort = null, string[] fields = null, bool useCursor = false)
        {
            var allResults = new JArray();
            foreach (var batch in numbers.Batch(1000))
            {
                var filter = new Filter(Operator.Or, batch.Select(num => new Filter("Loan.LoanNumber", num))?.ToArray());
                allResults.Merge(await (useCursor ? GetLoansWithCursor(filter, sort, fields) : GetLoans(filter, sort, fields)));
            }
            return allResults;
        }

        public async Task<JArray> GetLoans(Filter filter, IEnumerable<Sort> sort = null, string[] fields = null)
        {
            var request = new PipelineRequest(
                filter,
                fields != null ?
                new string[] {
                    "Loan.LoanName",
                    "Loan.LoanNumber",
                    "Loan.LoanFolder",
                    "Loan.LockStatus",
                    "Fields.GUID"
                }.Union(fields)
                : new string[] {
                    "Loan.LoanName",
                    "Loan.LoanNumber",
                    "Loan.LoanFolder",
                    "Loan.LockStatus",
                    "Fields.GUID"
                },
                sort
            );

            var response = await SendRequest($"/loanPipeline?limit=20000", httpMethod: HttpMethod.Post, body: request.RenderJson());
            if (response.IsError)
                throw new Exception(JsonConvert.SerializeObject(new
                {
                    Details = "An error occurred while retrieving Encompass data.",
                    StatusCode = response.StatusCode,
                    StatusReason = response.Reason,
                    Information = string.IsNullOrWhiteSpace(response.ResponseBody)
                    ? null
                    : (Operations.TryParse(response.ResponseBody, out JToken value) ? value : response.ResponseBody)
                }));

            if (string.IsNullOrWhiteSpace(response.ResponseBody))
                throw new Exception("No results returned from Encompass.");

            return new JArray(JArray.Parse(response.ResponseBody).Select(loan => loan?["fields"]));
        }

        public async Task<JArray> GetLoansWithCursor(Filter filter, IEnumerable<Sort> sort = null, string[] fields = null)
        {
            var targetFields = fields != null ?
                new string[] {
                    "Loan.LoanName",
                    "Loan.LoanNumber",
                    "Loan.LoanFolder",
                    "Loan.LockStatus",
                    "Fields.GUID"
                }.Union(fields)
                : new string[] {
                    "Loan.LoanName",
                    "Loan.LoanNumber",
                    "Loan.LoanFolder",
                    "Loan.LockStatus",
                    "Fields.GUID"
                };

            var request = new PipelineRequest(filter, targetFields, sort);

            var items = new JArray();
            var cursorResult = await SendRequest($"/loanPipeline?cursorType=randomAccess", httpMethod: HttpMethod.Post, body: request.RenderJson());

            if (cursorResult.IsError)
                throw new Exception(JsonConvert.SerializeObject(new
                {
                    Details = "An error occurred while retrieving creating pipeline cursor in Encompass",
                    Information = string.IsNullOrWhiteSpace(cursorResult.ResponseBody) ? null : JObject.Parse(cursorResult.ResponseBody)
                }));

            var primaryResults = JArray.Parse(cursorResult.ResponseBody);
            var nextPageStart = primaryResults.Count;
            items.Merge(primaryResults?.Select(a => a?["fields"]));

            var total = cursorResult.ResponseHeaders.TryGetValue("X-Total-Count", out IEnumerable<string> totals)
                ? int.TryParse(totals?.FirstOrDefault(), out int totalInt) ? totalInt : 0
                : 0;

            var cursor = cursorResult.ResponseHeaders.TryGetValue("X-Cursor", out IEnumerable<string> cursors)
                ? cursors?.FirstOrDefault()
                : null;

            request.Filter = null;
            var pageRequestBody = request.RenderJson();

            while (items.Count < total)
            {
                var pageResults = await SendRequest($"/loanPipeline?cursor={cursor}&start={nextPageStart}", httpMethod: HttpMethod.Post, body: pageRequestBody);
                if (pageResults.IsError)
                    throw new Exception(JsonConvert.SerializeObject(new
                    {
                        Details = "An error occurred while retrieving Encompass data.",
                        Information = string.IsNullOrWhiteSpace(pageResults.ResponseBody) ? null : JObject.Parse(pageResults.ResponseBody)
                    }));
                var jsonResult = JArray.Parse(pageResults.ResponseBody);
                nextPageStart += jsonResult.Count;
                items.Merge(jsonResult?.Select(a => a?["fields"]));
            }

            return items;
        }

        public async Task<List<T>> GetAllLoans<T>(Filter filter, IEnumerable<string> fields, IEnumerable<Sort> sort = null)
        {
            var request = new PipelineRequest(filter, fields, sort);
            var items = new List<T>();
            var cursorResult = await SendRequest($"/loanPipeline?cursorType=randomAccess", httpMethod: HttpMethod.Post, body: request.RenderJson());
            if (cursorResult.IsError)
                throw new Exception(JsonConvert.SerializeObject(new
                {
                    Details = "An error occurred while retrieving creating pipeline cursor in Encompass",
                    Information = string.IsNullOrWhiteSpace(cursorResult.ResponseBody) ? null : JObject.Parse(cursorResult.ResponseBody)
                }));

            items.AddRange(JArray.Parse(cursorResult.ResponseBody)
                    ?.Select(a => (a?["fields"] as JObject).ToObject<T>())
                    ?.ToList());

            var nextPageStart = items.Count;

            var total = cursorResult.ResponseHeaders.TryGetValue("X-Total-Count", out IEnumerable<string> totals)
                ? int.TryParse(totals?.FirstOrDefault(), out int totalInt) ? totalInt : 0
                : 0;

            var cursor = cursorResult.ResponseHeaders.TryGetValue("X-Cursor", out IEnumerable<string> cursors)
                ? cursors?.FirstOrDefault()
                : null;

            request.Filter = null;
            var pageRequestBody = request.RenderJson();

            while (items.Count < total)
            {
                var pageResults = await SendRequest($"/loanPipeline?cursor={cursor}&start={nextPageStart}", httpMethod: HttpMethod.Post, body: pageRequestBody);
                if (pageResults.IsError)
                    throw new Exception(JsonConvert.SerializeObject(new
                    {
                        Details = "An error occurred while retrieving Encompass data.",
                        Information = string.IsNullOrWhiteSpace(pageResults.ResponseBody) ? null : JObject.Parse(pageResults.ResponseBody)
                    }));

                items.AddRange(JArray.Parse(pageResults.ResponseBody)
                        ?.Select(a => (a?["fields"] as JObject).ToObject<T>())
                        ?.ToList());

                nextPageStart += items.Count;
            }

            return items;
        }

        public async Task<bool> UpdateAttachment(string loanId, string attachmentId, string title = null, bool? isActive = null)
        {
            var response = await SendRequest($"/loans/{loanId}/attachments/{attachmentId}", httpMethod: new HttpMethod("PATCH"), body: JsonConvert.SerializeObject(new
            {
                title,
                isActive
            }, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            return !response.IsError;
        }

        public async Task<bool> UpdateLoanAssociate(string loanId, string newAssociateId, string associateRole = "Loan Officer")
        {
            var logEntries = await GetLoanAssociateRoles(loanId);
            if (logEntries?.Count > 0)
            {
                string[] ids = logEntries
                    ?.Where(a => associateRole.Is((string)a?["loanAssociate"]?["roleName"]))
                    ?.Select(a => (string)a?["id"])
                    ?.ToArray();

                if (ids?.Length > 0)
                {
                    var updateBody = $@"{{""loanAssociateType"":""User"",""id"":""{newAssociateId}""}}";
                    foreach (var associateId in ids)
                    {
                        var response = await SendRequest($"/loans/{loanId}/associates/{associateId}", httpMethod: HttpMethod.Put, updateBody);
                        if (response.IsError)
                            throw new Exception($"Status code: {response?.StatusCode}, Reason: {response?.Reason}, Response: {response?.ResponseBody}");
                    }
                }
            }
            return true;
        }

        public async Task<bool> RemoveLoanAssociate(string loanId, string associateRole)
        {
            var logEntries = await GetLoanAssociateRoles(loanId);
            if (logEntries?.Count > 0)
            {
                string[] ids = logEntries
                    ?.Where(a => associateRole.Is((string)a?["loanAssociate"]?["roleName"]))
                    ?.Select(a => (string)a?["id"])
                    ?.ToArray();

                if (ids?.Length > 0)
                {
                    foreach (var associateId in ids)
                    {
                        var response = await SendRequest($"/loans/{loanId}/associates/{associateId}", httpMethod: HttpMethod.Delete);
                        if (response.IsError)
                            throw new Exception($"Status code: {response?.StatusCode}, Reason: {response?.Reason}, Response: {response?.ResponseBody}");
                    }
                }
            }
            return true;
        }

        public async Task<bool> UpdateLoanAssociateByID(string loanId, string newAssociateId, string associateRoleId)
        {
            var response = await SendRequest($"/loans/{loanId}/associates/{associateRoleId}", httpMethod: HttpMethod.Put,
                $@"{{""loanAssociateType"":""User"",""id"":""{newAssociateId}""}}");
            if (response.IsError)
                throw new Exception($"Status code: {response?.StatusCode}, Reason: {response?.Reason}, Response: {response?.ResponseBody}");
            return true;
        }

        public async Task<bool> RemoveLoanAssociateByID(string loanId, string associateRoleId)
        {
            var response = await SendRequest($"/loans/{loanId}/associates/{associateRoleId}", httpMethod: HttpMethod.Delete);
            if (response.IsError)
                throw new Exception($"Status code: {response?.StatusCode}, Reason: {response?.Reason}, Response: {response?.ResponseBody}");
            return true;
        }

        public async Task<JArray> GetLoanAssociateRoles(string loanId)
        {
            var getmilestoneRoles = await SendRequest($"/loans/{loanId}/milestones");
            if (getmilestoneRoles.IsError)
                throw new Exception($"Status code: {getmilestoneRoles?.StatusCode}, Reason: {getmilestoneRoles?.Reason}, Response: {getmilestoneRoles?.ResponseBody}");

            var getmilestoneFreeRoles = await SendRequest($"/loans/{loanId}/milestoneFreeRoles");
            if (getmilestoneFreeRoles.IsError)
                throw new Exception($"Status code: {getmilestoneFreeRoles?.StatusCode}, Reason: {getmilestoneFreeRoles?.Reason}, Response: {getmilestoneFreeRoles?.ResponseBody}");

            var logEntries = JArray.Parse(getmilestoneRoles.ResponseBody);
            logEntries.Merge(JArray.Parse(getmilestoneFreeRoles.ResponseBody));
            return logEntries;
        }

        public async Task<bool> DeleteDocument(string loanId, string documentId)
        {
            var response = await SendRequest($"/loans/{loanId}/documents/{documentId}", httpMethod: HttpMethod.Delete);
            return !response.IsError;
        }

        //Don't prefix Fields.
        public async Task<bool> BatchUpdateCustomFields(IEnumerable<string> loanGuids, IEnumerable<KeyValuePair<string, string>> values)
        {
            var response = await SendRequest($"/loanBatch/updateRequests", httpMethod: HttpMethod.Post, body: new
            {
                loanData = new { customFields = values?.Select(v => new { fieldName = v.Key, stringValue = v.Value }) },
                loanGuids
            });
            return !response.IsError;
        }

        public async Task<bool> UpdateLoan(string loanGuid, JObject loan)
        {
            var response = await SendRequest($"/loans/{loanGuid}?view=id", httpMethod: new HttpMethod("PATCH"), body: loan);
            if (response.IsError)
                throw new Exception($"Status code: {response?.StatusCode}, Reason: {response?.Reason}, Response: {response?.ResponseBody}");
            return true;
        }

        public async Task<bool> UploadDocumentWithAttachment(byte[] file, string loanId, string documentName, string attachmentName = null, string borrowerId = null)
        {
            //Get Attachment URL
            var title = attachmentName ?? documentName;
            var response = await SendRequest($"/loans/{loanId}/attachments/url?view=entity", httpMethod: HttpMethod.Post, body: new { fileWithExtension = $"{title}.pdf", title, createReason = 1 });

            if (response?.IsError != false || response?.ResponseBody?.IsEmpty() != false)
                throw new Exception($"An error occurred while *CREATING* the encompass *ATTACHMENT*. Status code: {response?.StatusCode}, Reason: {response?.Reason}, Response: {response?.ResponseBody}");

            var responseJson = JObject.Parse(response.ResponseBody);

            //Upload Attachment
            var mediaUrl = (string)responseJson["mediaUrl"];
            //using var content = new ByteArrayContent(file);
            // content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            var uploadResponse = await SendRequest(mediaUrl, httpMethod: HttpMethod.Put, body: file);

            if (uploadResponse?.IsError != false || uploadResponse?.ResponseBody?.IsEmpty() != false)
                throw new Exception($"An error occurred while *UPLOADING* the encompass *ATTACHMENT*. Status code: {uploadResponse?.StatusCode}, Reason: {uploadResponse?.Reason}, Response: {uploadResponse?.ResponseBody}");

            var uploadResponseJson = JObject.Parse(uploadResponse.ResponseBody);
            var attachmentId = (string)uploadResponseJson["attachmentId"];

            //Create document with attachment
            var documentResponse = await SendRequest(
                $"/loans/{loanId}/documents?view=entity",
                httpMethod: HttpMethod.Post,
                body: new
                {
                    title = documentName,
                    applicationId = borrowerId ?? "All",
                    attachments = new[]{
                        new {
                            entityType = "attachment",
                            entityId = attachmentId
                        }
                    }
                }
            );

            if (documentResponse?.IsError != false)
                throw new Exception($"An error occurred while *CREATING* the encompass *DOCUMENT*. Status code: {documentResponse?.StatusCode}, Reason: {documentResponse?.Reason}, Response: {documentResponse?.ResponseBody}");
            //var documentResponseJson = JObject.Parse(documentResponse.ResponseBody);
            return true;
        }

        public async Task<bool> CreateDocument(string loanId, string documentName, string borrowerId = null)
        {
            //Create document with attachment
            var documentResponse = await SendRequest(
                $"/loans/{loanId}/documents?view=entity",
                httpMethod: HttpMethod.Post,
                body: new
                {
                    title = documentName,
                    applicationId = borrowerId ?? "All"
                }
            );

            if (documentResponse?.IsError != false)
                throw new Exception($"An error occurred while *CREATING* the encompass *DOCUMENT*. Status code: {documentResponse?.StatusCode}, Reason: {documentResponse?.Reason}, Response: {documentResponse?.ResponseBody}");
            return true;
        }

        public async Task<JArray> GetAllItems(string uri, string idField = "id")
        {
            var allResults = new JArray();
            var response = await SendRequest(uri);
            if (response.IsError || response.ResponseBody == null)
                throw new Exception(JsonConvert.SerializeObject(new
                {
                    Reason = response.Reason,
                    StatusCode = response.StatusCode,
                    Details = "An error occurred while retrieving Encompass data.",
                    Information = string.IsNullOrWhiteSpace(response.ResponseBody) ? null : JObject.Parse(response.ResponseBody)
                }));

            var result = JArray.Parse(response.ResponseBody);
            allResults.Merge(result);

            var total = response.ResponseHeaders.TryGetValue("X-Total-Count", out IEnumerable<string> totals)
                ? int.TryParse(totals?.FirstOrDefault(), out int totalInt) ? totalInt : 0
                : 0;

            var nextPageStart = result.Count + 1;
            while (allResults.Count < total)
            {
                response = await SendRequest($"{uri}{(uri.Contains("?") ? "&" : "?")}start={nextPageStart}");
                if (response.IsError)
                    throw new Exception(JsonConvert.SerializeObject(new
                    {
                        Reason = response.Reason,
                        StatusCode = response.StatusCode,
                        Details = "An error occurred while retrieving Encompass data.",
                        Information = string.IsNullOrWhiteSpace(response.ResponseBody) ? null : JObject.Parse(response.ResponseBody)
                    }));
                result = JArray.Parse(response.ResponseBody);
                nextPageStart += result.Count;
                allResults.Merge(result);
            }

            //Doing DistinctBy because if someone adds a record while paging it will throw off the collection. 
            return new JArray(allResults?.DistinctBy(a => (string)a?[idField]));
        }


        private async Task<WebRequestResponse> SendRequest(string requestUri, HttpMethod httpMethod = null, object body = null, bool encodeResponse = false, bool retry = true)
        {
            var token = await _tokenProvider.GetToken();

            if (token.IsEmpty())
                throw new Exception("no_token");

            var response = await _client.Send(requestUri, body, httpMethod,
                new[] {
                       new KeyValuePair<string, string>(HttpRequestHeader.Accept.ToString(), "application/json"),
                       new KeyValuePair<string, string>(HttpRequestHeader.Authorization.ToString(), $"Bearer {token}"),
               }, encodeResponse
            );

            if (response.IsError && response.StatusCode != HttpStatusCode.NotFound && retry == true)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    await _tokenProvider.SetNewToken();
                return await SendRequest(requestUri, httpMethod, body, encodeResponse, false);
            }
            return response;
        }

        public void Dispose()
        {
            ((IDisposable)_client).Dispose();
        }
    }
}
