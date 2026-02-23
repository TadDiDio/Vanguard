using System.Collections.Generic;

namespace LobbyService.Example.Steam
{
    public static class SteamLobbyKeys
    {
        public const string Name = "name";
        public const string Type = "type";
        public const string ServerAddress = "server_address";

        // TODO: Make sure to add any new keys to the relevant list for discovery!!

        /// <summary>
        /// An exhaustive list of all lobby data keys.
        /// </summary>
        public static List<string> LobbyKeys => new() { Name, Type, ServerAddress };

        /// <summary>
        /// An exhaustive list of all member data keys.
        /// </summary>
        public static List<string> MemberKeys => new() { "nickname", "team"};
    }
}
