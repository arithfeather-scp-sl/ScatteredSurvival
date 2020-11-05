using Exiled.API.Features;
using System.Collections.Generic;
using Exiled.API.Enums;
using MEC;
using Random = UnityEngine.Random;

namespace ArithFeather.ScatteredSurvival
{
	public static class SpookyLights
	{
		private static Config Config => ScatteredSurvival.Configs;

		private static CoroutineHandle flickerLightHandle;

		private static readonly List<Room> hcz = new List<Room>();
		private static readonly List<Room> lcz = new List<Room>();
		private static readonly List<Room> ent = new List<Room>();

		public static void Enable()
		{
			Exiled.Events.Handlers.Server.RoundStarted += ServerEvents_RoundStarted;
			Exiled.Events.Handlers.Server.WaitingForPlayers += Server_WaitingForPlayers;
			Exiled.Events.Handlers.Server.RoundEnded += Server_RoundEnded;
		}

		public static void Disable()
		{
			Exiled.Events.Handlers.Server.RoundStarted -= ServerEvents_RoundStarted;
			Exiled.Events.Handlers.Server.WaitingForPlayers -= Server_WaitingForPlayers;
			Exiled.Events.Handlers.Server.RoundEnded -= Server_RoundEnded;
		}

		private static void Server_WaitingForPlayers()
		{
			ent.Clear();
			lcz.Clear();
			hcz.Clear();

			var rooms = Map.Rooms;
			var roomSize = rooms.Count;

			for (int i = 0; i < roomSize; i++)
			{
				var room = rooms[i];

				switch (room.Zone)
				{
					case ZoneType.Entrance:
						ent.Add(room);
						break;
					case ZoneType.HeavyContainment:
						hcz.Add(room);
						break;
					case ZoneType.LightContainment:
						lcz.Add(room);
						break;
				}
			}
		}

		private static void Server_RoundEnded(Exiled.Events.EventArgs.RoundEndedEventArgs ev) =>
			Timing.KillCoroutines(flickerLightHandle);

		private static void ServerEvents_RoundStarted() =>
			flickerLightHandle = Timing.RunCoroutine(FlickeringLights());


		private static IEnumerator<float> FlickeringLights()
		{
			var timer = 0;
			var nextFlicker = Random.Range(Config.MinTimeBetweenFlickers, Config.MaxTimeBetweenFlickers);

			while (Config.IsEnabled && Config.EnableSpookyLights)
			{
				yield return Timing.WaitForSeconds(1);
				timer++;

				if (timer >= nextFlicker)
				{
					timer = 0;
					nextFlicker = Random.Range(Config.MinTimeBetweenFlickers, Config.MaxTimeBetweenFlickers);

					var zone = (ZoneType)Random.Range(1, 4);

					TriggerZoneLightsOut(zone);

					if (Config.EnableFlickerAnnouncements)
					{
						yield return Timing.WaitForSeconds(1);
						timer++;
						TriggerCassieZoneMessage(zone);
					}
				}
			}
		}

		private static void TriggerZoneLightsOut(ZoneType zone)
		{
			List<Room> rooms = null;

			switch (zone)
			{
				case ZoneType.Entrance:
					rooms = ent;
					break;

				case ZoneType.HeavyContainment:
					rooms = hcz;
					break;

				case ZoneType.LightContainment:
					rooms = lcz;
					break;
			}

			if (rooms == null) return;

			var randomDuration = Random.Range(Config.MinDurationOfFlicker, Config.MaxDurationOfFlicker);
			var contSize = rooms.Count;
			for (int i = 0; i < contSize; i++)
			{
				rooms[i].TurnOffLights(randomDuration);
			}
		}

		private static void TriggerCassieZoneMessage(ZoneType zone)
		{
			switch (zone)
			{
				case ZoneType.Entrance:
					Cassie.Message("POWER OUTAGE IN ENTRANCE ZONE");
					break;

				case ZoneType.HeavyContainment:
					Cassie.Message("POWER OUTAGE IN HEAVY CONTAINMENT ZONE");
					break;

				case ZoneType.LightContainment:
					Cassie.Message("POWER OUTAGE IN LIGHT CONTAINMENT ZONE");
					break;
			}
		}
	}
}