using System;
using LobbyService;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendCard : MonoBehaviour
{
    [SerializeField] private Button inviteButton;
    [SerializeField] private TMP_Text username;
    
    public event Action<FriendCard> Invited;

    private LobbyMember _user;
    
    public void Init(LobbyMember friend)
    {
        _user = friend;
        username.text = friend.DisplayName;
        inviteButton.onClick.AddListener(OnInvited);
    }

    private void OnInvited() => Invited?.Invoke(this);

    private void OnDestroy()
    {
        inviteButton.onClick.RemoveAllListeners();
    }

    public LobbyMember GetUser() => _user;
}
