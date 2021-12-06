using Azure;
using Azure.Communication;
using Azure.Communication.Chat;
using Azure.Communication.Identity;
using Azure.Communication.CallingServer;

namespace ChatTeamsInterop
{
    class Program
    {
        static async Task Main()
        {
            await Init();
        }

        private static async Task Init()
        {
            Configuration configuration = new();

            if (string.IsNullOrEmpty(configuration.Username))
            {
                Console.Write("Enter your name: ");
                configuration.Username = Console.ReadLine() ?? "Anonymous";
            }

            Console.WriteLine("Connecting...");
            var userAndToken = await GetCommunicationIdentityToken(configuration.Endpoint, configuration.AccessKey);
            string _communicationIdentityToken = userAndToken.AccessToken.Token;

            Response<CallConnection> serverCall = await GetServerCall(userAndToken, configuration);
            
            //serverCall.Value.PlayAudio(new Uri("https://www2.cs.uic.edu/~i101/SoundFiles/PinkPanther60.wav"), false, "", new Uri("http://locahost"), null);

            try
            {

                Console.WriteLine($"Connected! Call ID: {serverCall.Value.CallConnectionId}");
                var chatClient = CreateChatClient(configuration.Endpoint, _communicationIdentityToken);

                // Get threadId using chatClient
                //AsyncPageable<ChatThreadItem> chatThreadItems = chatClient.GetChatThreadsAsync();

                //List<string> threadIds = new();
                //await foreach (ChatThreadItem chatThreadItem in chatThreadItems)
                //{
                //    threadIds.Add(chatThreadItem.Id);
                //}

                ChatThreadClient chatThreadClient = chatClient.GetChatThreadClient(configuration.ThreadId);//threadIds[0]);
                _ = await chatThreadClient.SendMessageAsync("initial msg");

                while (true)
                {
                    Console.Write("Enter your message: ");
                    var message = Console.ReadLine();
                    _ = await chatThreadClient.SendMessageAsync(message);
                    Console.WriteLine("Sent!");
                }
            }
            finally
            {
                serverCall.Value.Hangup();
            }
        }

        private static async Task<Response<CallConnection>> GetServerCall(CommunicationUserIdentifierAndToken userAndToken, Configuration configuration)
        {
            CallingServerClient callingServerClient = new($"endpoint={configuration.Endpoint};accesskey={configuration.AccessKey}");
            var joinCallOptions = new JoinCallOptions(new Uri("http://localhost"), new List<MediaType> { MediaType.Video, MediaType.Audio }, new List<EventSubscriptionType> { }) {  Subject = configuration.Username };
            var serverCall = await callingServerClient.JoinCallAsync(configuration.ServerCallId, userAndToken.User, joinCallOptions);            
            return serverCall;
        }

        private static ChatClient CreateChatClient(Uri endpoint, string communicationIdentityToken)
        {
            CommunicationTokenCredential communicationTokenCredential = new(communicationIdentityToken);
            return new ChatClient(endpoint, communicationTokenCredential);
        }

        private static async Task<CommunicationUserIdentifierAndToken> GetCommunicationIdentityToken(Uri endpoint, string accessKey)
        {
            CommunicationIdentityClient communicationIdentityClient = new(endpoint, new AzureKeyCredential(accessKey), new CommunicationIdentityClientOptions());
            IEnumerable<CommunicationTokenScope> scopes = new[] { CommunicationTokenScope.Chat, CommunicationTokenScope.VoIP };
            var response = await communicationIdentityClient.CreateUserAndTokenAsync(scopes);
            return response.Value;
        }
    }
}

