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
        public static BotData userData;
        public static string username="", password="";
        public static string userMessage;
        public static bool logoutProcess = false;
        public static List<User> users;
        public static string accName="", accValue="";
        public static int count=0;

        static Account account = null;

        [ResponseType(typeof(void))]
        public virtual async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

            activity.Recipient.Name = "Contoso Bank Bot";

            userMessage = activity.Text;
            string endOutput = $"Welcome {username} To Contoso Bank Bot Service";

            StateClient stateClient = activity.GetStateClient();
            userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

            if (activity.Type == ActivityTypes.Message)
            {
                 if (userData.GetProperty<bool>("isEnterPass"))
                {
                    password = userMessage;
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    userData.SetProperty<bool>("isEnterPass", false); // Set property isEnterPass is false;
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                // if property isEnterUser is true
                if (userData.GetProperty<bool>("isEnterUser"))
                {
                    username = userMessage;

                    endOutput = "Enter Your Password";

                    userData.SetProperty<bool>("isEnterUser", false); // Set property isEnterUser is false;
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    userData.SetProperty<bool>("isEnterPass", true); // Set property isEnterPass is true;
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                if (userData.GetProperty<bool>("isLoginSuccessful"))
                {
                    activity.From.Name = username;
                    
                    // Add new account
                    if (userData.GetProperty<bool>("isAddAccount"))
                    {
                        if (count == 1)
                        {
                            accName = userMessage;
                            endOutput = "Enter Deposit Value";
                            count++;
                            Activity quickreply = activity.CreateReply($"{endOutput}");
                            await connector.Conversations.ReplyToActivityAsync(quickreply);
                            var res = Request.CreateResponse(HttpStatusCode.OK);
                            return res;
                        }
                        else if(count == 2)
                        {
                            bool flag = false;
                            accValue = userMessage;
                            List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(users.First().ID);
                            foreach(Account account in accounts)
                            {
                                if (account.Name.Equals(accName)) flag = true; 
                            }

                            if (!flag)
                            {
                                if (!Dialog.DialogProcess)
                                {
                                    account = new Account()
                                    {
                                        Name = accName,
                                        Type = "Saving",
                                        Value = Convert.ToDecimal(accValue),
                                        UserID = users.First().ID
                                    };
                                }
                                
                                Dialog.DialogMessage = "Add New Account";
                                await Conversation.SendAsync(activity, () => new Dialog());
                                if (Dialog.Result == 1)
                                {
                                    count = 0;
                                    await AzureManager.AzureManagerInstance.AddCount(account);
                                    List<Account> list = await AzureManager.AzureManagerInstance.GetAccounts(users.First().ID);
                                    await connector.Conversations.SendToConversationAsync(DisplayAccount(list, activity));
                                    userData.SetProperty<bool>("isAddAccount", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                    var res = Request.CreateResponse(HttpStatusCode.OK);
                                    return res;
                                }
                                else if (Dialog.Result == -1)
                                {
                                    count = 0;
                                    userData.SetProperty<bool>("isAddAccount", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                    var res = Request.CreateResponse(HttpStatusCode.OK);
                                    return res;
                                }
                                else
                                {

                                    var res = Request.CreateResponse(HttpStatusCode.OK);
                                    return res;
                                }
                            }
                            else
                            {
                                count = 0;
                                List<Account> list = await AzureManager.AzureManagerInstance.GetAccounts(users.First().ID);
                                await connector.Conversations.SendToConversationAsync(DisplayAccount(list, activity));
                                userData.SetProperty<bool>("isAddAccount", false);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                endOutput = "The Account Name is Existed";
                                Activity rep = activity.CreateReply($"{endOutput}");
                                await connector.Conversations.ReplyToActivityAsync(rep);
                                var res = Request.CreateResponse(HttpStatusCode.OK);
                                return res;
                            }
                        }
                    }

                    // Update an account
                    if (userData.GetProperty<bool>("isUpdateAccount"))
                    {
                        if (count == 1)
                        {
                            try
                            {
                                if (!Dialog.DialogProcess)
                                {
                                    string[] pairs = Regex.Split(userMessage, "\\s+");
                                    accName = pairs[1];
                                    accValue = pairs[2];
                                }             
                                Dialog.DialogMessage = $"Update Account {accName}";
                                await Conversation.SendAsync(activity, () => new Dialog());
                                if(Dialog.Result == 1)
                                {
                                    count = 0;
                                    List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(users.First().ID);
                                    Account acc = accounts.Where(a => a.Name == accName).First();
                                    if(acc != null)
                                    {
                                        acc.Value = Convert.ToDecimal(accValue);
                                        await AzureManager.AzureManagerInstance.UpdateCount(acc);
                                        await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));
                                        userData.SetProperty<bool>("isUpdateAccount", false);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                        var res = Request.CreateResponse(HttpStatusCode.OK);
                                        return res;
                                    }
                                    else
                                    {
                                        endOutput = $"There is no account name: {accName}";
                                        Activity rep = activity.CreateReply($"{endOutput}");
                                        await connector.Conversations.ReplyToActivityAsync(rep);
                                        var res = Request.CreateResponse(HttpStatusCode.OK);
                                        return res;
                                    }
                                    
                                }
                                else if (Dialog.Result == -1)
                                {
                                    count = 0;
                                    List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(users.First().ID);
                                    await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));
                                    userData.SetProperty<bool>("isUpdateAccount", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                    var res = Request.CreateResponse(HttpStatusCode.OK);
                                    return res;
                                }
                                else
                                {
                                    var res = Request.CreateResponse(HttpStatusCode.OK);
                                    return res;
                                }
                                
                            }
                            catch (Exception e)
                            {
                                count = 0;
                                userData.SetProperty<bool>("isUpdateAccount", false);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                endOutput = "Syntax is not correct";
                                Activity rep = activity.CreateReply($"{endOutput}");
                                await connector.Conversations.ReplyToActivityAsync(rep);
                                var res = Request.CreateResponse(HttpStatusCode.OK);
                                return res;
                            }

                        }
                    }

                    // Delete an account
                    if (userData.GetProperty<bool>("isDeleteAccount"))
                    {
                        if(count == 1)
                        {
                            try
                            {
                                if (!Dialog.DialogProcess)
                                {
                                    string[] pairs = Regex.Split(userMessage, "\\s+");
                                    accName = pairs[1];
                                }
                                Dialog.DialogMessage = $"Delete Account {accName}";
                                await Conversation.SendAsync(activity, () => new Dialog());
                                if (Dialog.Result == 1)
                                {
                                    count = 0;
                                    List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(users.First().ID);
                                    Account acc = accounts.Where(a => a.Name == accName).First();
                                    if (acc != null)
                                    {
                                        await AzureManager.AzureManagerInstance.DeleteCount(acc);
                                        accounts.Remove(acc);
                                        await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));
                                        userData.SetProperty<bool>("isDeleteAccount", false);
                                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                        var res = Request.CreateResponse(HttpStatusCode.OK);
                                        return res;
                                    }
                                    else
                                    {
                                        endOutput = $"There is no account name: {accName}";
                                        Activity rep = activity.CreateReply($"{endOutput}");
                                        await connector.Conversations.ReplyToActivityAsync(rep);
                                        var res = Request.CreateResponse(HttpStatusCode.OK);
                                        return res;
                                    }

                                }
                                else if (Dialog.Result == -1)
                                {
                                    count = 0;
                                    List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(users.First().ID);
                                    await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));
                                    userData.SetProperty<bool>("isDeleteAccount", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                    var res = Request.CreateResponse(HttpStatusCode.OK);
                                    return res;
                                }
                                else
                                {
                                    var res = Request.CreateResponse(HttpStatusCode.OK);
                                    return res;
                                }

                            }
                            catch (Exception e)
                            {
                                count = 0;
                                userData.SetProperty<bool>("isDeleteAccount", false);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                endOutput = "Syntax is not correct";
                                Activity rep = activity.CreateReply($"{endOutput}");
                                await connector.Conversations.ReplyToActivityAsync(rep);
                                var res = Request.CreateResponse(HttpStatusCode.OK);
                                return res;
                            }
                        }
                    }

                    // Logout
                    if (userMessage.ToLower().Contains("logout") || userMessage.ToLower().Contains("6")) logoutProcess = true;
                    else if (userMessage.ToLower().Contains("menu"))
                    {
                        await connector.Conversations.SendToConversationAsync(DisplayMenu(activity));
                        var res = Request.CreateResponse(HttpStatusCode.OK);
                        return res;
                    }
                    else if (userMessage.ToLower().Contains("view account") || userMessage.ToLower().Contains("1"))
                    {
                        if (users != null && users.Count > 0)
                        {
                            List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(users.First().ID);
                            await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));
                            var res = Request.CreateResponse(HttpStatusCode.OK);
                            return res;
                        }
                    }
                    else if (userMessage.ToLower().Contains("add account") || userMessage.ToLower().Contains("2"))
                    {
                        userData.SetProperty<bool>("isAddAccount", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        endOutput = "Enter Account Name";
                        count++;
                        Activity rep = activity.CreateReply($"{endOutput}");
                        await connector.Conversations.ReplyToActivityAsync(rep);
                        var res = Request.CreateResponse(HttpStatusCode.OK);
                        return res;
                    }
                    else if(userMessage.ToLower().Contains("update account") || userMessage.ToLower().Contains("3"))
                    {
                        userData.SetProperty<bool>("isUpdateAccount", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(users.First().ID);
                        await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));
                        endOutput = "Update <AccountName> <Value>";
                        count++;
                        Activity rep = activity.CreateReply($"{endOutput}");
                        await connector.Conversations.ReplyToActivityAsync(rep);
                        var res = Request.CreateResponse(HttpStatusCode.OK);
                        return res;
                    }
                    else if (userMessage.ToLower().Contains("delete account") || userMessage.ToLower().Contains("4"))
                    {
                        userData.SetProperty<bool>("isDeleteAccount", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        List<Account> accounts = await AzureManager.AzureManagerInstance.GetAccounts(users.First().ID);
                        await connector.Conversations.SendToConversationAsync(DisplayAccount(accounts, activity));
                        endOutput = "Delete <AccountName>";
                        count++;
                        Activity rep = activity.CreateReply($"{endOutput}");
                        await connector.Conversations.ReplyToActivityAsync(rep);
                        var res = Request.CreateResponse(HttpStatusCode.OK);
                        return res;
                    }
                    else if (userMessage.ToLower().Contains("currency rate") || userMessage.ToLower().Contains("5"))
                    {
                        HttpClient client = new HttpClient();
                        string result = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=NZD"));
                        CurrencyRate currencyRate = JsonConvert.DeserializeObject<CurrencyRate>(result);
                        await connector.Conversations.SendToConversationAsync(DisplayCurrencyRate(currencyRate,activity));
                        var res = Request.CreateResponse(HttpStatusCode.OK);
                        return res;
                    }

                    // Logout process
                    if (logoutProcess)
                    {
                        Dialog.DialogMessage = "Logout";
                        await Conversation.SendAsync(activity, () => new Dialog());
                        if (Dialog.Result == 1)
                        {
                            username = "";
                            password = "";
                            userData.SetProperty<string>("username", username);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            userData.SetProperty<string>("password", password);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            userData.SetProperty<bool>("isLoginSuccessful", false);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            var res = Request.CreateResponse(HttpStatusCode.OK);
                            return res;
                        }
                        else
                        {
                            var res = Request.CreateResponse(HttpStatusCode.OK);
                            return res;
                        }
                    }
                }
                else
                {
                    if (userMessage.ToLower().Contains("login"))
                    {
                        endOutput = "Enter Your Username";
                        userData.SetProperty<bool>("isEnterUser", true); // set property isEnterUser is true
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    }

                    if (username != "" && password != "")
                    {
                        users = await AzureManager.AzureManagerInstance.GetUsers(username, password);

                        if (users.Count > 0)
                        {
                            endOutput = $"Login Successfull! Welcome {username} To Contoso Bank Bot Service";
                            activity.From.Name = username;
                            userData.SetProperty<string>("username", username);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
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
                        }

                        activity.Recipient.Name = "Contoso Bank Bot";  
                    }
                }
                // return our reply to the user
                //Activity reply = activity.CreateReply($"You sent {activity.Text} which was {length} characters");
                Activity reply = activity.CreateReply($"{endOutput}");
                await connector.Conversations.ReplyToActivityAsync(reply);
                if(userData.GetProperty<bool>("isLoginSuccessful") && !logoutProcess)
                    await connector.Conversations.SendToConversationAsync(DisplayMenu(activity));
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
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
    public class Dialog : IDialog<object>
    {
        private static int flag;
        public static int Result { get { return flag;} }
        public static bool DialogProcess { get; set; }
        public static string DialogMessage { get; set; }
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            flag = 0;
            DialogProcess = true;
            PromptDialog.Confirm(
                    context,
                    AfterAsync,
                    "Do You Want To "+DialogMessage+"?",
                    "Didn't get that!",
                    promptStyle: PromptStyle.PerLine);
        }
        public async Task AfterAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                flag = 1;
                string endOutput = DialogMessage+" Successfully!";
                await context.PostAsync($"{endOutput}");
                MessagesController.logoutProcess = false;
            }
            else
            {
                flag = -1;
                await context.PostAsync("Cancel Process");
                MessagesController.logoutProcess = false;
            }
            context.Wait(MessageReceivedAsync);
            DialogProcess = false;
        }
    }
}