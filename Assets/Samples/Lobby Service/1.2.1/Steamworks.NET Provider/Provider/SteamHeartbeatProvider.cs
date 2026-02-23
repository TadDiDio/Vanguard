using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LobbyService.Heartbeat;
using LobbyService.Procedure;
using Steamworks;
using UnityEngine;

namespace LobbyService.Example.Steam
{
    public struct HeartbeatMeta
    {
        public float LastPingTime;
        public ProviderId LobbyId;
    }
    
    public class SteamHeartbeatProvider : IHeartbeatProvider
    {
        private const string HeartbeatProcedureKey = "ping_heartbeat";

        private float _lastHeartbeatTime = float.NegativeInfinity;

        private float _heartbeatIntervalSeconds;
        private float _heartbeatTimeoutSeconds;

        private CancellationTokenSource _heartbeatCts;

        
        private Dictionary<LobbyMember, HeartbeatMeta> _heartbeats = new();
        
        public event Action<HeartbeatTimeout> OnHeartbeatTimeout;

        public void Initialize(IProcedureProvider procedures)
        {
            procedures.RegisterProcedureLocally(new LobbyProcedure
            {
                ExecuteLocally = false,
                InvokePermission = InvokePermission.All,
                Key = HeartbeatProcedureKey,
                Procedure = UpdateHeartbeatProcedure
            });
        }
        
        public void StartOwnHeartbeat(ProviderId lobbyId, float intervalSeconds, float othersTimeoutSeconds)
        {
            _heartbeatIntervalSeconds = intervalSeconds;
            _heartbeatTimeoutSeconds = othersTimeoutSeconds;
            _heartbeatCts ??= new CancellationTokenSource();
            _ = HeartbeatLoop(lobbyId);
        }

        public void StopOwnHeartbeat()
        {
            if (_heartbeatCts is { IsCancellationRequested: false })
            {
                _heartbeatCts.Cancel();
            }
            
            _heartbeats.Clear();
            _heartbeatCts?.Dispose();
            _heartbeatCts = null;
        }

        public void ClearSubscriptions()
        {
            _heartbeats.Clear();
        }

        public void SubscribeToHeartbeat(ProviderId lobbyId, LobbyMember member)
        {
            Debug.Log($"Subbing to {member} at time {Time.time}");
            _heartbeats[member] = new HeartbeatMeta
            {
                LobbyId = lobbyId,
                LastPingTime = Time.time + 10, // 10s grace period
            };
        }

        public void UnsubscribeFromHeartbeat(ProviderId lobbyId, LobbyMember member)
        {
            _heartbeats.Remove(member);
        }

        private async Task HeartbeatLoop(ProviderId lobbyId)
        {
            try
            {
                while (!_heartbeatCts.IsCancellationRequested)
                {
                    float now = Time.time;

                    // Send own heartbeat
                    if (now > _lastHeartbeatTime + _heartbeatIntervalSeconds)
                    {
                        Lobby.Procedure.Broadcast(
                            HeartbeatProcedureKey,
                            lobbyId.ToString(),
                            Lobby.LocalMember.Id.ToString()
                        );

                        _lastHeartbeatTime = now;
                    }

                    // Check others' heartbeats
                    var timedOut = new List<LobbyMember>();
                    foreach (var kvp in _heartbeats)
                    {
                        if (now > kvp.Value.LastPingTime + _heartbeatTimeoutSeconds)
                        {
                            Debug.Log($"{kvp.Key} timed out because now is {now} and last ping time was {kvp.Value.LastPingTime}");
                            timedOut.Add(kvp.Key);
                        }
                    }

                    foreach (var member in timedOut)
                    {
                        var senderLobbyId = _heartbeats[member].LobbyId;

                        // Prevent duplicating events for continuously timeout members
                        UnsubscribeFromHeartbeat(null, member);

                        OnHeartbeatTimeout?.Invoke(new HeartbeatTimeout
                        {
                            LobbyId = senderLobbyId,
                            Member = member
                        });
                    }

                    // How often we test, not how often we broadcast
                    await Task.Delay(TimeSpan.FromSeconds(0.5f), _heartbeatCts.Token);
                }
            }
            catch (OperationCanceledException) { /* Ignored */ }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // Lobby procedure - Expects two args: ulong: lobbyId | ulong: memberId
        private async Task UpdateHeartbeatProcedure(string[] args)
        {
            if (args.Length < 2) return;

            var lobbyIdStr = args[0];
            var memberIdStr = args[1];

            if (!SteamProvider.ValidSteamId(new ProviderId(lobbyIdStr), out _)) return;
            if (!SteamProvider.ValidSteamId(new ProviderId(memberIdStr), out var memberId)) return;

            var lobbyMember = new LobbyMember(new ProviderId(memberIdStr), SteamFriends.GetFriendPersonaName(memberId));

            if (_heartbeats.ContainsKey(lobbyMember))
            {
                Debug.Log($"Updating {lobbyMember}'s time to {Time.time}");
                _heartbeats[lobbyMember] = new HeartbeatMeta
                {
                    LobbyId = new ProviderId(lobbyIdStr),
                    LastPingTime = Time.time
                };
            }

            await Task.CompletedTask;
        }

        public void Dispose() { }
    }
}