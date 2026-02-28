using PurrNet;
using SessionService;
using UnityEngine;

namespace Vanguard
{
    public class Watchdog : MonoBehaviour
    {
        private void Awake()
        {
            NetworkManager.main.onNetworkShutdownSimple += OnShutdown;
        }

        private void OnShutdown(NetworkManager manager)
        {
            if (NetworkManager.main.isServer) return;

            print("EFJUIOSEFH SEIUFH SIUEHF ISUEFH ");
            if (Session.State == SessionState.Connected ||
                Session.State == SessionState.Connecting)
            {
                _ = Session.DisconnectAsync();
            }
        }
    }
}