using System;
using PurrNet;
using PurrNet.Steam;
using PurrNet.Transports;
using Steamworks;

namespace SessionService.Sample
{
    public class SteamworksSessionProvider : BaseClientHostProvider
    {
        private SteamTransport _transport;

        public SteamworksSessionProvider(NetworkManager network, SteamTransport transport) : base(network, transport)
        {
            _transport = transport;
        }

        protected override void SetTransport(NetworkManager network)
        {
            network.transport = _transport;
        }
        
        protected override SessionDetails GetDetails()
        {
            return new SessionDetails
            {
                ServerAddress = SteamUser.GetSteamID().ToString()
            };
        }

        protected override void SetDetails(GenericTransport transport, SessionDetails details)
        {
            if (transport is not SteamTransport steam)
                throw new InvalidCastException("Could not cast transport to steam transport.");

            steam.peerToPeer = true;
            steam.address = details.ServerAddress;
        }
    }
}