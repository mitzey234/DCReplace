﻿using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Loader;
using MEC;
using SharedLogicOrchestrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

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

		enum replacementType : ushort
		{
			Unknown = 0,
			NonScp = 1,
			Scp035 = 2,
			Serpents = 3,
			Spies = 4,
			Scp966 = 5
		}

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
				Log.Debug("Failed getting 035s: " + e);
			}

			return;
		}

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
		private void TrySpawn035(Player player)
		{

			Loader.Plugins.FirstOrDefault(pl => pl.Name == "scp035")?.Assembly?.GetType("scp035.API.Scp035Data")?.GetMethod("Spawn035", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { player });

		}


		private void ReplacePlayer(Player player)
		{
			bool is035 = false;
			bool isSH = false;
			bool is966 = false;
			if (isContain106 && player.Role == RoleType.Scp106) return;
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
				Log.Debug($"Who was player {player.Nickname} and were they 035: {is035} and if they weren't what was result: {data}", DCReplace.instance.Config.debugEnabled);

				if (is035)
				{
					currentReplaceType = replacementType.Scp035;
				}
			}
			catch (Exception x)
			{
				Log.Error(x);
				Log.Debug("SCP-035 is not installed, skipping method call...");
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
				Log.Debug("Serpents Hand is not installed, skipping method call...");
			}

			try
			{
				spies = TryGetSpies();

				if (spies != null)
				{

					Player replacement = Player.List.FirstOrDefault(x => x.Role == RoleType.Spectator && x.Id != cloneablePlayerInformation.Id);

					ReplacePlayerNowAvailable(replacement, replacementType.Spies, spies, cloneablePlayerInformation, player);
					return;
				}

			}
			catch (Exception x)
			{
				Log.Error(x);
				Log.Debug("CISpy is not installed, skipping method call...");
			}

			try
			{
				TryGet966();
				is966 = scp966PlayerReference != null && scp966PlayerReference == player;

				if (is966)
				{
					currentReplaceType = replacementType.Scp966;
				}
			}
			catch (Exception x)
			{
				Log.Error(x);
				Log.Debug("CISpy is not installed, skipping method call...");
			}

			if (currentReplaceType == replacementType.Unknown)
			{
				currentReplaceType = replacementType.NonScp;
			}

			currentReplacementCoroutines.Add(Timing.RunCoroutine(ReplacePlayerWhenAvailable(currentReplaceType, spies, cloneablePlayerInformation)));


		}

		private IEnumerator<float> ReplacePlayerWhenAvailable(replacementType currentReplaceType,
			Dictionary<Player, bool> spies, CloneablePlayerInformation cloneablePlayerInformation)
		{
			Player replacement = Player.List.FirstOrDefault(x => x.Role == RoleType.Spectator && x.Id != cloneablePlayerInformation.Id
			&& !(x.Nickname.Equals(cloneablePlayerInformation.Nickname)) && !x.IsOverwatchEnabled);
			//Prevents early leave issue
			while (replacement == null)
			{
				yield return Timing.WaitForSeconds(5);
				replacement = Player.List.FirstOrDefault(x => x.Role == RoleType.Spectator && x.Id != cloneablePlayerInformation.Id
			&& !(x.Nickname.Equals(cloneablePlayerInformation.Nickname)) && !x.IsOverwatchEnabled);
			}

			ReplacePlayerNowAvailable(replacement, currentReplaceType, spies, cloneablePlayerInformation);
		}

		private void ReplacePlayerNowAvailable(Player replacement, replacementType currentReplaceType,
			Dictionary<Player, bool> spies, CloneablePlayerInformation cloneablePlayerInformation, Player originalPlayer = null)
		{

			if (replacement != null)
			{

				PositionsToSpawn.Add(replacement, cloneablePlayerInformation.Position);
				replacement.SetRole(cloneablePlayerInformation.Role);
				if (currentReplaceType is replacementType.Serpents)
				{
					try
					{
						TrySpawnSH(replacement);
					}
					catch (Exception x)
					{
						Log.Debug("Serpents Hand is not installed, skipping method call...");
					}
				}
				else if (spies != null && spies.ContainsKey(originalPlayer))
				{
					try
					{
						TrySpawnSpy(replacement, originalPlayer, spies);
						loadPlayerWithReplacement(originalPlayer, replacement, cloneablePlayerInformation.Items.Select(x => x.Type).ToList());
					}
					catch (Exception x)
					{
						Log.Debug("CISpy is not installed, skipping method call...");
					}
					return;
				}
				else if (currentReplaceType is replacementType.Scp035)
				{
					try
					{
						TrySpawn035(replacement);
					}
					catch (Exception x)
					{
						Log.Debug($"SCP-035 is not installed, skipping method call... {x}");
					}
				}
				loadPlayerWithReplacement(replacement, cloneablePlayerInformation);
			}
		}

		private void loadPlayerWithReplacement(Player replacement, CloneablePlayerInformation prevInventory)
		{
			Log.Debug($"What was previous inventory {prevInventory}", DCReplace.instance.Config.debugEnabled);


			byte scp079lvl = 1;
			float scp079exp = 0f;
			if (prevInventory.Role == RoleType.Scp079)
			{
				scp079lvl = prevInventory.scp079lvl;
				scp079exp = prevInventory.scp079exp;
			}

			Timing.CallDelayed(0.5f, () =>
			{
				replacement.ResetInventory(prevInventory.Items.Select(x => x.Type).ToList());

				replacement.Health = prevInventory.Health;

				foreach (ItemType ammoType in prevInventory.Ammo.Keys)
				{
					replacement.Inventory.UserInventory.ReserveAmmo[ammoType] = prevInventory.Ammo[ammoType];
					replacement.Inventory.SendAmmoNextFrame = true;
				}
				replacement.Broadcast(5, "<i>You have replaced a player who has disconnected.</i>");
			});
		}


		private void loadPlayerWithReplacement(Player player, Player replacement, List<ItemType> inventory)
		{
			float health = player.Health;
			byte scp079lvl = 1;
			float scp079exp = 0f;
			if (player.Role == RoleType.Scp079)
			{
				scp079lvl = player.ReferenceHub.scp079PlayerScript.Lvl;
				scp079exp = player.ReferenceHub.scp079PlayerScript.Exp;
			}
			Dictionary<ItemType, ushort> ammo = new Dictionary<ItemType, ushort>();
			foreach (ItemType ammoType in player.Ammo.Keys) ammo.Add(ammoType, player.Ammo[ammoType]);

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
				if (replacement.Role == RoleType.Scp079)
				{
					replacement.ReferenceHub.scp079PlayerScript.Lvl = scp079lvl;
					replacement.ReferenceHub.scp079PlayerScript.Exp = scp079exp;
				}
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
				scp966PlayerReference = ((Player)Loader.Plugins.First(pl => pl.Name == "scp966")?.Assembly?.GetType("scp966.API.Scp966API")?.GetMethod("GetLastScp966", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null));
				return;
			}

			try
			{
				if (scp966Plugin == null)
				{
					scp966Plugin = Loader.Plugins.FirstOrDefault(pl => pl.Name == "scp966");
				}
				scp966PlayerReference = (Player)(scp966Plugin?.Assembly?.GetType("scp966.API.Scp966API")?.GetMethod("GetLastScp966", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null));
			}
			catch (Exception e)
			{
				Log.Debug("Failed getting 966s: " + e);
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
			if (ev.Reason != (SpawnReason)120) return;
			if (!isRoundStarted || ev.Player.Role == RoleType.Spectator || ev.Player.Role == RoleType.None || ev.Player.Position.y < -1997 || (ev.Player.CurrentRoom.Zone == ZoneType.LightContainment && Map.IsLczDecontaminated)) return;

			ReplacePlayer(ev.Player);
		}

		public void OnPlayerLeave(LeftEventArgs ev)
		{
			if (!isRoundStarted || ev.Player.Role == RoleType.Spectator || ev.Player.Role == RoleType.None || ev.Player.Position.y < -1997 || (ev.Player.CurrentRoom.Zone == ZoneType.LightContainment && Map.IsLczDecontaminated)) return;

			ReplacePlayer(ev.Player);
		}
	}
}
