using Il2CppSystem.Net.NetworkInformation;
using Nebula.Roles.Impostor;
using Nebula.VoiceChat;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Modifier;
using Virial.Helpers;
using Virial.Runtime;
using static Plana.Roles.Neutral.YandereEndCriteria;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Plana.Roles.Neutral;
[NebulaPreprocess(PreprocessPhase.BuildAssignmentTypes)]
public static class AssignmentSetUp
{
    static public Virial.Color Color = new(106, 252, 45);
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        try
        {
            PDebug.Log("Add Moriarty Assignment Types");
            preprocessor.RegisterAssignmentType(() => Moriarty.MyRole, (int[] lastArgs, DefinedRole role) => Moriarty.GenerateArgument(role), "moriartized", Color, (AbilityAssignmentStatus status, DefinedRole role) => status.HasFlag(AbilityAssignmentStatus.CanLoadToMadmate), () => ((ISpawnable)Moriarty.MyRole).CanSpawnInCurrentGame && Moriarty.MoriartizedRoleOption);
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
}
[NebulaPreprocess(PreprocessPhase.PostFixStructure)]
public class MoriartyEndCriteria
{
    static MoriartyEndCriteria()
    {
        DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() => new MoriartyCriteria().RegisterPermanently());
    }
    public class MoriartyCriteria : IModule, IGameOperator
    {
        [OnlyHost]
        void MoriartyWinCheck(GameUpdateEvent ev)
        {
            try
            {
                static bool isJackalTeam(GamePlayer p) => (p.Role.Role.Team == Jackal.MyTeam) || (p.Modifiers.Any(m => m.Modifier == SidekickModifier.MyRole));
                static bool isMoran(GamePlayer p) => (p.Role.Role == Moran.MyRole) || (p.Modifiers.Any(m => m.Modifier == MoranModifier.MyRole));
                int totalAlive = 0;
                int moriartyTeamNum = 0;
                if (GameOperatorManager.Instance?.Run<KillerTeamCallback>(new KillerTeamCallback(Moriarty.MyTeam), false, true).RemainingOtherTeam ?? false)
                {
                    return;
                }
                foreach (var p in NebulaGameManager.Instance.AllPlayerInfo)
                {
                    if (p == null || p.IsDead)
                    {
                        continue;
                    }
                    if (!p.IsDead)
                    {
                        totalAlive++;
                    }
                    if (Moriarty.Instance.IsSameTeam(p))
                    {
                        moriartyTeamNum++;
                    }
                }
                if (moriartyTeamNum * 2 >= totalAlive)
                {
                    Virial.Game.Game currentGame = NebulaAPI.CurrentGame;
                    if (currentGame != null)
                    {
                        currentGame.TriggerGameEnd(Moriarty.Instance.MoriartyTeamWin, GameEndReason.Situation, null);
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        [OnlyHost]
        void HolmesOnDead(PlayerMurderedEvent ev)
        {
            if (ev.Player.PlayerId == ev.Murderer.PlayerId)
            {
                return;
            }
            if (ev.Dead.Role.Role is Holmes)
            {
                if (ev.Murderer.Role is Moriarty.Instance || ev.Murderer.Role.Role is Moran || ev.Murderer.TryGetModifier<MoranModifier.Instance>(out var m))
                {
                    NebulaAPI.CurrentGame?.TriggerGameEnd(Moriarty.Instance.MoriartyTeamWin, GameEndReason.Situation);
                }
            }
        }
    }
}
[NebulaRPCHolder]
public class Moriarty : DefinedRoleTemplate, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder ,IAssignableDocument
{
    public static readonly Team MyTeam = new Team("teams.moriarty", AssignmentSetUp.Color, TeamRevealType.OnlyMe);
    private Moriarty(): base("moriarty", AssignmentSetUp.Color, RoleCategory.NeutralRole, MyTeam, [SidekickMaxNum,SidekickCooldown,CanSuicide,SuicideCooldown,CanWinByKillHolmes,MoriartizedRoleOption],true,true,null,null,()=>
    {
        if (CanWinByKillHolmes)
        {
            return [new AllocationParameters.ExtraAssignmentInfo((DefinedRole _, int playerId) => new ValueTuple<DefinedRole, int[]>(Holmes.MyRole, new int[] { 0, playerId
}), RoleCategory.CrewmateRole)];
        }
        return Array.Empty<AllocationParameters.ExtraAssignmentInfo>();
    }) 
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Moran.MyRole.ConfigurationHolder,Holmes.MyRole.ConfigurationHolder]);
    }
    DefinedRole[] DefinedRole.AdditionalRoles
    {
        get
        {
            if (CanWinByKillHolmes)
            {
                return [Holmes.MyRole];
            }
            return Array.Empty<DefinedRole>();
        }
    }
    IEnumerable<DefinedRole> DefinedRole.GetGuessableAbilityRoles()
    {
        if (!((ISpawnable)this).IsSpawnable || MoriartizedRoleOption)
        {
            return Array.Empty<DefinedRole>();
        }
        return [this];
    }
    Citation? HasCitation.Citation { get { return PCitations.LTS; } }
    static IntegerConfiguration SidekickMaxNum = NebulaAPI.Configurations.Configuration("options.role.moriarty.sidekickmaxnum", (1, 15), 1);
    static FloatConfiguration SidekickCooldown = NebulaAPI.Configurations.Configuration("options.role.moriarty.sidekickCooldown", (0f,60f,2.5f), 15f,FloatConfigurationDecorator.Second);
    static FloatConfiguration SuicideCooldown = NebulaAPI.Configurations.Configuration("options.role.moriarty.suicideCooldown", (0f, 60f, 2.5f), 0f,FloatConfigurationDecorator.Second);
    static BoolConfiguration CanSuicide = NebulaAPI.Configurations.Configuration("options.role.moriarty.Cansuicide", true);
    internal static BoolConfiguration CanWinByKillHolmes = NebulaAPI.Configurations.Configuration("options.role.moriarty.canwinbyholmes", true);
    public static BoolConfiguration MoriartizedRoleOption = NebulaAPI.Configurations.Configuration("options.role.moriarty.moriartizedRole",false);
    static public Moriarty MyRole = new Moriarty();
    bool IAssignableDocument.HasTips => true;
    public static int[] GenerateArgument(DefinedRole role)
    {
        return [SidekickMaxNum, role?.Id??-1, .. (role?.DefaultAssignableArguments??[])];
    }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments)
    {
        return new Instance(player, arguments.Get(0,SidekickMaxNum),Nebula.Roles.Roles.GetRole(arguments.Get(1, -1)), arguments.Skip(2).ToArray());
    }
    IUsurpableAbility? DefinedRole.GetUsurpedAbility(Virial.Game.Player player, int[] arguments)
    {
        var role = Nebula.Roles.Roles.GetRole(arguments.Get(1, -1));
        var ability = role?.GetUsurpedAbility(player, arguments.Skip(2).ToArray());
        if (ability != null) return new UsurpedImpostorAbility(player, role!, ability);
        return null;
    }
    string DefinedRole.GetDisplayName(IPlayerAbility ability)
    {
        if (ability is UsurpedImpostorAbility a) return a.Role.GetDisplayName(a.Ability);
        return (this as DefinedRole).DisplayName;
    }

    string DefinedRole.GetDisplayShort(IPlayerAbility ability)
    {
        if (ability is UsurpedImpostorAbility a) return a.Role.GetDisplayShort(a.Ability);
        return (this as DefinedRole).DisplayShort;
    }
    public class Instance(GamePlayer player, int sidekicknum, DefinedRole role, int[] moriartizedArguments) : RuntimeAssignableTemplate(player), RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        int[] MoriartyArguments => [sidekicknum, MyMoriartized?.Id ?? -1];
        int[]? RuntimeAssignable.RoleArguments => MoriartyArguments.Concat(MoriartizedAbility?.AbilityArguments ?? []).ToArray();
        int[]? RuntimeRole.UsurpedAbilityArguments => (MoriartizedAbility?.AbilityArguments ?? []).Prepend(MyMoriartized?.Id ?? -1).ToArray();
        public DefinedRole? MyMoriartized { get; set; } = role;
        public IPlayerAbility? MoriartizedAbility { get; private set; } = null;
        private int[] StoredMoriartizedArgument { get; set; } = moriartizedArguments;
        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp =>
            MyMoriartized != null ?[MyRole, MyMoriartized,.. MoriartizedAbility?.SubAssignableOnHelp??Array.Empty<DefinedAssignable>()] : [MyRole];
        string RuntimeRole.DisplayIntroRoleName => (MyMoriartized ?? MyRole).DisplayName;
        string RuntimeAssignable.DisplayColoredName => (this as RuntimeAssignable).DisplayName.Color(MyTeam.UnityColor);
        string RuntimeRole.DisplayShort => MyMoriartized?.GetDisplayShort(MoriartizedAbility!) ?? (MyRole as DefinedRole).DisplayShort;
        string RuntimeAssignable.DisplayName
        {
            get
            {
                var name = MyMoriartized?.GetDisplayName(MoriartizedAbility!) ?? (MyRole as DefinedRole).DisplayName;
                return name;
            }
        }
        IEnumerable<IPlayerAbility?> RuntimeAssignable.MyAbilities => MoriartizedAbility != null ? [MoriartizedAbility, .. MoriartizedAbility.SubAbilities] : [];
        DefinedRole RuntimeRole.Role=>MyRole;
        ModAbilityButton? killButton, sidekickButton;

        void RuntimeRole.Usurp()
        {
            (MoriartizedAbility as IUsurpableAbility)?.Usurp();
        }
        int sidekicknum = sidekicknum;
        bool RuntimeRole.CheckGuessAbility(DefinedRole abilityRole)
        {
            return abilityRole == MyMoriartized || abilityRole == Moriarty.MyRole;
        }
        static bool IsMySidekick(GamePlayer player)
        {
            return player.Role.GetAbility<Moran.Ability>()!=null||player.Modifiers.Any(m=>m is MoranModifier.Instance);
        }
        private void DecorateNameColor(PlayerDecorateNameEvent ev)
        {
            if (IsSameTeam(GamePlayer.LocalPlayer)&&IsSameTeam(ev.Player))
            {
                ev.Color = MyRole.RoleColor;
            }
        }

        public static bool IsSameTeam(GamePlayer player)
        {
            if (IsMySidekick(player))
            {
                return true;
            }
            return player.Role is Moriarty.Instance;
    }
        public static GameEnd MoriartyTeamWin = NebulaAPI.Preprocessor!.CreateEnd("moriarty", MyRole.RoleColor);
        void CheckWin(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd == MoriartyTeamWin && IsSameTeam(ev.Player));
        }
        [Local]
        void OnDead(PlayerMurderedEvent ev)
        {
            if (ev.Player.Role.Role is Holmes)
            {
                if (ev.Murderer.Role is Moriarty.Instance||ev.Murderer.Role.Role is Moran||ev.Murderer.TryGetModifier<MoranModifier.Instance>(out var m))
                {
                    new StaticAchievementToken("moriarty.common2");
                }
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                ObjectTracker<IPlayerlike> myTracker = ObjectTrackers.ForPlayerlike(this, null, base.MyPlayer, (IPlayerlike p) => ObjectTrackers.PlayerlikeLocalKillablePredicate(p) && !IsMySidekick(p.RealPlayer), MyRole.UnityColor, false, false);
                ObjectTracker<IPlayerlike> mySidekickTracker = ObjectTrackers.ForPlayerlike(this, null, base.MyPlayer, (IPlayerlike p) => ObjectTrackers.PlayerlikeStandardPredicate(p) && !IsMySidekick(p.RealPlayer), MyRole.UnityColor, false, false);
                sidekickButton = NebulaAPI.Modules.InteractButton<IPlayerlike>(this, base.MyPlayer, mySidekickTracker, new PlayerInteractParameter(true, false, true), false, true, VirtualKeyInput.SidekickAction, null, SidekickCooldown, "sidekick", Jackal.Instance.sidekickButtonSprite, delegate (IPlayerlike p, ModAbilityButton button)
                {
                    sidekicknum--;
                    button.UpdateUsesIcon(sidekicknum.ToString());
                    new StaticAchievementToken("moriarty.common1");
                    if (myTracker.CurrentTarget.RealPlayer.Role.Role is Holmes)
                    {
                        new StaticAchievementToken("moriarty.another1");
                    }
                    if (Moran.IsModifierOption)
                    {
                        IPlayerlike currentTarget = myTracker.CurrentTarget;
                        if (currentTarget != null)
                        {
                            currentTarget.RealPlayer.AddModifier(MoranModifier.MyRole);
                        }
                    }
                    else
                    {
                        IPlayerlike currentTarget2 = myTracker.CurrentTarget;
                        if (currentTarget2 != null)
                        {
                            currentTarget2.RealPlayer.SetRole(Moran.MyRole);
                        }
                    }
                    if (sidekicknum <= 0)
                    {
                        button.HideUsesIcon();
                    }
                }, (ModAbilityButton _) => true, (ModAbilityButton _) => sidekicknum > 0, false);
                sidekickButton.ShowUsesIcon(3, sidekicknum.ToString());
                if (CanSuicide)
                {
                    killButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, true, false, VirtualKeyInput.Kill, null, SuicideCooldown, "suicide", Madmate.Instance.SuicideButtonImage, (ModAbilityButton _) => myTracker.CurrentTarget != null && !this.MyPlayer.IsDived, (ModAbilityButton _) => base.MyPlayer.AllowToShowKillButtonByAbilities, false).SetLabelType(ModAbilityButton.LabelType.Impostor);
                    killButton.OnClick = delegate (ModAbilityButton button)
                    {
                        if (!NebulaGameManager.Instance.AllPlayerInfo.Any(p => !p.IsDead && p.Role.Role is Moran || p.TryGetModifier<MoranModifier.Instance>(out var m)))
                        {
                            new StaticAchievementToken("moriarty.challenge1");
                        }
                        MyPlayer.MurderPlayer(myTracker.CurrentTarget, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill,KillCondition.TargetAlive, null);
                        MyPlayer.Suicide(PlayerStates.Suicide, null, KillParameter.NormalKill);
                    };
                }
                if (PatchManager.MoriartyMeetingChat)
                {
                    PrivateChatManager.Instance.Register(MyRole.RoleColor, MyRole.RoleColor, "Moriarty", Language.Translate("chat.moriartytext"), this,() => MeetingHud.Instance, (p1, p2) => IsSameTeam(p1)&&IsSameTeam(p2));
                }
            }
            MoriartizedAbility = MyMoriartized?.GetAbilityOnRole(MyPlayer,AbilityAssignmentStatus.CanLoadToMadmate,StoredMoriartizedArgument)?.Register(this);
        }
    }
}
[NebulaRPCHolder]
public class Moran : DefinedSingleAbilityRoleTemplate<Moran.Ability>, HasCitation, DefinedRole ,IAssignableDocument
{
    private Moran() : base("moran", Moriarty.MyTeam.Color, RoleCategory.NeutralRole, Moriarty.MyTeam, [IsModifierOption,CanWinAsOriginalTeamOption,SnipeCoolDownOption, ShotSizeOption, ShotEffectiveRangeOption, ShotNoticeRangeOption, StoreRifleOnFireOption, StoreRifleOnUsingUtilityOption, CanSeeRifleInShadowOption, CanKillHidingPlayerOption, AimAssistOption, DelayInAimAssistOption,PatchManager.SniperCanLockMini],false,true,()=>((ISpawnable)Moriarty.MyRole).IsSpawnable)
    {
        ConfigurationHolder?.AddTags(ConfigurationTags.TagFunny, ConfigurationTags.TagDifficult);
        ConfigurationHolder?.ScheduleAddRelated(() => [Moriarty.MyRole.ConfigurationHolder, Holmes.MyRole.ConfigurationHolder]);
    }
    bool DefinedRole.IsKiller => true;
    string DefinedAssignable.InternalName
    {
        get
        {
            return "moriarty.moran";
        }
    }
    Citation? HasCitation.Citation => PCitations.LTS;
    bool ISpawnable.IsSpawnable
    {
        get
        {
            return ((ISpawnable)Moriarty.MyRole).IsSpawnable && !IsModifierOption;
        }
    }


    static public IRelativeCooldownConfiguration SnipeCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.moran.snipeCoolDown", CoolDownType.Immediate, (0f, 60f, 2.5f), 20f, (-40f, 40f, 2.5f), -10f, (0.125f, 2f, 0.125f), 1f);
    static public FloatConfiguration ShotSizeOption = NebulaAPI.Configurations.Configuration("options.role.moran.shotSize", (0.25f, 4f, 0.25f), 1f, FloatConfigurationDecorator.Ratio);
    static public FloatConfiguration ShotEffectiveRangeOption = NebulaAPI.Configurations.Configuration("options.role.moran.shotEffectiveRange", (2.5f, 50f, 2.5f), 25f, FloatConfigurationDecorator.Ratio);
    static public FloatConfiguration ShotNoticeRangeOption = NebulaAPI.Configurations.Configuration("options.role.moran.shotNoticeRange", (2.5f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Ratio);
    static public BoolConfiguration StoreRifleOnFireOption = NebulaAPI.Configurations.Configuration("options.role.moran.storeRifleOnFire", true);
    static public BoolConfiguration StoreRifleOnUsingUtilityOption = NebulaAPI.Configurations.Configuration("options.role.moran.storeRifleOnUsingUtility", false);
    static public BoolConfiguration CanSeeRifleInShadowOption = NebulaAPI.Configurations.Configuration("options.role.moran.canSeeRifleInShadow", false);
    static public BoolConfiguration CanKillHidingPlayerOption = NebulaAPI.Configurations.Configuration("options.role.moran.canKillHidingPlayer", false);
    static public BoolConfiguration AimAssistOption = NebulaAPI.Configurations.Configuration("options.role.moran.aimAssist", false);
    static public FloatConfiguration DelayInAimAssistOption = NebulaAPI.Configurations.Configuration("options.role.moran.delayInAimAssistActivation", (0f, 20f, 1f), 3f, FloatConfigurationDecorator.Second, () => AimAssistOption);
    internal static BoolConfiguration IsModifierOption = NebulaAPI.Configurations.Configuration("options.role.moran.isModifier", false);
    internal static BoolConfiguration CanWinAsOriginalTeamOption = NebulaAPI.Configurations.Configuration("options.role.moran.canWinAsOriginalTeam",false, () => IsModifierOption, null);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, AmongUsLLImpl.Instance.VanillaKillCooldown);
    static public Moran MyRole = new Moran();
    bool IAssignableDocument.HasTips => true;
    [NebulaRPCHolder]
    public class SniperRifle : EquipableAbility, IGameOperator
    {
        private static SpriteLoader rifleSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.SniperRifle.png", 100f);
        public SniperRifle(GamePlayer owner) : base(owner, CanSeeRifleInShadowOption, "SniperRifle")
        {
            Renderer.sprite = rifleSprite.GetSprite();
        }

        public IPlayerlike? GetTarget(float width, float maxLength)
        {
            float minLength = maxLength;
            IPlayerlike? result = null;

            foreach (var p in GamePlayer.AllPlayerlikes)
            {
                if (p.IsDead || p.AmOwner || ((!CanKillHidingPlayerOption) && p.Logic.InVent || p.IsDived)) continue;

                if (!PatchManager.SniperCanLockMini&&p.RealPlayer.TryGetModifier<PMini.Instance>(out PMini.Instance mini) && mini.IsMini())
                {
                    continue;
                }

                //吹っ飛ばされているプレイヤーは無視しない

                //不可視なプレイヤーは無視
                if (p.IsInvisible || p.WillDie) continue;

                var pos = p.TruePosition.ToUnityVector();
                Vector2 diff = pos - (Vector2)Renderer.transform.position;

                //移動と回転を施したベクトル
                var vec = diff.Rotate(-Renderer.transform.eulerAngles.z);

                if (vec.x > 0 && vec.x < minLength && Mathn.Abs(vec.y) < width * 0.5f)
                {
                    result = p;
                    minLength = vec.x;
                }
            }

            return result;
        }
    }

    [NebulaRPCHolder]
    public class Ability : AbstractPlayerAbility, IPlayerAbility
    {


        static private Image buttonSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.Buttons.SnipeButton.png", 115f);
        static private Image aimAssistSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.SniperGuide.png", 100f);

        public SniperRifle? MyRifle = null;
        bool IPlayerAbility.HideKillButton => !(equipButton?.IsBroken ?? false);

        void ClaimMoriartyRemaining(KillerTeamCallback callback)
        {
            if (callback.ExcludedTeam != Moriarty.MyTeam)
            {
                if (Player.AllPlayers.Any((Player p) => (p.Role.GetAbility<Moran.Ability>()!=null ||p.TryGetModifier<MoranModifier.Instance>(out var m))&& p.IsAlive))
                {
                    callback.MarkRemaining();
                }
            }
        }
        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            if (MyRifle != null && StoreRifleOnUsingUtilityOption)
            {
                var p = MyPlayer.VanillaPlayer;
                if (p.onLadder || p.inMovingPlat || p.inVent) RpcEquip.Invoke((MyPlayer.PlayerId, false));
            }
        }
        ModAbilityButton equipButton = null!;
        public Ability(GamePlayer player, float defaultCooldown) : base(player)
        {
            if (AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0f, 0.25f, 0.4f, 0f);
                equipButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, "sniper.equip",
                    0f, "equip", buttonSprite);
                equipButton.OnClick = (button) =>
                {
                    if (MyRifle == null)
                    {
                        NebulaGameManager.Instance?.RpcDoGameAction(MyPlayer, MyPlayer.Position, GameActionTypes.SniperEquippingAction);
                        NebulaAsset.PlaySE(NebulaAudioClip.SniperEquip, true);
                        equipButton.SetLabel("unequip");
                    }
                    else
                        equipButton.SetLabel("equip");

                    RpcEquip.Invoke((MyPlayer.PlayerId, MyRifle == null));

                    if (MyRifle != null)
                    {
                        var circle = EffectCircle.SpawnEffectCircle(AmongUsLLImpl.LocalPlayer.transform, Vector3.zero, NebulaTeams.ImpostorTeam.Color, ShotNoticeRangeOption, null, true);
                        var script = circle.gameObject.AddComponent<ScriptBehaviour>();
                        script.UpdateHandler += () =>
                        {
                            if (MyRifle == null) circle.Disappear();
                        };
                        this.BindGameObject(circle.gameObject);
                    }
                };
                equipButton.OnBroken = (button) =>
                {
                    if (MyRifle != null)
                    {
                        equipButton.SetLabel("equip");
                        RpcEquip.Invoke((MyPlayer.PlayerId, false));
                    }
                    Snatcher.RewindKillCooldown();
                };
                GameOperatorManager.Instance?.Subscribe<MeetingStartEvent>(ev => equipButton.SetLabel("equip"), this);

                var killButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, true, false, VirtualKeyInput.Kill, "sniper.kill",
                    SnipeCoolDownOption.GetCooldown(MyPlayer.TeamKillCooldown), "snipe", null,
                    _ => MyRifle != null, _ => !equipButton.IsBroken)
                    .SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor)
                    .SetAsMouseClickButton();
                killButton.OnClick = (button) =>
                {
                    NebulaAsset.PlaySE(NebulaAudioClip.SniperShot, true);
                    var target = MyRifle?.GetTarget(ShotSizeOption, ShotEffectiveRangeOption);
                    if (target != null && !(GameOperatorManager.Instance?.Run(new PlayerInteractPlayerLocalEvent(MyPlayer, target, new(IsKillInteraction: true))).IsCanceled ?? false))
                    {
                        bool isBlown = target.IsBlown;
                        MyPlayer.MurderPlayer(target, PlayerState.Sniped, EventDetail.Kill, KillParameter.RemoteKill,null);
                        new StaticAchievementToken("moran.common1");
                        if (target.RealPlayer.Role.Role is Holmes)
                        {
                            new StaticAchievementToken("moran.common2");
                            if (MyPlayer.IsInvisible)
                            {
                                new StaticAchievementToken("moran.another1");
                            }
                            var ability = target.RealPlayer.Role.GetAbility<Holmes.Ability>();
                            if (ability!=null)
                            {
                                if (ability.divideResults.ContainsKey(MyPlayer.PlayerId))
                                {
                                    new StaticAchievementToken("moran.challenge1");
                                }
                            }
                        }
                    }
                    else
                    {
                        NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.Missed, MyPlayer.VanillaPlayer, 0);
                    }
                    Sniper.RpcShowNotice.Invoke(MyPlayer.Position);

                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();

                    if (StoreRifleOnFireOption) RpcEquip.Invoke((MyPlayer.PlayerId, false));
                };
                if (PatchManager.MoriartyMeetingChat)
                {
                    PrivateChatManager.Instance.Register(MyRole.RoleColor, MyRole.RoleColor, "Moriarty", Language.Translate("chat.moriartytext"), this,() => MeetingHud.Instance, (p1, p2) => Moriarty.Instance.IsSameTeam(p1) && Moriarty.Instance.IsSameTeam(p2));
                }
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }

        [Local]
        [OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev)
        {
            if (MyRifle != null) RpcEquip.Invoke((MyPlayer.PlayerId, false));
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (MyRifle != null) RpcEquip.Invoke((MyPlayer.PlayerId, false));
        }

        System.Collections.IEnumerator CoShowAimAssist()
        {
            System.Collections.IEnumerator CoUpdateAimAssistArrow(PlayerControl player)
            {
                DeadBody? deadBody = null;
                Vector2 pos = Vector2.zero;
                Vector2 dir = Vector2.zero;
                Vector2 tempDir = Vector2.zero;
                bool isFirst = true;

                UnityEngine.Color targetColor = new UnityEngine.Color(55f / 225f, 1f, 0f);
                float t = 0f;

                SpriteRenderer? renderer = null;

                while (true)
                {
                    if (MeetingHud.Instance || MyPlayer.IsDead || MyRifle == null || IsDeadObject) break;

                    if (player.Data.IsDead && !deadBody) deadBody = Helpers.GetDeadBody(player.PlayerId);

                    //死亡して、死体も存在しなければ追跡を終了
                    if (player.Data.IsDead && !deadBody) break;

                    if (renderer == null)
                    {
                        renderer = UnityHelper.CreateObject<SpriteRenderer>("AimAssist", AmongUsLLImpl.HudManagerInstance.transform, Vector3.zero);
                        renderer.sprite = aimAssistSprite.GetSprite();
                    }

                    pos = player.Data.IsDead ? deadBody!.transform.position : player.transform.position;
                    tempDir = (pos - (Vector2)AmongUsLLImpl.LocalPlayer.transform.position).normalized;

                    NebulaGameManager.Instance!.WideCamera.CheckPlayerState(out var localScale, out var localRotateZ);
                    tempDir.x *= localScale.x;
                    tempDir.y *= localScale.y;

                    if (isFirst)
                    {
                        dir = tempDir;
                        isFirst = false;
                    }
                    else
                    {
                        dir = (tempDir + dir).normalized;
                    }

                    float angle = Mathn.Atan2(dir.y, dir.x) + localRotateZ.DegToRad();
                    renderer.transform.eulerAngles = new Vector3(0, 0, angle.RadToDeg());
                    renderer.transform.localPosition = new Vector3(Mathn.Cos(angle) * 2f, Mathn.Sin(angle) * 2f, -30f);

                    t += Time.deltaTime / 0.8f;
                    if (t > 1f) t = 1f;
                    renderer.color = UnityEngine.Color.Lerp(UnityEngine.Color.white, targetColor, t).AlphaMultiplied(0.6f);

                    yield return null;
                }

                if (renderer == null) yield break;

                float a = 0.6f;
                while (a > 0f)
                {
                    a -= Time.deltaTime / 0.8f;
                    var color = renderer.color;
                    color.a = a;
                    renderer.color = color;
                    yield return null;
                }

                GameObject.Destroy(renderer.gameObject);
            }

            yield return new WaitForSeconds(DelayInAimAssistOption);

            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (!p.AmOwner) NebulaManager.Instance.StartCoroutine(CoUpdateAimAssistArrow(p).WrapToIl2Cpp());
            }
        }

        void EquipRifle()
        {
            MyRifle = new SniperRifle(MyPlayer).Register(this);

            if (AmOwner && AimAssistOption) NebulaManager.Instance.StartCoroutine(CoShowAimAssist().WrapToIl2Cpp());
        }

        void UnequipRifle()
        {
            if (MyRifle != null) MyRifle.Release();
            MyRifle = null;
        }

        static RemoteProcess<(byte playerId, bool equip)> RpcEquip = new(
        "EquipRifleMoran",
        (message, _) =>
        {
            var role = NebulaGameManager.Instance?.GetPlayer(message.playerId)?.Role;
            var sniper = role.GetAbility<Ability>();
            if (sniper != null)
            {
                if (message.equip)
                    sniper.EquipRifle();
                else
                    sniper.UnequipRifle();
            }
        }
        );
    }

    private static SpriteLoader snipeNoticeSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.SniperRifleArrow.png", 200f);
    public static RemoteProcess<Vector2> RpcShowNotice = new(
        "ShowSnipeNoticeMoran",
        (message, _) =>
        {
            if ((message - (Vector2)AmongUsLLImpl.LocalPlayer.transform.position).magnitude < ShotNoticeRangeOption)
            {
                var arrow = new Arrow(snipeNoticeSprite.GetSprite(), false) { IsSmallenNearPlayer = false, IsAffectedByComms = false, FixedAngle = true, OnJustPoint = true };
                arrow.Register(arrow);
                arrow.TargetPos = message;
                NebulaManager.Instance.StartCoroutine(arrow.CoWaitAndDisappear(3f).WrapToIl2Cpp());
            }
        }
        );
}
[NebulaRPCHolder]
public class MoranModifier : DefinedModifierTemplate, HasCitation, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier> ,IAssignableDocument
{
    private MoranModifier() : base("moran", Moriarty.MyTeam.Color, null, false)
    {
        ConfigurationHolder?.AddTags(ConfigurationTags.TagFunny, ConfigurationTags.TagDifficult);
    }
    Citation? HasCitation.Citation => PCitations.LTS;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(Player player, int[] arguments)
    {
        return new Instance(player);
    }

    static public MoranModifier MyRole = new MoranModifier();
    bool IAssignableDocument.HasTips => true;
    IEnumerable<DefinedAssignable> DefinedAssignable.AchievementGroups => [Moran.MyRole, Moran.MyRole, MyRole];
    [NebulaRPCHolder]
    public class SniperRifle : EquipableAbility, IGameOperator
    {
        private static SpriteLoader rifleSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.SniperRifle.png", 100f);
        public SniperRifle(GamePlayer owner) : base(owner, Moran.CanSeeRifleInShadowOption, "SniperRifle")
        {
            Renderer.sprite = rifleSprite.GetSprite();
        }

        public IPlayerlike? GetTarget(float width, float maxLength)
        {
            float minLength = maxLength;
            IPlayerlike? result = null;

            foreach (var p in GamePlayer.AllPlayerlikes)
            {
                if (p.IsDead || p.AmOwner || ((!Moran.CanKillHidingPlayerOption) && p.Logic.InVent || p.IsDived)) continue;

                if (!PatchManager.SniperCanLockMini && p.RealPlayer.TryGetModifier<PMini.Instance>(out PMini.Instance mini) && mini.IsMini())
                {
                    continue;
                }

                //吹っ飛ばされているプレイヤーは無視しない

                //不可視なプレイヤーは無視
                if (p.IsInvisible || p.WillDie) continue;

                var pos = p.TruePosition.ToUnityVector();
                Vector2 diff = pos - (Vector2)Renderer.transform.position;

                //移動と回転を施したベクトル
                var vec = diff.Rotate(-Renderer.transform.eulerAngles.z);

                if (vec.x > 0 && vec.x < minLength && Mathn.Abs(vec.y) < width * 0.5f)
                {
                    result = p;
                    minLength = vec.x;
                }
            }

            return result;
        }
    }

    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedModifier RuntimeModifier.Modifier
        {
            get
            {
                return MoranModifier.MyRole;
            }
        }
        bool RuntimeAssignable.MyCrewmateTaskIsIgnored => true;
        bool RuntimeAssignable.InvalidateCrewmateTask => true;

        static private Image buttonSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.Buttons.SnipeButton.png", 115f);
        static private Image aimAssistSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.SniperGuide.png", 100f);
        public SniperRifle? MyRifle = null;
        void ClaimMoriartyRemaining(KillerTeamCallback callback)
        {
            if (callback.ExcludedTeam != Moriarty.MyTeam)
            {
                if (Player.AllPlayers.Any((Player p) => (p.Role.GetAbility<Moran.Ability>() != null || p.TryGetModifier<MoranModifier.Instance>(out var m)) && p.IsAlive))
                {
                    callback.MarkRemaining();
                }
            }
        }
        [OnlyMyPlayer]
        private void BlockWins(PlayerBlockWinEvent ev)
        {
            ev.IsBlocked |= !Moran.CanWinAsOriginalTeamOption && ev.GameEnd != Moriarty.Instance.MoriartyTeamWin;
        }
        [OnlyMyPlayer]
        private void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd==Moriarty.Instance.MoriartyTeamWin);
        }

        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            if (MyRifle != null && Moran.StoreRifleOnUsingUtilityOption)
            {
                var p = MyPlayer.VanillaPlayer;
                if (p.onLadder || p.inMovingPlat || p.inVent) RpcEquip.Invoke((MyPlayer.PlayerId, false));
            }
        }
        ModAbilityButton equipButton = null!;
        public Instance(GamePlayer player):base(player)
        {
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (base.AmOwner || canSeeAllInfo||inEndScene)
            {
                string append = "✿";
                name += append.Color(MyRole.UnityColor);
            }
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0f, 0.25f, 0.4f, 0f);
                equipButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, "sniper.equip",
                    0f, "equip", buttonSprite);
                equipButton.OnClick = (button) =>
                {
                    if (MyRifle == null)
                    {
                        NebulaGameManager.Instance?.RpcDoGameAction(MyPlayer, MyPlayer.Position, GameActionTypes.SniperEquippingAction);
                        NebulaAsset.PlaySE(NebulaAudioClip.SniperEquip, true);
                        equipButton.SetLabel("unequip");
                    }
                    else
                        equipButton.SetLabel("equip");

                    RpcEquip.Invoke((MyPlayer.PlayerId, MyRifle == null));

                    if (MyRifle != null)
                    {
                        var circle = EffectCircle.SpawnEffectCircle(AmongUsLLImpl.LocalPlayer.transform, Vector3.zero, NebulaTeams.ImpostorTeam.Color, Moran.ShotNoticeRangeOption, null, true);
                        var script = circle.gameObject.AddComponent<ScriptBehaviour>();
                        script.UpdateHandler += () =>
                        {
                            if (MyRifle == null) circle.Disappear();
                        };
                        this.BindGameObject(circle.gameObject);
                    }
                };
                equipButton.OnBroken = (button) =>
                {
                    if (MyRifle != null)
                    {
                        equipButton.SetLabel("equip");
                        RpcEquip.Invoke((MyPlayer.PlayerId, false));
                    }
                    Snatcher.RewindKillCooldown();
                };
                GameOperatorManager.Instance?.Subscribe<MeetingStartEvent>(ev => equipButton.SetLabel("equip"), this);

                var killButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, true, false, Virial.Compat.VirtualKeyInput.Kill, "sniper.kill",
                    Moran.SnipeCoolDownOption.GetCooldown(MyPlayer.TeamKillCooldown), "snipe", null,
                    _ => MyRifle != null, _ => !equipButton.IsBroken)
                    .SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor)
                    .SetAsMouseClickButton();
                killButton.OnClick = (button) =>
                {
                    NebulaAsset.PlaySE(NebulaAudioClip.SniperShot, true);
                    var target = MyRifle?.GetTarget(Moran.ShotSizeOption, Moran.ShotEffectiveRangeOption);
                    if (target != null && !(GameOperatorManager.Instance?.Run(new PlayerInteractPlayerLocalEvent(MyPlayer, target, new(IsKillInteraction: true))).IsCanceled ?? false))
                    {
                        bool isBlown = target.IsBlown;
                        MyPlayer.MurderPlayer(target, PlayerState.Sniped, EventDetail.Kill, KillParameter.RemoteKill, null);
                        new StaticAchievementToken("moran.common1");
                        if (target.RealPlayer.Role.Role is Holmes)
                        {
                            new StaticAchievementToken("moran.common2");
                            if (MyPlayer.IsInvisible)
                            {
                                new StaticAchievementToken("moran.another1");
                            }
                            var ability = target.RealPlayer.Role.GetAbility<Holmes.Ability>();
                            if (ability != null)
                            {
                                if (ability.divideResults.ContainsKey(MyPlayer.PlayerId))
                                {
                                    new StaticAchievementToken("moran.challenge1");
                                }
                            }
                        }
                    }
                    else
                    {
                        NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.Missed, MyPlayer.VanillaPlayer, 0);
                    }
                    Sniper.RpcShowNotice.Invoke(MyPlayer.Position);

                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();

                    if (Moran.StoreRifleOnFireOption) RpcEquip.Invoke((MyPlayer.PlayerId, false));
                };
                if (PatchManager.MoriartyMeetingChat)
                {
                    PrivateChatManager.Instance.Register(MyRole.RoleColor, MyRole.RoleColor, "Moriarty", Language.Translate("chat.moriartytext"), this,() => MeetingHud.Instance, (p1, p2) => Moriarty.Instance.IsSameTeam(p1) && Moriarty.Instance.IsSameTeam(p2));
                }
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }

        [Local]
        [OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev)
        {
            if (MyRifle != null) RpcEquip.Invoke((MyPlayer.PlayerId, false));
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (MyRifle != null) RpcEquip.Invoke((MyPlayer.PlayerId, false));
        }

        System.Collections.IEnumerator CoShowAimAssist()
        {
            System.Collections.IEnumerator CoUpdateAimAssistArrow(PlayerControl player)
            {
                DeadBody? deadBody = null;
                Vector2 pos = Vector2.zero;
                Vector2 dir = Vector2.zero;
                Vector2 tempDir = Vector2.zero;
                bool isFirst = true;

                UnityEngine.Color targetColor = new UnityEngine.Color(55f / 225f, 1f, 0f);
                float t = 0f;

                SpriteRenderer? renderer = null;

                while (true)
                {
                    if (MeetingHud.Instance || MyPlayer.IsDead || MyRifle == null || IsDeadObject) break;

                    if (player.Data.IsDead && !deadBody) deadBody = Helpers.GetDeadBody(player.PlayerId);

                    //死亡して、死体も存在しなければ追跡を終了
                    if (player.Data.IsDead && !deadBody) break;

                    if (renderer == null)
                    {
                        renderer = UnityHelper.CreateObject<SpriteRenderer>("AimAssist", AmongUsLLImpl.HudManagerInstance.transform, Vector3.zero);
                        renderer.sprite = aimAssistSprite.GetSprite();
                    }

                    pos = player.Data.IsDead ? deadBody!.transform.position : player.transform.position;
                    tempDir = (pos - (Vector2)AmongUsLLImpl.LocalPlayer.transform.position).normalized;

                    NebulaGameManager.Instance!.WideCamera.CheckPlayerState(out var localScale, out var localRotateZ);
                    tempDir.x *= localScale.x;
                    tempDir.y *= localScale.y;

                    if (isFirst)
                    {
                        dir = tempDir;
                        isFirst = false;
                    }
                    else
                    {
                        dir = (tempDir + dir).normalized;
                    }

                    float angle = Mathn.Atan2(dir.y, dir.x) + localRotateZ.DegToRad();
                    renderer.transform.eulerAngles = new Vector3(0, 0, angle.RadToDeg());
                    renderer.transform.localPosition = new Vector3(Mathn.Cos(angle) * 2f, Mathn.Sin(angle) * 2f, -30f);

                    t += Time.deltaTime / 0.8f;
                    if (t > 1f) t = 1f;
                    renderer.color = UnityEngine.Color.Lerp(UnityEngine.Color.white, targetColor, t).AlphaMultiplied(0.6f);

                    yield return null;
                }

                if (renderer == null) yield break;

                float a = 0.6f;
                while (a > 0f)
                {
                    a -= Time.deltaTime / 0.8f;
                    var color = renderer.color;
                    color.a = a;
                    renderer.color = color;
                    yield return null;
                }

                GameObject.Destroy(renderer.gameObject);
            }

            yield return new WaitForSeconds(Moran.DelayInAimAssistOption);

            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (!p.AmOwner) NebulaManager.Instance.StartCoroutine(CoUpdateAimAssistArrow(p).WrapToIl2Cpp());
            }
        }

        void EquipRifle()
        {
            MyRifle = new SniperRifle(MyPlayer).Register(this);

            if (AmOwner && Moran.AimAssistOption) NebulaManager.Instance.StartCoroutine(CoShowAimAssist().WrapToIl2Cpp());
        }

        void UnequipRifle()
        {
            if (MyRifle != null) MyRifle.Release();
            MyRifle = null;
        }

        static RemoteProcess<(byte playerId, bool equip)> RpcEquip = new(
        "EquipRifleMoranModifier",
        (message, _) =>
        {
            var player = NebulaGameManager.Instance?.GetPlayer(message.playerId);
            if (player==null)
            {
                return;
            }
            var sniper = player.Modifiers.FirstOrDefault(m => m is MoranModifier.Instance) as MoranModifier.Instance;
            if (sniper != null)
            {
                if (message.equip)
                    sniper.EquipRifle();
                else
                    sniper.UnequipRifle();
            }
        }
        );
    }

    private static SpriteLoader snipeNoticeSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.SniperRifleArrow.png", 200f);
    public static RemoteProcess<Vector2> RpcShowNotice = new(
        "ShowSnipeNoticeMoranModifier",
        (message, _) =>
        {
            if ((message - (Vector2)AmongUsLLImpl.LocalPlayer.transform.position).magnitude < Moran.ShotNoticeRangeOption)
            {
                var arrow = new Arrow(snipeNoticeSprite.GetSprite(), false) { IsSmallenNearPlayer = false, IsAffectedByComms = false, FixedAngle = true, OnJustPoint = true };
                arrow.Register(arrow);
                arrow.TargetPos = message;
                NebulaManager.Instance.StartCoroutine(arrow.CoWaitAndDisappear(3f).WrapToIl2Cpp());
            }
        }
        );
}
[NebulaRPCHolder]
public class Holmes : DefinedSingleAbilityRoleTemplate<Holmes.Ability>, DefinedRole,HasCitation
{
    private Holmes() : base("holmes", new(214, 156, 45), RoleCategory.CrewmateRole,NebulaTeams.CrewmateTeam, [SurveyCooldownOption, SurveyMaxUseNumOption],false,true,()=>((ISpawnable)Moriarty.MyRole).IsSpawnable&&Moriarty.CanWinByKillHolmes)
    {
        ConfigurationHolder?.AddTags(ConfigurationTags.TagChaotic);
        ConfigurationHolder?.ScheduleAddRelated(() => [Moriarty.MyRole.ConfigurationHolder, Moran.MyRole.ConfigurationHolder]);
    }

    static private readonly FloatConfiguration SurveyCooldownOption = NebulaAPI.Configurations.Configuration("options.role.holmes.surveyCooldown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private readonly IntegerConfiguration SurveyMaxUseNumOption = NebulaAPI.Configurations.Configuration("options.role.holmes.surveymaxUseNum", (1,15),4);
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player);
    bool ISpawnable.IsSpawnable => ((ISpawnable)Moriarty.MyRole).IsSpawnable && Moriarty.CanWinByKillHolmes;
    Citation? HasCitation.Citation { get { return PCitations.LTS; } }
    static public readonly Holmes MyRole = new();
    public class Ability : AbstractPlayerAbility, IPlayerAbility
    {

        static private readonly Image buttonSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.Buttons.OracleButton.png", 115f);
        public Dictionary<byte, (string longName, string shortName)> divideResults = [];
        bool playerisevil;
        GamePlayer exilePlayer;
        [Local]
        void OnTaskRestart(TaskPhaseRestartEvent ev)
        {
            if (playerisevil&&exilePlayer!=null&&exilePlayer.IsDead)
            {
                new StaticAchievementToken("holmes.common2");
            }
            playerisevil = false;
            exilePlayer = null;
        }
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (useskill&&!MyPlayer.IsDead)
            {
                new StaticAchievementToken("holmes.challenge1");
            }
        }
        [Local,OnlyMyPlayer]
        void OnMeDead(PlayerMurderedEvent ev)
        {
            if (moriartyTeamCheck&&(ev.Murderer.Role.Role is Moran||ev.Murderer.TryGetModifier<MoranModifier.Instance>(out var m)))
            {
                new StaticAchievementToken("holmes.another1");
            }
        }
        bool useskill;
        bool moriartyTeamCheck;
        public Ability(GamePlayer player) : base(player)
        {
            if (AmOwner)
            {
                playerisevil = false;
                exilePlayer = null;
                moriartyTeamCheck = false;
                int leftuse = SurveyMaxUseNumOption;
                var playerTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeStandardPredicate(p) && !divideResults.ContainsKey(p.RealPlayer.PlayerId));

                var oracleButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    SurveyCooldownOption, "survey", buttonSprite,
                    _ => playerTracker.CurrentTarget != null, null);
                oracleButton.OnClick = (button) => 
                {
                    leftuse--;
                    var player = playerTracker.CurrentTarget.RealPlayer;
                    var result = player.Role.Role;
                    if (player.Role is Moriarty.Instance || player.Role.Role is Moran || player.TryGetModifier<MoranModifier.Instance>(out var m))
                    {
                        moriartyTeamCheck = true;
                        var allroles = Nebula.Roles.Roles.AllRoles.Where(r=>!divideResults.Values.Any(v=>r.DisplayColoredName==v.longName));
                        result=allroles.Where(r => r.Category == RoleCategory.CrewmateRole && r.IsSpawnable).ToList().Random();
                    }
                    divideResults[player.PlayerId]=(result.DisplayColoredName,result.DisplayColoredShort);
                    if (player.IsImpostor||!player.IsCrewmate)
                    {
                        playerisevil = true;
                        exilePlayer = player;
                    }
                    button.UpdateUsesIcon(leftuse.ToString());
                    if (leftuse<=0)
                    {
                        button.HideUsesIcon();
                    }
                    new StaticAchievementToken("holmes.common1");
                    button.StartCoolDown();
                    useskill = true;
                };
                oracleButton.ShowUsesIcon(3, leftuse.ToString());
                oracleButton.Visibility = (button) => !MyPlayer.IsDead && leftuse > 0;
                oracleButton.SetLabel("survey");
            }
        }

        [Local]
        void ReflectRoleName(PlayerSetFakeRoleNameEvent ev)
        {
            if (divideResults.TryGetValue(ev.Player.PlayerId, out var roleName)) ev.Alternate(ev.InMeeting ? roleName.longName : roleName.shortName);
        }
    }
}