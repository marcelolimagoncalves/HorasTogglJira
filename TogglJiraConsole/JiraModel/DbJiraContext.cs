using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TogglJiraConsole.JiraModel
{
    public class DbJiraContext
    {
        public void ConsultarWorkLogs()
        {
            using (var connection = new MySql.Data.MySqlClient.MySqlConnection("Server=localhost;Database=world;Uid=root;Pwd=1qazxsw2@;"))
            {
                connection.Open();
                using (var command = new MySql.Data.MySqlClient.MySqlCommand("SELECT * FROM city", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

    }
}
