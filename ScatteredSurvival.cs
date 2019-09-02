using System.Reflection;
using ServerMod2.API;
using Smod2;
using Smod2.API;
using Smod2.Config;
using Smod2.Attributes;
using Smod2.Events;
using UnityEngine;
using UnityEngine.Networking;
using ArithFeather.ArithSpawningKit.IndividualSpawns;
using ArithFeather.ArithSpawningKit.RandomPlayerSpawning;
using Smod2.EventHandlers;
using ArithFeather.RandomItemSpawner;
using UnityEngine.Networking.NetworkSystem;
using GamemodeManager;

namespace ArithFeather.ScatteredSurvival
{
	[PluginDetails(
		author = "Arith",
		name = "Scattered Survival",
		description = "",
		id = "ArithFeather.ScatteredSurvival",
		configPrefix = "afss",
		version = ModVersion,
		SmodMajor = 3,
		SmodMinor = 4,
		SmodRevision = 0
		)]
	public class ScatteredSurvival : Plugin, IEventHandlerWaitingForPlayers, IEventHandlerSetConfig, IEventHandlerSetRole,
		IEventHandlerInitialAssignTeam, IEventHandlerPlayerJoin, IEventHandlerContain106, IEventHandlerPlayerHurt,
		IEventHandlerCheckEscape, IEventHandlerRoundStart, IEventHandlerCallCommand, IEventHandler079AddExp,
		IEventHandlerGeneratorFinish, IEventHandlerWarheadChangeLever, IEventHandlerPlayerDie
	{
		public const string ModVersion = "1.1";

		#region Const text values

		private const string ServerHighLightColor = "#38a8b5";

		private const string ServerInfoColor = "#23eb44";
		private const int ServerInfoSize = 50;

		private const string ClassInfoColor = "#23eb44";
		private const int ClassInfoSize = 50;

		private const string WinColor = "#b81111";
		private const int WinTextSize = 70;

		#endregion

		[ConfigOption] private readonly bool disablePlugin = false;
		[ConfigOption] private readonly bool showGameStartMessage = true;
		[ConfigOption] public readonly bool useDefaultConfig = true;
		[ConfigOption] private readonly float expMultiplier = 10;

		private readonly FieldInfo cachedPlayerConnFieldInfo = typeof(SmodPlayer).GetField("conn", BindingFlags.NonPublic | BindingFlags.Instance);

		//+ Round Data

		private bool femurBroke;
		private int generatorsActivated;
		private bool roundStarted;
		private Broadcast cachedBroadcast;

		// Plugin Methods

		public override void OnDisable() => Info(Details.name + " was disabled");
		public override void OnEnable() => Info($"{Details.name} has loaded, type 'fe' for details.");
		public override void Register()
		{
			AddEventHandlers(this, Priority.Lowest);

			RandomPlayerSpawning.Instance.DisablePlugin = false;

			var individualSpawns = IndividualSpawns.Instance;
			individualSpawns.DisablePlugin = false;
			individualSpawns.OnSpawnPlayer += OnSpawnPlayer;
		}

		#region Events

		public void OnCheckEscape(PlayerCheckEscapeEvent ev) => ev.AllowEscape = false;

		public void OnSetConfig(SetConfigEvent ev)
		{
			switch (ev.Key)
			{
				case "scp079_disable":
					ev.Value = true;
					break;
				case "smart_class_picker":
					ev.Value = false;
					break;
				case "team_respawn_queue":
					if (useDefaultConfig) ev.Value = "30333033333033333033333";
					break;

				case "disable_dropping_empty_boxes":
					ev.Value = true;
					break;

				case "disable_decontamination":
					ev.Value = true;
					break;

				// Class Editing
				case "default_item_scientist":
					if (useDefaultConfig) ev.Value = new[] { 19 };
					break;
			}
		}

		public void OnCallCommand(PlayerCallCommandEvent ev)
		{
			if (ev.Command == "HELP")
			{
				ev.ReturnMessage = $"Scattered Survival Mode v{ScatteredSurvival.ModVersion}\n" +
								   "https://discord.gg/DunUU82\n" +
								   "Scientists will spawn randomly in Entrance and Light Containment Zone.\n" +
								   "\n" +
								   "SCP win if the player's lives reach 0. Lives are shared between everyone.\n" +
								   "\n" +
								   "Items will spawn randomly across the map.\n" +
								   "You can not escape the facility. Decontamination and nuke are disabled.\n" +
								   "Unless all 5 generators are active, dead SCP will respawn as SCP079 with 10x experience gain. SCP079 does not need to be killed to win.\n" +
								   "SCP106 pelvis breaker no longer kills 106. Instead, it reduces their current HP by half.\n" +
								   "--Rules--\n" +
								   "1.Please don't group up with other teams.\n" +
								   "2.Don't harass other players. Instant ban\n" +
								   "3.Players found cheating will be recorded and reported for global ban.";
			}
		}

		public void OnPlayerJoin(PlayerJoinEvent ev)
		{
			if (showGameStartMessage)
			{
				try
				{
					PersonalBroadcast(ev.Player, 8,
						$"<size={ServerInfoSize}><color={ServerInfoColor}>Welcome to <color={ServerHighLightColor}>Scattered Survival v{ModVersion}!</color> Press ` to open the console and enter '<color={ServerHighLightColor}>.help</color>' for mod information!</color></size>");
					PersonalBroadcast(ev.Player, 8,
						$"<size={ServerInfoSize}><color={ServerInfoColor}>If you like the plugin, join the discord for updates!\n <color={ServerHighLightColor}>https://discord.gg/DunUU82</color></color></size>");
					PersonalBroadcast(ev.Player, 5, $"<size={ServerInfoSize}><color={ServerInfoColor}>Nuke and escaping are disabled.</color></size>");
				}
				catch
				{
					Info("Null ref on joining player. Ignoring...");
				}
			}
		}

		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			if (disablePlugin)
			{
				PluginManager.DisablePlugin(RandomItemSpawner.RandomItemSpawner.Instance);
				PluginManager.DisablePlugin("ArithFeather.ArithSpawningKit");
				PluginManager.DisablePlugin(PlayerLives.PlayerLives.Instance);
				PluginManager.DisablePlugin(this);
				return;
			}

			cachedBroadcast = GameObject.Find("Host").GetComponent<Broadcast>();
			femurBroke = false;
			roundStarted = false;
			generatorsActivated = 0;
			Round.Stats.ScientistsEscaped = 1;
		}

		public void OnAssignTeam(PlayerInitialAssignTeamEvent ev)
		{
			var player = ev.Player;
			var spawner = RandomPlayerSpawning.Instance;

			if (ev.Team != Smod2.API.Team.SCP)
			{
				spawner.MakePlayerNextSpawnRandom(player, Role.SCIENTIST);
			}
		}

		public void OnRoundStart(RoundStartEvent ev)
		{
			roundStarted = true;
			Server.Round.Stats.ScientistsEscaped = 1;

			RandomItemSpawner.RandomItemSpawner.Instance.ItemSpawning.SpawnItems(2, ZoneType.ENTRANCE, ItemType.MAJOR_SCIENTIST_KEYCARD, true);
		}

		public void OnPlayerDie(PlayerDeathEvent ev)
		{
			var player = ev.Player;
			if (player.TeamRole.Team == Smod2.API.Team.SCP && player.TeamRole.Role != Role.SCP_079)
			{
				var scpPlayers = Server.GetPlayers(Smod2.API.Team.SCP);
				var scpCount = scpPlayers.Count;
				var scpAlive = 0;

				for (int i = 0; i < scpCount; i++)
				{
					if (scpPlayers[i].TeamRole.Role != Role.SCP_079)
						scpAlive++;
				}

				if (scpAlive <= 1)
				{
					generatorsActivated = 5;

					for (int i = 0; i < scpCount; i++)
					{
						var scp = scpPlayers[i];
						if (scp.TeamRole.Role == Role.SCP_079) scp.ChangeRole(Role.SPECTATOR);
					}
				}
			}
		}

		private SavedSpawnInfo cachedSpawnInfo;
		public void OnSetRole(PlayerSetRoleEvent ev)
		{
			if (ev.Role == Role.SPECTATOR) return;

			var player = ev.Player;

			if (roundStarted && ev.TeamRole.Team == Smod2.API.Team.SCP)
			{
				PersonalBroadcast(player, 10,
					$"<size={ClassInfoSize}><color={ClassInfoColor}>Kill the players <color={WinColor}>{PlayerLives.PlayerLives.Instance.LivesLeft}</color> times to win.</color></size>");
			}
			// Send broadcast for roles.
			else if (ev.TeamRole.Role == Role.SCIENTIST)
			{
				PersonalBroadcast(player, 10, $"<size={ClassInfoSize}><color={ClassInfoColor}>Kill the SCP before your lives reach 0. Lives are shared.</color></size>");
				PersonalBroadcast(player, 5, $"<size={ClassInfoSize}><color={ClassInfoColor}>Search for loot and allies.</color></size>");

				if (roundStarted && cachedSpawnInfo != null && cachedSpawnInfo.PlayerID == ev.Player.PlayerId)
				{
					var itemSpawner = RandomItemSpawner.RandomItemSpawner.Instance.ItemSpawning;
					itemSpawner.CheckSpawns();

					itemSpawner.SpawnItems(1, cachedSpawnInfo.SpawnLocation.ZoneType, ItemType.ZONE_MANAGER_KEYCARD, true);
					itemSpawner.SpawnItems(1, cachedSpawnInfo.SpawnLocation.ZoneType, UnityEngine.Random.Range(0f, 1f) > 0.5f ? ItemType.MEDKIT : ItemType.RADIO, true);
				}
			}
		}

		public void On079AddExp(Player079AddExpEvent ev) => ev.ExpToAdd *= expMultiplier;
		public void OnGeneratorFinish(GeneratorFinishEvent ev) => generatorsActivated++;

		/// <summary>
		/// Instead of killing him instantly, damage him for half his current HP
		/// </summary>
		public void OnContain106(PlayerContain106Event ev)
		{
			if (!femurBroke)
			{
				femurBroke = true;

				var scps = Server.GetPlayers(Role.SCP_106);
				var scpCount = scps.Count;

				for (int i = 0; i < scpCount; i++)
				{
					var scp = scps[i];
					scp.Damage((int)(scp.GetHealth() / 2), DamageType.NONE);
				}

				Broadcast(5, $"<color={WinColor}>SCP 106 has been damaged for half their current HP!</color>");
			}
		}

		public void OnPlayerHurt(PlayerHurtEvent ev)
		{
			if (ev.DamageType == DamageType.RAGDOLLLESS && ev.Player.TeamRole.Role == Role.SCP_106) ev.Damage = 0;
		}

		public void OnChangeLever(WarheadChangeLeverEvent ev) => ev.Allow = false;

		// Custom Event
		private void OnSpawnPlayer(DeadPlayer deadPlayer)
		{
			if (!PlayerLives.PlayerLives.Instance.GameHasEnded)
			{
				var player = deadPlayer.Player;

				if (deadPlayer.PreviousTeamRole.Team == Smod2.API.Team.SCP && generatorsActivated != 5)
				{
					player.ChangeRole(Role.SCP_079);
				}
				else
				{
					RandomPlayerSpawning.Instance.UpdateFreeSpawns(1);
					cachedSpawnInfo = RandomPlayerSpawning.Instance.MakePlayerNextSpawnRandom(player, Role.SCIENTIST);
					player.ChangeRole(Role.SCIENTIST);
				}
			}
		}

		#endregion

		#region Custom Broadcasts

		//private void PersonalClearBroadcasts(Player player)
		//{
		//	var connection = cachedPlayerConnFieldInfo.GetValue(player) as NetworkConnection;

		//	if (connection == null)
		//	{
		//		return;
		//	}

		//	cachedBroadcast.CallTargetClearElements(connection);
		//}
		private void PersonalBroadcast(Player player, uint duration, string message)
		{
			var connection = cachedPlayerConnFieldInfo.GetValue(player) as NetworkConnection;

			if (connection == null)
			{
				return;
			}

			cachedBroadcast.CallTargetAddElement(connection, message, duration, false);
		}
		private void ClearBroadcasts() => cachedBroadcast.CallRpcClearElements();
		private void Broadcast(uint duration, string message)
		{
			cachedBroadcast.CallRpcAddElement(message, duration, false);
		}

		#endregion
	}
}