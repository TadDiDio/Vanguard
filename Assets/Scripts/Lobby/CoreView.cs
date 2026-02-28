using System;
using System.Collections.Generic;
using System.Linq;
using LobbyService;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Vanguard
{
    public class CoreView : MonoBehaviour, ICoreView
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject titlePanel;

        [SerializeField] private PlayerCard playerCardPrefab;
        
        [SerializeField] private Transform alienCardContainer;
        [SerializeField] private Transform marineCardContainer;

        [SerializeField] private GameObject invitedPanel;
        
        [SerializeField] private Button acceptInvite;
        [SerializeField] private Button declineInvite;
        [SerializeField] private TMP_Text invitedPanelText;

        private List<PlayerCard> _playerCards = new();
        
        
        private void Awake()
        {
            startButton.onClick.AddListener(OnStartButton);
            leaveButton.onClick.AddListener(OnLeaveButton);
            closeButton.onClick.AddListener(OnCloseButton);

            acceptInvite.onClick.AddListener(HandleAcceptInvite);
            acceptInvite.onClick.AddListener(HandleDeclineInvite);
        }

        private void Start()
        {
            Lobby.ConnectView(this);
        }

        private void OnDestroy()
        {
            startButton.onClick.RemoveAllListeners();
            leaveButton.onClick.RemoveAllListeners();
            closeButton.onClick.RemoveAllListeners();

            Lobby.DisconnectView(this);
        }

        private void OnStartButton()
        {
            ApplicationController.Instance.TransitionToGame();
        }

        private void OnLeaveButton()
        {
            Lobby.Leave();
            SwapToTitle();
            ApplicationController.Instance.AllowStartGame();
        }

        private void OnCloseButton()
        {
            Lobby.CloseAndLeave();
            SwapToTitle();
            ApplicationController.Instance.AllowStartGame();
        }

        private void SwapToTitle()
        {
            lobbyPanel.SetActive(false);
            titlePanel.SetActive(true);
        }
        
        public void ResetView(ILobbyCapabilities capabilities)
        {
            startButton.gameObject.SetActive(false);
            closeButton.gameObject.SetActive(false);
            invitedPanel.SetActive(false);
            
            foreach (var card in _playerCards)
            {
                DestroyViewCard(card, false);
            }
            
            _playerCards.Clear();

            SetLocalIsOwner(false);
        }
        
        public void DisplayExistingLobby(IReadonlyLobbyModel snapshot)
        {
            if (!snapshot.InLobby) return;
            
            foreach (var member in snapshot.Members)
            {
                Team team = TeamHelper.GetMemberTeam(member);
                bool isOwner = snapshot.Owner == member;
                
                AddMemberToView(member, team, isOwner);
            }
            
            SetLocalIsOwner(Lobby.IsOwner);
        }

        public void DisplayCreateRequested(CreateLobbyRequest request) { }

        public void DisplayCreateResult(EnterLobbyResult result)
        {
            if (!result.Success)
            {
                SwapToTitle();
                return;
            }

            AddMemberToTeamSafe(result.Owner, GetNextTeamToAddTo());
            DisplayEnter(result.Members);
        }

        private void AddMemberToTeamSafe(LobbyMember member, Team team)
        {
            if (!Lobby.IsOwner) return;

            TeamHelper.AddMemberToTeam(member, team);
        }
        
        private void RemoveMemberFromTeamSafe(LobbyMember member, Team team)
        {
            if (!Lobby.IsOwner) return;

            TeamHelper.RemoveMemberFromTeam(member, team);
        }

        private Team GetNextTeamToAddTo()
        {
            var marineCount = TeamHelper.GetTeamCount(Team.Marines);
            var alienCount = TeamHelper.GetTeamCount(Team.Aliens);
            
            return alienCount >= marineCount ? Team.Marines : Team.Aliens;
        }

        private void SwapTeamSafe(LobbyMember member)
        {
            if (!Lobby.IsOwner) return;
         
            var team = TeamHelper.GetMemberTeam(member);
            var otherTeam = team == Team.Marines ? Team.Aliens : Team.Marines;
            
            RemoveMemberFromTeamSafe(member, team);
            AddMemberToTeamSafe(member, otherTeam);
            SwapTeamView(member, otherTeam);
        }

        private void SwapTeamView(LobbyMember member, Team team)
        {
            var card = GetUserCard(member);

            if (!card)
            {
                Debug.LogError($"Display attempted to swap teams for {member} but they did not have a card");
                return;
            }
            
            card.SetTeam(team);
            card.transform.SetParent(team == Team.Marines ? marineCardContainer : alienCardContainer);
        }
        
        
        private PlayerCard GetUserCard(LobbyMember member)
        {
            return _playerCards.FirstOrDefault(c => c.GetUser() == member);
        }
        
        public void DisplayJoinResult(EnterLobbyResult result)
        {
            if (!result.Success)
            {
                SwapToTitle();
                return;
            }
            
            DisplayEnter(result.Members);
        }

        private void DisplayEnter(List<LobbyMember> members)
        {
            foreach (var member in members)
            {
                AddMemberToView(member, TeamHelper.GetMemberTeam(member), member == Lobby.Model.Owner);
            }
            
            SetLocalIsOwner(Lobby.IsOwner);
            
            titlePanel.SetActive(false);
            lobbyPanel.SetActive(true);
        }

        private void AddMemberToView(LobbyMember member, Team team, bool isOwner)
        {
            var parent = team == Team.Marines ? marineCardContainer : alienCardContainer;

            var card = Instantiate(playerCardPrefab, parent);
            card.Init(member, team, isOwner);

            if (Lobby.IsOwner)
            {
                card.Kicked += HandleKick;
                card.Promoted += HandlePromote;
                card.Swapped += HandleSwap;
                
                card.ToggleControls(true);
            }
            
            _playerCards.Add(card);
        }

        private void HandleKick(PlayerCard card)
        {
            Lobby.KickMember(card.GetUser());
        }

        private void HandlePromote(PlayerCard card)
        {
            Lobby.SetOwner(card.GetUser());
        }

        private void HandleSwap(PlayerCard card)
        {
            SwapTeamSafe(card.GetUser());
        }
        
        public void DisplayJoinRequested(JoinLobbyRequest request) { }

        public void DisplayLocalMemberLeft(LeaveInfo info)
        {
            ResetView(null);
        }

        public void DisplaySentInvite(InviteSentInfo info) { }

        private LobbyInvite _invite;
        public void DisplayReceivedInvite(LobbyInvite invite)
        {
            _invite = invite;
            invitedPanelText.text = $"You were invited to join {invite.Sender.DisplayName}'s lobby";
            invitedPanel.SetActive(true);
        }

        private void HandleAcceptInvite()
        {
            var request = new JoinLobbyRequest()
            {
                LobbyId = _invite.LobbyId
            };
            
            Lobby.Join(request);
            invitedPanel.SetActive(false);
        }

        private void HandleDeclineInvite()
        {
            invitedPanel.SetActive(false);
        }
        
        public void DisplayOtherMemberJoined(MemberJoinedInfo info)
        {
            Team team = GetNextTeamToAddTo();
            
            AddMemberToView(info.Member, team, false);
            AddMemberToTeamSafe(info.Member, team);
        }

        public void DisplayOtherMemberLeft(LeaveInfo info)
        {
            // Bug in local server binary means we get this twice on a kick
            if (!GetUserCard(info.Member)) return;
            
            // Brute force try both
            RemoveMemberFromTeamSafe(info.Member, Team.Marines);
            RemoveMemberFromTeamSafe(info.Member, Team.Aliens);

            var match = GetUserCard(info.Member);
            
            if (match)
            {
                DestroyViewCard(match);
            }
            else
            {
                Debug.LogWarning($"Member {info.Member} left but not found in view!");
            }
        }

        private void DestroyViewCard(PlayerCard card, bool remove = true)
        {
            card.Kicked -= HandleKick;
            card.Promoted -= HandlePromote;
            card.Swapped -= HandleSwap;
            
            Destroy(card.gameObject);
            
            if (remove) _playerCards.Remove(card);
        }
        
        
        public void DisplayUpdateOwner(LobbyMember newOwner)
        {
            SetLocalIsOwner(Lobby.IsOwner);
        }

        public void DisplayUpdateLobbyData(LobbyDataUpdate update)
        {
            foreach (var card in _playerCards)
            {
                var actualTeam = TeamHelper.GetMemberTeam(card.GetUser());
                if (card.GetTeam() != actualTeam)
                {
                    SwapTeamView(card.GetUser(), actualTeam);
                }
            }
        }
        public void DisplayUpdateMemberData(MemberDataUpdate update) { }

        private void SetLocalIsOwner(bool isOwner)
        {
            startButton.gameObject.SetActive(isOwner);
            closeButton.gameObject.SetActive(isOwner);
            
            foreach (var card in _playerCards)
            {
                card.Kicked -= HandleKick;
                card.Promoted -= HandlePromote;
                card.Swapped -= HandleSwap;

                if (Lobby.IsOwner)
                {
                    card.Kicked += HandleKick;
                    card.Promoted += HandlePromote;
                    card.Swapped += HandleSwap;
                }
                
                card.ToggleControls(isOwner);
            }
        }
    }
}