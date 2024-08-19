/*
Copyright (C) 2024 Deana Brcka

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/


using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace BumpNade
{
    [MinimumApiVersion(247)]
    public partial class BumpNade : BasePlugin
    {
        public override string ModuleName => "BumpNade";
        public override string ModuleVersion => "1.0";
        public override string ModuleAuthor => "Deana https://x.com/dea_bb/";
        public override string ModuleDescription => "A Plugin that turns HEs into Bump Nades.";

        const string bumpmine = "models/weapons/v_bumpmine.vmdl";
        const string parachute = "models/props_survival/parachute/chute.vmdl";

        public override void Load(bool hotReload)
        {
            RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
            {
                if (@event.Userid!.IsValid)
                {
                    var player = @event.Userid;

                    if (player.IsValid)
                    {
                        OnPlayerConnect(player);
                    }
                }
                return HookResult.Continue;
            });

            RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
            {
                if (@event.Userid!.IsValid)
                {
                    var player = @event.Userid;

                    if (player.IsValid)
                    {
                        OnPlayerDisconnect(player);
                    }
                }
                return HookResult.Continue;
            });

            RegisterListener<OnServerPrecacheResources>((manifest) =>
            {
                manifest.AddResource(parachute);
            });

            RegisterListener<OnTick>(OnTick);
            RegisterListener<OnEntityCreated>(OnEntityCreate);
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(this.OnTakeDamage, HookMode.Pre);
        }

        public override void Unload(bool hotReload)
        {
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        }

        private void OnEntityCreate(CEntityInstance entity)
        {
            if (!entity.IsValid) return;

            CHEGrenadeProjectile? heNade = entity as CHEGrenadeProjectile;
            if (heNade == null) return;

            if (heNade.DesignerName == "hegrenade_projectile")
            {
                var nadeSceneNode = heNade.CBodyComponent!.SceneNode!;

                heNade.GravityScale = 2.0f;

                if (nadeSceneNode.GetSkeletonInstance().ModelState.ModelName != bumpmine)
                    heNade.SetModel(bumpmine);
            }
        }

        HookResult OnTakeDamage(DynamicHook h)
        {
            try
            {
                var playerPawn = h.GetParam<CEntityInstance>(0);
                var info = h.GetParam<CTakeDamageInfo>(1);

                if (!playerPawn.IsValid || !info.Attacker.IsValid)
                    return HookResult.Continue;

                CCSPlayerController? player = null;

                var pawnController = new CCSPlayerPawn(pointer: playerPawn.Handle)?.Controller?.Value;
                if (pawnController != null)
                {
                    player = new CCSPlayerController(pointer: pawnController.Handle);
                }
                else
                {
                    return HookResult.Continue;
                }


                if (!playerInfos.TryGetValue(player!.Slot, out var playerInfo))
                {
                    playerInfos.Add(player.Slot, new PlayerInfo());
                }

                if (playerPawn.DesignerName == "player" && info.Inflictor.Value!.DesignerName == "hegrenade_projectile" && info.BitsDamageType == 64)
                {
                    info.Damage = 0;

                    Vector nadePos = info.Inflictor.Value!.AbsOrigin!;
                    Vector playerPos = player.PlayerPawn.Value!.CBodyComponent!.SceneNode!.AbsOrigin;
                    double distanceToPlayer = Distance(playerPos, nadePos);

                    float verticalDistance = Math.Abs(playerPos.Z - nadePos.Z);
                    float verticalMargin = 25f;

                    if (distanceToPlayer < 384)
                    {
                        Vector direction = Normalize(playerPos - nadePos);

                        if (verticalDistance <= verticalMargin)
                        {
                            float upwardFactor = 1f * (1f - (float)distanceToPlayer / 384);
                            Vector upwardDirection = new(direction.X, direction.Y, direction.Z + upwardFactor);
                            direction = Normalize(upwardDirection);
                        }

                        float mapBasedKnockbackStrength = Server.MapName.Contains("dz_") ? 1200f : 600f;

                        float knockbackStrength = mapBasedKnockbackStrength * (1 - (float)distanceToPlayer / 384);
                        Vector knockbackVelocity = direction * knockbackStrength;

                        Vector newVelocity = player.AbsVelocity + knockbackVelocity;

                        SetPlayerVelocity(player, newVelocity);

                        playerInfo!.KnckedBack = true;
                        playerInfo.KnockedBackTickStamp = Server.TickCount;

                        PrintDebug($"Knocked back player {player.PlayerName}.");
                    }
                }

                if (info.BitsDamageType == 32 && playerInfo!.KnckedBack)
                {
                    float initialDamage = info.Damage;

                    info.Damage = initialDamage * 0.75f;
                }
            }
            catch (Exception e)
            {
                PrintDebug("Error: " + e.Message);
            }
            return HookResult.Continue;
        }

        private void OnTick()
        {
            try
            {
                foreach (CCSPlayerController player in Utilities.GetPlayers().Where(x => x.IsValid && x != null && x.PawnIsAlive && !x.IsBot))
                {
                    if (!playerInfos.TryGetValue(player.Slot, out var playerInfo))
                    {
                        playerInfos.Add(player.Slot, new PlayerInfo());
                    }

                    var ticksSinceKnockedBack = Server.TickCount - playerInfo!.KnockedBackTickStamp;

                    if (((PlayerFlags)player.Pawn.Value!.Flags & PlayerFlags.FL_ONGROUND) == PlayerFlags.FL_ONGROUND &&
                        (ticksSinceKnockedBack > 32))
                    {
                        Server.NextFrame(() => playerInfo.KnckedBack = false);
                    }

                    if (playerInfo!.KnckedBack)
                    {
                        PlayerButtons? playerButtons = player.Buttons;

                        playerInfo.ParachuteActive = (playerButtons & PlayerButtons.Jump) != 0;
                        var parachuteEnoughTicks = ticksSinceKnockedBack > 16;
                        var parachuteActivated = !playerInfo.ParachuteActiveLastTick && playerInfo.ParachuteActive && parachuteEnoughTicks;
                        var parachuteDeactivated = playerInfo.ParachuteActiveLastTick && !playerInfo.ParachuteActive && parachuteEnoughTicks;

                        if (parachuteActivated)
                        {
                            PlaySound(player, "sounds/ui/panorama/mainmenu_press_loadout_01.vsnd");

                            var model = parachute;
                            var position = player.PlayerPawn?.Value!.AbsOrigin;
                            playerInfo.ParachuteEntity = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");

                            if (playerInfo.ParachuteEntity != null)
                            {
                                playerInfo.ParachuteEntity.DispatchSpawn();
                                playerInfo.ParachuteEntity.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                                playerInfo.ParachuteEntity.NoGhostCollision = true;

                                playerInfo.ParachuteEntity.Collision.CollisionGroup = 0;

                                playerInfo.ParachuteEntity.SetModel(model);

                                playerInfo.ParachuteEntity.AcceptInput("FollowEntity", player.PlayerPawn?.Value!, player.PlayerPawn?.Value!, "!activator");
                            }
                        }

                        if (playerInfo.ParachuteActive && parachuteEnoughTicks)
                        {
                            if (player?.PlayerPawn?.Value!.AbsVelocity.Length() > 400)
                            {
                                AdjustPlayerVelocity(player, 400);
                            }
                        }

                        if (parachuteDeactivated && playerInfo.ParachuteEntity!.IsValid)
                        {
                            playerInfo.ParachuteEntity?.Remove();
                        }

                        playerInfo.ParachuteActiveLastTick = playerInfo.ParachuteActive && parachuteEnoughTicks;
                    }
                    else
                    {
                        playerInfo.ParachuteActive = false;
                        playerInfo.ParachuteActiveLastTick = false;
                        if (playerInfo.ParachuteEntity != null && playerInfo.ParachuteEntity!.IsValid) playerInfo.ParachuteEntity?.Remove();
                    }
                }

                foreach (var entity in Utilities.FindAllEntitiesByDesignerName<CHEGrenadeProjectile>("hegrenade_projectile"))
                {
                    if (!entity.IsValid)
                    {
                        continue;
                    }

                    if (entity.CBodyComponent?.SceneNode?.AbsRotation != new QAngle(0, 0, 0)) entity.Teleport(null, new QAngle(0, 0, 0), entity.Bounces == 1 ? new Vector(0, 0, 0) : null);
                }
            }
            catch (Exception e)
            {
                PrintDebug("Error: " + e.Message);
            }
        }
        //thx for watching make sure to like and subscribe!
    }
}