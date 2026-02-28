using UnityEngine;
using UnityEngine.UI;

namespace Vanguard
{
    public class EscapeMenu : MonoBehaviour
    {
        [SerializeField] private Button leaveButton;
        [SerializeField] private GameObject canvas;        
        
        private void Awake()
        {
            leaveButton.onClick.AddListener(Leave);
            canvas.SetActive(false);
        }

        private void OnDestroy()
        {
            leaveButton.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                canvas.SetActive(!canvas.activeSelf);
            }
        }

        private void Leave() => ApplicationController.Instance.TransitionToTitle();
    }
}