using PurrNet;
using PurrNet.Transports;

namespace Vanguard
{
    public class LocalTestProvider : SessionProvider
    {
        private UDPTransport _transport;

        public LocalTestProvider(NetworkManager network, UDPTransport transport) : base(network)
        {
            _transport = transport;
        }

        protected override void SetTransport(NetworkManager network)
        {
            network.transport = _transport;
        }

        public override string GetAddress()
        {
            return "127.0.0.1";
        }

        protected override void SetAddress(string serverAddress)
        {
            _transport.address = serverAddress;
        }
    }
}