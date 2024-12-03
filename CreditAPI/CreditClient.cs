using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SharedResources
{
    public class CreditClient : IDisposable
    {
        private static readonly string _baseAddressProd = "https://credit.creditplus.com/inetapi/request_products.aspx";
        private static readonly string _baseAddressDev = "https://demo.mortgagecreditlink.com/inetapi/request_products.aspx";

        private readonly RestClient _restClient;
        private readonly string _sessionBaseAddress;
        private readonly CredentialSettings _settings;
        public CreditClient(CredentialSettings settings)
        {
            _settings = settings;
            _sessionBaseAddress = _settings.Environment == EnvironmentType.Production ? _baseAddressProd : _baseAddressDev;
            _restClient = new RestClient();
        }

        public async Task<XDocument> RetrieveExisting(string orderId, BorrowerInformation borrower1, BorrowerInformation borrower2 = null)
        {
            var renderedXML = Operations.RenderFromTemplate(
                 Assembly.GetExecutingAssembly(),
                 $"SharedResources.Resources.{(borrower2 == null ? "Individual_RetrieveExisting.xml" : "Joint_RetrieveExisting.xml")}",
                 new
                 {
                     Borrower1_FirstName = borrower1?.FirstName,
                     Borrower1_LastName = borrower1?.LastName,
                     Borrower1_SSN = borrower1?.SSN == null ? null : Regex.Replace(borrower1.SSN, @"[^0-9]", ""),
                     Borrower2_FirstName = borrower2?.FirstName,
                     Borrower2_LastName = borrower2?.LastName,
                     Borrower2_SSN = borrower2?.SSN == null ? null : Regex.Replace(borrower2.SSN, @"[^0-9]", ""),
                     OrderId = orderId
                 });

            var response = await SendRequest(_sessionBaseAddress, renderedXML);
            return ProcessResults(response);
        }

        //Seems to only work in production
        public async Task<XDocument> RetrieveExisting(string orderId, string borrower1SSN, string borrower2SSN = null)
        {
            var renderedXML = Operations.RenderFromTemplate(
                 Assembly.GetExecutingAssembly(),
                 $"SharedResources.Resources.{(borrower2SSN == null ? "Individual_RetrieveExistingLite.xml" : "Joint_RetrieveExistingLite.xml")}",
                 new
                 {
                     Borrower1_SSN = borrower1SSN == null ? null : Regex.Replace(borrower1SSN, @"[^0-9]", ""),
                     Borrower2_SSN = borrower2SSN == null ? null : Regex.Replace(borrower2SSN, @"[^0-9]", ""),
                     OrderId = orderId
                 });

            var response = await SendRequest(_sessionBaseAddress, renderedXML);
            return ProcessResults(response);
        }

        XDocument ProcessResults(WebRequestResponse response)
        {
            if (response.IsError)
                throw new WebException($"{response.StatusCode}, {response.Reason}: {response.ResponseBody}");
            using (MemoryStream stream = new MemoryStream(Encoding.Default.GetBytes(response.ResponseBody)))
            {
                XDocument report = XDocument.Load(stream);
                var ns = report.Root.Name.Namespace;

                var errors = report?.Descendants(ns + "ERRORS")?.ToList();
                if (errors?.Any() == true)
                {
                    var errorMessages = errors
                        ?.Descendants(ns + "ERROR_MESSAGE")
                        ?.Select(em =>
                        {
                            var statusCode = em.Element(ns + "ErrorMessageCategoryCode")?.Value;
                            var message = em.Element(ns + "ErrorMessageText")?.Value;
                            return $"{(!string.IsNullOrWhiteSpace(statusCode) ? $"{statusCode} :" : "")}{em.Element(ns + "ErrorMessageText")?.Value}";
                        });

                    throw new Exception($"Errors returned from Credit API: {(errorMessages?.Any() == true ? string.Join(Environment.NewLine, errorMessages) : "")}");
                }
                var status = report.Root?.NestedElement("DEAL_SETS/DEAL_SET/DEALS/DEAL/SERVICES/SERVICE/STATUSES/STATUS");
                if (status != null && status?.Element(ns + "StatusCode")?.Value == "Error")
                    throw new Exception($"Errors returned from Credit API Service: {status?.Element(ns + "StatusDescription")?.Value}");
                return report;
            }
        }

        async Task<WebRequestResponse> SendRequest(string requestUri, string body = null, HttpMethod method = null, string contentType = "application/xml")
        {
            using (var request = new HttpRequestMessage(method ?? HttpMethod.Post, requestUri))
            {
                return await _restClient.Send(
                    requestUri,
                    body != null ? new StringContent(body, Encoding.ASCII, contentType) : null,
                    method,
                    new Dictionary<string, string> {
                        { "MCL-Interface", _settings.Environment == EnvironmentType.Production ? "nationslending05102016" : "SmartAPITestingIdentifier" },
                        { "Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.Username}:{_settings.Password}"))}" }
                    }).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            ((IDisposable)_restClient).Dispose();
        }
    }
}
