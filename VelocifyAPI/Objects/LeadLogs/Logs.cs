using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SharedResources
{
    [Serializable, DesignerCategory("code"), XmlType(AnonymousType = true)]
    public class Logs
    {
        [XmlElement("CreationLog")]
        public CreationLog CreationLog { get; set; }

        [XmlArray("ActionLog"), XmlArrayItem("Action", IsNullable = true)]
        public ActionLog[] ActionLogs { get; set; }

        [XmlArray("DistributionLog"), XmlArrayItem("Distribution", IsNullable = true)]
        public DistributionLog[] DistributionLogs { get; set; }

        [XmlArray("EmailLog"), XmlArrayItem("Email", IsNullable = true)]
        public EmailLog[] EmailLogs { get; set; }

        [XmlArray("StatusLog"), XmlArrayItem("Status", IsNullable = true)]
        public StatusLog[] StatusLogs { get; set; }

        [XmlArray("SyncLog"), XmlArrayItem("Sync", IsNullable = true)]
        public SyncLog[] SyncLogs { get; set; }

        [XmlArray("ExportLog"), XmlArrayItem("Export", IsNullable = true)]
        public ExportLog[] ExportLogs{ get; set; }
    }
}
