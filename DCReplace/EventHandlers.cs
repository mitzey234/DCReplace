using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Loader;
using MEC;
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

		/// <summary>
		/// Firstly creates a reference to the plugin we are trying to get. If it doesn't already exist.
		/// Then uses the plugin to call the required API's to get 035 information. Saves player
		/// reference as private variable. Non-static. Under assumption of only 1 035's allowed at one time. 
		/// </summary>
		private void TryGet035()
		{

			Log.Info("Getting035");

			if (scp035Plugin != null)
			{

				//Under assumption of only 1 035 allowed at one time. 
				scp035PlayerReference = (Player)scp035Plugin?.Assembly?.GetType("scp035.API.Scp035Data")?.GetMethod("GetScp035", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
				return;
			}



			try
			{
				//Under assumption of only 1 035 allowed at one time. 
				scp035Plugin = Loader.Plugins.FirstOrDefault(pl => pl.Name == "scp035");
				scp035PlayerReference = (Player)scp035Plugin?.Assembly?.GetType("scp035.API.Scp035Data")?.GetMethod("GetScp035", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
			}
			catch (Exception e)
			{
				Log.Debug("Failed getting 035s: " + e);
			}

			return;
		}


		/// <summary>
		/// Firstly creates a reference to the plugin we are trying to get. If it doesn't already exist.
		/// Then uses the plugin to call the required API's to get 966 information. Saves player
		/// reference as private variable. Non-static. Under assumption of only 1 966 allowed at one time. 
		/// Assumption that API will send back player. Original implementation was some sort of string. 
		/// </summary>
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
				scp966PlayerReference = ((Player)scp966Plugin?.Assembly?.GetType("scp966.API.Scp966API")?.GetMethod("GetLastScp966", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null));
				return;
			}

			try
			{

				scp966Plugin = Loader.Plugins.FirstOrDefault(pl => pl.Name == "scp966");

				scp966PlayerReference = (Player)(scp966Plugin?.Assembly?.GetType("scp966.API.Scp966API")?.GetMethod("GetLastScp966", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null));
			}
			catch (Exception e)
			{
				Log.Debug("Failed getting 966s: " + e);
			}
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

			try
			{

				TryGet035();

				//May want to consider by nickname or something.
				is035 = scp035PlayerReference != null && scp035PlayerReference == player;
				string data = scp035PlayerReference != null ? scp035PlayerReference.Nickname : "Data returned null";

			}
			catch (Exception x)
			{
				Log.Error(x);
				Log.Debug("SCP-035 is not installed, skipping method call...");
			}

			try
			{
				isSH = TryGetSH().Contains(player);
			}
			catch (Exception x)
			{
				Log.Error(x);
				Log.Debug("Serpents Hand is not installed, skipping method call...");
			}

			try
			{
				spies = TryGetSpies();
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
			}
			catch (Exception x)
			{
				Log.Error(x);
				Log.Debug("CISpy is not installed, skipping method call...");
			}


			Player replacement = Player.List.FirstOrDefault(x => x.Role == RoleType.Spectator && x.Id != player.Id && !x.IsOverwatchEnabled);
			if (replacement != null)
			{
				// Have to do this early
				var inventory = player.Items.Select(x => x.Type).ToList();

				player.ClearInventory();

				PositionsToSpawn.Add(replacement, player.Position);
				if (isSH)
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
				else replacement.SetRole(player.Role);
				if (spies != null && spies.ContainsKey(player))
				{
					try
					{
						TrySpawnSpy(replacement, player, spies);
					}
					catch (Exception x)
					{
						Log.Debug("CISpy is not installed, skipping method call...");
					}
				}
				if (is035)
				{
					try
					{
						TrySpawn035(replacement);

					}
					catch (Exception x)
					{
						Log.Debug("SCP-035 is not installed, skipping method call...");
					}
				}

				/*if ((string)Loader.Plugins.First(pl => pl.Name == "scp966")?.Assembly?.GetType("scp966.API.Scp966API")?.GetMethod("GetLastScp966", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null) == player.UserId)
				{
					Loader.Plugins.First(pl => pl.Name == "scp966").Assembly.GetType("scp966.API.Scp966API").GetMethod("Spawn966", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { replacement });
				}*/

				// save info
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
		}


		public void OnRoundStart()
		{
			isContain106 = false;
			isRoundStarted = true;
			PositionsToSpawn.Clear();
		}

		public void OnRoundEnd(RoundEndedEventArgs ev) => isRoundStarted = false;

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
