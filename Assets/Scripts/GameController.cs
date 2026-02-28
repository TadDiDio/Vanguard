using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyService;
using PurrNet;
using UnityEngine;

namespace Vanguard
{
    public class GameController : NetworkBehaviour
    {
        [SerializeField] private List<Transform> spawnPoints;
        [SerializeField] private NetworkIdentity playerPrefab;

        private List<PlayerID> _marines = new();
        private List<PlayerID> _aliens = new();

        private Dictionary<PlayerID, string> _loadedGame;
        private TaskCompletionSource<bool> _sceneLoadedTcs;

        protected override void OnSpawned(bool asServer)
        {
            if (!asServer) return;
            
            _ = StartGameAsync();
        }

        private async Task StartGameAsync()
        {
            int idx = 0;

            _loadedGame = new Dictionary<PlayerID, string>();
            _sceneLoadedTcs = new TaskCompletionSource<bool>();
            
            foreach (var player in networkManager.players)
            {
                _loadedGame.Add(player, string.Empty);
            }
            
            await Task.WhenAny(_sceneLoadedTcs.Task, Task.Delay(5000));
            
            foreach (var (player, lobbyMember) in _loadedGame)
            {
                if (string.IsNullOrEmpty(lobbyMember)) continue;
                
                var parts = lobbyMember.Split(":");
                var member = new LobbyMember(new ProviderId(parts[0]), parts[1]);
                var team = TeamHelper.GetMemberTeam(member);
                
                if (team == Team.Marines) _marines.Add(player);
                else _aliens.Add(player);

                // 2. Spawn player
                var spawnPoint = spawnPoints[idx++ % spawnPoints.Count];
                var spawned = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                spawned.GetComponent<PlayerController>().SetTeam(team);
                spawned.GiveOwnership(player);
            }
        }
        
        [ServerRpc(requireOwnership:false)]
        public void NotifyServerClientLoaded(string lobbyMember, RPCInfo info)
        {
            if (!_loadedGame.ContainsKey(info.sender))
            {
                Debug.LogWarning("Got response from non-player client");
                return;
            }

            _loadedGame[info.sender] = lobbyMember;

            if (_loadedGame.Values.Any(string.IsNullOrEmpty))
            {
                return;
            }
            _sceneLoadedTcs.TrySetResult(true);
        }
    }
}