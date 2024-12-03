using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;

namespace SharedResources
{
    public class FaultFormatingBehavior : IEndpointBehavior
    {
        public void Validate(ServiceEndpoint endpoint) { }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new FaultMessageInspector());
        }
    }

    public class FaultMessageInspector : IClientMessageInspector
    {
        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            if (reply.IsFault)
            {
                using (XmlDictionaryReader reader = reply.GetReaderAtBodyContents())
                {
                    XmlDocument document = new XmlDocument();
                    document.Load(reader);
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
                    XmlNode fault = document.FirstChild;
                    XmlNode errorCodeNode = fault.SelectSingleNode("FaultCode");
                    XmlNode errorMessageNode = errorCodeNode.NextSibling.SelectSingleNode("FaultString");
                    throw new FaultException(new FaultReason(errorMessageNode?.InnerText), new FaultCode(errorCodeNode?.InnerText), "");
                }
            }
        }
    }
}