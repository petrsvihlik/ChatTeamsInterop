using System.Net;
using System.Text.RegularExpressions;

namespace ChatTeamsInterop
{
    internal partial class Configuration
    {
        public Uri Endpoint { get; set; }

        public string AccessKey { get; set; }

        public string TeamsMeetingLink { get; set; }

        public string Username { get; set; }

        public string ServerCallId { get; set; }

        public string ThreadId => WebUtility.UrlDecode(Regex.Match(TeamsMeetingLink, "(.*meetup-join\\/)(?<threadId>19.*)(\\/.*)").Groups["threadId"].Value);
    }
}
