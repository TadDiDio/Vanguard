using UnityEngine;
using UnityEngine.UI;

namespace LobbyService.Example
{
    public class ColorChanger : MonoBehaviour
    {
        [SerializeField] private float rate = 0.05f;

        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void Update()
        {
            Color.RGBToHSV(_image.color, out float h, out float s, out float v);

            h += rate * Time.deltaTime;
            h %= 1;
            
            _image.color = Color.HSVToRGB(h, s, v);
            _image.color *= new Color(1, 1, 1, 0.4f);
        }
    }
}