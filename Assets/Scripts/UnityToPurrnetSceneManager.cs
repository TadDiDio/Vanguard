using PurrNet;
using PurrNet.Modules;
using SceneService;
using SceneService.Unity;
using UnityEngine.SceneManagement;

namespace Vanguard
{
    public class UnityToPurrnetSceneManager : ISceneManager
    {
        private NetworkManager _network;
        public UnityToPurrnetSceneManager(NetworkManager manager)
        {
            _network = manager;
        }

        public ISceneLoadOperation LoadSceneAsync(string scenePath)
        {
            PurrSceneSettings settings = new()
            {
                isPublic = true,
                mode = LoadSceneMode.Additive,
            };

            var path = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            var operation = _network.sceneModule.LoadSceneAsync(path, settings);
            return new AsyncSceneLoadOperation(operation);
        }

        public ISceneLoadOperation UnloadSceneAsync(string scenePath)
        {
            var operation = SceneManager.UnloadSceneAsync(scenePath);
            return new AsyncSceneLoadOperation(operation);
        }

        public void SetActiveScene(string scenePath)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            SceneManager.SetActiveScene(scene);
        }
    }
}