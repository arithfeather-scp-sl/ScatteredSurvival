using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using ServerMod2.API;
using Smod2;
using Smod2.API;
using Smod2.Config;
using Smod2.Attributes;
using Smod2.Events;
using Smod2.EventSystem.Events;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

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
	public class ScatteredSurvival : Plugin
	{
		public const string ModVersion = "1.03";
		
		private const string ServerHighLightColor = "#38a8b5";

		private const string ServerInfoColor = "#23eb44";
		private const int ServerInfoSize = 50;

		private const string ClassInfoColor = "#23eb44";
		private const int ClassInfoSize = 50;

		private const string WinColor = "#b81111";
		private const int WinTextSize = 70;

		[ConfigOption] private readonly bool disablePlugin = false;
		/// <summary>
		/// Displays all the info
		/// </summary>
		[ConfigOption] private readonly int endGameTime = 10;
		[ConfigOption] private readonly int messageRefreshTime = 10;
		[ConfigOption] private readonly int messageTime = 3;
		/// <summary>
		/// How much pvp do we want? ~5-15 (the higher, the less pvp)
		/// </summary>
		[ConfigOption] private readonly int spawnsUntilChaos = 9;
		[ConfigOption] private readonly bool disableChaosSpawns = false;
		[ConfigOption] private readonly int scpSpawnTime = 10;

		[ConfigOption] private readonly bool showGameStartMessage = true;
		[ConfigOption] public readonly bool useDefaultConfig = true;

		private const string ItemSpawnDataFileLocation = "sm_plugins/ItemSpawnPointData.txt";
		private const string PlayerSpawnDataFileLocation = "sm_plugins/PlayerSpawnPointData.txt";
		private readonly FieldInfo cachedPlayerConnFieldInfo = typeof(SmodPlayer).GetField("conn", BindingFlags.NonPublic | BindingFlags.Instance);

		// Item Spawns

		private SpawnData itemSpawnData;
		private SpawnData ItemSpawnData => itemSpawnData ?? (itemSpawnData = new SpawnData());

		private ItemRoomData itemRoomData;
		private ItemRoomData ItemRoomData
		{
			get
			{
#if EDITOR_MODE
				return itemRoomData ?? (itemRoomData = new ItemRoomData(this));
#else
				return itemRoomData ?? (itemRoomData = new ItemRoomData());
#endif
			}
		}

		private List<Player> deadSCP;
		private List<Player> DeadSCP => deadSCP ?? (deadSCP = new List<Player>());

		// Player Spawning

		[ConfigOption] private readonly float safeSpawnDistance = 30;
		[ConfigOption] private readonly float safeScpSpawnDistance = 60;

		private SpawnData playerSpawnData;
		private SpawnData PlayerSpawnData => playerSpawnData ?? (playerSpawnData = new SpawnData());

		private List<SpawnPoint> playerLoadedSpawns;
		private List<SpawnPoint> PlayerLoadedSpawns => playerLoadedSpawns ?? (playerLoadedSpawns = new List<SpawnPoint>());

		private List<SavedSpawnData> spawnGroup;
		private List<SavedSpawnData> SpawnGroup => spawnGroup ?? (spawnGroup = new List<SavedSpawnData>());

		private class SavedSpawnData
		{
			public readonly int ID;
			public readonly Vector SpawnLocation;
			public readonly Role Role;

			public SavedSpawnData(int id, Vector spawnLocation, Role role)
			{
				ID = id;
				SpawnLocation = spawnLocation;
				Role = role;
			}
		}

		//+ Round Data

		/// <summary>
		/// Counts the deaths to see if it's game over.
		/// </summary>
		private int playerDeathCounter;

		/// <summary>
		/// Set at the start of game.
		/// </summary>
		private int maxAmountOfLives;

		private bool isRoundStarted;
		private bool femurBroke;

		private bool triggerEnding;
		private float endingTriggerTimer;

		public int GeneratorCounter;
		private int spawnedChaos;
		private float scpSpawnTimer;

		private Broadcast cachedBroadcast;
		private Vector cachedChaosSpawn;

		/// <summary>
		/// Timer between messages to keep updating the messages without spamming.
		/// </summary>
		private float messageTimer;

		private List<ScpPlayer> scpPlayers;
		private List<ScpPlayer> ScpPlayers => scpPlayers ?? (scpPlayers = new List<ScpPlayer>());

		private class ScpPlayer
		{
			public readonly string Name;
			public bool IsDead;

			public ScpPlayer(string name)
			{
				Name = name;
				IsDead = false;
			}
		}

		// Plugin Methods

		public override void OnDisable() => Info(Details.name + " was disabled");
		public override void OnEnable()
		{
			cachedPlayerPos = new List<Vector>(Server.MaxPlayers);
			LoadItemData();
			LoadPlayerData();
			Info($"{Details.name} has loaded, type 'fe' for details.");
		}
		public override void Register()
		{
			AddEventHandlers(new Events(this));

			var com = new SpawningCommands(this);
			AddEventHandlers(com);
			AddCommand("afss", com);
		}

		public void PlayerJoin(PlayerJoinEvent ev)
		{
			if (showGameStartMessage)
			{
				try
				{
					PersonalBroadcast(ev.Player, 10,
						$"<size={ServerInfoSize}><color={ServerInfoColor}>Welcome to <color={ServerHighLightColor}>Scattered Survival v{ModVersion}!</color> Press ` to open the console and enter '<color={ServerHighLightColor}>.help</color>' for mod information!</color></size>");
					//PersonalBroadcast(ev.Player, 10,
					//	$"<size={ServerInfoSize}><color={ServerInfoColor}>If you like the plugin, join the discord for updates!\n <color={ServerHighLightColor}>https://discord.gg/DunUU82</color></color></size>");
				}
				catch
				{
					Info("Null ref on joining player. Ignoring...");
				}
			}
		}

		/// <summary>
		/// Called when a player disconnects
		/// Restarts the server whenever there's only 1 or less players.
		/// </summary>
		public void CheckNotEnoughPlayers()
		{
#if !EDITOR_MODE
			if (Server.NumPlayers <= 1)
			{
				Server.Round.RestartRound();
			}
#endif
		}

		#region New Game

		/// <summary>
		/// Called on WaitingForPlayers.
		/// </summary>
		public void ResetRound()
		{
			if (disablePlugin)
			{
				PluginManager.DisablePlugin(this);
				return;
			}

			playerDeathCounter = 0;
			GeneratorCounter = 0;
			cachedBroadcast = GameObject.Find("Host").GetComponent<Broadcast>();
			cachedChaosSpawn = Server.Map.GetSpawnPoints(Role.CHAOS_INSURGENCY)[0];
			spawnedChaos = disableChaosSpawns ? int.MaxValue : spawnsUntilChaos - 1;
			scpSpawnTimer = scpSpawnTime;
			femurBroke = false;
			DeadSCP.Clear();
			SpawnGroup.Clear();
			ScpPlayers.Clear();

			// Reset rooms
			ItemRoomData.Reset();

			//Get rooms
			LoadZone("EntranceRooms", ZoneType.ENTRANCE);
			LoadZone("HeavyRooms", ZoneType.HCZ);
			LoadZone("LightRooms", ZoneType.LCZ);

			// Load right after getting rooms to set their extra data
			SpawnDataIO.LoadItemRoomData(ItemRoomData);

			// Player spawn
			PlayerLoadedSpawns.Clear();

			var playerPointCount = PlayerSpawnData.Points.Count;
			var itemPointCount = ItemSpawnData.Points.Count;
			var roomCount = ItemRoomData.Rooms.Count;

			// Create player spawn points on map
			for (var i = 0; i < roomCount; i++)
			{
				var r = ItemRoomData.Rooms[i];

				for (var j = 0; j < playerPointCount; j++)
				{
					var p = PlayerSpawnData.Points[j];

					if (p.RoomType == r.Name && p.ZoneType == r.Zone)
					{
						PlayerLoadedSpawns.Add(new SpawnPoint(p.RoomType, p.ZoneType,
							Tools.Vec3ToVector(r.Transform.TransformPoint(Tools.VectorTo3(p.Position))) + new Vector(0, 0.3f, 0),
							Tools.Vec3ToVector(r.Transform.TransformDirection(Tools.VectorTo3(p.Rotation)))));
					}
				}
			}

			cachedFreeRoom = new List<SpawnPoint>(PlayerLoadedSpawns.Count);

			// Create item spawn points on map
#if EDITOR_MODE
			var spawnCounter = 0;
#endif
			for (var j = 0; j < itemPointCount; j++)
			{
				var p = ItemSpawnData.Points[j];

				for (var i = 0; i < roomCount; i++)
				{
					var r = ItemRoomData.Rooms[i];

					if (p.RoomType != r.Name) continue;

#if EDITOR_MODE
					spawnCounter++;
#endif

					r.ItemSpawnPoints.Add(new ItemSpawnPoint(p.RoomType, p.ZoneType,
						Tools.Vec3ToVector(r.Transform.TransformPoint(Tools.VectorTo3(p.Position))) + new Vector(0, 0.1f, 0),
						Tools.Vec3ToVector(r.Transform.TransformDirection(Tools.VectorTo3(p.Rotation)))));
				}
			}

#if EDITOR_MODE
			var lcz = 0;
			var ent = 0;
			foreach (var item in PlayerLoadedSpawns)
			{
				switch (item.ZoneType)
				{
					case ZoneType.ENTRANCE:
						ent++;
						break;
					case ZoneType.LCZ:
						lcz++;
						break;
				}
			}

			Info("Round has been reloaded.\n" +
				 $"Found {ItemRoomData.Rooms.Count} Rooms.\n" +
				 $"Created {ent} Entrance player spawn points\n" +
				 $"Created {lcz} LCZ player spawn points\n" +
				 $"Created {spawnCounter} item spawn points\n");
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string FormatRoomString(string name)
		{
			var length = name.Length;
			for (var i = 0; i < length; i++)
			{
				if (name[i] == '(')
				{
					return name.Remove(i - 1, length - i + 1);
				}
			}
			return name;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void LoadZone(string name, ZoneType zone)
		{
			var zoneObj = GameObject.Find(name).transform;
			for (int i = 0; i < zoneObj.childCount; i++)
			{
				var child = zoneObj.GetChild(i);
				if (child.gameObject.activeSelf) ItemRoomData.AddRoom(new CustomRoom(child, FormatRoomString(child.name), zone));
			}
		}

		private List<SpawnPoint> cachedFreeRoom;
		public void BeforeSpawn()
		{
			// Spawn game start items
			ItemRoomData.LevelLoaded();

			cachedFreeRoom.AddRange(PlayerLoadedSpawns);
		}

		/// <summary>
		/// Called for every player
		/// Assigns player roles and spawn locations if they are not SCP.
		/// </summary>
		public void AssignTeam(PlayerInitialAssignTeamEvent ev)
		{
			var player = ev.Player;
			var team = ev.Team;

#if EDITOR_MODE
			Info($"Game assigned {player.ID} to spawn as {team}");
#endif

			if (team != Smod2.API.Team.SCP)
			{
				var players = Server.GetPlayers();
				var playerCount = players.Count;

				if (playerLoadedSpawns.Count >= playerCount)
				{
					spawnedChaos++;
					if (spawnedChaos == spawnsUntilChaos)
					{
						spawnedChaos = 0;
						ev.Team = Smod2.API.Team.CHAOS_INSURGENCY;
						SpawnGroup.Add(new SavedSpawnData(player.PlayerId, cachedChaosSpawn, Role.CHAOS_INSURGENCY));
#if EDITOR_MODE
						Info($"Plugin assigned {player.ID} to spawn as Chaos Insurgency");
#endif
					}
					else
					{
						// Assign spawn points.
						int randomN = Random.Range(0, cachedFreeRoom.Count);
						var point = cachedFreeRoom[randomN];
						cachedFreeRoom.RemoveAt(randomN);
						SpawnGroup.Add(new SavedSpawnData(player.PlayerId, point.Position, Role.SCIENTIST));
#if EDITOR_MODE
						Info($"Plugin assigned {player.ID} to spawn as {role}");
#endif
					}
				}
			}
		}

		public void RoundStart()
		{
#if EDITOR_MODE
			Info("Round started");
#endif

			isRoundStarted = true;
			cachedFreeRoom.Clear();

			// Set player Lives
			var scpCount = Round.Stats.SCPStart;
			switch (scpCount)
			{
				case 0:
				case 1:
					maxAmountOfLives = 5;
					break;
				case 2:
					maxAmountOfLives = scpCount * 7;
					break;
				case 3:
					maxAmountOfLives = scpCount * 9;
					break;
				default:
					maxAmountOfLives = scpCount * 10;
					break;
			}

			// Saved the SCP players
			var players = Server.GetPlayers();
			var numPlayers = players.Count;
			for (int i = 0; i < numPlayers; i++)
			{
				var p = players[i];
				if (p.TeamRole.Team == Smod2.API.Team.SCP)
				{
					ScpPlayers.Add(new ScpPlayer(p.Name));
					PersonalBroadcast(p, 10,
						$"<size={ClassInfoSize}><color={ClassInfoColor}>Kill the players <color={WinColor}>{(maxAmountOfLives - playerDeathCounter)}</color> times to win.</color></size>");
				}
				// Send broadcast for roles.
				else if (p.TeamRole.Role == Role.SCIENTIST)
				{
					PersonalBroadcast(p, 10, $"<size={ClassInfoSize}><color={ClassInfoColor}>Kill the SCP before your lives reach 0. Lives are shared.</color></size>");
				}
				else if (p.TeamRole.Role == Role.CHAOS_INSURGENCY)
				{
					PersonalBroadcast(p, 10, $"<size={ClassInfoSize}><color={ClassInfoColor}>Set off the nuke to win before lives reach 0. Everyone is your enemy.</color></size>");
				}
			}

			messageTimer = 0;
		}

		#endregion

		#region GamePlay

		private List<Vector> cachedPlayerPos;
		/// <summary>
		/// Gets called when a group of dead players respawns.
		/// If an SCP died, turn them into the computer.
		/// If SCP079 died. Freeze as spectator.
		/// In this one instant, find all other spawn points, roles, and spawn items.
		/// </summary>
		public void TeamRespawn(TeamRespawnEvent ev)
		{
			var playersSpawning = ev.PlayerList;
			ev.SpawnChaos = false;

			if (triggerEnding)
			{
				playersSpawning.Clear();
				return;
			}

#if EDITOR_MODE
			Info($"Respawning {ev.PlayerList.Count} players");
#endif

			var spawnCount = playersSpawning.Count;
			SpawnGroup.Clear();

			// Get Spawn points and roles for everyone else.
			if (spawnCount <= 0 || playerLoadedSpawns.Count < spawnCount) return;

			// Get Player positions
			var players = Server.GetPlayers();
			var playerCount = players.Count;
			cachedPlayerPos.Clear();
			for (int i = 0; i < playerCount; i++)
			{
				cachedPlayerPos.Add(players[i].GetPosition());
			}

			// Find spawns that have no players or SCP near
			cachedFreeRoom.Clear();
			var loadedSpawnCount = playerLoadedSpawns.Count;
			var distance = safeSpawnDistance;
			var firstLoop = true;

			while (cachedFreeRoom.Count < spawnCount)
			{
				for (var i = 0; i < loadedSpawnCount; i++)
				{
					var loadedSpawnPoint = playerLoadedSpawns[i];

					if (!firstLoop && !loadedSpawnPoint.IsFreePoint) continue;

					var isPlayerIn = false;
					for (var j = 0; j < playerCount; j++)
					{
						var currentPlayerPos = cachedPlayerPos[j];
						var d = Vector.Distance(currentPlayerPos, loadedSpawnPoint.Position);
						if (players[j].TeamRole.Team == Smod2.API.Team.SCP)
						{
							if (d < safeScpSpawnDistance)
							{
								isPlayerIn = true;
								break;
							}
						}
						else if (d < distance)
						{
							isPlayerIn = true;
							break;
						}
					}

					if (!isPlayerIn)
					{
						cachedFreeRoom.Add(loadedSpawnPoint);
						loadedSpawnPoint.IsFreePoint = false;
					}
					else
					{
						loadedSpawnPoint.IsFreePoint = true;
					}
				}

				// Reduce distance check by 5 until we have enough spawn points to spawn everyone.
				distance = Mathf.Clamp((int)(distance - 5), 0, 500);
				firstLoop = false;
			}

			// Assign spawn points.
			var entrance = 0;
			var lcz = 0;
			for (var i = 0; i < spawnCount; i++)
			{
				var spawningPlayer = playersSpawning[i];

				if (spawningPlayer.TeamRole.Team == Smod2.API.Team.SCP) continue;

				spawnedChaos++;
				if (spawnedChaos == spawnsUntilChaos)
				{
					spawnedChaos = 0;
					SpawnGroup.Add(new SavedSpawnData(spawningPlayer.PlayerId, cachedChaosSpawn,
						Role.CHAOS_INSURGENCY));

#if EDITOR_MODE
					Info($"Plugin assigned {spawningPlayer.ID} to spawn as Chaos Insurgency");
#endif
				}
				else
				{
					int randomN = Random.Range(0, cachedFreeRoom.Count);
					var point = cachedFreeRoom[randomN];

					SpawnGroup.Add(new SavedSpawnData(spawningPlayer.PlayerId, point.Position, Role.SCIENTIST));

					cachedFreeRoom.RemoveAt(randomN);

#if EDITOR_MODE
					Info($"Adding {spawningPlayer.ID} as {role}");
#endif

					if (point.ZoneType == ZoneType.ENTRANCE)
					{
						entrance++;
					}
					else
					{
						lcz++;
					}
				}
			}

			// Spawn safe items
			var hasLcz = lcz > 0;
			var hasEnt = entrance > 0;
			if (!hasLcz && !hasEnt) return;
			ItemRoomData.CheckSpawns();

			if (hasLcz)
			{
				ItemRoomData.SpawnSafeItems(ZoneType.LCZ, lcz);
			}
			if (hasEnt)
			{
				ItemRoomData.SpawnSafeItems(ZoneType.ENTRANCE, entrance);
			}
		}

		public void PlayerSpawn(PlayerSpawnEvent ev)
		{
			if (triggerEnding) return;
			var player = ev.Player;
			var playerId = player.PlayerId;

			var spawnCount = SpawnGroup.Count;
			for (var i = 0; i < spawnCount; i++)
			{
				var p = SpawnGroup[i];

				if (p.ID != playerId) continue;

				ev.SpawnPos = p.SpawnLocation;

#if EDITOR_MODE
				Info($"Spawning: Found match for {player.ID}");
#endif
				return;
			}

#if EDITOR_MODE
			Info($"Spawning: No match found for {player.ID}");
#endif
		}

		public void Update()
		{
			var timer = Time.deltaTime;
			if (!isRoundStarted) return;
			if (triggerEnding)
			{
				endingTriggerTimer -= timer;

				if (!(endingTriggerTimer <= 0)) return;

				triggerEnding = false;
				isRoundStarted = false;
				Server.Round.RestartRound();
			}
			else
			{
				if (DeadSCP.Count > 0)
				{
					scpSpawnTimer += timer;
					if (scpSpawnTimer >= scpSpawnTime)
					{
						foreach (var scp in DeadSCP)
						{
							scp.ChangeRole(Role.SCP_079);
						}

						DeadSCP.Clear();
					}
				}

				// Refresh Message
				messageTimer += timer;

				if (!(messageTimer >= messageRefreshTime)) return;

				messageTimer = 0;
				Broadcast((uint)messageTime, $"Lives left: <color={WinColor}>{maxAmountOfLives - playerDeathCounter}</color>");
			}
		}

		public void SetRole(PlayerSetRoleEvent ev)
		{
			if (triggerEnding)
			{
				ev.Role = Role.SPECTATOR;
				return;
			}

			var playerId = ev.Player.PlayerId;

			var spawnCount = SpawnGroup.Count;
			for (var i = 0; i < spawnCount; i++)
			{
				var p = SpawnGroup[i];

				if (p.ID != playerId) continue;

				ev.Role = p.Role;
#if EDITOR_MODE
				Info($"Setting {ev.Player.ID}'s role to {p.Role}");
#endif

				if (p.Role == Role.CHAOS_INSURGENCY)
				{
					var players = Server.GetPlayers();
					var playerCount = players.Count;
					for (int j = 0; j < playerCount; j++)
					{
						var player = players[j];
						if (player.TeamRole.Team == Smod2.API.Team.CHAOS_INSURGENCY && player.PlayerId != ev.Player.PlayerId)
						{
							PersonalBroadcast(player, 5, "<color=#0c9126>Chaos Insurgency have spawned to aid you!");
						}
					}
				}

				return;
			}
		}

		/// <summary>
		/// Increased death counter, check for game over.
		/// </summary>
		public void PlayerDied(PlayerDeathEvent ev)
		{
#if EDITOR_MODE
			Info($"{ev.Player.ID} died.");
#endif
			var player = ev.Player;

			// Remove the team spawn
			var length = SpawnGroup.Count;
			for (var i = 0; i < length; i++)
			{
				if (SpawnGroup[i].ID != player.PlayerId) continue;

				SpawnGroup.RemoveAt(i);
				break;
			}

			if (!triggerEnding && ev.Killer.TeamRole.Team != Smod2.API.Team.NONE)
			{
				// Not SCP Death
				if (player.TeamRole.Team != Smod2.API.Team.SCP)
				{
					// killed by SCP or yourself?
					if (ev.Killer.TeamRole.Team == Smod2.API.Team.SCP ||
						ev.Killer.TeamRole.Team == player.TeamRole.Team ||
						ev.Killer.Name == player.Name)
					{
						playerDeathCounter++;

						// Check SCP win
						if (!triggerEnding && playerDeathCounter >= maxAmountOfLives)
						{
							TriggerEnding($"<size={WinTextSize}><color={WinColor}>SCP Win!</color></size>\n Players ran out of lives.");
							return;
						}

						messageTimer = 0;
						ClearBroadcasts();
						Broadcast((uint)messageTime, $"Lives left: <color={WinColor}>{maxAmountOfLives - playerDeathCounter}</color>");
					}

					if (!isRoundStarted) return;

					ItemRoomData.CheckSpawns();
					ItemRoomData.PlayerDead();
				}
				// If SCP check to see if only SCP079 left, if they are, kill them all for SCP loss
				else if (player.TeamRole.Team == Smod2.API.Team.SCP)
				{
					var scpCount = ScpPlayers.Count;
					var aliveScPs = 0;

					for (var i = 0; i < scpCount; i++)
					{
						var scp = ScpPlayers[i];

						if (scp.Name == player.Name)
						{
							scp.IsDead = true;
							DeadSCP.Add(player);
							scpSpawnTimer = 0;

#if EDITOR_MODE
							Info($"Settings {scp.ID} to dead");
#endif
						}
						else if (!scp.IsDead)
						{
							aliveScPs++;
						}
					}

					if (aliveScPs > 0) return;

					TriggerEnding($"<size={WinTextSize}><color={WinColor}>Scientists Win!</color></size>\n All SCP have been contained");
				}
			}
		}

		/// <summary>
		/// Instead of killing him instantly, damage him for half his current HP
		/// </summary>
		public void Contain106(PlayerContain106Event ev)
		{
			//if (!femurBroke)
			//{
			femurBroke = true;

			var scps = Server.GetPlayers(Role.SCP_106);
			var scpCount = scps.Count;

			for (int i = 0; i < scpCount; i++)
			{
				var scp = scps[i];
				scp.Damage((int) (scp.GetHealth() / 2), DamageType.NONE);
			}

			Broadcast(5, $"<color={WinColor}>SCP 106 has been damaged for half their current HP!</color>");
		}

		/// <summary>
		/// Called when nuke goes off
		/// </summary>
		public void Boom() => TriggerEnding($"<size={WinTextSize}><color={WinColor}>Chaos Insurgency Wins!</color></size>\n They set off the nuke.");

		/// <summary>
		/// Check for zombie end
		/// </summary>
		public void Zombie(PlayerRecallZombieEvent ev)
		{
			var players = Server.GetPlayers();
			var playerCount = players.Count;

			var scpCounter = 1;

			for (int i = 0; i < playerCount; i++)
			{
				if (players[i].TeamRole.Team == Smod2.API.Team.SCP)
				{
					scpCounter++;
				}
			}

			if (scpCounter == playerCount)
			{
				TriggerEnding(
					$"<size={WinTextSize}><color={WinColor}>SCP Wins!</color></size>\n All players were converted to SCP!");
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void TriggerEnding(string message)
		{
			triggerEnding = true;
			endingTriggerTimer = endGameTime;

			foreach (var item in Server.GetPlayers())
			{
				item.Kill(DamageType.NONE);
			}

			ClearBroadcasts();
			Broadcast((uint)endGameTime, message);
		}

		#endregion

		#region Item Spawn Editing

		public void SaveItemData() => SpawnDataIO.Save(ItemSpawnData, ItemSpawnDataFileLocation);

		public void LoadItemData() => itemSpawnData = SpawnDataIO.Open(ItemSpawnDataFileLocation);

		public string AddItemPoint(PosVectorPair point)
		{
			var playerPos = Tools.VectorTo3(point.Position);
			var closestRoom = FindClosestRoomToPoint(playerPos);
			var roomName = closestRoom.Name;
			ItemSpawnData.Points.Add(new SpawnPoint(roomName, closestRoom.Zone, Tools.Vec3ToVector(closestRoom.Transform.InverseTransformPoint(playerPos)),
				Tools.Vec3ToVector(closestRoom.Transform.InverseTransformDirection(Tools.VectorTo3(point.Rotation)))));
			return $"Created Item spawn point in {roomName}  ({closestRoom.Zone.ToString()})";
		}

		#endregion

		#region Player Spawn Editing

		public void SavePlayerData() => SpawnDataIO.Save(PlayerSpawnData, PlayerSpawnDataFileLocation);

		public void LoadPlayerData() => playerSpawnData = SpawnDataIO.Open(PlayerSpawnDataFileLocation);

		public string AddPlayerPoint(PosVectorPair point)
		{
			var playerPos = Tools.VectorTo3(point.Position);
			var closestRoom = FindClosestRoomToPoint(playerPos);
			var roomName = closestRoom.Name;
			PlayerSpawnData.Points.Add(new SpawnPoint(roomName, closestRoom.Zone, Tools.Vec3ToVector(closestRoom.Transform.InverseTransformPoint(playerPos)),
				Tools.Vec3ToVector(closestRoom.Transform.InverseTransformDirection(Tools.VectorTo3(point.Rotation)))));
			return $"Created Player spawn point in {roomName}  ({closestRoom.Zone.ToString()})";
		}

		/// <summary>
		/// Shared between player and item spawn editing.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private CustomRoom FindClosestRoomToPoint(Vector3 point)
		{
			// Find closest room
			CustomRoom closestRoom = null;
			float distanceToClosest = 10000;

			var roomLength = ItemRoomData.Rooms.Count;

			for (int i = 0; i < roomLength; i++)
			{
				var r = ItemRoomData.Rooms[i];
				var distance = Vector3.Distance(point, r.Transform.position);

				if (distance < distanceToClosest)
				{
					closestRoom = r;
					distanceToClosest = distance;
				}
			}

			return closestRoom;
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
#if EDITOR_MODE
			ServerConsole.AddLog($"Broadcasted: {message} to: {player.ID}");
#endif
		}
		private void ClearBroadcasts() => cachedBroadcast.CallRpcClearElements();
		private void Broadcast(uint duration, string message)
		{
			cachedBroadcast.CallRpcAddElement(message, duration, false);
#if EDITOR_MODE
			ServerConsole.AddLog($"Broadcasted: {message}");
#endif
		}

		#endregion
	}
}
