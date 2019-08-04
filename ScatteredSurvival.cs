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

/// <summary>
/// Remember to disable nuke
/// tell them that you can't escape
/// </summary>
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
	public class ScatteredSurvival : Plugin, IEventHandlerWaitingForPlayers, IEventHandlerSetConfig, IEventHandlerSetRole, IEventHandlerInitialAssignTeam,
		IEventHandlerPlayerJoin, IEventHandlerContain106, IEventHandlerPlayerHurt, IEventHandlerCheckEscape,
		IEventHandlerCallCommand
	{
		public const string ModVersion = "1.04";

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

		// Item Spawning addition
		[ConfigOption] private readonly int[] onSpawnItems = new int[] { 0, 4, 9 };

		private readonly FieldInfo cachedPlayerConnFieldInfo = typeof(SmodPlayer).GetField("conn", BindingFlags.NonPublic | BindingFlags.Instance);

		//+ Round Data

		private bool femurBroke;

		private Broadcast cachedBroadcast;

		// Plugin Methods

		public override void OnDisable() => Info(Details.name + " was disabled");
		public override void OnEnable() => Info($"{Details.name} has loaded, type 'fe' for details.");
		public override void Register()
		{
			AddEventHandlers(this, Priority.Lowest);

			ArithSpawningKit.RandomPlayerSpawning.RandomPlayerSpawning.Instance.DisablePlugin = false;

			var individualSpawns = ArithFeather.ArithSpawningKit.IndividualSpawns.IndividualSpawns.Instance;
			individualSpawns.DisablePlugin = false;
			individualSpawns.OnSpawnPlayer += OnSpawnPlayer;
		}

		#region Events

		public void OnCheckEscape(PlayerCheckEscapeEvent ev) => ev.AllowEscape = false;

		public void OnSetConfig(SetConfigEvent ev)
		{
			switch (ev.Key)
			{
				//           case "manual_end_only":
				//               ev.Value = true;
				//break;

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

				//case "antifly_enable":
				//    ev.Value = false;
				//    break;

				//case "anti_player_wallhack":
				//    ev.Value = false;
				//    break;

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

			}
		}

		public void OnPlayerJoin(PlayerJoinEvent ev)
		{
			if (showGameStartMessage)
			{
				try
				{
					PersonalBroadcast(ev.Player, 10,
						$"<size={ServerInfoSize}><color={ServerInfoColor}>Welcome to <color={ServerHighLightColor}>Scattered Survival v{ModVersion}!</color> Press ` to open the console and enter '<color={ServerHighLightColor}>.help</color>' for mod information!</color></size>");
					PersonalBroadcast(ev.Player, 10,
						$"<size={ServerInfoSize}><color={ServerInfoColor}>If you like the plugin, join the discord for updates!\n <color={ServerHighLightColor}>https://discord.gg/DunUU82</color></color></size>");
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
				PluginManager.DisablePlugin(this);
				return;
			}

			cachedBroadcast = GameObject.Find("Host").GetComponent<Broadcast>();
			femurBroke = false;
		}

		public void OnAssignTeam(PlayerInitialAssignTeamEvent ev)
		{
			var player = ev.Player;
			var spawner = ArithSpawningKit.RandomPlayerSpawning.RandomPlayerSpawning.Instance;
			
			if (ev.Team != Smod2.API.Team.SCP)
			{
				spawner.MakePlayerNextSpawnRandom(player, Role.SCIENTIST);
			}
		}

		private SavedSpawnInfo cachedSpawnInfo;
		public void OnSetRole(PlayerSetRoleEvent ev)
		{
			if (ev.Role == Role.SPECTATOR) return;

			var player = ev.Player;

			if (ev.TeamRole.Team == Smod2.API.Team.SCP)
			{
				PersonalBroadcast(player, 10,
					$"<size={ClassInfoSize}><color={ClassInfoColor}>Kill the players <color={WinColor}>{PlayerLives.PlayerLives.Instance.LivesLeft}</color> times to win.</color></size>");
			}
			// Send broadcast for roles.
			else if (ev.TeamRole.Role == Role.SCIENTIST)
			{
				PersonalBroadcast(player, 10, $"<size={ClassInfoSize}><color={ClassInfoColor}>Kill the SCP before your lives reach 0. Lives are shared.</color></size>");

				if (cachedSpawnInfo != null && cachedSpawnInfo.PlayerID == ev.Player.PlayerId)
				{
					var itemSpawner = RandomItemSpawner.RandomItemSpawner.Instance.ItemSpawning;
					itemSpawner.CheckSpawns();

					var spawnLength = onSpawnItems.Length;
					var itemTypes = new ItemType[spawnLength];

					for (int i = 0; i < spawnLength; i++)
					{
						itemTypes[i] = ItemSpawning.GetRandomRarityItem((ItemRarity)onSpawnItems[i]);
					}

					itemSpawner.SpawnItems(itemTypes, cachedSpawnInfo.SpawnLocation.ZoneType, true);
				}
			}
		}

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
			if (ev.Player.TeamRole.Role == Role.SCP_106 && ev.DamageType == DamageType.CONTAIN) ev.Damage = 0;
		}

		// Custom Event
		private void OnSpawnPlayer(DeadPlayer deadPlayer)
		{
			var player = deadPlayer.Player;

			RandomPlayerSpawning.Instance.UpdateFreeSpawns(1);
			RandomPlayerSpawning.Instance.MakePlayerNextSpawnRandom(player, Role.SCIENTIST);
			player.ChangeRole(Role.SCIENTIST);
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