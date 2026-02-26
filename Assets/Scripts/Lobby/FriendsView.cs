using System.Collections.Generic;
using LobbyService;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendsView : MonoBehaviour, IFriendView
{
    [SerializeField] private FriendCard friendCardPrefab;
    [SerializeField] private Transform friendsContainer;
    [SerializeField] private Button showFriends;
    
    private List<FriendCard> _friendCards = new();
    private bool _showingFriends;
    
    private void Start()
    {
        Lobby.ConnectView(this);
        friendsContainer.gameObject.SetActive(false);
        showFriends.onClick.AddListener(ShowFriends);
    }

    private void ShowFriends()
    {
        _showingFriends = !_showingFriends;
        friendsContainer.gameObject.SetActive(_showingFriends);
        showFriends.GetComponentInChildren<TMP_Text>().text = _showingFriends ? "Hide Friends" : "See Friends";
    }
    
    public void ResetView(ILobbyCapabilities capabilities)
    {
        foreach (var card in _friendCards)
        {
            card.Invited -= HandleInvite;
            Destroy(card.gameObject);
        }
        
        _friendCards.Clear();
    }

    public void DisplayUpdatedFriendList(IReadOnlyList<LobbyMember> friends)
    {
        ResetView(null);

        foreach (var friend in friends)
        {
            var card = Instantiate(friendCardPrefab, friendsContainer);
            card.Init(friend);
            card.Invited += HandleInvite;
            _friendCards.Add(card);
        }
    }

    private void HandleInvite(FriendCard card)
    {
        Lobby.SendInvite(card.GetUser());
    }
    
    public void DisplayFriendAvatar(LobbyMember member, Texture2D avatar) { }
}
