using System;
using System.Collections.Generic;
using System.Linq;
using ArithFeather.AriToolKit.CustomEnding;
using ArithFeather.AriToolKit.IndividualSpawns;
using ArithFeather.CustomPlayerSpawning;
using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;
using MEC;
using LeadingTeam = ArithFeather.AriToolKit.CustomEnding.LeadingTeam;
using ServerEvents = Exiled.Events.Handlers.Server;
using PlayerEvents = Exiled.Events.Handlers.Player;

namespace ArithFeather.ScatteredSurvival {
	public class ScatteredSurvival : Plugin<Config>
	{
		public static Config Configs;

		#region Const text values

		// Old spawn queueu: 30333033333033333033333

		private const string ServerHighLightColor = "#38a8b5";

		private const string ServerInfoColor = "#23eb44";
		private const int ServerInfoSize = 50;

		private const string WinColor = "#b81111";

		#endregion

		private bool _initialSpawnsFinished;

		public override string Author => "Arith";

		public override Version Version => new Version("2.00");

		private readonly Harmony _harmony = new Harmony("ScatteredSurvival");

		private readonly List<int> deadScp = new List<int>();

		public override void OnEnabled() {
			base.OnEnabled();
			Configs = Config;

			var settings = new SpawnSettings();
			settings.DefineSafeSpawnDistances(new DistanceCheckInfo(new []{ RoleType.ClassD }, Config.SafeTeamSpawnDistance, Config.SafeEnemySpawnDistance));
			SpawnerAPI.ApplySettings(settings);

			_harmony.PatchAll();

			CustomItemSpawner.Spawning.EndlessSpawning.Enable();
			CustomEnding.Instance.OnCheckEndGame += Instance_OnCheckEndGame;

			var individualSpawns = IndividualSpawns.Instance;
			individualSpawns.Enable();
			individualSpawns.OnSpawnPlayer += IndividualSpawns_OnSpawnPlayer;

			SpawnerAPI.OnPlayerSpawningAtPoint += SpawnerAPI_OnPlayerSpawningAtPoint;

			ServerEvents.RoundStarted += ServerEvents_RoundStarted;
			ServerEvents.WaitingForPlayers += ServerEvents_WaitingForPlayers;
			ServerEvents.SendingConsoleCommand += ServerEvents_SendingConsoleCommand;

			PlayerEvents.Died += PlayerEvents_Died;
			PlayerEvents.Joined += PlayerEvents_Joined;
			PlayerEvents.Left += PlayerEvents_Left;
			PlayerEvents.InteractingElevator += PlayerEvents_InteractingElevator;

			Exiled.Events.Handlers.Scp106.Containing += Scp106_Containing;
			Exiled.Events.Handlers.Warhead.Starting += Warhead_Starting;
			Exiled.Events.Handlers.Scp914.Activating += Scp914_Activating;
		}

		public override void OnDisabled() {
			CustomItemSpawner.Spawning.EndlessSpawning.Disable();
			CustomEnding.Instance.OnCheckEndGame -= Instance_OnCheckEndGame;

			var individualSpawns = IndividualSpawns.Instance;
			individualSpawns.Disable();
			individualSpawns.OnSpawnPlayer -= IndividualSpawns_OnSpawnPlayer;

			SpawnerAPI.OnPlayerSpawningAtPoint -= SpawnerAPI_OnPlayerSpawningAtPoint;

			ServerEvents.RoundStarted -= ServerEvents_RoundStarted;
			ServerEvents.WaitingForPlayers -= ServerEvents_WaitingForPlayers;
			ServerEvents.SendingConsoleCommand -= ServerEvents_SendingConsoleCommand;

			PlayerEvents.Died -= PlayerEvents_Died;
			PlayerEvents.Joined -= PlayerEvents_Joined;
			PlayerEvents.Left -= PlayerEvents_Left;
			PlayerEvents.InteractingElevator += PlayerEvents_InteractingElevator;

			Exiled.Events.Handlers.Scp106.Containing -= Scp106_Containing;
			Exiled.Events.Handlers.Warhead.Starting -= Warhead_Starting;
			Exiled.Events.Handlers.Scp914.Activating -= Scp914_Activating;

			base.OnDisabled();
		}
		private void PlayerEvents_InteractingElevator(Exiled.Events.EventArgs.InteractingElevatorEventArgs ev)
		{
			var currentRoom = ev.Player.CurrentRoom.Type;
			if (currentRoom == RoomType.EzGateA || currentRoom == RoomType.EzGateB)
			{
				ev.IsAllowed = false;
			}
		}

		private void Scp914_Activating(Exiled.Events.EventArgs.ActivatingEventArgs ev) => ev.IsAllowed = false;

		private void ServerEvents_RoundStarted() => Timing.RunCoroutine(WaitForSpawns());

		private void ServerEvents_WaitingForPlayers()
		{
			deadScp.Clear();
			PlayerManager.localPlayer.GetComponent<CharacterClassManager>().Classes[(int)RoleType.Scp079].banClass = true;
			_initialSpawnsFinished = false;
		}

		private IEnumerator<float> WaitForSpawns()
		{
			yield return Timing.WaitForSeconds(0.5f);
			_initialSpawnsFinished = true;
			Map.Broadcast(8, "Players can also win by activating all 5 generators.");
		}

		private void Instance_OnCheckEndGame(RoundSummary.SumInfo_ClassList classList, EndGameInformation endGameInfo)
		{
			if (endGameInfo.IsGameEnding) return;

			if (Map.ActivatedGenerators == 5)
			{
				endGameInfo.WinningTeam = LeadingTeam.ChaosInsurgency;
				endGameInfo.IsGameEnding = true;
			}
		}

		private void SpawnerAPI_OnPlayerSpawningAtPoint(PlayerSpawnPoint playerSpawnPoint) {
			CustomItemSpawner.CustomItemSpawner.CheckRoomItemsSpawned(playerSpawnPoint.Room.Id);

			if (!_initialSpawnsFinished) return;

			switch (playerSpawnPoint.Room.Room.Zone)
			{
				case ZoneType.Entrance:

					CustomItemSpawner.Spawning.EndlessSpawning.SpawnItemsInEndlessGroup("lczsafe");

					break;
				case ZoneType.LightContainment:

					CustomItemSpawner.Spawning.EndlessSpawning.SpawnItemsInEndlessGroup("entsafe");

					break;
			}
		}

		private void IndividualSpawns_OnSpawnPlayer(Player player)
		{
			var deadScpCount = deadScp.Count;
			if (deadScpCount != 0)
			{
				for (int i = 0; i < deadScpCount; i++)
				{
					if (deadScp[i] == player.Id) {
						player.SetRole(RoleType.Scp079);
						deadScp.RemoveAt(i);
						return;
					}
				}
			}

			player.SetRole(RoleType.ClassD);
		}

		private void PlayerEvents_Left(Exiled.Events.EventArgs.LeftEventArgs ev) {
			var deadScpCount = deadScp.Count;
			if (deadScpCount != 0) {
				for (int i = 0; i < deadScpCount; i++) {
					if (deadScp[i] == ev.Player.Id) {
						deadScp.RemoveAt(i);
						return;
					}
				}
			}
		}

		private void ServerEvents_SendingConsoleCommand(Exiled.Events.EventArgs.SendingConsoleCommandEventArgs ev) {
			if (ev.Name.ToLowerInvariant() == "sshelp") {
				ev.ReturnMessage = $"Scattered Survival Mode v{Version}\n" +
								   "Scientists will spawn randomly in Entrance and Light Containment Zone.\n" +
								   "\n" +
								   "SCP win if the player's lives reach 0. Lives are shared between everyone.\n" +
								   "Players win by killing the SCP or activating all 5 generators.\n" +
								   "\n" +
								   "-Items will spawn randomly across the map.\n" +
								   "-Decontamination and nuke are disabled.\n" +
								   "-SCP914 is disabled.\n" +
								   "-Unless all 5 generators are active, dead SCP will respawn as SCP079. SCP079 does not need to be killed to win.\n" +
								   "-SCP106 pelvis breaker no longer kills 106. Instead, it reduces their current HP by half.\n";
				ev.Allow = true;
			}
		}

		/// <summary>
		/// Broadcast start game message
		/// </summary>
		private void PlayerEvents_Joined(Exiled.Events.EventArgs.JoinedEventArgs ev) {
			if (Config.ShowGameStartMessage) {
				ev.Player.Broadcast(8,
					$"<size={ServerInfoSize}><color={ServerInfoColor}><color={ServerHighLightColor}>Scattered Survival Mode v{Version}!</color> Press ` to open the console and enter '<color={ServerHighLightColor}>.sshelp</color>' for mod information!</color></size>");
			}
		}

		/// <summary>
		/// Instead of killing him instantly, damage him for half his current HP
		/// </summary>
		private void Scp106_Containing(Exiled.Events.EventArgs.ContainingEventArgs ev) {
			ev.IsAllowed = false;

			var players = Player.List.ToList();
			var scpCount = players.Count;

			for (int i = 0; i < scpCount; i++) {
				var player = players[i];
				if (player.Role == RoleType.Scp106)
					player.Hurt((int)(player.Health / 2), DamageTypes.None);
			}

			Map.Broadcast(5, $"<color={WinColor}>SCP 106 has been damaged for half their current HP!</color>");
		}

		/// <summary>
		/// Disable Allowing Nuke
		/// </summary>
		private void Warhead_Starting(Exiled.Events.EventArgs.StartingEventArgs ev) => ev.IsAllowed = false;

		private void PlayerEvents_Died(Exiled.Events.EventArgs.DiedEventArgs ev) {
			var player = ev.Target;
			if (player.Team == Team.SCP && player.Role != RoleType.Scp079) {
				deadScp.Add(player.Id);
			}
		}
	}
}