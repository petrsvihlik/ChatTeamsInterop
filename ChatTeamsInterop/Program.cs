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
using Azure.Communication.CallingServer;
using System.Net.Cache;

namespace ChatTeamsInterop
{
    class Program
    {
        static void Main(string[] args)
        {
            Init().Wait();
        }

        static string _communicationIdentityToken;
        static Configuration _configuration;

        private static async Task Init()
        {
            _configuration = new Configuration();

            Console.Write("Enter your name: ");
            var name = Console.ReadLine();

            Console.WriteLine("Connecting...");
            var userAndToken = await GetCommunicationIdentityToken(_configuration.Endpoint, _configuration.AccessKey);
            _communicationIdentityToken = userAndToken.AccessToken.Token;

            
            CallingServerClient callingServerClient = new CallingServerClient($"endpoint={_configuration.Endpoint};accesskey={_configuration.AccessKey}");

            var joinCallOptions = new Azure.Communication.CallingServer.JoinCallOptions(new Uri("http://localhost"), new List<MediaType> { MediaType.Video, MediaType.Audio }, new List<EventSubscriptionType> { }) { };
            var serverCall = await callingServerClient.JoinCallAsync(WebUtility.UrlDecode("19%3ameeting_MWJkMTVmNDUtMjAzMC00YWUzLTgzMDItMWMyNjYwOWVmZjFi%40thread.v2"), userAndToken.User, joinCallOptions);
            

            Console.WriteLine($"Connected! Call ID: {serverCall.Value.CallConnectionId}");
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

        private static async Task<CommunicationUserIdentifierAndToken> GetCommunicationIdentityToken(Uri endpoint, string accessKey)
        {
            CommunicationIdentityClient communicationIdentityClient = new CommunicationIdentityClient(endpoint, new AzureKeyCredential(accessKey), new CommunicationIdentityClientOptions());
            IEnumerable<CommunicationTokenScope> scopes = new[] { CommunicationTokenScope.Chat, CommunicationTokenScope.VoIP };
            var response = await communicationIdentityClient.CreateUserAndTokenAsync(scopes);
            return response.Value;
        }

    }
}

