using Plana.Core;
using Plana.Roles.Neutral;

namespace Plana.Roles.Crewmate;

public class LoveMesseger : DefinedRoleTemplate, DefinedRole, HasCitation, IAssignableDocument
{
    private record RegretExtraDeadInfo(GamePlayer Target) : GamePlayer.ExtraDeadInfo(PatchManager.Regret)
    {
        public override string ToStateText() => "for " + Target.PlayerName;
    }
    static private readonly RemoteProcess<(GamePlayer lm, GamePlayer target)> RpcShareExtraInfo = new("ShareExInfoLM",
        (message, _) => {
            message.lm.PlayerStateExtraInfo = new RegretExtraDeadInfo(message.target);
        }
    );
    private LoveMesseger(): base("lovemesseger", new(255, 164, 204), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [CooldownOption, TargetCrewmateDeadOption, TargetNeutralDeadOption,TargetImpostorDeadOption, SealAbilityUntilReportingDeadBodiesOption,SheriffCannotKillHaslovePlayer]) {
        IConfigurationHolder? configurationHolder = base.ConfigurationHolder!;
        if (configurationHolder != null)
        {
            configurationHolder.AddTags(new ConfigurationTag[] { ConfigurationTags.TagFunny });
            configurationHolder.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImages/LoveMesseger.png")!.AsImage(115f)!;
        }
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    internal static FloatConfiguration CooldownOption = NebulaAPI.Configurations.Configuration("options.role.lovemesseger.cooldown", (5f, 40f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    internal static BoolConfiguration TargetCrewmateDeadOption = NebulaAPI.Configurations.Configuration("options.role.lovemesseger.crewmate", false);
    internal static BoolConfiguration TargetNeutralDeadOption = NebulaAPI.Configurations.Configuration("options.role.lovemesseger.neutral", false);
    internal static BoolConfiguration TargetImpostorDeadOption = NebulaAPI.Configurations.Configuration("options.role.lovemesseger.impostor", true);
    internal static BoolConfiguration SealAbilityUntilReportingDeadBodiesOption = NebulaAPI.Configurations.Configuration("options.role.lovemesseger.beforeReportUseSkill", true);
    internal static BoolConfiguration SheriffCannotKillHaslovePlayer = NebulaAPI.Configurations.Configuration("options.role.lovemesseger.cannotkillhasloveplayer", false);
    static public LoveMesseger MyRole = new LoveMesseger();
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/LoveMesseger.png")!.AsImage();

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(LoveMesseger.Instance.buttonImage!, "role.lovemesseger.ability.love");
    }
    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%C%", Language.Translate(TargetCrewmateDeadOption ? "role.lovemesseger.ability.crewmate" : "role.lovemesseger.ability.nocrewmate"));
        yield return new("%N%", Language.Translate(TargetNeutralDeadOption ? "role.lovemesseger.ability.neutral" : "role.lovemesseger.ability.noneutral"));
        yield return new("%I%", Language.Translate(TargetImpostorDeadOption ? "role.lovemesseger.ability.impostor" : "role.lovemesseger.ability.noimpostor"));
    }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance(GamePlayer player) : RuntimeAssignableTemplate(player), RuntimeRole, RuntimeAssignable,ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;

        static internal Virial.Media.Image? buttonImage = NebulaAPI.AddonAsset.GetResource("LMButton.png")?.AsImage(115f);
        int GenerateModifierNum = 1;
        ModAbilityButton? skillButton;
        private void OnGameStarted(GameStartEvent ev)
        {
            skillButton = null;
            GenerateModifierNum = 1;
            setColorNamePlayer = null;
            selectplayer = null;
        }
        [Local]
        private void OnGameEnd(GameEndEvent ev)
        {
            PDebug.Log("GameEnd");
            if (selectplayer == null)
            {
                return;
            }
            if (ev.EndState.EndCondition == NebulaGameEnd.JesterWin && selectplayer.Role is Jester.Instance)
            {
                new StaticAchievementToken("lovemesseger.another1");
                return;
            }
            if (ev.EndState.EndCondition == NebulaGameEnd.ScarletWin && selectplayer.Role.Role.InternalName.Contains("scarlet"))
            {
                new StaticAchievementToken("lovemesseger.another3");
                return;
            }
            int alive = 0;
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!player.Data.IsDead)
                {
                    alive++;
                }
            }
            if (alive <= 5 && ev.EndState.EndCondition == NebulaGameEnd.CrewmateWin && selectPlayerIsJackal && !selectplayer.IsDead)
            {
                new StaticAchievementToken("lovemesseger.challenge2");
            }
        }
            [Local]
            private void DecorateSidekickColor(PlayerDecorateNameEvent ev)
            {
                if (MyPlayer != null && MyPlayer.Role is LoveMesseger.Instance)
                {
                    if (setColorNamePlayer != null && ev.Player.PlayerId == setColorNamePlayer.PlayerId)
                    {
                        ev.Color = new Virial.Color?(MyRole.RoleColor);
                    }
                    else if (ev.Player.PlayerId == MyPlayer.PlayerId)
                    {
                        ev.Color = new Virial.Color?(MyRole.RoleColor);
                    }
                }
            }
        bool selectPlayerIsJackal = false;
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                selectplayer = null;
                GenerateModifierNum = 1;
                SpriteRenderer? lockSprite = null;
                ObjectTracker<GamePlayer> myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, p => ObjectTrackers.StandardPredicate(p) && selectplayer == null, MyRole.RoleColor.ToUnityColor(), false, false);
                skillButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Ability, null);
                skillButton.SetImage(buttonImage);
                skillButton.Availability = (ModAbilityButton button) => myTracker.CurrentTarget != null && this.MyPlayer.CanMove && lockSprite == null;
                skillButton.Visibility = (ModAbilityButton button) => !this.MyPlayer.IsDead && GenerateModifierNum > 0;
                skillButton.ShowUsesIcon(4, GenerateModifierNum.ToString());
                if (SealAbilityUntilReportingDeadBodiesOption)
                {
                    if (!GamePlayer.AllPlayers.Any(p => p.IsDead))
                    {
                        lockSprite = (skillButton as ModAbilityButtonImpl).VanillaButton.AddLockedOverlay();
                    }
                }
                lockspr = lockSprite;
                this.skillButton.OnClick = delegate (ModAbilityButton button)
                {
                    GenerateModifierNum -= 1;
                    if (GenerateModifierNum > 0)
                    {
                        button.UpdateUsesIcon(GenerateModifierNum.ToString());
                    }
                    button.StartCoolDown();
                    try
                    {
                        var player = myTracker.CurrentTarget;
                        var targetRoleCategory = player.Role.Role.Category;
                        if ((TargetCrewmateDeadOption && targetRoleCategory == RoleCategory.CrewmateRole) ||
                            (TargetNeutralDeadOption && targetRoleCategory == RoleCategory.NeutralRole) ||
                            (TargetImpostorDeadOption && targetRoleCategory == RoleCategory.ImpostorRole))
                        {
                            new StaticAchievementToken("lovemesseger.another2");
                            MyPlayer.Suicide(PatchManager.Regret, null, KillParameter.NormalKill, null);
                            RpcShareExtraInfo.Invoke((MyPlayer, player));
                            return;
                        }
                        if (player.TryGetModifier<SkinnerDog.Instance>(out var d))
                        {
                            new StaticAchievementToken("lovemesseger.another2");
                            MyPlayer.Suicide(PatchManager.Regret, null, KillParameter.NormalKill, null);
                            RpcShareExtraInfo.Invoke((MyPlayer, player));
                            return;
                        }
                        if (player.Role is Jackal.Instance instance)
                        {
                            new StaticAchievementToken("lovemesseger.challenge1");
                            if (NebulaGameManager.Instance.AllPlayerInfo.Count(p => !p.IsDead) <= 5)
                            {
                                selectPlayerIsJackal = true;
                            }
                            instance.PromoteSidekick(false);
                        }
                        if (player.Role is Skinner.Instance)
                        {
                            var dogs = NebulaGameManager.Instance!.AllPlayerInfo.Where(p => !p.IsDead && p.TryGetModifier<SkinnerDog.Instance>(out var d));
                            dogs.Do(p =>
                                {
                                    using (RPCRouter.CreateSection("SidekickPromote"))
                                    {
                                        p.AddModifier(HasLove.MyRole);
                                        if (p.Role is Jackal.Instance i)
                                        {
                                            i.PromoteSidekick(false);
                                        }
                                    }
                                    new StaticAchievementToken("lovemesseger.challenge1");
                                    if (NebulaGameManager.Instance.AllPlayerInfo.Count(p => !p.IsDead) <= 5)
                                    {
                                        selectPlayerIsJackal = true;
                                    }
                                });
                        }
                        setColorNamePlayer = player;
                        selectplayer = player;
                        new StaticAchievementToken("lovemesseger.common1");
                        player.AddModifier(HasLove.MyRole, null);
                    }
                    catch (Exception e)
                    {
                        PDebug.Log("LoveMessegerGenerateModifierFailed Error:" + e.ToString());
                    }
                };
                skillButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, CooldownOption).SetAsAbilityTimer().Start();
                skillButton.StartCoolDown();
                this.skillButton.SetLabel("Love");
            }
        }
        public GamePlayer? selectplayer,setColorNamePlayer;
        SpriteRenderer lockspr;
        private void OnMeetingStart(MeetingStartEvent ev)
        {
            if (lockspr)
            {
                    UnityEngine.Object.Destroy(lockspr.gameObject);
                    lockspr = null;
            }
        }
    }
}
[NebulaRPCHolder]
public class HasLove : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
{
    private HasLove() : base("haslove", new(255, 164, 204),null,true,()=>false)
    {
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }

    static public HasLove MyRole = new HasLove();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public Instance(GamePlayer player) : base(player)
        {
        }
        [OnlyMyPlayer]
        private void BlockWins(PlayerBlockWinEvent ev)
        {
            ev.IsBlocked = ev.GameEnd != NebulaGameEnd.CrewmateWin;
        }

        [OnlyMyPlayer]
        [EventPriority(EventPriority.VeryLow)]
        private void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.IsWin = ev.GameEnd == NebulaGameEnd.CrewmateWin;
        }

        [OnlyMyPlayer]
        void OnCheckKill(PlayerCheckCanKillLocalEvent ev)
        {
            ev.SetAsCanKillForcedly();
        }
        void RuntimeAssignable.OnActivated()
        {
            if (base.AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0f, 0.25f, 0.4f, 0f);
            }
        }
        [Local]
        private void DecorateHasLoveColor(PlayerDecorateNameEvent ev)
        {
            if (MyPlayer!=null&&MyPlayer.Modifiers.Any((RuntimeModifier m) => m is HasLove.Instance))
            {
                if (ev.Player != null && ev.Player.Role is LoveMesseger.Instance)
                {
                    ev.Color = new Virial.Color?(MyRole.RoleColor);
                }
                if (ev.Player != null && ev.Player.PlayerId == MyPlayer.PlayerId)
                {
                    ev.Color = new Virial.Color?(MyRole.RoleColor);
                }
            }
        }
        void OnSheriffKill(SheriffCheckKillEvent ev)
        {
             if (LoveMesseger.SheriffCannotKillHaslovePlayer&&ev.Target==MyPlayer)
            {
                ev.CanKill = false;
            }
        }
        string RuntimeAssignable.OverrideRoleName(string lastRoleName, bool isShort,bool canSeeAllInfo)
        {
            return Language.Translate("role.lovemesseger.prefix").Color(MyRole.RoleColor.ToUnityColor())+lastRoleName;
        }
    }
}

