using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Text.RegularExpressions;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Newtonsoft.Json;
using ContosoBankBot.Model;
using System.Collections.Generic;

namespace ContosoBankBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public BotData userData;
        public string userMessage;
        
    
        [ResponseType(typeof(void))]
        public virtual async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

            activity.Recipient.Name = "Contoso Bank Bot";

            userMessage = activity.Text;
            string endOutput = $"Welcome To Contoso Bank Bot Service";

            StateClient stateClient = activity.GetStateClient();
            userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
            User user = (userData.GetProperty<User>("user") != null) ? userData.GetProperty<User>("user") : null;

            if (activity.Type == ActivityTypes.Message)
            {
                if (userData.GetProperty<bool>("isLoginSuccessful"))
                {
                    activity.From.Name = userData.GetProperty<string>("username");
                    
                    // Add new account
                    if (userData.GetProperty<bool>("isAddAccount"))
                    {
                        int count = userData.GetProperty<int>("count");
                        if (count == 1)
                        {
                            string[] pairs = Regex.Split(userMessage, "\\s+");
                            if (pairs.Length > 1)
                            {
                                userMessage = "";
                                foreach (string str in pairs)
                                {
                                    userMessage += str;
                                }
                            }

                            userData.SetProperty<string>("accName", userMessage);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                            endOutput = "Enter Deposit Value";
                         
                            userData.SetProperty<int>("count", 2);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                            Activity quickreply = activity.CreateReply($"{endOutput}");
                            await connector.Conversations.ReplyToActivityAsync(quickreply);
                            return Request.CreateResponse(HttpStatusCode.OK);
                        }
                        else if(count == 2)
                        {
                            bool flag = false;
                            string accValue = userMessage;
                            string accName = userData.GetProperty<string>("accName");
                            List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(user.ID);
                            foreach(Account account in accounts)
                            {
                                if (account.Name.Equals(accName)) flag = true; 
                            }

                            if (!flag)
                            {
                                if (userData.GetProperty<bool>("process"))
                                {
                                    Account account = new Account()
                                    {
                                        Name = accName,
                                        Type = "Saving",
                                        Value = Convert.ToDecimal(accValue),
                                        UserID = user.ID
                                    };

                                    userData.SetProperty<Account>("account", account);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    userData.SetProperty<bool>("process", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                }

                                if (userData.GetProperty<bool>("confirm"))
                                {
                                    await Conversation.SendAsync(activity, () => new EchoDialog());
                                }
                                    
                                string result = userMessage;

                                if (result.ToLower().Equals("yes") || result.ToLower().Equals("1"))
                                {
                                    

                                    userData.SetProperty<int>("count", 0);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    await AzureManager.AzureManagerInstance.AddCount(userData.GetProperty<Account>("account"));
                                    List<Account> list = await AzureManager.AzureManagerInstance.GetAccounts(user.ID);
                                    await connector.Conversations.SendToConversationAsync(DisplayAccount(list, activity));

                                    userData.SetProperty<bool>("confirm", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    userData.SetProperty<bool>("isAddAccount", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    return Request.CreateResponse(HttpStatusCode.OK);
                                }
                                else if(result.ToLower().Equals("no") || result.ToLower().Equals("2"))
                                {
                                    userData.SetProperty<bool>("isAddAccount", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    userData.SetProperty<bool>("confirm", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    endOutput = $"Process is cancelled";
                                    Activity rep = activity.CreateReply($"{endOutput}");
                                    await connector.Conversations.ReplyToActivityAsync(rep);
                                    return Request.CreateResponse(HttpStatusCode.OK);
                                }
                                
                            }
                            else
                            {
                                userData.SetProperty<int>("count", 0);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                List<Account> list = await AzureManager.AzureManagerInstance.GetAccounts(user.ID);
                                await connector.Conversations.SendToConversationAsync(DisplayAccount(list, activity));

                                userData.SetProperty<bool>("isAddAccount", false);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                userData.SetProperty<bool>("confirm", false);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                endOutput = $"{accName} Is Existed!";
                                Activity rep = activity.CreateReply($"{endOutput}");
                                await connector.Conversations.ReplyToActivityAsync(rep);
                                return Request.CreateResponse(HttpStatusCode.OK);
                            }
                        }
                    }

                    // Update an account
                    if (userData.GetProperty<bool>("isUpdateAccount"))
                    {
                        int count = userData.GetProperty<int>("count");
                        if (count == 1)
                        {
                            try
                            {
                                if (userData.GetProperty<bool>("process"))
                                {
                                    string[] pairs = Regex.Split(userMessage, "\\s+");

                                    userData.SetProperty<string>("accName", pairs[1]);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    userData.SetProperty<string>("accValue", pairs[2]);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    userData.SetProperty<bool>("process", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                }

                                string name = userData.GetProperty<string>("accName");
                                List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(user.ID);
                                Account acc = (accounts.Where(a => a.Name == name).Count() > 0) ? accounts.Where(a => a.Name == name).First() : null;

                                if (acc != null)
                                {
                                    if (userData.GetProperty<bool>("confirm"))
                                    {
                                        await Conversation.SendAsync(activity, () => new EchoDialog());
                                    }

                                    string result = userMessage;

                                    if (result.ToLower().Equals("yes") || result.ToLower().Equals("1"))
                                    {
                                        userData.SetProperty<int>("count", 0);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        acc.Value = Convert.ToDecimal(userData.GetProperty<string>("accValue"));
                                        await AzureManager.AzureManagerInstance.UpdateCount(acc);

                                        await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));

                                        userData.SetProperty<bool>("isUpdateAccount", false);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        userData.SetProperty<bool>("confirm", false);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        return Request.CreateResponse(HttpStatusCode.OK);
                                    }
                                    else if (result.ToLower().Equals("no") || result.ToLower().Equals("2"))
                                    {
                                        userData.SetProperty<int>("count", 0);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));
                                        userData.SetProperty<bool>("isUpdateAccount", false);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        userData.SetProperty<bool>("confirm", false);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        endOutput = $"Process is cancelled";
                                        Activity rep = activity.CreateReply($"{endOutput}");
                                        await connector.Conversations.ReplyToActivityAsync(rep);

                                        return Request.CreateResponse(HttpStatusCode.OK);
                                    }
                                }
                                else
                                {
                                    userData.SetProperty<int>("count", 0);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));

                                    userData.SetProperty<bool>("isUpdateAccount", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    userData.SetProperty<bool>("confirm", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    userData.SetProperty<bool>("process", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    endOutput = $"Account {name} is not existed!";
                                    Activity rep = activity.CreateReply($"{endOutput}");
                                    await connector.Conversations.ReplyToActivityAsync(rep);
                                    return Request.CreateResponse(HttpStatusCode.OK);
                                }
                            }
                            catch (Exception e)
                            {
                                userData.SetProperty<int>("count", 0);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                userData.SetProperty<bool>("confirm", false);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                userData.SetProperty<bool>("process", false);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                endOutput = $"{userMessage} - Syntax is not correct";
                                Activity rep = activity.CreateReply($"{endOutput}");
                                await connector.Conversations.ReplyToActivityAsync(rep);
                                return Request.CreateResponse(HttpStatusCode.OK);
                            }

                        }
                    }

                    // Delete an account
                    if (userData.GetProperty<bool>("isDeleteAccount"))
                    {
                        int count = userData.GetProperty<int>("count");
                        if(count == 1)
                        {
                            try
                            {
                                if (userData.GetProperty<bool>("process"))
                                {
                                    string[] pairs = Regex.Split(userMessage, "\\s+");
                                                                
                                    userData.SetProperty<string>("accName", pairs[1]);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    userData.SetProperty<bool>("process", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                }

                                string name = userData.GetProperty<string>("accName");
                                List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(user.ID);
                                Account acc = (accounts.Where(a => a.Name == name).Count() > 0) ? accounts.Where(a => a.Name == name).First() : null;

                                if (acc != null)
                                {
                                    if (userData.GetProperty<bool>("confirm"))
                                    {
                                        await Conversation.SendAsync(activity, () => new EchoDialog());
                                    }

                                    string result = userMessage;

                                    if (result.ToLower().Equals("yes") || result.ToLower().Equals("1"))
                                    {
                                        userData.SetProperty<int>("count", 0);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        await AzureManager.AzureManagerInstance.DeleteCount(acc);
                                        accounts.Remove(acc);
                                        await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));
                                        userData.SetProperty<bool>("isDeleteAccount", false);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        userData.SetProperty<bool>("confirm", false);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        return Request.CreateResponse(HttpStatusCode.OK);
                                    }
                                    else if (result.ToLower().Equals("no") || result.ToLower().Equals("2"))
                                    {
                                        userData.SetProperty<int>("count", 0);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));
                                        userData.SetProperty<bool>("isDeleteAccount", false);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        userData.SetProperty<bool>("confirm", false);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                        endOutput = $"Process is cancelled";
                                        Activity rep = activity.CreateReply($"{endOutput}");
                                        await connector.Conversations.ReplyToActivityAsync(rep);

                                        return Request.CreateResponse(HttpStatusCode.OK);
                                    }


                                }
                                else
                                {
                                    userData.SetProperty<int>("count", 0);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));
                                    userData.SetProperty<bool>("isDeleteAccount", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    userData.SetProperty<bool>("confirm", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    userData.SetProperty<bool>("process", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                    endOutput = $"Account {name} Is Not Existing!";
                                    Activity rep = activity.CreateReply($"{endOutput}");
                                    await connector.Conversations.ReplyToActivityAsync(rep);
                                    return Request.CreateResponse(HttpStatusCode.OK);
                                }
                            }
                            catch (Exception e)
                            {
                                userData.SetProperty<int>("count", 0);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                userData.SetProperty<bool>("confirm", false);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                userData.SetProperty<bool>("process", false);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                                endOutput = $"{userMessage} - Syntax is not correct";
                                Activity rep = activity.CreateReply($"{endOutput}");
                                await connector.Conversations.ReplyToActivityAsync(rep);
                                return Request.CreateResponse(HttpStatusCode.OK);
                            }
                        }
                    }

                    // Logout
                    if (userMessage.ToLower().Equals("logout") || userMessage.ToLower().Equals("6"))
                    {
                        userData.SetProperty<bool>("logoutProcess", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    }
                    else if (userMessage.ToLower().Equals("menu"))
                    {
                        await connector.Conversations.SendToConversationAsync(DisplayMenu(activity));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    else if (userMessage.ToLower().Equals("view account") || userMessage.ToLower().Equals("1"))
                    {
                        if (user != null)
                        {
                            List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(user.ID);
                            await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));
                            return Request.CreateResponse(HttpStatusCode.OK);
                        }
                    }
                    else if (userMessage.ToLower().Equals("add account") || userMessage.ToLower().Equals("2"))
                    {
                        userData.SetProperty<bool>("isAddAccount", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        endOutput = "Enter Account Name";

                        userData.SetProperty<int>("count", 1);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        userData.SetProperty<bool>("confirm", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        userData.SetProperty<bool>("process", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        Activity rep = activity.CreateReply($"{endOutput}");
                        await connector.Conversations.ReplyToActivityAsync(rep);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    else if (userMessage.ToLower().Equals("update account") || userMessage.ToLower().Equals("3"))
                    {
                        userData.SetProperty<bool>("isUpdateAccount", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(user.ID);
                        await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));
                        endOutput = "Update <AccountName> <Value>";
                        
                        userData.SetProperty<int>("count", 1);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        userData.SetProperty<bool>("confirm", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        userData.SetProperty<bool>("process", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        Activity rep = activity.CreateReply($"{endOutput}");
                        await connector.Conversations.ReplyToActivityAsync(rep);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    else if (userMessage.ToLower().Equals("delete account") || userMessage.ToLower().Equals("4"))
                    {
                        userData.SetProperty<bool>("isDeleteAccount", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(user.ID);
                        await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));

                        userData.SetProperty<bool>("confirm", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        userData.SetProperty<bool>("process", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        userData.SetProperty<int>("count", 1);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        endOutput = "Delete <AccountName>";
                        Activity rep = activity.CreateReply($"{endOutput}");
                        await connector.Conversations.ReplyToActivityAsync(rep);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    else if (userMessage.ToLower().Equals("currency rate") || userMessage.ToLower().Equals("5"))
                    {
                        HttpClient client = new HttpClient();
                        string result = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=NZD"));
                        CurrencyRate currencyRate = JsonConvert.DeserializeObject<CurrencyRate>(result);
                        await connector.Conversations.SendToConversationAsync(DisplayCurrencyRate(currencyRate, activity));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    // Logout process
                    if (userData.GetProperty<bool>("logoutProcess"))
                    {
                        string username = "";
                        string password = "";
                        userData.SetProperty<string>("username", username);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        userData.SetProperty<string>("password", password);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        userData.SetProperty<bool>("isLoginSuccessful", false);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        endOutput = $"Logout successful! See You Again";
                        Activity rep = activity.CreateReply($"{endOutput}");
                        await connector.Conversations.ReplyToActivityAsync(rep);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                }
                else
                {
                    if (userMessage.ToLower().Equals("login"))
                    {
                        endOutput = "Enter Your Username";
                        userData.SetProperty<bool>("isEnterUser", true); // set property isEnterUser is true
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        Activity rep = activity.CreateReply($"{endOutput}");
                        await connector.Conversations.ReplyToActivityAsync(rep);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    if (userData.GetProperty<bool>("isEnterPass"))
                    {
                        userData.SetProperty<string>("password", userMessage);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        userData.SetProperty<bool>("isEnterPass", false);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    }

                    if (userData.GetProperty<bool>("isEnterUser"))
                    {
                        userData.SetProperty<string>("username", userMessage);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        endOutput = "Enter Your Password";

                        userData.SetProperty<bool>("isEnterUser", false);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        userData.SetProperty<bool>("isEnterPass", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    }

                    string username = userData.GetProperty<string>("username");
                    string password = userData.GetProperty<string>("password");
                    if (username != null && password != null && userMessage != "" && password != "")
                    {
                        List<User> users = await AzureManager.AzureManagerInstance.GetUsers(username, password);

                        if (users.Count > 0)
                        {
                            userData.SetProperty<User>("user", users.First());
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                            endOutput = $"Login Successfull! Welcome {username} To Contoso Bank Bot Service";
                            userData.SetProperty<string>("username", username);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            activity.From.Name = userData.GetProperty<string>("username");
                            userData.SetProperty<string>("password", password);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            userData.SetProperty<bool>("isLoginSuccessful", true);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        }
                        else
                        {
                            endOutput = "Login Fail!";
                            username = "";
                            password = "";
                            userData.SetProperty<string>("username", username);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            activity.From.Name = userData.GetProperty<string>("username");
                            userData.SetProperty<string>("password", password);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        }

                        activity.Recipient.Name = "Contoso Bank Bot";  
                    }
                }

                if (!userData.GetProperty<bool>("confirm"))
                {
                    Activity reply = activity.CreateReply($"{endOutput}");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                    if (userData.GetProperty<bool>("isLoginSuccessful"))
                        await connector.Conversations.SendToConversationAsync(DisplayMenu(activity));
                }
                
            }
            else
            {
                HandleSystemMessage(activity);
            }
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        private Activity DisplayCurrencyRate(CurrencyRate currencyRate, Activity activity)
        {
            Activity replyToConversation = activity.CreateReply("");
            replyToConversation.Recipient = activity.From;
            replyToConversation.Type = "message";
            replyToConversation.Attachments = new List<Attachment>();
            List<ReceiptItem> items = new List<ReceiptItem>();
            
            string[] countryCurrency = {"USD","EUR","GBP","AUD","CAD","SGD","CHF","MYR","JPY","CNY"};
            foreach(string str in countryCurrency)
            {
                Rates r = currencyRate.rates;
                var v=0.0;
                var propInfo = currencyRate.rates.GetType().GetProperty(str);
                if (propInfo != null)
                    v = Convert.ToDouble(propInfo.GetValue(r));
                ReceiptItem item = new ReceiptItem()
                {
                    Subtitle = str + " - " + v
                };
                items.Add(item);
            }

            ReceiptCard Card = new ReceiptCard()
            {
                Title = "Currency Rate - NZD base",
                Items = items
            };

            Attachment plAttachment = Card.ToAttachment();
            replyToConversation.Attachments.Add(plAttachment);
            return replyToConversation;
        }

        private Activity DisplayAccount(List<Account> accounts, Activity activity)
        {
            Activity replyToConversation = activity.CreateReply("");
            replyToConversation.Recipient = activity.From;
            replyToConversation.Type = "message";
            replyToConversation.Attachments = new List<Attachment>();
            List<ReceiptItem> items = new List<ReceiptItem>();

            decimal total = 0;
            foreach(Account acc in accounts)
            {
                total += acc.Value;
                ReceiptItem item = new ReceiptItem()
                {
                    Title = acc.Name,
                    Subtitle = acc.Type,
                    Price = acc.Value.ToString()
                };
                items.Add(item);
            }

            ReceiptCard Card = new ReceiptCard()
            {
                Title = "Accounts",
                Items = items,
                Total = total.ToString()
            };

            Attachment plAttachment = Card.ToAttachment();
            replyToConversation.Attachments.Add(plAttachment);
            return replyToConversation;
        }

        private Activity DisplayMenu(Activity activity)
        {
            Activity replyToConversation = activity.CreateReply("");
            replyToConversation.Recipient = activity.From;
            replyToConversation.Type = "message";
            replyToConversation.Attachments = new List<Attachment>();
            List<ReceiptItem> items = new List<ReceiptItem>();

            ReceiptItem item1 = new ReceiptItem()
            {
                Text = "1. View Account"
            };
            items.Add(item1);

            ReceiptItem item2 = new ReceiptItem()
            {
                Text = "2. Add Account"
            };
            items.Add(item2);

            ReceiptItem item3 = new ReceiptItem()
            {
                Text = "3. Update Account"
            };
            items.Add(item3);

            ReceiptItem item4 = new ReceiptItem()
            {
                Text = "4. Delete Account"
            };
            items.Add(item4);

            ReceiptItem item5 = new ReceiptItem()
            {
                Text = "5. Currency Rate"
            };
            items.Add(item5);

            ReceiptItem item6 = new ReceiptItem()
            {
                Text = "6. Logout"
            };
            items.Add(item6);

            ReceiptCard Card = new ReceiptCard()
            {
                Title = "Menu Options",
                Items = items
            };

            Attachment plAttachment = Card.ToAttachment();
            replyToConversation.Attachments.Add(plAttachment);
            return replyToConversation;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }

    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            PromptDialog.Confirm(
                     context,
                     AfterAsync,
                     "Are you sure you want to process?",
                     "Didn't get that!",
                     promptStyle: PromptStyle.PerLine);
        }
        public async Task AfterAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                await context.PostAsync("Process done!");
            }
            else
            {
                //await context.PostAsync("Process Cancelled!");
            }
            context.Wait(MessageReceivedAsync);
        }
    }
}