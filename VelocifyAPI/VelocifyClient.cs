using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using VelocifyService;

namespace SharedResources
{
    public interface IVelocifyOptions
    {
        CredentialSettings Velocify { get; set; }
    }

    // For new project adding the velocify service reference,
    // update Nuget package System.NetTcp (and potentially any others) to the latest version (currently 4.8.1) or SOAP 1.2 wont work.
    public class VelocifyClient
    {
        private readonly CredentialSettings _credentials;
        private readonly ClientServiceSoapClient _client;
        public VelocifyClient(CredentialSettings credentials)
        {
            _credentials = credentials;
            _client = new ClientServiceSoapClient(GetBinding(), GetEndpoint());
            _client.Endpoint.EndpointBehaviors.Add(new FaultFormatingBehavior());

        }

        public CustomBinding GetBinding()
        {
            CustomBinding binding = new CustomBinding()
            {
                SendTimeout = TimeSpan.FromMinutes(20),
                ReceiveTimeout = TimeSpan.FromMinutes(20),
                OpenTimeout = TimeSpan.FromMinutes(20),
                CloseTimeout = TimeSpan.FromMinutes(20)
            };

            binding.Elements.AddRange(
                new TextMessageEncodingBindingElement()
                {
                    MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None)
                },
                new HttpsTransportBindingElement()
                {
                    AllowCookies = true,
                    MaxBufferSize = int.MaxValue,
                    MaxBufferPoolSize = int.MaxValue,
                    MaxReceivedMessageSize = int.MaxValue
                }
            );

            return binding;
        }

        public EndpointAddress GetEndpoint()
        {
            return new EndpointAddress("https://service.leads360.com/ClientService.asmx");
        }

        public async Task<Campaign[]> GetCampaigns()
            => Serializer<CampaignList>(await _client.GetCampaignsAsync(_credentials.Username, _credentials.Password)).Campaigns;

        public async Task<CampaignGroup[]> GetCampaignGroups()
           => Serializer<CampaignGroupList>(await _client.GetCampaignGroupsAsync(_credentials.Username, _credentials.Password)).CampaignGroups;

        public async Task<FieldGroup[]> GetFieldGroups()
            => Serializer<FieldGroupList>(await _client.GetFieldGroupsAsync(_credentials.Username, _credentials.Password)).FieldGroups;

        public async Task<Field[]> GetFields()
            => Serializer<FieldList>(await _client.GetFieldsAsync(_credentials.Username, _credentials.Password)).Fields;

        public async Task<Group[]> GetGroups()
            => Serializer<GroupList>(await _client.GetGroupsAsync(_credentials.Username, _credentials.Password)).Groups;

        public async Task<Status[]> GetStatuses()
            => Serializer<StatusList>(await _client.GetStatusesAsync(_credentials.Username, _credentials.Password)).Statuses;
        public async Task<Agent[]> GetAgents()
            => Serializer<AgentList>(await _client.GetAgentsAsync(_credentials.Username, _credentials.Password)).Agents;

        public async Task<Agent> GetAgent(int id)
            => Serializer<AgentList>(await _client.GetAgentAsync(_credentials.Username, _credentials.Password, id)).Agents?.FirstOrDefault();

        public async Task<Lead[]> GetLeads(DateTime? from = null, DateTime? to = null)
            => Serializer<LeadList>(await _client.GetLeadsAsync(_credentials.Username, _credentials.Password, from ?? DateTime.Now.AddDays(-1), to ?? DateTime.Now)).Leads;

        public async Task<JObject> AddLead(NewLead lead, bool mailer = false)
            => await AddLeads(new[] { lead }, mailer);

        public async Task<JObject> AddLeads(IEnumerable<NewLead> leads, bool mailer = false)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var leadsXml = leads.Select(lead => Operations.RenderFromTemplate(assembly, "SharedResources.Resources.Lead.xml", lead, true, true, true));
            string render = Operations.RenderFromTemplate(assembly, "SharedResources.Resources.AddLeads.xml", new { Leads = string.Join(Environment.NewLine, leadsXml) }, true, true);
            
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(render);

            var leadResponse = await (mailer
                ? _client.AddDirectMailLeadsAsync(_credentials.Username, _credentials.Password, doc.DocumentElement)
                : _client.AddLeadsAsync(_credentials.Username, _credentials.Password, doc.DocumentElement));
            return (JObject)leadResponse.ToJson();

        }
        public async Task GetData()
        {
            var test = await AddLead(new NewLead { AgentId = "seth", CampaignId = "1097", BranchManager = "test" });

            //var data = await GetLeads();
            //string render = Operations.RenderFromTemplate(Assembly.GetExecutingAssembly(), "SharedResources.Resources.AddLeads.xml", new { Leads = "" }, true, true);

            //var d = await GetLeads(DateTime.Now.AddHours(-24), DateTime.Now);
            // var data2 = await _client.GetLeadsAsync(_credentials.Username, _credentials.Password,  DateTime.Now.AddDays(-1), DateTime.Now);
            // var data = await _client.GetAgentAsync(_credentials.Username, _credentials.Password, 20);
        }

        //public async Task<string> GetLeads(DateTime from, DateTime? to = null) => (await Try(() => _client.GetLeadsAsync(_username, _password, from, to ?? DateTime.Now)))?.OuterXml;

        private T Serializer<T>(XmlNode root) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (TextReader reader = new StringReader(root.OuterXml))
                return (T)serializer.Deserialize(reader);
        }
    }
}
