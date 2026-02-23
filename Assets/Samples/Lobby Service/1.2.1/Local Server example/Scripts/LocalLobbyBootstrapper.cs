using UnityEngine;
using LobbyService.Example;

namespace LobbyService.LocalServer.Example
{
    public class LocalLobbyBootstrapper : MonoBehaviour
    {
        [SerializeField] private LocalSampleView view;
        
        private void Start()
        {
            Lobby.ConnectView(view);
        }
    }
}