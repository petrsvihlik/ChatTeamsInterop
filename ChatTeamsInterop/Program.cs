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

            Console.Write("Enter your name: ");
            var name = Console.ReadLine();

            Console.WriteLine("Connecting...");
            _communicationIdentityToken = await GetCommunicationIdentityToken(_configuration.Endpoint, _configuration.AccessKey);
            CallAgent callAgent = await CreateCallAgent(name, _communicationIdentityToken);
            _call = await callAgent.JoinAsync(new TeamsMeetingLinkLocator(_configuration.TeamsMeetingLink), new JoinCallOptions());
            _call.OnStateChanged += Call__OnStateChanged;

            // Wait for events
            Thread.Sleep(Timeout.Infinite);
        }

        private static async Task<CallAgent> CreateCallAgent(string name, string communicationIdentityToken)
        {
            var token_credential = new Azure.WinRT.Communication.CommunicationTokenCredential(communicationIdentityToken);
            var call_agent = await new CallClient().CreateCallAgent(token_credential, new CallAgentOptions() { DisplayName = name });
            return call_agent;
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

            var user_token_ = tokenResponseUser.Value.Token;
            return user_token_;
        }

        private async static void Call__OnStateChanged(object sender, PropertyChangedEventArgs args)
        {
            Console.WriteLine(_call.State.ToString());

            switch (_call.State)
            {
                case CallState.Connected:
                    Console.WriteLine("Connected!");
                    var _chatClient = CreateChatClient(_configuration.Endpoint, _communicationIdentityToken);
                    var thread_Id_ = WebUtility.UrlDecode(Regex.Match(_configuration.TeamsMeetingLink, "(.*meetup-join\\/)(?<threadId>19.*)(\\/.*)").Groups["threadId"].Value);
                    ChatThreadClient chatThreadClient = _chatClient.GetChatThreadClient(thread_Id_);

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

