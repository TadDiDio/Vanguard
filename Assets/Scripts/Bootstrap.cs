using System;
using System.Threading.Tasks;
using LobbyService;
using LobbyService.Example.Steam;
using LobbyService.LocalServer;
using SessionService;
using SessionService.Sample;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vanguard
{
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private bool useSteam;
        
        [SerializeField] private GLOBALS GLOBALS_PREFAB;
        [SerializeField] private LobbyRules rules;
        [SerializeField] private GameObject SteamworksManager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Startup()
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
        }

        private static bool spawned; 
        
        private void Start()
        {
            if (spawned)
            {
                SceneManager.LoadScene("Lobby");
                return;
            }
            
            var globals = Instantiate(GLOBALS_PREFAB);
            
            if (useSteam)
            {
                Instantiate(SteamworksManager, globals.transform).name = "Steam Manager";
            }

            _ = InitializeAsync(globals);
        }

        private async Task InitializeAsync(GLOBALS globals)
        {
            try
            {
                Lobby.SetRules(rules);
                Lobby.SetPreInitStrategy(new DropPreInitStrategy());

                if (useSteam)
                {
                    var provider = new SteamProvider(ApplicationController.LobbyKeys, ApplicationController.MemberKeys);
                    Lobby.SetProvider(provider);
                    Session.SetProvider(new SteamworksSessionProvider(globals.NetworkManager, globals.SteamTransport));
                }
                else
                {
                    if (!await LocalLobby.WaitForInitializationAsync(destroyCancellationToken)) return;
                
                    var provider = new LocalProvider();
                    Lobby.SetProvider(provider);
                    Session.SetProvider(new LocalTestProvider(globals.NetworkManager, globals.UDPTransport));
                }

                DontDestroyOnLoad(globals);
                spawned = true;
                SceneManager.LoadScene("Lobby");
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