using LobbyService.Example.Steam;
using UnityEngine;

namespace LobbyService.Example
{
    public class SteamworksLobbyBootstrapper : MonoBehaviour
    {
        public void Awake()
        {
            // We pass in the list of keys we are using so steam knows what data to pull during updates.
            // Modify SteamLobbyKeys.LobbyKeys and .MemberKeys to include any additional keys you care about.
            var provider = new SteamProvider(SteamLobbyKeys.LobbyKeys, SteamLobbyKeys.MemberKeys);
            
            // REQUIRED: This is the only call needed to start the lobby system. 
            // It must know which backend to use. This can be safely called again whenever you wish to hotswap backends.
            Lobby.SetProvider(provider);
        }
    }
}