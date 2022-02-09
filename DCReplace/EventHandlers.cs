using MEC;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using Exiled.Events.EventArgs;
using Exiled.API.Features;
using Exiled.API.Enums;
using Exiled.Loader;
using System.Reflection;
using CustomPlayerEffects;

namespace DCReplace
{
	class EventHandlers
	{
		private bool isContain106;
		private bool isRoundStarted = false;

		private Dictionary<Player, Vector3> PositionsToSpawn = new Dictionary<Player, Vector3>();

		private List<Player> TryGet035()
		{
			List<Player> scp035 = null;

			foreach (var plugin in Loader.Plugins)
			{
				if (plugin.Name == "scp035")
				{
					try
					{
						scp035 = (List<Player>)Loader.Plugins.First(pl => pl.Name == "scp035").Assembly.GetType("scp035.API.Scp035Data").GetMethod("GetScp035s", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
					}
					catch (Exception e)
					{
						Log.Debug("Failed getting 035s: " + e);
						scp035 = new List<Player>();
					}
				}
			}
			if (scp035 == null) scp035 = new List<Player>();
			return scp035;
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
			if (Loader.Plugins.FirstOrDefault(pl => pl.Name == "CiSpy") != null)
				players = (Dictionary<Player, bool>)Loader.Plugins.First(pl => pl.Name == "CiSpy").Assembly.GetType("CISpy.API.SpyData").GetMethod("GetSpies", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
			return players;
		}

		private void TrySpawnSpy(Player player, Player dc, Dictionary<Player, bool> spies) 
		{
			if (Loader.Plugins.FirstOrDefault(pl => pl.Name == "CiSpy") != null)
			{
				Loader.Plugins.First(pl => pl.Name == "CiSpy").Assembly.GetType("CISpy.API.SpyData").GetMethod("MakeSpy", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { player, spies[dc], false });
			}
		}

		private void TrySpawnSH(Player player) 
		{
			if (Loader.Plugins.FirstOrDefault(pl => pl.Name == "SerpentsHand") != null)
			{
				Loader.Plugins.First(pl => pl.Name == "SerpentsHand").Assembly.GetType("SerpentsHand.API.SerpentsHand").GetMethod("SpawnPlayer", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { player, false });
			}
		} 
		private void TrySpawn035(Player player)
		{
			if (Loader.Plugins.FirstOrDefault(pl => pl.Name == "scp035") != null)
			{
				Loader.Plugins.First(pl => pl.Name == "scp035").Assembly.GetType("scp035.API.Scp035Data").GetMethod("Spawn035", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { player });
			}
		}

		private void ReplacePlayer(Player player)
		{
			bool is035 = false;
			bool isSH = false;
			if (isContain106 && player.Role == RoleType.Scp106) return;
			Dictionary<Player, bool> spies = null;

			try
			{
				is035 = TryGet035().Contains(player);
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
				Log.Debug("Serpents Hand is not installed, skipping method call...");
			}

			try
			{
				spies = TryGetSpies();
			}
			catch (Exception x)
			{
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
