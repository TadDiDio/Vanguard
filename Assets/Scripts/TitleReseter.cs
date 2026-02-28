using PurrNet;
using SessionService;
using Vanguard;

public class TitleReseter : NetworkBehaviour
{
    protected override void OnSpawned(bool asServer)
    {
        if (asServer)
        {
            if (networkManager.playerCount <= 1)
            {
                ApplicationController.Instance.AllowStartGame();
                _ = Session.DisconnectAsync();
                return;
            }
            networkManager.onPlayerLeft += OnLeave;
        }

        if (isServer) return;

        _ = Session.DisconnectAsync();
    }

    protected override void OnDespawned()
    {
        networkManager.onPlayerLeft -= OnLeave;
    }

    private void OnLeave(PlayerID id, bool asServer)
    {
        if (!asServer) return;
        
        if (networkManager.playerCount <= 1)
        {
            _ = Session.DisconnectAsync();
        }

        ApplicationController.Instance.AllowStartGame();
    }
}
