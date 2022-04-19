using Exiled.API.Features;

namespace DCReplace
{
	public class DCReplace : Plugin<Config>
	{
		private EventHandlers ev;

		private bool state = false;

		internal static DCReplace instance;

		public override void OnEnabled()
		{
			if (state) return;

			if (!Config.IsEnabled) return;

            instance = this;

            ev = new EventHandlers();

			Exiled.Events.Handlers.Player.Left += ev.OnPlayerLeave;
			Exiled.Events.Handlers.Scp106.Containing += ev.OnContain106;
			Exiled.Events.Handlers.Server.RoundStarted += ev.OnRoundStart;
			Exiled.Events.Handlers.Player.Spawning += ev.OnSpawning;
			Exiled.Events.Handlers.Player.ChangingRole += ev.OnChangeRole;
			Exiled.Events.Handlers.Server.RoundEnded += ev.OnRoundEnd;
			Exiled.Events.Handlers.Scp106.Containing += ev.OnContain106;

			state = true;
			base.OnEnabled();
		}

		public override void OnDisabled()
		{
			if (!state) return;

			instance = null;

			Exiled.Events.Handlers.Player.Left -= ev.OnPlayerLeave;
			Exiled.Events.Handlers.Scp106.Containing -= ev.OnContain106;
			Exiled.Events.Handlers.Server.RoundStarted -= ev.OnRoundStart;
			Exiled.Events.Handlers.Player.Spawning -= ev.OnSpawning;
			Exiled.Events.Handlers.Player.ChangingRole -= ev.OnChangeRole;
			Exiled.Events.Handlers.Server.RoundEnded -= ev.OnRoundEnd;
			Exiled.Events.Handlers.Scp106.Containing -= ev.OnContain106;

			ev = null;

			state = false;
			base.OnDisabled();
		}

		public override string Name => "DcReplace";
	}
}
