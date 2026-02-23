using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyService.Procedure;
using Steamworks;
using UnityEngine;

namespace LobbyService.Example.Steam
{
    public class SteamProvider : BaseProvider
    {
        private List<string> _lobbyKeys;
        private List<string> _memberKeys;

        private Callback<GameLobbyJoinRequested_t> _lobbyJoinRequested;
        private Callback<LobbyChatMsg_t> _lobbyChatMsg;
        private Callback<LobbyChatUpdate_t> _lobbyChatUpdated;
        private Callback<LobbyDataUpdate_t> _lobbyDataUpdated;
        private Callback<LobbyInvite_t> _lobbyInvited;

        private const string CloseProcedureKey = "close_lobby";
        private const string KickProcedureKey = "kick_member";
        // ==== End Core ====

        /// <summary>
        /// Creates a mew steam lobby provider.
        /// </summary>
        /// <param name="lobbyKeys">Exhaustive list of keys for all lobby data.</param>
        /// <param name="memberKeys">Exhaustive list of keys for all member data.</param>
        public SteamProvider(List<string> lobbyKeys, List<string> memberKeys)
        {
            _lobbyKeys = lobbyKeys;
            _memberKeys = memberKeys;

            
        }

        public override void Initialize()
        {
            _lobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(ProcessAcceptedInvitation);
            _lobbyChatMsg = Callback<LobbyChatMsg_t>.Create(ProcessChatMessage);
            _lobbyChatUpdated = Callback<LobbyChatUpdate_t>.Create(ProcessLobbyChatUpdated);
            _lobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(ProcessLobbyDataUpdated);
            _lobbyInvited = Callback<LobbyInvite_t>.Create(ProcessReceivedInvitation);

            RegisterProcedures();
			((SteamHeartbeatProvider)Heartbeat).Initialize(Procedures);
        }

        private void RegisterProcedures()
        {
            // Close
            Procedures.RegisterProcedureLocally(new LobbyProcedure
            {
                ExecuteLocally = false,
                InvokePermission = InvokePermission.Owner,
                Key = CloseProcedureKey,
                Procedure = CloseProcedure
            });

            // Kick
            Procedures.RegisterProcedureLocally(new LobbyProcedure
            {
                ExecuteLocally = false,
                InvokePermission = InvokePermission.Owner,
                Key = KickProcedureKey,
                Procedure = KickProcedure
            });
        }

        public override void DisposeThis()
        {
            _lobbyJoinRequested?.Dispose();
            _lobbyChatMsg?.Dispose();
            _lobbyChatUpdated?.Dispose();
            _lobbyDataUpdated?.Dispose();
            _lobbyInvited?.Dispose();
        }

        #region Core
        public override IHeartbeatProvider Heartbeat { get; } = new SteamHeartbeatProvider();
        public override IBrowserProvider Browser { get; } = new SteamBrowserProvider();
        public override IFriendProvider Friends { get; } = new SteamFriendsProvider();
        public override IChatProvider Chat { get; } = new SteamChatProvider();
        public override IProcedureProvider Procedures { get; } = new SteamProcedureProvider();
        public override event Action<MemberJoinedInfo> OnOtherMemberJoined;
        public override event Action<LeaveInfo> OnOtherMemberLeft;
        public override event Action<LobbyDataUpdate> OnLobbyDataUpdated;
        public override event Action<MemberDataUpdate> OnMemberDataUpdated;
        public override event Action<LobbyInvite> OnReceivedInvitation;
        public override event Action<KickInfo> OnLocalMemberKicked;
        public override event Action<LobbyMember> OnOwnerUpdated;

        public static bool ValidSteamId(ProviderId id, out CSteamID steamId)
        {
            steamId = CSteamID.Nil;
            if (id == null) return false;

            if (!ulong.TryParse(id.ToString(), out ulong ulongId))
            {
                Debug.LogError($"Invalid lobby id: {id}. Could not parse to ulong.");
                return false;
            }

            steamId = (CSteamID)ulongId;
            return true;
        }

        public static bool EnsureInitialized()
        {
            if (SteamManager.Initialized) return true;
            
            Debug.LogError("Using steam lobby provider but steam is not initialized.");
            return false;
        }
        
        private List<LobbyMember> GetLobbyMembers(ProviderId lobbyId)
        {
            if (!ValidSteamId(lobbyId, out var steamId)) return null;
            var members = new List<LobbyMember>();

            int memberCount = SteamMatchmaking.GetNumLobbyMembers(steamId);
            for (int i = 0; i < memberCount; i++)
            {
                CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(steamId, i);
                string name = SteamFriends.GetFriendPersonaName(memberId);
                members.Add(new LobbyMember(new ProviderId(memberId.ToString()), name));
            }

            return members;
        }


        private LobbyMember GetOwner(CSteamID lobbyId)
        {
            var ownerId = SteamMatchmaking.GetLobbyOwner(lobbyId);
            return new LobbyMember(new ProviderId(ownerId.ToString()), SteamFriends.GetFriendPersonaName(ownerId));
        }

        private Metadata GetAllLobbyData(ProviderId lobbyId)
        {
            if (!ValidSteamId(lobbyId, out var steamId)) return null;

            var data = new Metadata();
            foreach (var key in _lobbyKeys)
            {
                var value = SteamMatchmaking.GetLobbyData(steamId, key);
                data.Set(key, string.IsNullOrEmpty(value) ? null : value);
            }

            return data;
        }

        private Metadata GetAllMemberData(ProviderId lobbyId, LobbyMember member)
        {
            if (!ValidSteamId(lobbyId, out var steamId)) return null;
            if (!ValidSteamId(member.Id, out var memberId)) return null;

            Metadata data = new();
            foreach (var key in _memberKeys)
            {
                var value = SteamMatchmaking.GetLobbyMemberData(steamId, memberId, key);
                data.Set(key, string.IsNullOrEmpty(value) ? null : value);
            }

            return data;
        }

        private Dictionary<LobbyMember, Metadata> GetAllMemberData(ProviderId lobbyId)
        {
            if (!ValidSteamId(lobbyId, out var steamId)) return null;

            var memberCount = SteamMatchmaking.GetNumLobbyMembers(steamId);

            var allData = new Dictionary<LobbyMember, Metadata>();

            for (int i = 0; i < memberCount; i++)
            {
                var memberId = SteamMatchmaking.GetLobbyMemberByIndex(steamId, i);
                var member = new LobbyMember(new ProviderId(memberId.ToString()), SteamFriends.GetFriendPersonaName(memberId));

                var data = GetAllMemberData(lobbyId, member);
                allData.Add(member, data);
            }

            return allData;
        }

        public override LobbyMember GetLocalUser()
        {
            if (!EnsureInitialized()) return null;

            var id = SteamUser.GetSteamID();
            var name = SteamFriends.GetPersonaName();
            return new LobbyMember(new ProviderId(id.ToString()), name);
        }

        public override async Task<EnterLobbyResult> CreateAsync(CreateLobbyRequest request)
        {
            if (!EnsureInitialized()) return EnterLobbyResult.Failed(EnterFailedReason.BackendNotInitialized);

            var tcs = new TaskCompletionSource<bool>();

            ELobbyType type = request.LobbyType switch
            {
                LobbyType.Public      => ELobbyType.k_ELobbyTypePublic,
                LobbyType.InviteOnly  => ELobbyType.k_ELobbyTypePrivate,
                _                     => throw new ArgumentOutOfRangeException()
            };

            ProviderId lobbyId = null;

            using var callResult = CallResult<LobbyCreated_t>.Create();
            var handle = SteamMatchmaking.CreateLobby(type, request.Capacity);

            callResult.Set(handle, (result, error) =>
            {
                if (error || result.m_eResult is not EResult.k_EResultOK)
                {
                    tcs.TrySetResult(false);
                    return;
                }

                var id = (CSteamID)result.m_ulSteamIDLobby;

                SteamMatchmaking.SetLobbyData(id, SteamLobbyKeys.ServerAddress, SteamUser.GetSteamID().ToString());
                SteamMatchmaking.SetLobbyData(id, SteamLobbyKeys.Name, request.Name);
                SteamMatchmaking.SetLobbyData(id, SteamLobbyKeys.Type, request.LobbyType.ToString());

                lobbyId = new ProviderId(id.ToString());
                tcs.SetResult(true);
            });

            if (!await tcs.Task) return EnterLobbyResult.Failed(EnterFailedReason.General);

            var localMember = GetLocalUser();
            var members = GetLobbyMembers(lobbyId);
            var lobbyData = GetAllLobbyData(lobbyId);
            var memberData = GetAllMemberData(lobbyId);
            var capacity = request.Capacity;

            return EnterLobbyResult.Succeeded
            (
                lobbyId,
                localMember,
                localMember,
                capacity,
                request.LobbyType,
                members,
                lobbyData,
                memberData
            );
        }

        public override async Task<EnterLobbyResult> JoinAsync(JoinLobbyRequest request)
        {
            if (!EnsureInitialized()) return EnterLobbyResult.Failed(EnterFailedReason.BackendNotInitialized);

            var tcs = new TaskCompletionSource<bool>();
            if (!ValidSteamId(request.LobbyId, out var lobbySteamId)) return EnterLobbyResult.Failed(EnterFailedReason.InvalidId);

            var handle = SteamMatchmaking.JoinLobby(lobbySteamId);
            using var callResult = CallResult<LobbyEnter_t>.Create();

            callResult.Set(handle, (result, error) =>
            {
                if (error || result.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
                {
                    tcs.SetResult(false);
                    return;
                }

                tcs.SetResult(true);
            });

            if (!await tcs.Task) return EnterLobbyResult.Failed(EnterFailedReason.General);

            var providerLobbyId = new ProviderId(lobbySteamId.ToString());
            var owner = GetOwner(lobbySteamId);

            var localMember = GetLocalUser();
            var members = GetLobbyMembers(providerLobbyId);
            var lobbyData = GetAllLobbyData(providerLobbyId);
            var memberData = GetAllMemberData(providerLobbyId);

            var capacity = SteamMatchmaking.GetLobbyMemberLimit(lobbySteamId);
            var type = SteamMatchmaking.GetLobbyData(lobbySteamId, SteamLobbyKeys.Type);
            Enum.TryParse<LobbyType>(type, out var strongType);

            return EnterLobbyResult.Succeeded
            (
                providerLobbyId,
                owner,
                localMember,
                capacity,
                strongType,
                members,
                lobbyData,
                memberData
            );
        }

        private void ProcessAcceptedInvitation(GameLobbyJoinRequested_t request)
        {
            if (!EnsureInitialized()) return;
            
            Lobby.Join(new JoinLobbyRequest
            {
                LobbyId = new ProviderId(request.m_steamIDLobby.ToString())
            });
        }

        private void ProcessReceivedInvitation(LobbyInvite_t invite)
        {
            var name = SteamFriends.GetFriendPersonaName((CSteamID)invite.m_ulSteamIDUser);
            var sender = new LobbyMember(new ProviderId(invite.m_ulSteamIDUser.ToString()), name);

            OnReceivedInvitation?.Invoke(new LobbyInvite
            {
                LobbyId = new ProviderId(invite.m_ulSteamIDLobby.ToString()),
                Sender = sender
            });
        }

        public override void Leave(ProviderId lobbyId)
        {
            if (!EnsureInitialized()) return;
            if (!ValidSteamId(lobbyId, out var id)) return;
            
            SteamMatchmaking.LeaveLobby(id);
        }

        public override bool CloseAndLeave(ProviderId lobbyId)
        {
            if (!EnsureInitialized()) return false;
            if (!ValidSteamId(lobbyId, out var id)) return false;

            var result = Lobby.Procedure.Broadcast
            (
                CloseProcedureKey,
                lobbyId.ToString()
            );

            if (!result) return false;

            SteamMatchmaking.LeaveLobby(id);
            return true;
        }

        public override bool SendInvite(ProviderId lobbyId, LobbyMember member)
        {
            if (!EnsureInitialized()) return false;
            if (!ValidSteamId(lobbyId, out var lobbySteamId)) return false;
            if (!ValidSteamId(member.Id, out var memberSteamId)) return false;

            SteamMatchmaking.InviteUserToLobby(lobbySteamId, memberSteamId);
            return true;
        }

        public override bool SetOwner(ProviderId lobbyId, LobbyMember newOwner)
        {
            if (!EnsureInitialized()) return false;
            if (!ValidSteamId(lobbyId, out var lobbySteamId)) return false;
            if (!ValidSteamId(newOwner.Id, out var newOwnerSteamId)) return false;

            return SteamMatchmaking.SetLobbyOwner(lobbySteamId, newOwnerSteamId);
        }

        public override bool KickMember(ProviderId lobbyId, LobbyMember member)
        {
            if (!EnsureInitialized()) return false;
            if (!ValidSteamId(member.Id, out var memberId)) return false;
            if (!Lobby.IsOwner) return false;

            var sent = Lobby.Procedure.Broadcast(
                KickProcedureKey,
                lobbyId.ToString(),
                memberId.ToString(),
                nameof(KickReason.General)
            );

            return sent;
        }

        public override bool SetLobbyData(ProviderId lobbyId, string key, string value)
        {
            if (!EnsureInitialized()) return false;
            if (!ValidSteamId(lobbyId, out var steamId)) return false;
            return SteamMatchmaking.SetLobbyData(steamId, key, value);
        }

        public override void SetLocalMemberData(ProviderId lobbyId, string key, string value)
        {
            if (!EnsureInitialized()) return;
            if (!ValidSteamId(lobbyId, out var steamId)) return;
            SteamMatchmaking.SetLobbyMemberData(steamId, key, value);
        }

        public override string GetLobbyData(ProviderId lobbyId, string key, string defaultValue)
        {
            if (!EnsureInitialized()) return defaultValue;
            if (!ValidSteamId(lobbyId, out var steamId)) return defaultValue;

            var result = SteamMatchmaking.GetLobbyData(steamId, key);

            return string.IsNullOrEmpty(result) ? defaultValue : result;
        }

        public override string GetMemberData(ProviderId lobbyId, LobbyMember member, string key, string defaultValue)
        {
            if (!EnsureInitialized()) return defaultValue;
            if (!ValidSteamId(lobbyId, out var steamId)) return defaultValue;
            if (!ValidSteamId(member.Id, out var memberId)) return defaultValue;

            var result = SteamMatchmaking.GetLobbyMemberData(steamId, memberId, key);

            return string.IsNullOrEmpty(result) ? defaultValue : result;
        }

        private void ProcessLobbyChatUpdated(LobbyChatUpdate_t update)
        {
            const int enteredMask = 0x0001;
            const int leftMask    = 0x0002;

            bool entered = (update.m_rgfChatMemberStateChange & enteredMask) != 0;
            bool left    = (update.m_rgfChatMemberStateChange & leftMask)    != 0;

            var userId = update.m_ulSteamIDUserChanged;
            var displayName = SteamFriends.GetFriendPersonaName((CSteamID)userId);
            LobbyMember member = new LobbyMember(new ProviderId(userId.ToString()), displayName);

            var lobbyId = new ProviderId(update.m_ulSteamIDLobby.ToString());

            if (entered)
            {
                OnOtherMemberJoined?.Invoke(new MemberJoinedInfo
                {
                    Member = member,
                    Data = GetAllMemberData(lobbyId, member)
                });
            }
            else if (left)
            {
                OnOtherMemberLeft?.Invoke(new LeaveInfo
                {
                    Member = member,
                    LeaveReason = LeaveReason.UserRequested,
                    KickInfo = null
                });
            }
        }

        private void ProcessLobbyDataUpdated(LobbyDataUpdate_t update)
        {
            if (!EnsureInitialized()) return;
            
            if (update.m_ulSteamIDLobby == update.m_ulSteamIDMember)
            {
                var owner = GetOwner((CSteamID)update.m_ulSteamIDLobby);
                OnOwnerUpdated?.Invoke(owner);

                var data = new LobbyDataUpdate
                {
                    Data = GetAllLobbyData(new ProviderId(update.m_ulSteamIDLobby.ToString()))
                };

                OnLobbyDataUpdated?.Invoke(data);
            }
            else
            {
                var id = new ProviderId(update.m_ulSteamIDMember.ToString());
                var name = SteamFriends.GetFriendPersonaName((CSteamID)update.m_ulSteamIDMember);
                var member = new LobbyMember(id, name);
                var data = new MemberDataUpdate
                {
                    Member = member,
                    Data = GetAllMemberData(new ProviderId(update.m_ulSteamIDLobby.ToString()), member)
                };

                OnMemberDataUpdated?.Invoke(data);
            }
        }
        #endregion

        #region Procedures
        // Kick member procedure: Expects 3 args: ulong: lobby id | ulong target id | KickReason: reason
        private async Task KickProcedure(string[] args)
        {
            if (args.Length < 3) return;
            if (!ValidSteamId(new ProviderId(args[0]), out var steamId)) return;
            if (!ValidSteamId(new ProviderId(args[1]), out var memberId)) return;

            if (!Enum.TryParse<KickReason>(args[2], out var reason))
            {
                reason = KickReason.General;
            }

            var info = new KickInfo
            {
                Reason = reason
            };

            if (memberId == SteamUser.GetSteamID())
            {
                SteamMatchmaking.LeaveLobby(steamId);
                OnLocalMemberKicked?.Invoke(info);
            }
            else
            {
                var member = new LobbyMember(new ProviderId(memberId.ToString()),
                    SteamFriends.GetFriendPersonaName(memberId));
                OnOtherMemberLeft?.Invoke(new LeaveInfo
                {
                    Member = member,
                    LeaveReason = LeaveReason.Kicked,
                    KickInfo = info
                });
            }

            await Task.CompletedTask;
        }
        
        // Close procedure: Expects 1 argL ulong lobbyId.
        private async Task CloseProcedure(string[] args)
        {
            if (args.Length < 1) return;
            if (!EnsureInitialized()) return;
            if (!ValidSteamId(new ProviderId(args[0]), out var steamId)) return;

            SteamMatchmaking.LeaveLobby(steamId);

            OnLocalMemberKicked?.Invoke(new KickInfo
            {
                Reason = KickReason.LobbyClosed
            });

            await Task.CompletedTask;
        }
        #endregion
        
        #region Steam Chat
        private void ProcessChatMessage(LobbyChatMsg_t msg)
        {
            var text = GetText(msg, out var senderId);
            if (string.IsNullOrEmpty(text)) return;
            
            if (ExecuteIfProcedure(text, msg)) return;

            ((SteamChatProvider)Chat).ReceiveChatMessage(text, senderId);
        }

        private string GetText(LobbyChatMsg_t msg, out CSteamID senderId)
        {
            const int normal = 1;
            senderId = default;
            
            if (msg.m_eChatEntryType is not normal) return null;

            var steamId = (CSteamID)msg.m_ulSteamIDLobby;
            var msgId = (int)msg.m_iChatID;
            var bufferSize = 2048;
            var buffer = new byte[bufferSize];

            int size = SteamMatchmaking.GetLobbyChatEntry(steamId, msgId, out senderId, buffer, bufferSize, out _);
            if (size <= 0) return null;

            // Convert the byte array into a string (UTF-8)
            string text = System.Text.Encoding.UTF8.GetString(buffer, 0, size);
            return text.Trim();
        }

        private bool ExecuteIfProcedure(string text, LobbyChatMsg_t msg)
        {
            var procedure = SteamProcedureProvider.DecodeProcedure(text);

            if (!procedure.Valid) return false;
            
            ((SteamProcedureProvider)Procedures).HandleProcedure(procedure, msg);
            return true;
        }
        
        #endregion
    }
}
