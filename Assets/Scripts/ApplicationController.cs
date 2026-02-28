using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyService;
using PurrNet;
using PurrNet.Modules;
using SessionService;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vanguard
{
    public class ApplicationController : MonoBehaviour
    {
        public static readonly string AlienTeamKey  = "team.alien";
        public static readonly string MarineTeamKey = "team.marine";
        public static readonly string LobbyToGameId = "session.idmap";

        private bool _allowStartGame = true;
        
        public static readonly List<string> LobbyKeys = new() 
        { 
            SessionHostIdKey, 
            SessionConnectKey, 
            AlienTeamKey,
            MarineTeamKey,
            LobbyService.LocalServer.LobbyKeys.NameKey,
            LobbyService.LocalServer.LobbyKeys.ReadyKey,
        };
        
        public static readonly List<string> MemberKeys = new()
        {
            LobbyToGameId    
        };
        
        public static ApplicationController Instance { get; private set; }
        
        [SerializeField] private NetworkManager networkManager;

        private TaskCompletionSource<bool> startGameTcs;
        
        private const string SessionHostIdKey = "session.hostid";
        private const string SessionConnectKey = "session.connect";
       
        
        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Debug.LogError("Multiple application controllers found!");
                return;
            }

            Instance = this;

            Lobby.OnLobbyDataUpdated += OnLobbyDataUpdated;
        }

        public void TransitionToGame()
        {
            _ = StartGameAsync();
        }

        private async Task StartGameAsync()
        {
            if (!_allowStartGame) return;
            
            var request = new SessionCreateRequest
            {
                TimeoutSeconds = 5
            };

            startGameTcs = new TaskCompletionSource<bool>();

            networkManager.onPlayerJoined += OnPlayerJoined;
            
            var createResult = await Session.CreateSessionAsync(request, destroyCancellationToken);
            if (!createResult.Connected)
            {
                Debug.Log("Failed to create session!");
                return;
            }
            
            networkManager.ResetOriginalScene(SceneManager.GetActiveScene());
                
            Lobby.SetLobbyData(SessionHostIdKey, createResult.SessionDetails.ServerAddress);
            Lobby.SetLobbyData(SessionConnectKey, "True");
            
            var timeout = Task.Delay(10000);
            await Task.WhenAny(timeout, startGameTcs.Task);
            
            networkManager.onPlayerJoined -= OnPlayerJoined;
                
            // Continue to game even without all expected players
            PurrSceneSettings settings = new()
            {
                isPublic = true,
                mode = LoadSceneMode.Single
            };
            
            await networkManager.sceneModule.LoadSceneAsync("Game", settings);
        }
        
        private void OnPlayerJoined(PlayerID id, bool isReconnect, bool asServer)
        {
            if (id.isServer) return;

            Debug.Log($"Player {id} joined the game. Connected players: {networkManager.playerCount}, Lobby count: {Lobby.Model.Members.Count}");
            
            if (networkManager.playerCount >= Lobby.Model.Members.Count) startGameTcs.TrySetResult(true);
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

            var result = await Session.JoinSessionAsync(request, destroyCancellationToken);

            if (!result.Connected)
            {
                Debug.LogError("Failed to join session!");
            }
        }

        public void TransitionToTitle()
        {
            Lobby.SetLobbyData(SessionConnectKey, "False");
            _ = StopGameAsync();
        }

        private async Task StopGameAsync()
        {
            PurrSceneSettings settings = new()
            {
                isPublic = true,
                mode = LoadSceneMode.Single,
            };
            
            DisallowStartGame();
            await networkManager.sceneModule.LoadSceneAsync("Lobby", settings);
        }

        public void AllowStartGame()
        {
            _allowStartGame = true;
        }

        public void DisallowStartGame()
        {
            _allowStartGame = false;
        }
    }
}