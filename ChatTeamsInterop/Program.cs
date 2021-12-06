using Azure;
using Azure.Communication;
using Azure.Communication.Calling;
using Azure.Communication.Chat;
using Azure.Communication.Identity;
using Azure.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;

namespace ChatTeamsInterop
{
    class Program
    {
        static void Main(string[] args)
        {
            Init().Wait();
        }

        static Call _call;
        static string _communicationIdentityToken;
        static Configuration _configuration;

        private static async Task Init()
        {
            _configuration = new Configuration();

            if (string.IsNullOrEmpty(_configuration.Username))
            {
                Console.Write("Enter your name: ");
                _configuration.Username = Console.ReadLine();
            }

            Console.WriteLine("Connecting...");
            _communicationIdentityToken = await GetCommunicationIdentityToken(_configuration.Endpoint, _configuration.AccessKey);
            CallAgent callAgent = await CreateCallAgent(_configuration.Username, _communicationIdentityToken);
            _call = await callAgent.JoinAsync(new TeamsMeetingLinkLocator(_configuration.TeamsMeetingLink), new JoinCallOptions());
            _call.OnStateChanged += Call_OnStateChanged;

            // Wait for events
            Thread.Sleep(Timeout.Infinite);
        }

        private static async Task<CallAgent> CreateCallAgent(string name, string communicationIdentityToken)
        {
            var tokenCredential = new Azure.WinRT.Communication.CommunicationTokenCredential(communicationIdentityToken);
            var callAgent = await new CallClient().CreateCallAgent(tokenCredential, new CallAgentOptions() { DisplayName = name });
            return callAgent;
        }

        private static ChatClient CreateChatClient(Uri endpoint, string communicationIdentityToken)
        {
            CommunicationTokenCredential communicationTokenCredential = new CommunicationTokenCredential(communicationIdentityToken);
            return new ChatClient(endpoint, communicationTokenCredential);
        }

        private static async Task<string> GetCommunicationIdentityToken(Uri endpoint, string accessKey)
        {
            CommunicationIdentityClient communicationIdentityClient = new CommunicationIdentityClient(endpoint, new AzureKeyCredential(accessKey), new CommunicationIdentityClientOptions());
            Response<CommunicationUserIdentifier> user = await communicationIdentityClient.CreateUserAsync();
            IEnumerable<CommunicationTokenScope> scopes = new[] { CommunicationTokenScope.Chat, CommunicationTokenScope.VoIP };
            Response<AccessToken> tokenResponseUser = await communicationIdentityClient.GetTokenAsync(user.Value, scopes);
            return tokenResponseUser.Value.Token;
        }

        private async static void Call_OnStateChanged(object sender, PropertyChangedEventArgs args)
        {
            Console.WriteLine(_call.State.ToString());

            switch (_call.State)
            {
                case CallState.Connected:
                    Console.WriteLine("Connected!");
                    var chatClient = CreateChatClient(_configuration.Endpoint, _communicationIdentityToken);

                    // Get threadId using chatClient
                    //AsyncPageable<ChatThreadItem> chatThreadItems = chatClient.GetChatThreadsAsync();
                    //var enumerator = chatThreadItems.GetAsyncEnumerator();
                    //while (await enumerator.MoveNextAsync())
                    //{
                    //    var chatThreadItem = enumerator.Current;
                    //}

                    var threadId = WebUtility.UrlDecode(Regex.Match(_configuration.TeamsMeetingLink, "(.*meetup-join\\/)(?<threadId>19.*)(\\/.*)").Groups["threadId"].Value);
                    ChatThreadClient chatThreadClient = chatClient.GetChatThreadClient(threadId);

                    while (true)
                    {
                        Console.Write("Enter your message: ");
                        var message = Console.ReadLine();
                        _ = await chatThreadClient.SendMessageAsync(message);
                        Console.WriteLine("Sent!");
                    }
                default:
                    break;
            }
        }
    }
}

