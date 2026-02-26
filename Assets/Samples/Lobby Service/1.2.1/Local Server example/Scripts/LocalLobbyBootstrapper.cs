using UnityEngine;
using Vanguard;

namespace LobbyService.LocalServer.Example
{
    public class LocalLobbyBootstrapper : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private CoreView coreView;
        
        private void Start()
        {
            canvas.worldCamera = Camera.main;
            Lobby.ConnectView(coreView);
        }
    }
}