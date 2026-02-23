using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace LobbyService.Example.Steam
{
    public class SteamFriendsProvider : IFriendProvider
    {
        public event Action<List<LobbyMember>> FriendsUpdated;
        public FriendCapabilities Capabilities { get; } = new FriendCapabilities
        {
            SupportsAvatars = true
        };
        
        private float _interval;
        private FriendDiscoveryFilter _filter;
        private CancellationTokenSource _tokenSource;
        
        public void StartFriendPolling(FriendDiscoveryFilter filter, float intervalSeconds, CancellationToken token = default)
        {
            if (!SteamProvider.EnsureInitialized()) return;

            _interval = intervalSeconds;
            _filter = filter;

            _tokenSource = new CancellationTokenSource();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(_tokenSource.Token, token);

            _ = DiscoverFriends(cts.Token);
        }

        public void SetFriendPollingInterval(float intervalSeconds)=> _interval = intervalSeconds;

        public void SetFriendPollingFilter(FriendDiscoveryFilter filter) => _filter = filter;

        public void StopFriendPolling()
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _tokenSource = null;
        }

        public async Task<Texture2D> GetFriendAvatar(LobbyMember member, CancellationToken token = default)
        {
            if (!SteamProvider.EnsureInitialized()) return Texture2D.blackTexture;
            if (!SteamProvider.ValidSteamId(member.Id, out var id)) return Texture2D.blackTexture;

            int handle = SteamFriends.GetMediumFriendAvatar(id);
            
            if (!SteamUtils.GetImageSize(handle, out uint width, out uint height))
            {
                Debug.LogError("Failed to get image size for avatar.");
                return Texture2D.blackTexture;
            }
            
            var imageBuffer = new byte[width * height * 4];
            if (!SteamUtils.GetImageRGBA(handle, imageBuffer, (int)(width * height * 4)))
            {
                Debug.LogError("Failed to get RGBA data for avatar.");
                return Texture2D.blackTexture;
            }

            var texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(imageBuffer);
            texture.Apply();

            await Task.CompletedTask;
            return texture;
        }

        private async Task DiscoverFriends(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    List<LobbyMember> members = new();
                    if (!SteamManager.Initialized)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_interval), token);
                        continue;
                    }

                    var flags = EFriendFlags.k_EFriendFlagImmediate;
                    var count = SteamFriends.GetFriendCount(flags);
                    var results = new CSteamID[count];

                    for (int i = 0; i < count; i++)
                    {
                        results[i] = SteamFriends.GetFriendByIndex(i, flags);
                    }

                    foreach (var id in results)
                    {
                        var state = SteamFriends.GetFriendPersonaState(id);

                        if (_filter is FriendDiscoveryFilter.All || state is EPersonaState.k_EPersonaStateOnline)
                        {
                            members.Add(new LobbyMember(new ProviderId(id.ToString()),
                                SteamFriends.GetFriendPersonaName(id)));
                        }
                    }

                    FriendsUpdated?.Invoke(members);
                    await Task.Delay(TimeSpan.FromSeconds(_interval), token);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignored
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void Dispose()
        {
            // Resources cleaned up by controlling module
        }
    }
}