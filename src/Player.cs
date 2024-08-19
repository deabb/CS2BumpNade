using CounterStrikeSharp.API.Core;

namespace BumpNade
{
    public partial class BumpNade
    {
        public Dictionary<int, PlayerInfo> playerInfos = [];
        public class PlayerInfo
        {
            public bool KnckedBack { get; set; }
            public int KnockedBackTickStamp { get; set; }
            public bool ParachuteActive { get; set; }
            public bool ParachuteActiveLastTick { get; set; } = false;
            public CDynamicProp? ParachuteEntity { get; set; } = null;
        }

        private void OnPlayerConnect(CCSPlayerController? player)
        {
            try
            {
                if (player == null)
                {
                    PrintDebug("Player object is null.");
                    return;
                }

                if (player.PlayerPawn == null)
                {
                    PrintDebug("PlayerPawn is null.");
                    return;
                }

                int playerSlot = player.Slot;

                try
                {
                    playerInfos[playerSlot] = new PlayerInfo();
                }
                finally
                {
                    if (playerInfos[playerSlot] == null)
                    {
                        playerInfos.Remove(playerSlot);
                    }
                }
            }
            catch (Exception ex)
            {
                PrintDebug($"Error in OnPlayerConnect: {ex.Message}");
            }
        }

        private void OnPlayerDisconnect(CCSPlayerController? player)
        {
            if (player == null) return;

            try
            {
                if (playerInfos.TryGetValue(player.Slot, out var playerInfo))
                {
                    playerInfos.Remove(player.Slot);
                }
            }
            catch (Exception ex)
            {
                PrintDebug($"Error in OnPlayerDisconnect (probably replay bot related lolxd): {ex.Message}");
            }
        }
    }
}