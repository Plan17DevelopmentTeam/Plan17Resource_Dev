
using Virial.Game;

namespace Plana.Roles.Modifier;

public class PMini : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier, IAssignableDocument
{
    private PMini() : base("mini", "mi", Virial.Color.White, [growupneedtime, MiniDeadEndOption,MiniCannotGuessOption,MiniCooldownSpeedBuff,InMiniBuffRatio,NoMiniBuffRatio])
    {
    }

    Citation? HasCitation.Citation => Citations.TheOtherRoles;

    static public FloatConfiguration growupneedtime = NebulaAPI.Configurations.Configuration(
        "options.role.mini.growupneedtime",
        (30f, 3000f, 30f),
        600f,
        FloatConfigurationDecorator.Second
    );
    static BoolConfiguration MiniDeadEndOption = NebulaAPI.Configurations.Configuration("options.role.mini.MiniDeadEnd", false);
    static BoolConfiguration MiniCannotGuessOption = NebulaAPI.Configurations.Configuration("options.role.mini.minicannotguess", true);
    static BoolConfiguration MiniCooldownSpeedBuff = NebulaAPI.Configurations.Configuration("options.role.mini.cooldownspeedbuff", false);
    static FloatConfiguration InMiniBuffRatio = NebulaAPI.Configurations.Configuration("options.role.mini.InMiniRatio", (0f, 5f, 0.125f), 0.5f, FloatConfigurationDecorator.Ratio,()=>MiniCooldownSpeedBuff);
    static FloatConfiguration NoMiniBuffRatio = NebulaAPI.Configurations.Configuration("options.role.mini.noMiniRatio", (0f, 5f, 0.125f), 1.25f, FloatConfigurationDecorator.Ratio,()=>MiniCooldownSpeedBuff);
    static public PMini MyRole = new PMini();
    bool IAssignableDocument.HasTips => true;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public static GameEnd MiniDeadEnd = NebulaAPI.Preprocessor!.CreateEnd("MiniDead", MyRole.RoleColor,0);
        public Instance(GamePlayer player) : base(player)
        {
        }
        public float growtime;
        void RuntimeAssignable.OnActivated()
        {
            growtime = 0f;
        }
        public bool IsMini()
        {
            if (Math.Floor((growtime/growupneedtime)*18)<18)
            {
                return true;
            }
            return false;
        }
        [OnlyMyPlayer]
        void GuessEdited(PlayerCanGuessPlayerLocalEvent ev)
        {
            if (MiniCannotGuessOption)
            {
                if (IsMini())
                {
                    ev.CanGuess = false;
                }
            }
        }
        private void OnGameStarted(GameStartEvent ev)
        {
            growtime = 0f;
        }
        [OnlyHost]
        [OnlyMyPlayer]
        private void OnExiled(PlayerExiledEvent ev)
        {
            if (MiniDeadEndOption)
            {
                if (MyPlayer.Role.Role.Category == RoleCategory.CrewmateRole && !MyPlayer.TryGetModifier<SidekickModifier.Instance>(out var s) &&!MyPlayer.IsModMadmate() && IsMini())
                {
                    if (NebulaAPI.CurrentGame != null)
                    {
                        NebulaAPI.CurrentGame.TriggerGameEnd(MiniDeadEnd, GameEndReason.Special);
                    }
                }
            }
        }
        private void OnMeetingEnd(FixExileTextEvent ev)
        {
            if (ev.Exiled.Any((Player p) => p.PlayerId==MyPlayer.PlayerId))
            {
                if (MyPlayer.Role.Role.Category == RoleCategory.CrewmateRole && !MyPlayer.TryGetModifier<SidekickModifier.Instance>(out var s) && !MyPlayer.IsModMadmate() && IsMini())
                {
                    ExileController.Instance.ImpostorText.text=Language.Translate("ExileNiceMiniText");
                }
            }
        }
        [OnlyMyPlayer]
        private void CheckKill(PlayerCheckKilledEvent ev)
        {
            if (ev.Killer.PlayerId == base.MyPlayer.PlayerId)
            {
                return;
            }
            if (ev.PlayerState == PlayerState.Embroiled)
            {
                return;
            }
            if (ev.PlayerState == PlayerState.Guessed&&!MiniCannotGuessOption)
            {
                return;
            }
            if (IsMini())
            {
                if (ev.PlayerState != PlayerState.Bubbled && ev.PlayerState != PlayerState.Crushed)
                {
                    ev.Result = KillResult.Rejected;
                }
            }
        }
        void OnCheckKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (ev.Target.RealPlayer.PlayerId==MyPlayer.PlayerId)
            {
                if (IsMini()&& GamePlayer.LocalPlayer!.Role.GetAbility<Destroyer.Ability>() != null)
                {
                    ev.SetAsCannotKillForcedly();
                }
            }
        }
        void OnKill(PlayerTryVanillaKillLocalEvent ev)
        {
            if (ev.Target.RealPlayer.PlayerId == MyPlayer.PlayerId)
            {
                if (IsMini() && GamePlayer.LocalPlayer!.Role.GetAbility<Destroyer.Ability>() == null)
                {
                    ev.Cancel();
                }
            }
        }
        void Update(GameUpdateEvent ev)
        {
            growtime += Time.deltaTime;
        }
        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            if (MiniCooldownSpeedBuff)
            {
                MyPlayer.GainAttribute(PlayerAttributes.CooldownSpeed, 0.25f, IsMini() ? InMiniBuffRatio : NoMiniBuffRatio, false, 100, "LI::CooldownSpeed");
            }
            var outfit = MyPlayer.Unbox().CurrentOutfit;
            float num = Mathn.Min(1f - (growupneedtime - growtime) / growupneedtime * 0.5f, 1f);
            if (IsMini())
            {
                if (!outfit.Tag.ToLower().Contains("camo") && !outfit.Tag.ToLower().Contains("commscfeffect"))
                {
                    MyPlayer.GainSpeedAttribute((float)((growupneedtime - growtime) / growupneedtime * 0.5 + 1), 0.25f, true, 0, "mini:speedfast");
                    MyPlayer.GainSizeAttribute(new UnityEngine.Vector2(num, num), 0.25f, true, 100, "mini:small");
                }
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            var outfit = MyPlayer.Unbox().CurrentOutfit;
            if (IsMini())
            {
                if (!outfit.Tag.Contains("camo", StringComparison.CurrentCultureIgnoreCase) && !outfit.Tag.Contains("commscfeffect", StringComparison.CurrentCultureIgnoreCase))
                {
                    name += ("(" + (Math.Floor(growtime / growupneedtime * 18)).ToString() + ")").Color("yellow");
                }
            }
        }
    }
}
