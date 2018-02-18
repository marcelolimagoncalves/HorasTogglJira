using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TogglJiraService
{
    [XmlRoot(ElementName = "User")]
    public class User
    {
        [XmlElement(ElementName = "xNome")]
        public string XNome { get; set; }
        [XmlElement(ElementName = "xTokenToggl")]
        public string XTokenToggl { get; set; }
        [XmlElement(ElementName = "xTokenJira")]
        public string xTokenJira { get; set; }
    }

    [XmlRoot(ElementName = "Users")]
    public class Users
    {
        public Users()
        {
            User = new List<User>();
        }
        [XmlElement(ElementName = "User")]
        public List<User> User { get; set; }
    }
}
