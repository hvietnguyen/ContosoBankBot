using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContosoBankBot.Model
{
    public class Account
    {
        public string ID { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public string Version { get; set; }
        public bool Deleted { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string UserID { get; set; }
        public decimal Value { get; set; }
    }
}