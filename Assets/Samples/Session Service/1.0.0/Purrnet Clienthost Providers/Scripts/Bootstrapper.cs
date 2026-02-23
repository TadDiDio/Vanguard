using PurrNet;
using PurrNet.Steam;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SessionService.Sample
{
    public class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private NetworkManager network;
        [SerializeField] private SteamTransport steamTransport;
        [SerializeField] private LocalTransport localTransport;
        
        private void Awake()
        {
            Session.SetProvider(new LocalSessionProvider(network, localTransport));
        }

        private void Update()
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                _ = Session.CreateSessionAsync(new SessionCreateRequest
                {
                    TimeoutSeconds = 10
                }, destroyCancellationToken);
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                _ = Session.DisconnectAsync();
            }
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                Session.SetProvider(new SteamworksSessionProvider(network, steamTransport));
            }
        }
    }
}