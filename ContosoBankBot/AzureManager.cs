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
        private IMobileServiceTable<Account> accountTable;
        
        public AzureManager()
        {
            this.client = new MobileServiceClient("https://contosobankdb.azurewebsites.net");
            this.userTable = this.client.GetTable<User>();
            this.accountTable = this.client.GetTable<Account>();
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

        public async Task<List<User>> GetUsers(string username, string password)
        {
            return await this.userTable.Where(user=>user.Username==username && user.Password == password).ToListAsync();
        }

        public async Task AddCount(Account account)
        {
            await this.accountTable.InsertAsync(account);
        }

        public async Task UpdateCount(Account account)
        {
            await this.accountTable.UpdateAsync(account);
        }

        public async Task DeleteCount(Account account)
        {
            await this.accountTable.DeleteAsync(account);
        }

        public async Task<List<Account>> GetAccounts(string userID)
        {
            return await this.accountTable.Where(account => account.UserID == userID).ToListAsync();
        }
    }
}