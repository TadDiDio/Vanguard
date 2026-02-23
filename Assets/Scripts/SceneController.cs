using SceneService;

namespace Vanguard
{
    public class SceneController
    {
        public static ISceneController Instance { get; private set; }
        
        public static void SetController(ISceneController controller) => Instance = controller;
    }
}