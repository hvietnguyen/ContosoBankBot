using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContosoBankBot.Model
{
    public class User
    {
        public string ID { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public string Version { get; set; }
        public bool Deleted { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}