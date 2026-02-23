using System.Collections.Generic;
using PurrNet;
using UnityEngine;

namespace Vanguard
{
    public class GameController : NetworkBehaviour
    {
        [SerializeField] private List<Transform> spawnPoints;
        [SerializeField] private NetworkIdentity playerPrefab;
        protected override void OnSpawned()
        {
            if (!isServer) return;

            StartGame();
        }

        private void StartGame()
        {
            int idx = 0;
            
            foreach (var player in networkManager.players)
            {
                var t = spawnPoints[idx++ % spawnPoints.Count];
                var spawned = Instantiate(playerPrefab, t.position, t.rotation);
                spawned.GiveOwnership(player);
            }
        }
    }
}