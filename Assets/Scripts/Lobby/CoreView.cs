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
        private const string TeamDelimiter = ":";
        
        
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
        }

        private void OnStartButton()
        {
            Debug.Log("Starting game!");
            // ApplicationController.Instance.StartGame();
        }

        private void OnLeaveButton()
        {
            Lobby.Leave();
            SwapToTitle();
        }

        private void OnCloseButton()
        {
            Lobby.CloseAndLeave();
            SwapToTitle();
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
            
            titlePanel.SetActive(true);
            lobbyPanel.SetActive(false);
        }
        
        public void DisplayExistingLobby(IReadonlyLobbyModel snapshot)
        {
            if (!snapshot.InLobby)
            {
                return;
            }
            
            var alienTeamRaw = snapshot.LobbyData.GetOrDefault(ApplicationController.AlienTeamKey, "");
            var alienTeam  = LinqUtility.ToHashSet(alienTeamRaw.Split(TeamDelimiter));
            
            foreach (var member in snapshot.Members)
            {
                bool isMarine = !alienTeam.Contains(member.Id.ToString());
                bool isOwner = snapshot.Owner == member;
                
                AddMemberToView(member, isMarine, isOwner);
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

            AddMemberToTeamSafe(result.Owner, AddToMarinesNext());
            DisplayEnter(result.Members);
        }

        private void AddMemberToTeamSafe(LobbyMember member, bool marines)
        {
            if (!Lobby.IsOwner) return;
            
            string key = marines ? ApplicationController.MarineTeamKey : ApplicationController.AlienTeamKey;
            string list = Lobby.GetLobbyDataOrDefault(key, "");

            var newList = $"{list}{member.Id}{TeamDelimiter}";
            Lobby.SetLobbyData(key, newList);
        }
        
        private void RemoveMemberFromTeamSafe(LobbyMember member, bool marines)
        {
            if (!Lobby.IsOwner) return;

            string key = marines ? ApplicationController.MarineTeamKey : ApplicationController.AlienTeamKey;
            
            string list = Lobby.GetLobbyDataOrDefault(key, "");

            list = list.Replace(member.Id.ToString(), "");
            list = list.Replace($"{TeamDelimiter}{TeamDelimiter}", TeamDelimiter);
            if (list == TeamDelimiter) list = "";

            Lobby.SetLobbyData(key, list);
        }

        private bool AddToMarinesNext()
        {
            string aliensTeam = Lobby.GetLobbyDataOrDefault(ApplicationController.AlienTeamKey, "");
            string marinesTeam = Lobby.GetLobbyDataOrDefault(ApplicationController.MarineTeamKey, "");
            
            var aliens = LinqUtility.ToHashSet(aliensTeam.Split(TeamDelimiter));
            var marines = LinqUtility.ToHashSet(marinesTeam.Split(TeamDelimiter));
            
            return aliens.Count >= marines.Count;
        }

        private void SwapTeamSafe(LobbyMember member)
        {
            if (!Lobby.IsOwner) return;
         
            bool isMarine = IsMemberMarine(member);
            
            RemoveMemberFromTeamSafe(member, isMarine);
            AddMemberToTeamSafe(member, !isMarine);
            SwapTeamView(member, !isMarine);
        }

        private void SwapTeamView(LobbyMember member, bool toMarines)
        {
            var card = GetUserCard(member);

            if (!card)
            {
                Debug.LogError($"Display attempted to swap teams for {member} but they did not have a card");
                return;
            }
            
            card.SetTeam(toMarines);
            card.transform.SetParent(toMarines ? marineCardContainer : alienCardContainer);
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
                AddMemberToView(member, IsMemberMarine(member), member == Lobby.Model.Owner);
            }
            
            SetLocalIsOwner(Lobby.IsOwner);
            
            titlePanel.SetActive(false);
            lobbyPanel.SetActive(true);
        }

        private bool IsMemberMarine(LobbyMember member)
        {
            string aliensString = Lobby.GetLobbyDataOrDefault(ApplicationController.AlienTeamKey, "");
            
            var aliens = aliensString.Split(TeamDelimiter);
            
            return !aliens.Contains(member.Id.ToString());
        }

        private void AddMemberToView(LobbyMember member, bool isMarine, bool isOwner)
        {
            var parent = isMarine ? marineCardContainer : alienCardContainer;

            var card = Instantiate(playerCardPrefab, parent);
            card.Init(member, isMarine, isOwner);

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
            bool willBeMarine = AddToMarinesNext();
            
            AddMemberToView(info.Member, willBeMarine, false);
            AddMemberToTeamSafe(info.Member, willBeMarine);
        }

        public void DisplayOtherMemberLeft(LeaveInfo info)
        {
            // Bug in local server binary means we get this twice on a kick
            if (!GetUserCard(info.Member)) return;
            
            // Brute force try both
            RemoveMemberFromTeamSafe(info.Member, true);
            RemoveMemberFromTeamSafe(info.Member, false);

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
            string aliensString = Lobby.GetLobbyDataOrDefault(ApplicationController.AlienTeamKey, "");
            var aliens = aliensString.Split(TeamDelimiter);
            
            foreach (var card in _playerCards)
            {
                if (card.IsMarine() && aliens.Contains(card.GetUser().Id.ToString()))
                {
                    SwapTeamView(card.GetUser(), false);
                }
                if (!card.IsMarine() && !aliens.Contains(card.GetUser().Id.ToString()))
                {
                    SwapTeamView(card.GetUser(), true);
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