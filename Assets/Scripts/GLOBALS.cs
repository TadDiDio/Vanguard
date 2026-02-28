using PurrNet;
using PurrNet.Steam;
using PurrNet.Transports;
using UnityEngine;

namespace Vanguard
{
    public class GLOBALS : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UDPTransport udpTransport;
        [SerializeField] private SteamTransport steamTransport;
        
        public NetworkManager NetworkManager => networkManager;
        public UDPTransport UDPTransport => udpTransport;
        public SteamTransport SteamTransport => steamTransport;
    }
}