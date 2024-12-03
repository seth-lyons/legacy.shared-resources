using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResources
{
    public interface IMailClient
    {
        void SendMail(string subject, string body, string to, IEnumerable<string> cc = null, IEnumerable<string> bcc = null, IEnumerable<(string FileName, byte[] FileData)> attachments = null);
        void SendMail(string subject, string body, IEnumerable<string> to, IEnumerable<string> cc = null, IEnumerable<string> bcc = null, IEnumerable<(string FileName, byte[] FileData)> attachments = null);
        void SendExceptionReport(Exception exception, IEnumerable<string> to = null, string customMessage = null, string customSubject = null);
    }
}
