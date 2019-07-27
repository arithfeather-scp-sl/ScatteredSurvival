using Smod2.API;
using Smod2.Commands;
using Smod2.EventHandlers;
using Smod2.Events;
using UnityEngine;

namespace ArithFeather.ScatteredSurvival
{
	public class SpawningCommands : ICommandHandler, IEventHandlerCallCommand
	{
		private readonly ScatteredSurvival plugin;

		public SpawningCommands(ScatteredSurvival plugin) => this.plugin = plugin;

		public string GetCommandDescription() =>
				   "\nss p ADD - Create Player Spawns\n" +
				   "ss p save - Save Player spawns\n" +
				   "ss p load - Load Player spawns\n" +
				   "ss i ADD - Create Item Spawns\n" +
				   "ss i save - Save Item spawns\n" +
				   "ss i load - Load Item spawns";

		public string GetUsage() => "Creates spawn points for players and items";

		/// <summary>
		/// Server(Console) Commands
		/// </summary>
		public string[] OnCall(ICommandSender sender, string[] args)
		{
			if (args[0].ToUpper() == "P")
			{
				switch (args[1].ToUpper())
				{
					case "ADD":
						return AddThings(plugin.Server.GetPlayers()[0], args);
					case "SAVE":
						plugin.SavePlayerData();
						return new[] { "Saved Points" };
					case "LOAD":
						plugin.LoadPlayerData();
						return new[] { "Points Loaded" };
				}
			}
			else if (args[0].ToUpper() == "I")
			{
				switch (args[1].ToUpper())
				{
					case "ADD":
						return AddThings(plugin.Server.GetPlayers()[0], args);
					case "SAVE":
						plugin.SaveItemData();
						return new[] { "Saved Points" };
					case "LOAD":
						plugin.LoadItemData();
						return new[] { "Points Loaded" };
				}
			}

			return new[] { GetCommandDescription() };
		}

		private string[] AddThings(Player player, string[] args)
		{
			switch (args[0].ToUpper())
			{
				case "P":
				{
					var scp049Component = ((GameObject)player.GetGameObject()).GetComponent<Scp049PlayerScript>();
					Vector3 plyRot = scp049Component.plyCam.transform.forward;
					var where = Tools.VectorTo3(player.GetPosition());
					if (@where.Equals(Vector3.zero))
					{
						return new[] { "Error creating spawn point." };
					}

					Vector rotation = new Vector(-plyRot.x, plyRot.y, -plyRot.z), position = Tools.Vec3ToVector(@where) + (Vector.Up * 0.1f);
					return new[] { plugin.AddPlayerPoint(new PosVectorPair(position, rotation)) };
				}

				case "I":
				{
					var scp049Component = ((GameObject)player.GetGameObject()).GetComponent<Scp049PlayerScript>();
					var scp106Component = (player.GetGameObject() as GameObject)?.GetComponent<Scp106PlayerScript>();
					var cameraRotation = scp049Component.plyCam.transform.forward;

					if (scp106Component == null) return new[] {GetCommandDescription()};

					Physics.Raycast(scp049Component.plyCam.transform.position, cameraRotation, out RaycastHit where,
						40f, scp106Component.teleportPlacementMask);

					if (@where.point.Equals(Vector3.zero))
					{
						return new[] {"Error creating spawn point."};
					}

					Vector rotation = new Vector(-cameraRotation.x, cameraRotation.y, -cameraRotation.z),
						position = Tools.Vec3ToVector(@where.point) + (Vector.Up * 0.1f);
					return new[] {plugin.AddItemPoint(new PosVectorPair(position, rotation))};
				}

				default:
					return new[] { GetCommandDescription() };
			}
		}

		public void OnCallCommand(PlayerCallCommandEvent ev)
		{
			if (ev.Command == "help")
			{
				ev.ReturnMessage = "Scattered Survival Mode v1.00\n" +
				                   "https://discord.gg/DunUU82\n" +
				                   "Scientists and ClassD are working together to defeat the SCP and Chaos.\n" +
				                   "They will spawn randomly in Entrance and Light Containment Zone.\n" +
				                   "\n" +
				                   "SCP win if the player's lives reach 0. Lives are shared between class D and Scientists.\n" +
				                   "\n" +
				                   "There is a chance for Chaos to spawn.\n" +
				                   "They automatically win if they set off the nuke. Stop them.\n" +
				                   "\n" +
				                   "Items will spawn randomly across the map.\n" +
				                   "You can not escape the facility. Decontamination is disabled.\n" +
				                   "Unless all 5 generators are active,dead SCP will respawn as SCP079 with 3x experience gain. SCP079 does not need to be killed to win.\n" +
								   "SCP106 pelvis breaker no longer kills 106. Instead, it reduces their current HP by half.\n" +
				                   "--Rules--\n" +
				                   "1. Friendly fire is on, please don't shoot your teammates on purpose.\n" +
				                   "Teams are: 1 - Scientists and Class D. 2 - SCP. 3 - Chaos Insurgency\n" +
				                   "2.Please don't group up with other teams.\n" +
				                   "3.Don't harass other players. Instant ban\n" +
				                   "4.Players found cheating will be recorded and reported for global ban.";
			}

#if EDITOR_MODE
                var message = AddThings(ev.Player, new[] { ev.Command })[0];
                ev.ReturnMessage = message;
                plugin.Info(message);
#endif
		}
	}
}