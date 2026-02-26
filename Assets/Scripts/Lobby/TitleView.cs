using LobbyService;
using UnityEngine;
using UnityEngine.UI;

public class TitleView : MonoBehaviour
{
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Canvas canvas;
    
    private void Awake()
    {
        canvas.worldCamera = Camera.main;
        
        createLobbyButton.onClick.AddListener(Create);
        
        titlePanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }

    private void Create()
    {
        var request = new CreateLobbyRequest
        {
            Name = "Vanguard lobby",
            Capacity = 12,
            LobbyType = LobbyType.InviteOnly
        };
        
        Lobby.Create(request);
        
        lobbyPanel.SetActive(true);
        titlePanel.SetActive(false);
    }
}
