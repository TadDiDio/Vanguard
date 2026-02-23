using System.Collections.Generic;
using LobbyService.LocalServer;
using TMPro;
using Vanguard;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Slider = UnityEngine.UI.Slider;

namespace LobbyService.Example
{
    public class LocalSampleView : MonoBehaviour, ICoreView, IFriendView, IBrowserView, IChatView
    {
        public TMP_Text lobbyNameText;
        public TMP_Text localUserText;

        public Button createButton;
        public Button friendsButton;
        
        public TMP_InputField nameInput;
        public Slider capacitySlider;
        public TMP_Text capacityText;
        
        public GameObject friendsPanel;
        public GameObject friendContainer;
        public GameObject friendCardPrefab;

        public Button acceptInviteButton;
        public Button rejectInviteButton;
        public GameObject invitePanel;
        public TMP_Text inviteText;
        
        public MemberCard memberCardPrefab;
        public GameObject memberCardContainer;

        public Button leaveButton;
        public Button closeButton;

        public Button toggleBrowserButton;
        public GameObject browserPanel;
        public GameObject browserContainer;
        public GameObject lobbyCardPrefab;
        public Button searchButton;
        public TMP_InputField searchName;
        public TMP_Text slotsAvailableText;
        public Slider slotsAvailableSlider;

        public TMP_Text chatLog;
        public TMP_InputField chatInput;
        public GameObject chatPanel;
        public ScrollRect chatView;
        
        private LobbyInvite? _invite;

        public Button startGameButton;
        
        private Dictionary<LobbyMember, MemberCard> _members = new();
        private List<GameObject> _browserLobbies = new();
        private bool _queueScrollDown;        
        private void Awake()
        {
            createButton.onClick.AddListener(Create);
            friendsButton.onClick.AddListener(ToggleFriends);
            capacitySlider.onValueChanged.AddListener(UpdateCapacity);
            
            acceptInviteButton.onClick.AddListener(AcceptInvite);
            rejectInviteButton.onClick.AddListener(RejectInvite);
            
            leaveButton.onClick.AddListener(Leave);
            closeButton.onClick.AddListener(Close);
            
            toggleBrowserButton.onClick.AddListener(ToggleBrowser);
            searchButton.onClick.AddListener(Browse);
            slotsAvailableSlider.onValueChanged.AddListener(UpdateSearchCapacityText);
            slotsAvailableText.text = $"Slots available: {slotsAvailableSlider.value}";
            chatInput.onSubmit.AddListener(SendChat);
            
            startGameButton.onClick.AddListener(ApplicationController.Instance.StartGame);
        }

        private void OnDestroy()
        {
            Lobby.DisconnectView(this);
            
            createButton.onClick.RemoveAllListeners();
            friendsButton.onClick.RemoveAllListeners();
            capacitySlider.onValueChanged.RemoveAllListeners();
            acceptInviteButton.onClick.RemoveAllListeners();
            rejectInviteButton.onClick.RemoveAllListeners();
            leaveButton.onClick.RemoveAllListeners();
            closeButton.onClick.RemoveAllListeners();
            toggleBrowserButton.onClick.RemoveAllListeners();
            searchButton.onClick.RemoveAllListeners();
            slotsAvailableSlider.onValueChanged.RemoveAllListeners();
            chatInput.onSubmit.RemoveAllListeners();
            startGameButton.onClick.RemoveAllListeners();
        }

        private void SendChat(string message)
        {
            Lobby.Chat.SendChatMessage(message);
            chatInput.text = string.Empty;
            _queueScrollDown = true;
            chatInput.ActivateInputField();
        }
        
        private void UpdateSearchCapacityText(float value)
        {
            slotsAvailableText.text = $"Slots available: {slotsAvailableSlider.value}";
        }
        
        private void Browse()
        {
            Lobby.Browser.Filter.AddSlotsAvailableFilter((int)slotsAvailableSlider.value);
            
            if (!string.IsNullOrEmpty(searchName.text))
            {
                Lobby.Browser.Filter.AddStringFilter(new LobbyStringFilter
                {
                    Key = LobbyKeys.NameKey,
                    Value = searchName.text
                });
            }
            
            Lobby.Browser.Browse();
        }
        
        private void ToggleBrowser()
        {
            browserPanel.SetActive(!browserPanel.activeSelf);
        }
        
        private void ToggleFriends()
        {
            friendsPanel.SetActive(!friendsPanel.activeSelf);
        }

        private void UpdateCapacity(float value)
        {
            capacityText.text = value.ToString();
        }
        
        private void AddMember(LobbyMember member)
        {
            var card = Instantiate(memberCardPrefab, memberCardContainer.transform);
            card.Initialize(member);

            if (Lobby.IsOwner && Lobby.LocalMember != member)
            {
                card.EnableOwnerButtons(true);
                card.kickButton.onClick.AddListener(() => Lobby.KickMember(member));
                card.promoteButton.onClick.AddListener(() => Lobby.SetOwner(member));
            }
            
            card.SetOwner(member == Lobby.Model.Owner);
            
            _members.Add(member, card);
        }
        
        private void Create()
        {
            Lobby.Create(new CreateLobbyRequest
            {
                Capacity = (int)capacitySlider.value,
                LobbyType =  LobbyType.Public,
                Name = string.IsNullOrEmpty(nameInput.text) ? $"{Lobby.LocalMember.DisplayName}'s Lobby" : nameInput.text
            });
        }

        private void AcceptInvite()
        {
            invitePanel.SetActive(false);
            if (!_invite.HasValue) return;
            
            Lobby.Join(new JoinLobbyRequest
            {
                LobbyId = _invite.Value.LobbyId
            });
        }

        private void RejectInvite()
        {
            invitePanel.SetActive(false);
            _invite = null;
        }
        
        public void Leave()
        {
            Lobby.Leave();
        }

        public void Close()
        {
            Lobby.CloseAndLeave();
        }
        
        public void DisplayExistingLobby(IReadonlyLobbyModel snapshot)
        {
            Debug.Log("Displaying existing lobby");
        }

        public void DisplayCreateRequested(CreateLobbyRequest request)
        {
            Debug.Log("Creating lobby...");
        }

        public void DisplayCreateResult(EnterLobbyResult result)
        {
            if (!result.Success) return;

            AddMember(result.LocalMember);

            OnEnterLobby();
        }

        public void DisplayJoinRequested(JoinLobbyRequest request)
        {
            Debug.Log("Joining lobby...");
        }

        public void DisplayJoinResult(EnterLobbyResult result)
        {
            if (!result.Success) return;

            foreach (var member in result.Members)
            {
                AddMember(member);
            }

            OnEnterLobby();
        }

        private void OnEnterLobby()
        {
            lobbyNameText.text = Lobby.GetLobbyDataOrDefault(LobbyKeys.NameKey, $"{Lobby.Model.Owner}'s lobby");

            if (string.IsNullOrEmpty(lobbyNameText.text) && Lobby.IsOwner)
            {
                Lobby.SetLobbyData(LobbyKeys.NameKey, $"{Lobby.LocalMember}'s lobby");
            }
            
            leaveButton.gameObject.SetActive(true);
            chatPanel.SetActive(true);
            SetViewIsOwner(Lobby.IsOwner);
        }
        
        public void DisplayLocalMemberLeft(LeaveInfo info)
        {
            localUserText.text = $"You are {Lobby.LocalMember.DisplayName}";
            lobbyNameText.text = "Create or join a lobby";

            leaveButton.gameObject.SetActive(false);
            chatPanel.SetActive(false);
            chatLog.text = string.Empty;
            SetViewIsOwner(false);
            
            foreach (var member in _members.Values)
            {
                Destroy(member.gameObject);
            }
            _members.Clear();
        }

        public void DisplaySentInvite(InviteSentInfo info)
        {
            if (info.InviteSent) Debug.Log($"Sending invite to {info.Member}...");
        }

        public void DisplayReceivedInvite(LobbyInvite invite)
        {
            invitePanel.SetActive(true);
            inviteText.text = $"Accept invitation from {invite.Sender}";
            _invite = invite;
        }

        public void DisplayOtherMemberJoined(MemberJoinedInfo info)
        {
            AddMember(info.Member);
        }
        
        public void DisplayOtherMemberLeft(LeaveInfo info)
        {
            if (info.LeaveReason is LeaveReason.Kicked) Debug.Log($"{info.Member} was Kicked");
            if (!_members.TryGetValue(info.Member, out var card)) return;
            
            Destroy(card.gameObject);
            _members.Remove(info.Member);
        }

        public void DisplayUpdateOwner(LobbyMember newOwner)
        {
            SetViewIsOwner(Lobby.IsOwner);
            
            foreach (var card in _members.Values)
            {
                card.SetOwner(newOwner == card.Member);
                card.EnableOwnerButtons(Lobby.IsOwner && Lobby.LocalMember != card.Member);

                if (Lobby.IsOwner && Lobby.LocalMember != card.Member)
                {
                    card.kickButton.onClick.AddListener(() => Lobby.KickMember(card.Member));
                    card.promoteButton.onClick.AddListener(() => Lobby.SetOwner(card.Member));
                }
            }
        }

        private void SetViewIsOwner(bool isOwner)
        {
            closeButton.gameObject.SetActive(isOwner);
            startGameButton.gameObject.SetActive(isOwner);
        }
        
        public void DisplayUpdateLobbyData(LobbyDataUpdate update)
        {
            lobbyNameText.text = update.Data.GetOrDefault(LobbyKeys.NameKey, "UNKNOWN NAME");
        }

        public void DisplayUpdateMemberData(MemberDataUpdate update)
        {
            Debug.Log($"{update.Member} ready: {update.Data.GetOrDefault(LobbyKeys.ReadyKey, "false")}");
        }

        private struct FriendCard
        {
            public GameObject Card;
            public LobbyMember Member;
            public void Invite()
            {
                Lobby.SendInvite(Member);
            }
            public void Destroy()
            {
                if (!Card) return;
                
                Card.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
                Object.Destroy(Card);
            }
        }
        
        private List<FriendCard> _friendCards = new();
        public void DisplayUpdatedFriendList(IReadOnlyList<LobbyMember> friends)
        {
            _friendCards.ForEach(c => c.Destroy());
            _friendCards.Clear();

            foreach (var friend in friends)
            {
                var card = new FriendCard
                {
                    Card = Instantiate(friendCardPrefab, friendContainer.transform),
                    Member = friend,
                };

                card.Card.GetComponentInChildren<TMP_Text>().text = $"Invite {friend} to lobby";
                card.Card.GetComponentInChildren<Button>().onClick.AddListener(card.Invite);
                _friendCards.Add(card);
            }
        }

        public void DisplayFriendAvatar(LobbyMember member, Texture2D avatar) { }

        public void ResetView(ILobbyCapabilities capabilities)
        {
            localUserText.text = $"You are {Lobby.LocalMember.DisplayName}";
            lobbyNameText.text = "Create or join a lobby";

            foreach (var member in _members.Values)
            {
                Destroy(member.gameObject);
            }
            _members.Clear();

            foreach (var friend in _friendCards)
            {
                friend.Destroy();
            }
            _friendCards.Clear();
            
            nameInput.text = string.Empty;
            capacitySlider.value = 4;
            capacityText.text = "4";
            friendsPanel.SetActive(false);
            invitePanel.SetActive(false);
            inviteText.text = "Accept invitation from ";
            _invite = null;
        }

        public void DisplayStartedBrowsing()
        {
            foreach (var lobby in _browserLobbies)
            {
                Destroy(lobby.gameObject);
            }
            _browserLobbies.Clear();
        }

        public void DisplayBrowsingResult(List<LobbyDescriptor> lobbies)
        {
            foreach (var lobby in lobbies)
            {
                var card = Instantiate(lobbyCardPrefab, browserContainer.transform);
                _browserLobbies.Add(card);
                card.GetComponent<LobbyCard>().Initialize(lobby);
            }
        }   

        public void DisplayAddedNumberFilter(LobbyNumberFilter filter)
        {
            
        }

        public void DisplayAddedStringFilter(LobbyStringFilter filter)
        {
            
        }

        public void DisplayRemovedNumberFilter(string key)
        {
            
        }

        public void DisplayRemovedStringFilter(string key)
        {
            
        }

        public void DisplaySetSlotsAvailableFilter(int numAvailable)
        {
            
        }

        public void DisplayClearedSlotsAvailableFilter()
        {
            
        }

        public void DisplaySetLimitResponsesFilter(int limit)
        {
            
        }

        public void DisplayClearLimitResponsesFilter()
        {
            
        }

        public void DisplayAddedDistanceFilter(LobbyDistance filter)
        {
            
        }

        public void DisplayClearedDistanceFilter()
        {
            
        }

        public void DisplayClearedAllFilters()
        {
            
        }

        public void DisplayAddedSorter(ILobbySorter sorter, string key)
        {
            
        }

        public void DisplayRemovedSorter(string key)
        {
            
        }

        public void DisplayClearedAllSorters()
        {
           
        }

        public void DisplayMessage(LobbyChatMessage message)
        {
            var color = message.Sender == Lobby.LocalMember ? "00FF00" : "0000FF"; 
            chatLog.text += $"<color=#{color}>[{message.Sender}]</color> {message.Content}\n";
            _queueScrollDown = true;
        }

        public void DisplayDirectMessage(LobbyChatMessage message)
        {
            var color = message.Sender == Lobby.LocalMember ? "00FF00" : "0000FF"; 
            chatLog.text += $"<color=#{color}>[{message.Sender} <whisper>]</color> {message.Content}\n";
            _queueScrollDown = true;
        }

        private void Update()
        {
            if (_queueScrollDown)
            {
                _queueScrollDown = false;                
                chatView.verticalNormalizedPosition = 0;
            }
        }
    }
}