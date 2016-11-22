using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.MobileServices;
using ContosoBankBot.Model;
using System.Threading.Tasks;

namespace ContosoBankBot
{
    public class AzureManager
    {
        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<User> userTable;
        
        public AzureManager()
        {
            this.client = new MobileServiceClient("https://contosobankdb.azurewebsites.net");
            this.userTable = this.client.GetTable<User>();
        } 

        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstance
        {
            get
            {
                if(instance == null) instance = new AzureManager();
                return instance;
            }
        }

        public async Task<List<User>> GetUsers()
        {
            return await this.userTable.ToListAsync();
        }
    }
}