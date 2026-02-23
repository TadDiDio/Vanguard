using System;
using System.Threading.Tasks;
using LobbyService;
using LobbyService.LocalServer;
using SceneService;
using UnityEngine;

namespace Vanguard
{
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private LobbyRules rules;
        
        private void Start()
        {
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                if (!await LocalLobby.WaitForInitializationAsync(destroyCancellationToken)) return;
                
                // The provider depends on the API, so create this afterwards
                var provider = new LocalProvider();

                Lobby.SetRules(rules);
                Lobby.SetPreInitStrategy(new DropPreInitStrategy());
            
                // REQUIRED: This is the only call needed to start the lobby system. 
                // It must know which backend to use. This can be safely called again whenever you wish to hotswap backends.
                Lobby.SetProvider(provider);
            
                ISceneController sceneController = Scenes.BuildSceneController();

                await sceneController.LoadGroupAsync("Title");
            }
            catch (OperationCanceledException)
            {
                // Expected if the operation is cancelled
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}