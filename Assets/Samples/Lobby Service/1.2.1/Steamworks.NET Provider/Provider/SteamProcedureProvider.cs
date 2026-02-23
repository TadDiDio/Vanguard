using System;
using System.Collections.Generic;
using LobbyService.Procedure;
using Steamworks;
using UnityEngine;

namespace LobbyService.Example.Steam
{
    public struct SteamProcedureMeta
    {
        public bool Valid;
        public string Key;
        public string[] Arguments;
        public CSteamID? Target; // Null if broadcast
    }
    public class SteamProcedureProvider : IProcedureProvider
    {
        private Dictionary<string, LobbyProcedure> _procedureMap = new();
        private const string ProcedureHeader = "[PROC]";
        
        
        public void RegisterProcedureLocally(LobbyProcedure procedure)
        {
            _procedureMap[procedure.Key] = procedure;
        }

        public bool Broadcast(ProviderId lobbyId, string key, params string[] args)
        {
            if (!SteamProvider.EnsureInitialized()) return false;
            if (!SteamProvider.ValidSteamId(lobbyId, out var steamId)) return false;

            bool isOwner = Lobby.IsOwner;

            if (!HasPermissionToSend(isOwner, _procedureMap[key]))
            {
                Debug.LogWarning($"Attempted to invoke procedure with key '{key}' but did not have permission.");
                return false;
            }

            var meta = new SteamProcedureMeta
            {
                Valid = true,
                Key = key,
                Arguments = args,
                Target = null
            };

            string strEncoded = EncodeProcedure(meta);

            byte[] encoded = System.Text.Encoding.UTF8.GetBytes(strEncoded);

            return SteamMatchmaking.SendLobbyChatMsg(steamId, encoded, encoded.Length);
        }

        public bool Target(ProviderId lobbyId, LobbyMember target, string key, params string[] args)
        {
            if (!SteamProvider.EnsureInitialized()) return false;
            if (!SteamProvider.ValidSteamId(lobbyId, out var steamId)) return false;
            if (!SteamProvider.ValidSteamId(target.Id, out var memberId)) return false;

            bool isOwner = Lobby.LocalMember.Id.Equals(new ProviderId(SteamMatchmaking.GetLobbyOwner(steamId).ToString()));

            if (!HasPermissionToSend(isOwner, _procedureMap[key]))
            {
                Debug.LogWarning($"Attempted to invoke procedure with key '{key}' but did not have permission.");
                return false;
            }

            var meta = new SteamProcedureMeta
            {
                Valid = true,
                Key = key,
                Arguments = args,
                Target = memberId
            };

            string strEncoded = EncodeProcedure(meta);
            byte[] encoded = System.Text.Encoding.UTF8.GetBytes(strEncoded);

            return SteamMatchmaking.SendLobbyChatMsg(steamId, encoded, encoded.Length);
        }

        private static bool HasPermissionToSend(bool isOwner, LobbyProcedure procedure)
        {
            switch (procedure.InvokePermission)
            {
                case InvokePermission.Owner:
                    if (!isOwner) return false;
                    break;
                case InvokePermission.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        private static string EncodeProcedure(SteamProcedureMeta steamProcedureMeta)
        {
            string targetPart = steamProcedureMeta.Target.HasValue ? steamProcedureMeta.Target.Value.ToString() : "";

            // Join arguments with '|'
            string argsPart = string.Join("|", steamProcedureMeta.Arguments);

            // Format: Header:Key:Target:arg1|arg2|...
            return $"{ProcedureHeader}:{steamProcedureMeta.Key}:{targetPart}:{argsPart}";
        }

        public static SteamProcedureMeta DecodeProcedure(string rawText)
        {
            var meta = new SteamProcedureMeta { Valid = false };

            if (string.IsNullOrEmpty(rawText)) return meta;

            // Format: Header:Key:Target:arg1|arg2|...
            // Split into four parts: Header, Key, Target, Args
            string[] parts = rawText.Split(new[] { ':' }, 4); // limit to 4 splits
            if (parts.Length < 3) return meta;                // need at least Header, Key and Target

            if (parts[0] != ProcedureHeader) return meta;
            meta.Key = parts[1];

            // Parse Target SteamID if present
            if (string.IsNullOrEmpty(parts[2]))
                meta.Target = null;
            else
                meta.Target = new CSteamID(ulong.Parse(parts[2]));

            // Parse arguments
            meta.Arguments = parts.Length == 4 && !string.IsNullOrEmpty(parts[3])
                ? parts[3].Split('|')
                : Array.Empty<string>();

            meta.Valid = true;
            return meta;
        }
        
        public void HandleProcedure(SteamProcedureMeta meta, LobbyChatMsg_t msg)
        {
            if (!meta.Valid) return;

            if (!_procedureMap.TryGetValue(meta.Key, out var procedure))
            {
                Debug.LogWarning($"Could not find a procedure with the key {meta.Key}");
                return;
            }

            var localId = SteamUser.GetSteamID();
            bool sentByUs = (CSteamID)msg.m_ulSteamIDUser == localId;
            bool isLocalExecution = sentByUs && procedure.ExecuteLocally;
            bool isRemoteBroadcast = !meta.Target.HasValue && !sentByUs;
            bool isExplicitTarget = meta.Target == localId;

            bool isTarget = isLocalExecution || isRemoteBroadcast || isExplicitTarget;

            if (isTarget) RunProcedure(procedure, meta.Arguments);
        }

        private async void RunProcedure(LobbyProcedure procedure, string[] args)
        {
            try
            {
                await procedure.Procedure(args);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void Dispose() { }
    }
}