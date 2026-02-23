using LobbyService.LocalServer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyService.Example
{
    public class LobbyCard : MonoBehaviour
    {
        public Button joinButton;
        public TMP_Text nameText;
        public TMP_Text capacity;
        
        public void Initialize(LobbyDescriptor descriptor)
        {
            joinButton.onClick.AddListener(() => Lobby.Join(new JoinLobbyRequest
            {
                LobbyId = descriptor.LobbyId
            }));

            nameText.text = Lobby.GetLobbyDataOrDefault(LobbyKeys.NameKey, "Unnamed Lobby", descriptor.LobbyId);
            capacity.text = $"{descriptor.MemberCount} / {descriptor.Capacity}";
        }
    }
}