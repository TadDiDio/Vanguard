using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyService;
using PurrNet;
using SessionService;
using UnityEngine;

namespace Vanguard
{
    public class ApplicationController : MonoBehaviour
    {
        public static readonly List<string> LobbyKeys = new() { SessionHostIdKey, SessionConnectKey, 
            LobbyService.LocalServer.LobbyKeys.NameKey,
            LobbyService.LocalServer.LobbyKeys.ReadyKey
        };
        public static readonly List<string> MemberKeys = new();
        
        public static ApplicationController Instance { get; private set; }
        
        [SerializeField] private NetworkManager networkManager;

        private TaskCompletionSource<bool> startGameTcs;
        
        private const string SessionHostIdKey = "session.hostid";
        private const string SessionConnectKey = "session.connect";
        
        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Debug.LogError("Multiple GameControllers found!");
                return;
            }

            Instance = this;

            Lobby.OnLobbyDataUpdated += OnLobbyDataUpdated;
        }

        public void StartGame()
        {
            _ = StartGameAsync();
        }

        private async Task StartGameAsync()
        {
            var request = new SessionCreateRequest
            {
                TimeoutSeconds = 5
            };
            
            var createResult = await Session.CreateSessionAsync(request, destroyCancellationToken);

            if (!createResult.Connected)
            {
                Debug.Log("Failed to create session!");
                return;
            }

            networkManager.onPlayerJoined += OnPlayerJoined;
            startGameTcs = new TaskCompletionSource<bool>();
            
            Lobby.SetLobbyData(SessionHostIdKey, createResult.SessionDetails.ServerAddress);
            Lobby.SetLobbyData(SessionConnectKey, "True");
            
            var timeout = Task.Delay(10000);
            await Task.WhenAny(timeout, startGameTcs.Task);
            
            networkManager.onPlayerJoined -= OnPlayerJoined;
            
            // Continue to game even without all expected players
            await SceneController.Instance.LoadGroupAsync("Game", new UnityToPurrnetSceneManager(networkManager));
        }

        private void OnPlayerJoined(PlayerID id, bool isReconnect, bool asServer)
        {
            Debug.Log($"Player {id} joined the lobby. Connected players: {networkManager.playerCount}, Lobby count: {Lobby.Model.Members.Count}");
            
            if (networkManager.playerCount >= Lobby.Model.Members.Count) startGameTcs.SetResult(true);
        }

        private void OnLobbyDataUpdated(LobbyDataUpdate update)
        {
            if (Lobby.IsOwner) return;

            // Doesn't matter if this is valid or not, we always set it on host before connecting
            var hostId = update.Data.GetOrDefault(SessionHostIdKey, "none");

            if (update.Data.GetOrDefault(SessionConnectKey, "False") == "True")
            {
                _ = JoinGameAsync(hostId);
            }
        }

        private async Task JoinGameAsync(string hostid)
        {
            var request = new SessionJoinRequest
            {
                TimeoutSeconds = 10f,
                SessionDetails = new SessionDetails
                {
                    ServerAddress = hostid
                }
            };

            await SceneController.Instance.BeginExternalControl();
            
            var result = await Session.JoinSessionAsync(request, destroyCancellationToken);

            if (!result.Connected)
            {
                Debug.LogError("Failed to join session!");
                await SceneController.Instance.EndExternalControl("Title");
            }
        }
    }
}