using LobbyService;
using PurrNet;
using UnityEngine;
using Vanguard;

public class GameInitNotifier : NetworkBehaviour
{
    [SerializeField] private GameController gameController;
    
    protected override void OnSpawned(bool asServer)
    {
        if (asServer) return;
        
        gameController.NotifyServerClientLoaded($"{Lobby.LocalMember.Id}:{Lobby.LocalMember.DisplayName}", default);
    }
}
