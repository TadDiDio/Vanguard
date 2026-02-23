using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyService.Example
{
    public class MemberCard : MonoBehaviour
    {
        public TMP_Text memberName;
        public RawImage avatarImage;
        public Button kickButton;
        public Button promoteButton;
        public Image ownerCrown;
        
        public LobbyMember Member;
        
        private void Awake()
        {
            kickButton.gameObject.SetActive(false);
            ownerCrown.gameObject.SetActive(false);
            promoteButton.gameObject.SetActive(false);
        }

        public void Initialize(LobbyMember member)
        {
            Member = member;
            memberName.text = member.DisplayName;
        }

        public void SetAvatar(Texture2D avatar)
        {
            avatarImage.texture = avatar;
        }
        
        public void EnableOwnerButtons(bool buttonEnabled)
        {
            kickButton.gameObject.SetActive(buttonEnabled);
            promoteButton.gameObject.SetActive(buttonEnabled);
        }

        public void SetOwner(bool isOwner)
        {
            ownerCrown.gameObject.SetActive(isOwner);
        }
    }
}