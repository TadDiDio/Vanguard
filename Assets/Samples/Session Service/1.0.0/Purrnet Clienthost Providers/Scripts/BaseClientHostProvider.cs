using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PurrNet;
using PurrNet.Transports;
using UnityEngine;

namespace SessionService.Sample
{
    public abstract class BaseClientHostProvider : ISessionProvider
    {
        private NetworkManager _network;
        private GenericTransport _transport;

        protected BaseClientHostProvider(NetworkManager network, GenericTransport transport)
        {
            if (!network) throw new NullReferenceException(nameof(network));
            if (!transport) throw new NullReferenceException(nameof(transport));
            
            _network = network;
            _transport = transport;
        }

        public void Initialize()
        {
            if (!_network.isOffline)
                throw new InvalidOperationException("Cannot initialize a new provider while the network is online"); 

            SetTransport(_network);
        }

        /// <summary>
        /// Initializes the transport and network manager.
        /// </summary>
        protected abstract void SetTransport(NetworkManager network);
        
        /// <summary>
        /// Gets the details required to connect to the local client's session.
        /// </summary>
        /// <returns></returns>
        protected abstract SessionDetails GetDetails();

        /// <summary>
        /// Sets details on the transport to prepare for connection.
        /// </summary>
        protected abstract void SetDetails(GenericTransport transport, SessionDetails details);
        
        public async Task<SessionCreateResult> CreateSessionAsync(SessionCreateRequest request, CancellationToken token)
        {
            var timeout = request.TimeoutSeconds;

            var serverTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            void ServerCallback(ConnectionState state) => ListenForConnection(state, serverTcs);
            if (!await TryConnectAsync(true, timeout, ServerCallback, serverTcs, token))
            {
                _network.StopServer(); // Clean up a lingering attempt
                return SessionCreateResult.Fail();
            }

            var clientTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            void ClientCallback(ConnectionState state) => ListenForConnection(state, clientTcs);
            if (!await TryConnectAsync(false, timeout, ClientCallback, clientTcs, token))
            {
                _network.StopClient(); // Clean up a lingering attempt
                return SessionCreateResult.Fail();
            }

            var details = GetDetails();
            SetDetails(_transport, details);
            
            return SessionCreateResult.Success(details);
        }
        
        public async Task<SessionJoinResult> JoinSessionAsync(SessionJoinRequest request, CancellationToken token)
        {
            var timeout = request.TimeoutSeconds;

            SetDetails(_transport, request.SessionDetails);

            var clientTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            void ClientCallback(ConnectionState state) => ListenForConnection(state, clientTcs);
            if (!await TryConnectAsync(false, timeout, ClientCallback, clientTcs, token))
            {
                _network.StopClient(); // Clean up a lingering attempt
                return SessionJoinResult.Fail();
            }

            return SessionJoinResult.Success();
        }
        
        public async Task DisconnectAsync(CancellationToken token)
        {
            if (!_network.isClient && !_network.isServer) return;

            if (_network.isClient)
            {
                var clientTcs = new TaskCompletionSource<bool>();
                Action<ConnectionState> clientCallback = state => ListenForDisconnect(state, clientTcs);
                try
                {
                    token.Register(() => clientTcs.TrySetCanceled(token));
                    _network.onClientConnectionState += clientCallback;
                    _network.StopClient();

                    await clientTcs.Task;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return;
                }
                finally
                {
                    _network.onClientConnectionState -= clientCallback;
                }
            }

            if (_network.isServer)
            {
                var serverTcs = new TaskCompletionSource<bool>();
                Action<ConnectionState> serverCallback = state => ListenForDisconnect(state, serverTcs);
                try
                {
                    token.Register(() => serverTcs.TrySetCanceled(token));
                    _network.onServerConnectionState += serverCallback;
                    _network.StopServer();

                    await serverTcs.Task;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    _network.onServerConnectionState -= serverCallback;
                }
            }

            return;

            void ListenForDisconnect(ConnectionState state, TaskCompletionSource<bool> tcs)
            {
                if (state is ConnectionState.Disconnected)
                {
                    tcs.TrySetResult(true);
                }
            }
        }
        private void ListenForConnection(ConnectionState state, TaskCompletionSource<bool> tcs)
        {
            switch (state)
            {
                case ConnectionState.Connected:
                    tcs.TrySetResult(true);
                    break;
                case ConnectionState.Disconnected:
                    tcs.TrySetResult(false);
                    break;
            }
        }
        private async Task<bool> TryConnectAsync(bool server,
            float timeout,
            Action<ConnectionState> callback,
            TaskCompletionSource<bool> tcs,
            CancellationToken token)
        {
            try
            {
                if (server)
                {
                    _network.onServerConnectionState += callback;
                    _network.StartServer();
                }
                else
                {
                    _network.onClientConnectionState += callback;
                    _network.StartClient();
                }

                var delayTask = Task.Delay(TimeSpan.FromSeconds(timeout), token);
                var completedTask = await Task.WhenAny(delayTask, tcs.Task);

                return completedTask == tcs.Task && await tcs.Task;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            finally
            {
                if (server) _network.onServerConnectionState -= callback;
                else _network.onClientConnectionState -= callback;
            }
        }
        public void Dispose() { }
    }
}