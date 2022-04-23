using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs;
using Exiled.Loader;
using InventorySystem.Items;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using SharedLogicOrchestrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static DCReplace.UtilityInformation.UtilityInfo;
using static SharedLogicOrchestrator.DebugFilters;

namespace DCReplace
{
    class EventHandlers
    {
        private bool isContain106;
        private bool isRoundStarted = false;

        private Dictionary<Player, Vector3> PositionsToSpawn = new Dictionary<Player, Vector3>();

        private Player scp035PlayerReference = null;
        private Exiled.API.Interfaces.IPlugin<Exiled.API.Interfaces.IConfig> scp035Plugin;
        private Player scp966PlayerReference;
        private Exiled.API.Interfaces.IPlugin<Exiled.API.Interfaces.IConfig> scp966Plugin;

        private List<CoroutineHandle> currentReplacementCoroutines;

        /// <summary>
        /// Gets the last 035 player from Scp035 DLL logic, which is a hash of the player and their information.
        /// </summary>
        /// <param name="currPlayer"></param>
        private void TryGet035(Player currPlayer)
        {
            Log.Info("Getting035");

            if (scp035Plugin != null)
            {

                //Under assumption of only 1 035 allowed at one time. 
                scp035PlayerReference = (Player)scp035Plugin?.Assembly?.GetType("scp035.API.Scp035Data")?.GetMethod("GetScp035IfRecentlyExisted", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { currPlayer });
                return;
            }

            try
            {
                //Under assumption of only 1 035 allowed at one time. 

                scp035Plugin = Loader.Plugins.FirstOrDefault(pl => pl.Name == "scp035");

                scp035PlayerReference = (Player)scp035Plugin?.Assembly?.GetType("scp035.API.Scp035Data")?.GetMethod("GetScp035IfRecentlyExisted", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { currPlayer });
            }
            catch (Exception e)
            {
                Log.Debug("Failed getting 035s: " + e, DCReplace.instance.Config.DebugFilters[DebugFilter.Finer]);
            }

            return;
        }

        /// <summary>
        /// Used to get the previous 035 items, if needed.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private CloneablePlayerInformation TryGetLast035Items(Player player)
        {
            return (CloneablePlayerInformation)Loader.Plugins.FirstOrDefault(pl => pl.Name == "scp035")?.Assembly?.GetType("scp035.API.Scp035Data")?.GetMethod("GetScp035ItemsIfRecentlyExisted", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { player });
        }

        private List<Player> TryGetSH()
        {
            List<Player> Serpants;
            if (Loader.Plugins.Where(pl => pl.Name == "SerpentsHand").ToList().Count > 0)
            {
                try
                {
                    Serpants = (List<Player>)Loader.Plugins.First(pl => pl.Name == "SerpentsHand").Assembly.GetType("SerpentsHand.API.SerpentsHand").GetMethod("GetSHPlayers", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                }
                catch (System.Exception e)
                {
                    Serpants = new List<Player>();
                }
            }
            else
            {
                Serpants = new List<Player>();
            }
            return Serpants;
        }

        private Dictionary<Player, bool> TryGetSpies()
        {
            Dictionary<Player, bool> players = new Dictionary<Player, bool>();

            players = (Dictionary<Player, bool>)Loader.Plugins.FirstOrDefault(pl => pl.Name == "CiSpy")?.Assembly?.GetType("CISpy.API.SpyData")?.GetMethod("GetSpies", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
            return players;
        }

        private void TrySpawnSpy(Player player, Player dc, Dictionary<Player, bool> spies)
        {
            Loader.Plugins.FirstOrDefault(pl => pl.Name == "CiSpy")?.Assembly?.GetType("CISpy.API.SpyData")?.GetMethod("MakeSpy", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { player, spies[dc], false });
        }

        private void TrySpawnSH(Player player)
        {
            Loader.Plugins.FirstOrDefault(pl => pl.Name == "SerpentsHand")?.Assembly?.GetType("SerpentsHand.API.SerpentsHand")?.GetMethod("SpawnPlayer", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { player, false });
        }

        /// <summary>
        /// Gets the last 035 player from Scp035 DLL logic, which is a hash of the player and their information.
        /// </summary>
        /// <param name="player"></param>
        private void TrySpawn035(Player player)
        {
            Loader.Plugins.FirstOrDefault(pl => pl.Name == "scp035")?.Assembly?.GetType("scp035.API.Scp035Data")?.GetMethod("Spawn035", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { player });
        }


        /// <summary>
        /// Gets the last 966 player from Scp966 DLL logic, which is a hash of the player and their information.
        /// </summary>
        /// <param name="player"></param>
        private void TrySpawn966(Player player)
        {
            scp966Plugin?.Assembly?.GetType("scp966.API.Scp966API")?.GetMethod("Spawn966", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { player });

        }

        /// <summary>
        /// Replaces the player based on the type of player, uses the same items, ammo, etc if possible.
        /// </summary>
        /// <param name="player"></param>
        private void ReplacePlayer(Player player)
        {
            bool is035 = false;
            bool isSH = false;
            bool is966 = false;
            if (isContain106 && player.Role == RoleType.Scp106)
            {
                return;
            }

            Dictionary<Player, bool> spies = null;
            //We need this very early on
            replacementType currentReplaceType = replacementType.Unknown;
            CloneablePlayerInformation cloneablePlayerInformation = CloneablePlayerInformation.clonePlayer(player);
            player.ClearInventory();

            try
            {

                TryGet035(player);
                //May want to consider by nickname or something.
                is035 = scp035PlayerReference != null && scp035PlayerReference == player;
                string data = scp035PlayerReference != null ? scp035PlayerReference.Nickname : "Data returned null";
                Log.Debug($"Who was player {player.Nickname} and were they 035: {is035} and if they weren't what was result: {data}", DCReplace.instance.Config.DebugFilters[DebugFilter.Finer]);

                if (is035)
                {
                    currentReplaceType = replacementType.Scp035;
                }
            }
            catch (Exception x)
            {
                Log.Error(x);
                Log.Debug("SCP-035 is not installed, skipping method call...", DCReplace.instance.Config.DebugFilters[DebugFilter.Finer]);
            }

            try
            {
                isSH = TryGetSH().Contains(player);

                if (isSH)
                {
                    currentReplaceType = replacementType.Serpents;
                }
            }
            catch (Exception x)
            {
                Log.Error(x);
                Log.Debug("Serpents Hand is not installed, skipping method call...", DCReplace.instance.Config.DebugFilters[DebugFilter.Finer]);
            }

            try
            {
                spies = TryGetSpies();

                //Because I don't have access to spies, I have to make less optimal path if spies exists.
                if (spies != null)
                {

                    Player replacement = Player.List.FirstOrDefault(x => x.Role == RoleType.Spectator && x.Id != cloneablePlayerInformation.Id && !x.IsOverwatchEnabled);

                    ReplacePlayerNowAvailable(replacement, replacementType.Spies, spies, cloneablePlayerInformation, player);
                    return;
                }

            }
            catch (Exception x)
            {
                Log.Error(x);
                Log.Debug("CISpy is not installed, skipping method call...", DCReplace.instance.Config.DebugFilters[DebugFilter.Finer]);
            }

            try
            {
                TryGet966();
                is966 = scp966PlayerReference != null && scp966PlayerReference == player;

                if (is966)
                {
                    Log.Debug("This player was 966");
                    currentReplaceType = replacementType.Scp966;
                }
            }
            catch (Exception x)
            {
                Log.Error(x);
                Log.Debug("CISpy is not installed, skipping method call...", DCReplace.instance.Config.DebugFilters[DebugFilter.Finer]);
            }

            //If we were not uique SCP's, we assume we are either SCP or non-scp
            if (currentReplaceType == replacementType.Unknown)
            {
                currentReplaceType = replacementType.NonUniqueScp;
            }
            currentReplacementCoroutines.Add(Timing.RunCoroutine(ReplacePlayerWhenAvailable(currentReplaceType, spies, cloneablePlayerInformation)));
        }

        private IEnumerator<float> ReplacePlayerWhenAvailable(replacementType currentReplaceType,
            Dictionary<Player, bool> spies, CloneablePlayerInformation cloneablePlayerInformation)
        {
            Log.Debug($"What was the previous CloneablePlayerInformation health {cloneablePlayerInformation.Health}", DCReplace.instance.Config.DebugFilters[DebugFilter.Finer]);
            Player replacement = Player.List.FirstOrDefault(x => x.Role == RoleType.Spectator && x.Id != cloneablePlayerInformation.Id && !(x.Nickname.Equals(cloneablePlayerInformation.Nickname)) && !x.IsOverwatchEnabled);
            //Prevents early leave issue
            while (replacement == null)
            {
                yield return Timing.WaitForSeconds(5);
                replacement = Player.List.FirstOrDefault(x => x.Role == RoleType.Spectator && x.Id != cloneablePlayerInformation.Id && !(x.Nickname.Equals(cloneablePlayerInformation.Nickname)) && !x.IsOverwatchEnabled);
            }

            ReplacePlayerNowAvailable(replacement, currentReplaceType, spies, cloneablePlayerInformation);
        }

        private void ReplacePlayerNowAvailable(Player replacement, replacementType currentReplaceType,
            Dictionary<Player, bool> spies, CloneablePlayerInformation cloneablePlayerInformation, Player originalPlayer = null)
        {
            if (replacement != null)
            {
                bool is079 = cloneablePlayerInformation.Role == RoleType.Scp079;

                if (!is079)
                {
                    PositionsToSpawn.Add(replacement, cloneablePlayerInformation.Position);
                }
                replacement.SetRole(cloneablePlayerInformation.Role);
                if (currentReplaceType is replacementType.Serpents)
                {
                    try
                    {
                        TrySpawnSH(replacement);
                    }
                    catch (Exception serpantHandFailedToLoad)
                    {
                        Log.Debug($"Serpents Hand is not installed, skipping method call... {serpantHandFailedToLoad}", DCReplace.instance.Config.DebugFilters[DebugFilter.Finer]);
                    }
                }
                else if (spies != null && spies.ContainsKey(originalPlayer))
                {
                    try
                    {
                        TrySpawnSpy(replacement, originalPlayer, spies);
                        loadPlayerWithReplacement(originalPlayer, replacement, cloneablePlayerInformation.Items.Select(x => x.Type).ToList());
                    }
                    catch (Exception spiesFailedToLoad)
                    {
                        Log.Debug($"CISpy is not installed, skipping method call... {spiesFailedToLoad}", DCReplace.instance.Config.DebugFilters[DebugFilter.Finer]);
                    }
                    return;
                }
                else if (currentReplaceType is replacementType.Scp035)
                {
                    try
                    {
                        TrySpawn035(replacement);
                    }
                    catch (Exception scp035FailedToLoad)
                    {
                        Log.Debug($"SCP-035 is not installed, skipping method call... {scp035FailedToLoad}", DCReplace.instance.Config.DebugFilters[DebugFilter.Finer]);
                    }
                }

                else if (currentReplaceType is replacementType.Scp966)
                {
                    try
                    {
                        TrySpawn966(replacement);
                    }
                    catch (Exception scp966FailedToLoad)
                    {
                        Log.Debug($"SCP-966 is not installed, skipping method call... {scp966FailedToLoad}", DCReplace.instance.Config.DebugFilters[DebugFilter.Finer]);
                    }
                }

                loadPlayerWithReplacement(replacement, cloneablePlayerInformation, is079);
            }
        }

        private void loadPlayerWithReplacement(Player replacement, CloneablePlayerInformation prevInventory, bool is079)
        {
            Timing.CallDelayed(0.5f, () =>
            {
                if (!is079)
                {
                    replacement.ResetInventory(prevInventory.Items.Select(x => x.Type).ToList());
                    Log.Debug($"What was the previous health {prevInventory.Health}", DCReplace.instance.Config.DebugFilters[DebugFilter.Finer]);
                    //TODO fix AHP
                    replacement.Health = prevInventory.Health;

                    foreach (ItemType ammoType in prevInventory.Ammo.Keys)
                    {
                        replacement.Inventory.UserInventory.ReserveAmmo[ammoType] = prevInventory.Ammo[ammoType];
                        replacement.Inventory.SendAmmoNextFrame = true;
                    }

                    Timing.CallDelayed(0.5f, () =>
                    {
                        foreach (KeyValuePair<ushort, ItemBase> item in replacement.Inventory.UserInventory.Items)
                        {
                            Scp330Bag scp330Bag;
                            if ((object)(scp330Bag = (item.Value as Scp330Bag)) != null)
                            {
                                scp330Bag.Candies.Clear();
                                scp330Bag.Candies.AddRange(prevInventory.currentCandies);
                                scp330Bag.ServerConfirmAcqusition();
                                break;
                            }
                        }
                    });
                }
                else
                {
                    Scp079Role scp079 = replacement.Role.As<Scp079Role>();
                    scp079.Level = prevInventory.scp079lvl;
                    scp079.Experience = prevInventory.scp079exp;
                }
                replacement.Broadcast(5, "<i>You have replaced a player who has disconnected.</i>");
            });
        }

        private void loadPlayerWithReplacement(Player player, Player replacement, List<ItemType> inventory)
        {
            float health = player.Health;
            Dictionary<ItemType, ushort> ammo = new Dictionary<ItemType, ushort>();
            foreach (ItemType ammoType in player.Ammo.Keys)
            {
                ammo.Add(ammoType, player.Ammo[ammoType]);
            }

            Timing.CallDelayed(0.5f, () =>
            {
                replacement.ResetInventory(inventory);
                replacement.Health = health;
                foreach (ItemType ammoType in ammo.Keys)
                {
                    replacement.Inventory.UserInventory.ReserveAmmo[ammoType] = ammo[ammoType];
                    replacement.Inventory.SendAmmoNextFrame = true;
                }
                replacement.Broadcast(5, "<i>You have replaced a player who has disconnected.</i>");
            });
        }

        private void TryGet966()
        {
            /*
			 * 	
			 * 	if ((string)Loader.Plugins.First(pl => pl.Name == "scp966")?.Assembly?.GetType("scp966.API.Scp966API")?.GetMethod("GetLastScp966", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null) == player.UserId)
				{
					Loader.Plugins.First(pl => pl.Name == "scp966").Assembly.GetType("scp966.API.Scp966API").GetMethod("Spawn966", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { replacement });
				}

			*/
            if (scp966Plugin != null)
            {
                scp966PlayerReference = ((Player)Loader.Plugins.First(pl => pl.Name == "scp966")?.Assembly?.GetType("scp966.API.Scp966API")?.GetMethod("GetLastScp966Player", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null));
                return;
            }

            try
            {
                if (scp966Plugin == null)
                {
                    scp966Plugin = Loader.Plugins.FirstOrDefault(pl => pl.Name == "scp966");
                }
                scp966PlayerReference = (Player)(scp966Plugin?.Assembly?.GetType("scp966.API.Scp966API")?.GetMethod("GetLastScp966Player", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null));
            }
            catch (Exception e)
            {
                Log.Debug("Failed getting 966s: " + e, DCReplace.instance.Config.DebugFilters[DebugFilter.Finer]);
            }
        }

        public void OnRoundStart()
        {
            isContain106 = false;
            isRoundStarted = true;
            PositionsToSpawn.Clear();
            currentReplacementCoroutines = new List<CoroutineHandle>();
        }

        public void OnRoundEnd(RoundEndedEventArgs ev)
        {
            isRoundStarted = false;
            Timing.KillCoroutines(currentReplacementCoroutines.ToArray());
            currentReplacementCoroutines.Clear();
        }

        public void OnContain106(ContainingEventArgs ev) => isContain106 = true;

        public void OnSpawning(SpawningEventArgs ev)
        {
            if (PositionsToSpawn.ContainsKey(ev.Player))
            {
                ev.Position = PositionsToSpawn[ev.Player];
                PositionsToSpawn.Remove(ev.Player);
            }
        }

        public void OnChangeRole(ChangingRoleEventArgs ev)
        {
            if (ev.Reason != (SpawnReason)120)
            {
                return;
            }

            if (!isRoundStarted || ev.Player.Role == RoleType.Spectator || ev.Player.Role == RoleType.None || ev.Player.Position.y < -1997 || (ev.Player.CurrentRoom.Zone == ZoneType.LightContainment && Map.IsLczDecontaminated))
            {
                return;
            }

            ReplacePlayer(ev.Player);
        }

        public void OnPlayerLeave(LeftEventArgs ev)
        {
            if (!isRoundStarted || ev.Player.Role == RoleType.Spectator || ev.Player.Role == RoleType.None || ev.Player.Position.y < -1997 || (ev.Player.CurrentRoom.Zone == ZoneType.LightContainment && Map.IsLczDecontaminated))
            {
                return;
            }

            ReplacePlayer(ev.Player);
        }
    }
}
