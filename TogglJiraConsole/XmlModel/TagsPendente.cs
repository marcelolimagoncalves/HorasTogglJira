using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TogglJiraConsole.XmlModel
{
    [XmlRoot(ElementName = "tags")]
    public class TagsPendente
    {
        [XmlElement(ElementName = "tag")]
        public List<string> Tag { get; set; }
    }
}
