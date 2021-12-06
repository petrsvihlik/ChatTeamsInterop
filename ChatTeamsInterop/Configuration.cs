using System;

namespace ChatTeamsInterop
{
    internal partial class Configuration
    {
        public Uri Endpoint { get; set; }

        public string AccessKey { get; set; }

        public string TeamsMeetingLink { get; set; }

        public string Username { get; set; }
    }
}
