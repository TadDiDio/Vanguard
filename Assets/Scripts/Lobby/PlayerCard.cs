using System;
using LobbyService;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Vanguard
{
    public class PlayerCard : MonoBehaviour
    {
        [SerializeField] private Image ownerCrown;

        [SerializeField] private Image swapImage;
        [SerializeField] private Image promoteImage;
        [SerializeField] private Image kickImage;

        [SerializeField] private Button swapButton;
        [SerializeField] private Button promoteButton;
        [SerializeField] private Button kickButton;

        [SerializeField] private TMP_Text nameText;
        
        private LobbyMember _user;
        private bool _isMarine;
        
        public event Action<PlayerCard> Promoted;
        public event Action<PlayerCard> Kicked;
        public event Action<PlayerCard> Swapped;
        
        private void Awake()
        {
            SetOwner(false);
            ToggleControls(false);
            
            swapButton.onClick.AddListener(OnSwap);
            promoteButton.onClick.AddListener(OnPromote);
            kickButton.onClick.AddListener(OnKick);
        }

        private void OnDestroy()
        {
            swapButton.onClick.RemoveAllListeners();
            promoteButton.onClick.RemoveAllListeners();
            kickButton.onClick.RemoveAllListeners();
        }

        private void OnSwap() => Swapped?.Invoke(this);
        private void OnKick() => Kicked?.Invoke(this);
        private void OnPromote() =>Promoted?.Invoke(this);

        public void Init(LobbyMember user, bool marine, bool isOwner)
        {
            _user = user;
            nameText.text = user.DisplayName;
            SetTeam(marine);
            SetOwner(isOwner);
        }

        public LobbyMember GetUser() => _user;

        public bool IsMarine() => _isMarine;
        public void SetTeam(bool marines)
        {
            _isMarine = marines;
            
            if (marines)
            {
                swapImage.color = Color.blue;
                promoteImage.color = Color.blue;
                kickImage.color = Color.blue;
                
                nameText.color = Color.white;
                
                swapImage.transform.localRotation = quaternion.identity;
            }
            else
            {
                swapImage.color = Color.red;
                promoteImage.color = Color.red;
                kickImage.color = Color.red;
                
                nameText.color = Color.black;

                swapImage.transform.localRotation = Quaternion.Euler(0, 0, 180);
            }
        }

        public void ToggleControls(bool enable)
        {
            swapImage.enabled = enable;
            promoteImage.enabled = enable;
            kickImage.enabled = enable;
        }
        
        public void SetOwner(bool isOwner)
        {
            ownerCrown.enabled = isOwner;
        }
    }
}
