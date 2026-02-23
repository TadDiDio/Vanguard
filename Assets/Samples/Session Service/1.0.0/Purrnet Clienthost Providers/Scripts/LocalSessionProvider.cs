using PurrNet;
using PurrNet.Transports;

namespace SessionService.Sample
{
    public class LocalSessionProvider : BaseClientHostProvider
    {
        private LocalTransport _transport;

        public LocalSessionProvider(NetworkManager network, LocalTransport transport) : base(network, transport)
        {
            _transport = transport;    
        }
        
        protected override void SetTransport(NetworkManager network)
        {
            network.transport = _transport;
        }
        
        protected override SessionDetails GetDetails()
        {
            return new SessionDetails();
        }

        protected override void SetDetails(GenericTransport transport, SessionDetails details)
        {
            // No-op
        }
    }
}