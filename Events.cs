using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.EventSystem.Events;

namespace ArithFeather.ScatteredSurvival
{
	public class Events : IEventHandlerWaitingForPlayers, IEventHandlerRoundStart, IEventHandlerSpawn,
        IEventHandlerUpdate, IEventHandlerSetConfig, IEventHandlerSetRole, IEventHandlerPlayerDie,
        IEventHandler079AddExp, IEventHandlerTeamRespawn, IEventHandlerPlayerJoin, IEventHandlerInitialAssignTeam,
        IEventHandlerGeneratorFinish, IEventHandlerCheckEscape, IEventHandlerWarheadDetonate,
        IEventHandlerDecideTeamRespawnQueue, IEventHandlerDisconnect, IEventHandlerRecallZombie,
		IEventHandlerContain106, IEventHandlerPlayerHurt

    {
        private ScatteredSurvival plugin;

        public Events(ScatteredSurvival plugin) => this.plugin = plugin;

        public void OnSetRole(PlayerSetRoleEvent ev) => plugin.SetRole(ev);
        public void OnPlayerDie(PlayerDeathEvent ev) => plugin.PlayerDied(ev);
        public void OnWaitingForPlayers(WaitingForPlayersEvent ev) => plugin.ResetRound();
        public void OnUpdate(UpdateEvent ev) => plugin.Update();
        public void OnRoundStart(RoundStartEvent ev) => plugin.RoundStart();
        public void OnSpawn(PlayerSpawnEvent ev) => plugin.PlayerSpawn(ev);
        public void On079AddExp(Player079AddExpEvent ev) => ev.ExpToAdd *= 20;
        public void OnTeamRespawn(TeamRespawnEvent ev) => plugin.TeamRespawn(ev);
        public void OnGeneratorFinish(GeneratorFinishEvent ev) => plugin.GeneratorCounter++;
        public void OnCheckEscape(PlayerCheckEscapeEvent ev) => ev.AllowEscape = false;
        public void OnDetonate() => plugin.Boom();
        public void OnPlayerJoin(PlayerJoinEvent ev) => plugin.PlayerJoin(ev);
        public void OnAssignTeam(PlayerInitialAssignTeamEvent ev) => plugin.AssignTeam(ev);
        public void OnDecideTeamRespawnQueue(DecideRespawnQueueEvent ev) => plugin.BeforeSpawn();
        public void OnDisconnect(DisconnectEvent ev) => plugin.CheckNotEnoughPlayers();
        public void OnRecallZombie(PlayerRecallZombieEvent ev) => plugin.Zombie(ev);
		public void OnContain106(PlayerContain106Event ev) => plugin.Contain106(ev);
		public void OnPlayerHurt(PlayerHurtEvent ev)
		{
			if (ev.Player.TeamRole.Role == Role.SCP_106 && ev.DamageType == DamageType.CONTAIN) ev.Damage = 0;
		}

		/// <summary>
		/// Changing Config values.
		/// Team Respawn Queue 34034304343403434034
		/// Friendly Fire true
		/// 
		/// </summary>
		public void OnSetConfig(SetConfigEvent ev)
        {
            switch (ev.Key)
            {
                case "start_round_minimum_players":
                    ev.Value = 3;
                    break;
                case "manual_end_only":
                    ev.Value = true;
					break;
				case "team_respawn_queue":
					if (plugin.useDefaultConfig) ev.Value = "34034304343403434034";
					break;
				case "maximum_MTF_respawn_amount":
					ev.Value = 100;
					break;
                case "smart_class_picker":
                    ev.Value = false;
                    break;
                case "disable_dropping_empty_boxes":
                    ev.Value = true;
                    break;

                case "disable_decontamination":
                    ev.Value = true;
                    break;
                case "cassie_respawn_announcements":
                    ev.Value = false;
                    break;

                case "antifly_enable":
                    ev.Value = false;
                    break;
                case "anti_player_wallhack":
                    ev.Value = false;
                    break;

                case "scp079_disable":
                    ev.Value = true;
                    break;

				// Class Editing
				case "default_item_scientist":
					if (plugin.useDefaultConfig) ev.Value = new[] { 19 };
					break;
				case "default_item_ci":
					if (plugin.useDefaultConfig) ev.Value = new[] { 10, 12, 14, 13, 25, 19, 24 };
					break;
			}
        }
    }
}
