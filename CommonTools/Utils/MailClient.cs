using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;

namespace SharedResources
{
    public class MailClient : IMailClient, IDisposable
    {
        static MailClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls

        }


        private readonly NetworkCredential _credentials;
        private readonly string _from;
        private readonly string _host;
        private readonly int _port;
        private readonly Dictionary<string, string> _messageHeaders;

        private SmtpClient _client;


        public MailClient(ServerSettings settings, string from = null, Dictionary<string, string> messageHeaders = null)
        {
            _credentials = new NetworkCredential(settings?.Username, settings?.Password);
            _from = from ?? settings?.Username;
            _host = settings?.Host;
            _port = settings?.Port ?? 587;
            _messageHeaders = messageHeaders;

            InitializeClient();
        }

        private void InitializeClient()
        {
            _client = new SmtpClient()
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Port = _port,
                Host = _host,
                Credentials = _credentials
            };
        }

        public void Dispose() => ((IDisposable)_client).Dispose();

        public void SendMail(string subject, string body, string to, IEnumerable<string> cc = null, IEnumerable<string> bcc = null, IEnumerable<(string FileName, byte[] FileData)> attachments = null) =>
            MailHandler(subject, body, new[] { to }, cc, bcc, attachments);

        public void SendMail(string subject, string body, IEnumerable<string> to, IEnumerable<string> cc = null, IEnumerable<string> bcc = null, IEnumerable<(string FileName, byte[] FileData)> attachments = null) =>
            MailHandler(subject, body, to, cc, bcc, attachments);

        private void MailHandler(string subject, string body, IEnumerable<string> to, IEnumerable<string> cc = null, IEnumerable<string> bcc = null, IEnumerable<(string FileName, byte[] FileData)> attachments = null, int retryLimit = 3)
        {
            try
            {
                using (MailMessage message = new MailMessage()
                {
                    From = new MailAddress(_from),
                    Subject = subject,
                    BodyEncoding = UTF8Encoding.UTF8,
                    DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure,
                    IsBodyHtml = true
                })
                {
                    to?.ForEach(address => message.To.Add(address));
                    cc?.ForEach(address => message.CC.Add(address));
                    bcc?.ForEach(address => message.Bcc.Add(address));

                    //doing it after assignment so we cycle the enumerable first
                    if (message.To.Count <= 0 && message.CC.Count <= 0 && message.Bcc.Count <= 0)
                        throw new Exception("No recipients");

                    message.Body = Operations.RenderFromTemplate("EmailTemplate.html", new { Content = body }, useExecutingAssembly: true);

                    List<Attachment> files = attachments
                        ?.Select(attachment =>
                        {
                            Attachment file = new Attachment(new MemoryStream(attachment.FileData), attachment.FileName);
                            message.Attachments.Add(file);
                            return file;
                        })?.ToList();

                    _messageHeaders?.ForEach(a => message.Headers.Add(a.Key, a.Value));
                    _client.Send(message);
                    files?.ForEach(file => { file.ContentStream.Dispose(); file.Dispose(); });
                }
            }
            catch (Exception e)
            {
                if (e is SmtpException && ((SmtpException)e).StatusCode == SmtpStatusCode.MustIssueStartTlsFirst)
                    throw;

                if (retryLimit == 1)
                    InitializeClient();

                if (retryLimit > 0)
                    MailHandler(subject, body, to, cc, bcc, attachments, --retryLimit);
                else
                    throw;
            }
        }

        public void SendExceptionReport(Exception exception, IEnumerable<string> to = null, string customMessage = null, string customSubject = null)
        {
            string assemblyName = Assembly.GetEntryAssembly()?.GetName()?.Name ?? Assembly.GetCallingAssembly()?.GetName()?.Name ?? Assembly.GetExecutingAssembly()?.GetName()?.Name;
            string message =
                $"An exception occurred in program {assemblyName}<br><br>" +
                (
                    customMessage.IsEmpty()
                    ? ""
                    : $"<strong>Custom Message:</strong><br>{customMessage}<br><br>"
                ) +
                $"<strong>Exception Message:</strong><br>{exception.Message}<br><br>" +
                $"<strong>Details:</strong><br>{exception}<br><br>" +
                (
                    exception.Data?.Count > 0
                    ? $"<strong>Data:</strong><br>{string.Join("<br>", exception.Data.Keys.Cast<object>().Select(k => $"{k}: {exception.Data[k]}"))}<br><br>"
                    : ""
                ) +
                (
                    exception.InnerException != null && !string.IsNullOrWhiteSpace(exception.InnerException.Message)
                    ? $"<strong>Inner Exception Details:</strong><br>{exception.InnerException}<br><br>" +
                        (
                             exception.InnerException.Data?.Count > 0
                            ? $"<strong>Inner Exception Data:</strong><br>{string.Join("<br>", exception.Data.Keys.Cast<object>().Select(k => $"{k}: {exception.Data[k]}"))}<br><br>"
                            : ""
                        )
                    : ""
                ) +
                $"<strong>Stack Trace:</strong><br>{exception.StackTrace}<br>";

            MailHandler(customSubject ?? ("Exception Report: " + assemblyName), message, to ?? new[] { "seth.lyons@nationslending.com" });
        }
    }

    public interface IMailOptions
    {
        ServerSettings MailSettings { get; set; }
    }
}
