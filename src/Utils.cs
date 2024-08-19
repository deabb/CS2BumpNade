using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace BumpNade
{
    public partial class BumpNade
    {
        private static void PlaySound(CCSPlayerController player, string v)
        {
            player.ExecuteClientCommand($"play {v}");
        }

        private void SetPlayerVelocity(CCSPlayerController? player, Vector newVelocity)
        {
            try
            {
                var currentVelocity = player!.PlayerPawn.Value!.AbsVelocity;
                var currentSpeed = currentVelocity.Length();

                if (currentSpeed == 0)
                {
                    player.PlayerPawn.Value!.AbsVelocity.X = (float)newVelocity.X;
                    player.PlayerPawn.Value!.AbsVelocity.Y = (float)newVelocity.Y;
                    player.PlayerPawn.Value!.AbsVelocity.Z = (float)newVelocity.Z;
                }
                else
                {
                    player.PlayerPawn.Value!.AbsVelocity.X += (float)newVelocity.X;
                    player.PlayerPawn.Value!.AbsVelocity.Y += (float)newVelocity.Y;
                    player.PlayerPawn.Value!.AbsVelocity.Z += (float)newVelocity.Z;
                }
            }
            catch (Exception ex)
            {
                PrintDebug($"Error in SetPlayerVelocity: {ex.Message}");
            }
        }

        private void AdjustPlayerVelocity(CCSPlayerController? player, float velocity)
        {
            try
            {
                var currentX = player!.PlayerPawn.Value!.AbsVelocity.X;
                var currentY = player!.PlayerPawn.Value!.AbsVelocity.Y;
                var currentZ = player!.PlayerPawn.Value!.AbsVelocity.Z;

                var currentSpeedSquared = currentX * currentX + currentY * currentY + currentZ * currentZ;

                // Check if current speed is not zero to avoid division by zero
                if (currentSpeedSquared > 0)
                {
                    var currentSpeed = Math.Sqrt(currentSpeedSquared);

                    var normalizedX = currentX / currentSpeed;
                    var normalizedY = currentY / currentSpeed;
                    var normalizedZ = currentZ / currentSpeed;

                    var adjustedX = normalizedX * velocity; // Adjusted speed limit
                    var adjustedY = normalizedY * velocity; // Adjusted speed limit
                    var adjustedZ = normalizedZ * velocity; // Adjusted speed limit

                    player!.PlayerPawn.Value!.AbsVelocity.X = (float)adjustedX;
                    player!.PlayerPawn.Value!.AbsVelocity.Y = (float)adjustedY;
                    player!.PlayerPawn.Value!.AbsVelocity.Z = (float)adjustedZ;
                }
            }
            catch (Exception ex)
            {
                PrintDebug($"Error in AdjustPlayerVelocity: {ex.Message}");
            }
        }

        public static double Distance(Vector vector1, Vector vector2)
        {
            if (vector1 == null || vector2 == null)
            {
                return 0;
            }

            double deltaX = vector1.X - vector2.X;
            double deltaY = vector1.Y - vector2.Y;
            double deltaZ = vector1.Z - vector2.Z;

            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }

        public static Vector Normalize(Vector v)
        {
            double length = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            if (length == 0) return new Vector(0, 0, 0);
            return new Vector((float?)(v.X / length), (float?)(v.Y / length), (float?)(v.Z / length));
        }

        public void PrintDebug(string msg)
        {
            Logger.LogInformation($"\u001b[33m[BumpNade] \u001b[37m{msg}");
        }
    }
}