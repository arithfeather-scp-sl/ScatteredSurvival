using System.Collections.Generic;
using System.Linq;
using ArithFeather.ScatteredSurvival.Patches;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using PlayerEvents = Exiled.Events.Handlers.Player;

namespace ArithFeather.ScatteredSurvival
{
	/// <summary>
	/// When a player dies, this will raise an event to spawn them after X seconds. (It will not spawn them automatically)
	/// Recommend using ChangeRole() to spawn players - this will call both OnSpawn and OnRole.
	/// Use the OnSpawnPlayer event to register for spawning players.
	/// </summary>
	public class IndividualSpawns
	{

		public static readonly IndividualSpawns Instance = new IndividualSpawns();

		public delegate void SpawnPlayer(Player player);
		public event SpawnPlayer OnSpawnPlayer;

		public float RespawnSpeed { get; set; } = 30;

		public void Enable()
		{
			AllowTeamSpawnPatch.OnForceTeamSpawn += AllowTeamSpawnPatch_OnForceTeamSpawn;

			Exiled.Events.Handlers.Map.AnnouncingNtfEntrance += Map_AnnouncingNtfEntrance;
			Exiled.Events.Handlers.Server.RespawningTeam += Server_RespawningTeam;
			PlayerEvents.Joined += PlayerEvents_Joined;
			PlayerEvents.Died += PlayerEvents_Died;
		}

		public void Disable()
		{
			AllowTeamSpawnPatch.OnForceTeamSpawn -= AllowTeamSpawnPatch_OnForceTeamSpawn;

			Exiled.Events.Handlers.Map.AnnouncingNtfEntrance -= Map_AnnouncingNtfEntrance;
			Exiled.Events.Handlers.Server.RespawningTeam -= Server_RespawningTeam;
			PlayerEvents.Joined -= PlayerEvents_Joined;
			PlayerEvents.Died -= PlayerEvents_Died;
		}

		private void AllowTeamSpawnPatch_OnForceTeamSpawn()
		{
			var players = Player.List.ToList();
			var playerCount = players.Count;
			for (int i = 0; i < playerCount; i++)
			{
				var player = players[i];

				if (player.Role == RoleType.Spectator && !player.IsOverwatchEnabled && Round.IsStarted)
				{
					OnSpawnPlayer?.Invoke(player);
				}
			}

		}

		private void PlayerEvents_Died(DiedEventArgs ev) => Timing.RunCoroutine(RespawnIn30(ev.Target));

		private IEnumerator<float> RespawnIn30(Player player)
		{
			player.ClearBroadcasts();
			player.Broadcast(5, $"Respawning in {RespawnSpeed} seconds.");

			yield return Timing.WaitForSeconds(RespawnSpeed);

			if (Round.IsStarted && player.ReferenceHub != null && player.Role == RoleType.Spectator)
			{

				OnSpawnPlayer?.Invoke(player);
			}
		}

		private static void Server_RespawningTeam(RespawningTeamEventArgs ev) => ev.Players.Clear();
		private static void Map_AnnouncingNtfEntrance(AnnouncingNtfEntranceEventArgs ev) => ev.IsAllowed = false;

		private void PlayerEvents_Joined(JoinedEventArgs ev)
		{
			if (Round.IsStarted) Timing.RunCoroutine(RespawnIn30(ev.Player));
		}
	}
}