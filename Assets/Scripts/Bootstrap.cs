using System;
using System.Threading.Tasks;
using LobbyService;
using LobbyService.Example.Steam;
using LobbyService.LocalServer;
using PurrNet;
using PurrNet.Steam;
using PurrNet.Transports;
using SceneService;
using SessionService;
using SessionService.Sample;
using UnityEngine;

namespace Vanguard
{
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private bool useSteam;
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UDPTransport udpTransport;
        [SerializeField] private SteamTransport steamTransport;
        [SerializeField] private LobbyRules rules;
        [SerializeField] private GameObject SteamworksManager;
        
        private void Awake()
        {
            if (useSteam)
            {
                Instantiate(SteamworksManager).name = "Steam Manager";
            }

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                Lobby.SetRules(rules);
                Lobby.SetPreInitStrategy(new DropPreInitStrategy());
                SceneController.SetController(Scenes.BuildSceneController());

                if (useSteam)
                {
                    var provider = new SteamProvider(ApplicationController.LobbyKeys, ApplicationController.MemberKeys);
                    Lobby.SetProvider(provider);
                    Session.SetProvider(new SteamworksSessionProvider(networkManager, steamTransport));
                }
                else
                {
                    if (!await LocalLobby.WaitForInitializationAsync(destroyCancellationToken)) return;
                
                    var provider = new LocalProvider();
                    Lobby.SetProvider(provider);
                    Session.SetProvider(new LocalTestProvider(networkManager, udpTransport));
                }

                await SceneController.Instance.LoadGroupAsync("Title");
            }
            catch (OperationCanceledException)
            {
                // Expected if the operation is canceled
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}