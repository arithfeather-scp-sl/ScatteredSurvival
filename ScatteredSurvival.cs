using System;
using System.Collections.Generic;
using System.Linq;
using ArithFeather.AriToolKit.CustomEnding;
using ArithFeather.AriToolKit.IndividualSpawns;
using ArithFeather.CustomPlayerSpawning;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using ServerEvents = Exiled.Events.Handlers.Server;
using PlayerEvents = Exiled.Events.Handlers.Player;

namespace ArithFeather.ScatteredSurvival {
	public class ScatteredSurvival : Plugin<Config> {

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

		public override void OnEnabled() {
			base.OnEnabled();

			var individualSpawns = IndividualSpawns.Instance;
			individualSpawns.Enable();
			individualSpawns.OnSpawnPlayer += IndividualSpawns_OnSpawnPlayer;

			SpawnerAPI.OnPlayerSpawningAtPoint += SpawnerAPI_OnPlayerSpawningAtPoint;

			var settings = new SpawnSettings();
			settings.DefineSafeSpawnDistances(new DistanceCheckInfo(new []{ RoleType.ClassD }, Config.SafeTeamSpawnDistance, Config.SafeEnemySpawnDistance));
			SpawnerAPI.ApplySettings(settings);

			CustomItemSpawner.Spawning.EndlessSpawning.Enable();
			CustomEnding.Instance.OnCheckEndGame += Instance_OnCheckEndGame;

			ServerEvents.RoundStarted += ServerEvents_RoundStarted;
			ServerEvents.WaitingForPlayers += ServerEvents_WaitingForPlayers;
			PlayerEvents.Joined += PlayerEvents_Joined;
			Exiled.Events.Handlers.Scp106.Containing += Scp106_Containing;
			ServerEvents.SendingConsoleCommand += ServerEvents_SendingConsoleCommand;
			Exiled.Events.Handlers.Warhead.Starting += Warhead_Starting;
		}

		public override void OnDisabled() {
			var individualSpawns = IndividualSpawns.Instance;
			individualSpawns.Disable();
			individualSpawns.OnSpawnPlayer -= IndividualSpawns_OnSpawnPlayer;

			ServerEvents.RoundStarted -= ServerEvents_RoundStarted;
			ServerEvents.WaitingForPlayers -= ServerEvents_WaitingForPlayers;
			SpawnerAPI.OnPlayerSpawningAtPoint -= SpawnerAPI_OnPlayerSpawningAtPoint;

			CustomItemSpawner.Spawning.EndlessSpawning.Disable();
			CustomEnding.Instance.OnCheckEndGame -= Instance_OnCheckEndGame;

			PlayerEvents.Joined -= PlayerEvents_Joined;
			Exiled.Events.Handlers.Scp106.Containing -= Scp106_Containing;
			ServerEvents.SendingConsoleCommand -= ServerEvents_SendingConsoleCommand;
			Exiled.Events.Handlers.Warhead.Starting -= Warhead_Starting;

			base.OnDisabled();
		}

		private void ServerEvents_RoundStarted() => Timing.RunCoroutine(WaitForSpawns());
		private void ServerEvents_WaitingForPlayers() => _initialSpawnsFinished = false;

		private IEnumerator<float> WaitForSpawns()
		{
			yield return Timing.WaitForSeconds(0.5f);
			_initialSpawnsFinished = true;
		}

		private void Instance_OnCheckEndGame(RoundSummary.SumInfo_ClassList classList, EndGameInformation endGameInfo)
		{
			if (endGameInfo.IsGameEnding) return;

			if (Map.ActivatedGenerators == 5)
			{
				endGameInfo.WinningTeam = LeadingTeam.ChaosInsurgency;
			}
		}

		private void SpawnerAPI_OnPlayerSpawningAtPoint(PlayerSpawnPoint playerSpawnPoint) {
			CustomItemSpawner.CustomItemSpawner.CheckRoomItemsSpawned(playerSpawnPoint.Room.Id);

			if (!_initialSpawnsFinished) return;

			switch (playerSpawnPoint.Room.Room.Zone)
			{
				case ZoneType.Entrance:

					CustomItemSpawner.Spawning.EndlessSpawning.SpawnQueuedListInGroups("SafeEz", "rooms");

					break;
				case ZoneType.LightContainment:

					CustomItemSpawner.Spawning.EndlessSpawning.SpawnQueuedListInGroups("SafeLcz", "rooms");

					break;
			}
		}

		private void IndividualSpawns_OnSpawnPlayer(Player player) {
			player.SetRole(RoleType.Scientist);
		}

		private void ServerEvents_SendingConsoleCommand(Exiled.Events.EventArgs.SendingConsoleCommandEventArgs ev) {
			if (ev.Name.ToLowerInvariant() == "sshelp") {
				ev.ReturnMessage = $"Scattered Survival Mode v{Version}\n" +
								   "Scientists will spawn randomly in Entrance and Light Containment Zone.\n" +
								   "\n" +
								   "SCP win if the player's lives reach 0. Lives are shared between everyone.\n" +
								   "\n" +
								   "-Items will spawn randomly across the map.\n" +
								   "-Decontamination and nuke are disabled.\n" +
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
					$"<size={ServerInfoSize}><color={ServerInfoColor}>Welcome to <color={ServerHighLightColor}>Scattered Survival v{Version}!</color> Press ` to open the console and enter '<color={ServerHighLightColor}>.sshelp</color>' for mod information!</color></size>");
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
	}
}