using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using UnityEngine;
using PlayerEvents = Exiled.Events.Handlers.Player;
using ServerEvents = Exiled.Events.Handlers.Server;

namespace ArithFeather.ScatteredSurvival
{
	/// <summary>
	/// When a player commits suicide or is killed by an SCP, their lives go down.
	/// When lives = 0, SCP win.
	/// Removed SCP default victory for killing everyone at once.
	/// Removed Chaos-SCP victory.
	/// </summary>
	public static class PlayerLives
	{
		private static int _playerDeaths;
		private static int _messageTimer;

		public static int MaxLives { get; private set; }
		public static int LivesLeft => MaxLives - _playerDeaths;

		private static Config Config => ScatteredSurvival.Configs;

		public static void Enable()
		{
			ServerEvents.WaitingForPlayers += ServerEvents_WaitingForPlayers;
			PlayerEvents.Died += PlayerEvents_Died;
			ServerEvents.EndingRound += ServerEvents_EndingRound;
			ServerEvents.RoundStarted += ServerEvents_RoundStarted;
		}

		public static void Disable()
		{
			ServerEvents.WaitingForPlayers -= ServerEvents_WaitingForPlayers;
			PlayerEvents.Died -= PlayerEvents_Died;
			ServerEvents.EndingRound -= ServerEvents_EndingRound;
			ServerEvents.RoundStarted -= ServerEvents_RoundStarted;
		}

		private static void ServerEvents_WaitingForPlayers()
		{
			_playerDeaths = 0;
			_messageTimer = Mathf.Max(0, Config.MessageRefreshTime - 5);
		}

		private static void PlayerEvents_Died(DiedEventArgs ev)
		{
			var killer = ev.Killer;
			var player = ev.Target;

			// Not SCP and Killed by SCP or suicide.
			if (player.Team != Team.SCP &&
				(killer.Team == Team.SCP ||
				 killer.Id == player.Id))
			{
				_playerDeaths++;
				_messageTimer = 0;
				Map.ClearBroadcasts();
				Map.Broadcast(Config.MessageTime, string.Format(Config.DisplayMessageFormat, LivesLeft));
			}
		}

		private static IEnumerator<float> Broadcasting()
		{
			yield return Timing.WaitForSeconds(5);
			while (Round.IsStarted)
			{
				yield return Timing.WaitForSeconds(1);
				_messageTimer++;

				if (_messageTimer >= Config.MessageRefreshTime)
				{
					_messageTimer = 0;
					Map.Broadcast(Config.MessageTime, string.Format(Config.DisplayMessageFormat, LivesLeft));
				}
			}
		}

		private static void ServerEvents_RoundStarted() => Timing.RunCoroutine(DelayedRoundStart(), Segment.LateUpdate);

		private static IEnumerator<float> DelayedRoundStart()
		{
			const string classInfoColor = "#23eb44";
			const int classInfoSize = 50;
			const string winColor = "#b81111";

			yield return Timing.WaitForSeconds(1);

			// Count SCP
			var players = Player.List.ToList();
			var scpCount = 0;
			foreach (var player in players)
			{
				if (player.Team == Team.SCP)
				{
					scpCount++;
				}
			}

			// Set lives based on number of SCP
			switch (scpCount)
			{
				case 0:
					MaxLives = 2;
					break;
				case 1:
					MaxLives = Config.LivesPerScp;
					break;
				case 2:
					MaxLives = (int)(2 * (Config.LivesPerScp + Config.LifeScalingIncreasePerScp));
					break;
				case 3:
					MaxLives = (int)(3 * (Config.LivesPerScp + (Config.LifeScalingIncreasePerScp * 2)));
					break;
				default:
					MaxLives = (int)(scpCount * (Config.LivesPerScp + (Config.LifeScalingIncreasePerScp * (scpCount - 1))));
					break;
			}

			var scpMessage =
				$"<size={classInfoSize}><color={classInfoColor}>Kill the players <color={winColor}>{LivesLeft}</color> times to win.</color></size>";
			var playerMessage =
				$"<size={classInfoSize}><color={classInfoColor}>Kill the SCP before your lives reach 0. Lives are shared.</color></size>";
			foreach (var player in players)
			{
				player.Broadcast(8, player.Team == Team.SCP ? scpMessage : playerMessage);
			}
			Timing.RunCoroutine(Broadcasting());

			if (Config.EnableGeneratorWinCondition)
			{
				yield return Timing.WaitForSeconds(8);
				Map.Broadcast(5, $"<size={classInfoSize}><color={classInfoColor}>Players can also win by activating all 5 generators.</color></size>");
			}
		}

		private static void ServerEvents_EndingRound(EndingRoundEventArgs ev)
		{
			var classList = ev.ClassList;

			// If Round ending from external source
			if (ev.IsRoundEnded)
			{
				// Make sure it's not because all players are dead.
				if (0 == classList.class_ds +
					classList.mtf_and_guards +
					classList.scientists)
				{
					ev.IsRoundEnded = false;
				}
			}

			// SCP win if lives run out
			if (!ev.IsRoundEnded && LivesLeft <= 0)
			{
				ev.IsRoundEnded = true;
				ev.LeadingTeam = LeadingTeam.Anomalies;
			}
		}
	}
}