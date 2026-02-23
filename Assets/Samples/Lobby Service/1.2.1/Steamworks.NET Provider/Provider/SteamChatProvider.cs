using System;
using Steamworks;
using UnityEngine;

namespace LobbyService.Example.Steam
{
    public class SteamChatProvider : IChatProvider
    {
        private const string DirectMessageHeader = "[DIRECT]";
        public ChatCapabilities Capabilities { get; } = new ChatCapabilities
        {
            SupportsDirectMessages = true,
            SupportsGeneralMessages = true
        };
        
        public event Action<LobbyChatMessage> OnChatMessageReceived;
        public event Action<LobbyChatMessage> OnDirectMessageReceived;
        public void SendChatMessage(ProviderId lobbyId, string message)
        {
            if (!SteamProvider.EnsureInitialized()) return;
            if (!SteamProvider.ValidSteamId(lobbyId, out var steamId)) return;

            var cmd = SteamProcedureProvider.DecodeProcedure(message);
            if (cmd.Valid)
            {
                Debug.LogWarning("User attempted to invoke a procedure through chat! Caught and ignored.");
                return;
            }

            byte[] encoded = System.Text.Encoding.UTF8.GetBytes(message);
            SteamMatchmaking.SendLobbyChatMsg(steamId, encoded, encoded.Length);
        }

        public void SendDirectMessage(ProviderId lobbyId, LobbyMember member, string message)
        {
            if (!SteamProvider.EnsureInitialized()) return;
            if (!SteamProvider.ValidSteamId(lobbyId, out var steamId)) return;
            if (!SteamProvider.ValidSteamId(member.Id, out var memberId)) return;

            var cmd = SteamProcedureProvider.DecodeProcedure(message);
            if (cmd.Valid)
            {
                Debug.LogWarning("User attempted to invoke a procedure through chat! Caught and ignored.");
                return;
            }

            byte[] encoded = System.Text.Encoding.UTF8.GetBytes($"{DirectMessageHeader}:{memberId}:{message}");
            SteamMatchmaking.SendLobbyChatMsg(steamId, encoded, encoded.Length);
        }

        public void ReceiveChatMessage(string text, CSteamID senderId)
        {
            var sender = new LobbyMember(new ProviderId(senderId.ToString()), SteamFriends.GetFriendPersonaName(senderId));

            // If direct it will follow the format HEADER:(ulong)targetId:message
            var parts = text.Split(new[] { ':' }, 3, StringSplitOptions.None);

            if (parts.Length == 3 && parts[0] == DirectMessageHeader)
            {
                if (!SteamProvider.ValidSteamId(new ProviderId(parts[1]), out var targetId)) return;

                // Only deliver to sender or intended recipient
                var selfId = SteamUser.GetSteamID();
                if (selfId != targetId && selfId != senderId)
                    return;

                var message = parts[2]; // keeps any ':' inside the message

                OnDirectMessageReceived?.Invoke(new LobbyChatMessage
                {
                    Content = message,
                    Sender = sender,
                    Direct = true
                });
                return;
            }

            // Regular chat message
            OnChatMessageReceived?.Invoke(new LobbyChatMessage
            {
                Content = text,
                Sender = sender,
                Direct = false
            });
        }

        public void Dispose() { }
    }
}