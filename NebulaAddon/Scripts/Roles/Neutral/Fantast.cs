using Nebula.Game.Statistics;
using Nebula.Roles.Complex;
using Nebula.Roles.Modifier;
using Nebula.VoiceChat;
using Virial;
using Virial.Assignable;
using Virial.Components;
using Virial.Configuration;
using Virial.DI;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Game;
using Virial.Runtime;
using Virial.Helpers;
using Plana.Roles.Crewmate;

namespace Plana.Roles.Neutral;

[NebulaPreprocess(PreprocessPhase.BuildAssignmentTypes)]
internal static class FantastAssignmentSetUp
{
    static public Virial.Color Color = new(107,107,255);
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        PDebug.Log("Add Fantast Assignment Type");
        preprocessor.RegisterAssignmentType(() => Fantast.MyRole, (lastArgs, role) => Fantast.GenerateArgument(lastArgs.Get(0,0), role), "fantastized", Color, (status, role) => status.HasFlag(AbilityAssignmentStatus.CanLoadToKillNeutral), () => (Fantast.MyRole as ISpawnable).CanSpawnInCurrentGame && Fantast.FantastizedImpostorOption);
    }
}
[NebulaPreprocess(PreprocessPhase.PostRoles)]
internal class FantastTeamAllocator : AbstractModule<Virial.Game.Game>, IGameOperator
{
    private static void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule<Virial.Game.Game>(() => new FantastTeamAllocator());
    }

    private FantastTeamAllocator()
    {
        this.RegisterPermanently();
    }

    private void OnFixAssignmentTable(PreFixAssignmentEvent ev)
    {
        byte[] fantasts = ev.RoleTable.GetPlayers(Fantast.MyRole).ToArray<byte>();
        int i;
        for (i = 0; i < fantasts.Length; i++)
        {
            ev.RoleTable.EditRole(fantasts[i], last=>
            {
                if (last.Item2.Length < 1)
                {
                    return new ValueTuple<DefinedRole, int[]>(last.Item1, new int[] { i });
                }
                last.Item2[0] = i;
                return new ValueTuple<DefinedRole, int[]>(last.Item1, last.Item2);
            });
        }
    }
}
public class Fantast : DefinedRoleTemplate, HasCitation, DefinedRole, IAssignableDocument
{
    static readonly public RoleTeam MyTeam = new Team("teams.fantast", FantastAssignmentSetUp.Color, TeamRevealType.OnlyMe, () => KillCooldown);
    static readonly public GameEnd FantastWin = NebulaAPI.Preprocessor!.CreateEnd("fantast", MyTeam.Color);
    private Fantast() : base("fantast", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [KillCoolDownOption,InitialCreateFancyNumOption,KillAddCreateFancyNumOption,SidekickCooldown,WhenleftFancynumAllAwareOption,guessDeadNoPromote,MaxCreateFancyNumOption, FantastizedImpostorOption,TeamGuesserOption,TeamGuessNum])
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Fancy.MyRole.ConfigurationHolder!]);
        ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImages/Fantast.png")!.AsImage(115f)!;

    }
    bool AssignableFilterHolder.CanLoadDefault(DefinedAssignable assignable)
    {
        return base.CanLoadDefaultTemplate(assignable) && (!TeamGuesserOption || !(assignable is Nebula.Roles.Complex.GuesserModifier));
    }
    IEnumerable<DefinedRole> DefinedRole.GetGuessableAbilityRoles() => ((this as DefinedRole).IsSpawnable && !FantastizedImpostorOption) ? [this] : [];
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments.Get(0, player.PlayerId),arguments.Get(1,TeamGuessNum), Nebula.Roles.Roles.GetRole(arguments.Get(2, -1)), arguments.Skip(3).ToArray());
    static public int[] GenerateArgument(int FantastTeamId, DefinedRole? role) =>[FantastTeamId,TeamGuessNum, role?.Id ?? -1, .. (role?.DefaultAssignableArguments ?? [])];
    static public IRelativeCooldownConfiguration KillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.fantast.killCoolDown", CoolDownType.Relative, (0f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), -5f, (0.125f, 2f, 0.125f), 1f);
    static public IntegerConfiguration InitialCreateFancyNumOption = NebulaAPI.Configurations.Configuration("options.role.fantast.InitialCreateFancyNum", (0,16),1);
    static public IntegerConfiguration KillAddCreateFancyNumOption = NebulaAPI.Configurations.Configuration("options.role.fantast.KillAddCreateFancyNum", (0, 16), 1);
    static public FloatConfiguration SidekickCooldown = NebulaAPI.Configurations.Configuration("options.role.fantast.sidekickCooldown", (0f, 120f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static public IntegerConfiguration WhenleftFancynumAllAwareOption = NebulaAPI.Configurations.Configuration("options.role.fantast.whenleftFancyNumAllAware", (0, 16), 1);
    static public BoolConfiguration guessDeadNoPromote = NebulaAPI.Configurations.Configuration("options.role.fantast.guessDeadNoPromote", true);
    static public IntegerConfiguration MaxCreateFancyNumOption = NebulaAPI.Configurations.Configuration("options.role.fantast.maxcreateFancyNum", (1, 20), 3);
    static public BoolConfiguration FantastizedImpostorOption = NebulaAPI.Configurations.Configuration("options.role.fantast.fantastizedImpostor", false);
    static public BoolConfiguration TeamGuesserOption = NebulaAPI.Configurations.Configuration("options.role.fantast.teamguesser", true);
    static public IntegerConfiguration TeamGuessNum = NebulaAPI.Configurations.Configuration("options.role.fantast.teamguessnum",(1,15),5);
    static public float KillCooldown => KillCoolDownOption.Cooldown;
    
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Fantast.png")!.AsImage();
    static public Fantast MyRole = new Fantast();
    bool IAssignableDocument.HasTips => true;
    static public GameStatsEntry StatsFancy = NebulaAPI.CreateStatsEntry("stats.fantast.createfancy", GameStatsCategory.Roles, MyRole);

    bool DefinedRole.IsKiller => true;
    public static bool IsFantastLeader(GamePlayer player, int teamId, bool excludeDefeatedFantast = false)
    {
        if (player.Role is Instance j) return j.FantastTeamId == teamId && (!excludeDefeatedFantast || !j.IsDefeatedFantast);
        return false;
    }

    public static bool IsFantastTeam(GamePlayer player, int teamId, bool excludedModifier = false)
    {
        if (player.Role is Instance j) return j.FantastTeamId == teamId;
        if(player.Role is Fancy.Instance s) return s.FantastTeamId == teamId;
        if (excludedModifier) return false;
        return player.Unbox().GetModifiers<FancyModifier.Instance>().Any(m => m.FantastTeamId == teamId);
    }

    static public bool ShouldShowTeamId(int id, bool canSeeAllInfo)
    {
        canSeeAllInfo |= !AmongUsLLImpl.AmongUsClientInstance || AmongUsLLImpl.AmongUsClientInstance.GameState == InnerNet.InnerNetClient.GameStates.Ended;
        if (!canSeeAllInfo) return false;
        if (id > 0) return true;
        bool moreThanOne = ((Fantast.MyRole as DefinedSingleAssignable)?.AllocationParameters?.RoleCountSum ?? 0) > 1;
        return moreThanOne && canSeeAllInfo;
    }
    static public string GetDisplayTeamId(int id) => ((char)('A' + id)).ToString();

    static public string AppendTeamIdIfNecessary(string orig, int id, bool canSeeAlllInfo, bool withParentheses) => ShouldShowTeamId(id, canSeeAlllInfo) ? withParentheses ? orig + "(" + GetDisplayTeamId(id) + ")" : orig + " " + GetDisplayTeamId(id) : orig;

    IUsurpableAbility? DefinedRole.GetUsurpedAbility(Virial.Game.Player player, int[] arguments)
    {
        var role = Nebula.Roles.Roles.GetRole(arguments.Get(0, -1));
        var ability = role?.GetUsurpedAbility(player, arguments.Skip(1).ToArray());
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
    public static readonly RemoteProcess<(GamePlayer, int)> RpcUpdateFantastTeamLeftGuess = new("RpcUpdateFantastTeamLeftGuess", (m, _) =>
    {
        if (m.Item1?.Role is Instance instance)
        {
            instance.LeftGuess = m.Item2;
        }
    });
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        void OnCheckTeamSide(PlayerIsSameSideCallback ev)
        {
            if (IsSameTeam(ev.Player1,true) && IsSameTeam(ev.Player2,true))
            {
                ev.MarkAsSameSide();
            }
        }
        DefinedRole RuntimeRole.Role => MyRole;

        public int FantastTeamId;
        public int LeftGuess;
        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp => MyFantastized != null ? [MyRole, MyFantastized,..FantastizedAbility?.SubAssignableOnHelp??[]] : [MyRole];
        public DefinedRole? MyFantastized { get; private set; }
        public IPlayerAbility? FantastizedAbility { get; private set; } = null;
        private int[] StoredFantastizedArgument { get; set; }

        string RuntimeRole.DisplayIntroBlurb => (MyFantastized ?? MyRole).DisplayIntroBlurb;
        string RuntimeRole.DisplayIntroRoleName => (MyFantastized ?? MyRole).DisplayName;
       
        IEnumerable<IPlayerAbility?> RuntimeAssignable.MyAbilities => FantastizedAbility != null ? [FantastizedAbility, ..FantastizedAbility.SubAbilities] : [];
        int leftCreateFancyNum;
        private int LeftCreateFancy => Math.Max(0, leftCreateFancyNum);
        public Instance(GamePlayer player,int FantastTeamId,int leftGuess, DefinedRole? Fantastized, int[] FantastizedArgument) : base(player)
        {
            this.FantastTeamId = FantastTeamId;
            LeftGuess = leftGuess;
            MyFantastized = Fantastized;
            StoredFantastizedArgument = FantastizedArgument;
        }

        static private Image FancyButtonSprite = NebulaAPI.AddonAsset.GetResource("FantastInvite.png")!.AsImage(115f)!;

        bool RuntimeRole.CanUseVent => true;
        //RoleAssignmentでもこの配置を前提とした部分がある点に注意(ジャッカル化の割り当てのため)
        int[] FantastArguments => [FantastTeamId,LeftGuess, MyFantastized?.Id ?? -1];
        int[]? RuntimeAssignable.RoleArguments => FantastArguments.Concat(FantastizedAbility?.AbilityArguments ?? []).ToArray();
        int[]? RuntimeRole.UsurpedAbilityArguments => (FantastizedAbility?.AbilityArguments ?? []).Prepend(MyFantastized?.Id ?? -1).ToArray();
        bool RuntimeRole.CheckGuessAbility(DefinedRole abilityRole) => abilityRole == MyFantastized || abilityRole == MyRole;
        public int[] RoleArgumentsForFancy
        {
            get
            {
                return (this as RuntimeAssignable).RoleArguments ?? [];
            }
        }
        public bool IsMyFancy(GamePlayer? player,bool excludedModifier=false,bool excludedRole=false)
        {
            if (player == null) return false;
            if (!excludedRole)
            {
                if (player.Role is Fancy.Instance Fancy && Fancy.FantastTeamId == FantastTeamId) return true;
            }
            if (!excludedModifier)
            {
                if (player.Modifiers.Any(m => m is FancyModifier.Instance Fancy && Fancy.FantastTeamId == FantastTeamId)) return true;
            }
            return false;
        }

        public bool IsSameTeam(GamePlayer? player,bool excludedModifier=false,bool excludedRole=false)
        {
            if (IsMyFancy(player,excludedModifier,excludedRole)) return true;
            if (player?.Role is Instance Fantast && Fantast.FantastTeamId == FantastTeamId) return true;
            return false;
        }

        public bool IsMyFantast(GamePlayer? player)
        {
            if (player?.Role is Instance Fantast && Fantast.FantastTeamId == FantastTeamId) return true;
            return false;
        }

        //勝敗の決定時に使用
        internal bool IsDefeatedFantast = false;

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd == FantastWin && NebulaGameManager.Instance!.AllPlayerInfo.Any(p=>IsFantastLeader(p,FantastTeamId,true)));
        }

        void RuntimeRole.Usurp()
        {
            (FantastizedAbility as IUsurpableAbility)?.Usurp();
        }
        int createFancynum;
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                createFancynum = 0;
                leftCreateFancyNum = InitialCreateFancyNumOption;
                var myKillTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeLocalKillablePredicate(p) && !IsMyFancy(p.RealPlayer), MyRole.UnityColor, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption);
                var myFancyTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeStandardPredicate(p) && !IsMyFancy(p.RealPlayer), MyRole.UnityColor, false);
                ModAbilityButton? FancyButton = null;
                FancyButton = NebulaAPI.Modules.InteractButton(this, MyPlayer, myFancyTracker, new PlayerInteractParameter(RealPlayerOnly: true), false, true, Virial.Compat.VirtualKeyInput.SidekickAction, null,
                    SidekickCooldown, "cfancy", FancyButtonSprite,
                    (p, button) =>
                    {
                        button.StartCoolDown();
                        createFancynum++;
                        leftCreateFancyNum--;
                        button.ShowUsesIcon(3,LeftCreateFancy.ToString());
                        myFancyTracker.CurrentTarget?.RealPlayer.AddModifier(FancyModifier.MyRole, [1, FantastTeamId]);
                        StatsFancy.Progress();
                    },
                    _ => LeftCreateFancy >0 && !MyPlayer.IsDived,_=>createFancynum<MaxCreateFancyNumOption);
                FancyButton.ShowUsesIcon(3, LeftCreateFancy.ToString());
                var killButton = NebulaAPI.Modules.InteractButtonInner(this, MyPlayer, myKillTracker, new(false, true, true), true, true, false, Virial.Compat.VirtualKeyInput.Kill, null,
                    KillCoolDownOption.Cooldown, "kill", ModAbilityButton.LabelType.Impostor, null!,
                    (player, button) =>
                    {
                        var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEvent(MyPlayer, player));
                        if (!(cancelable?.IsCanceled ?? false))
                        {
                            MyPlayer.MurderPlayer(player, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                        }
                        if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    }, (_) => !MyPlayer.IsDived, (_) => MyPlayer.AllowToShowKillButtonByAbilities,null
                    );
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
                GameOperatorManager.Instance?.Subscribe<PlayerMurderedEvent>(ev =>
                {
                    if (ev.Murderer.AmOwner&&!ev.Dead.AmOwner)
                    {
                        if (ev.Dead.PlayerState == PlayerState.Guessed) return;//推察キルは対象外
                        if (KillAddCreateFancyNumOption>0)
                        {
                            leftCreateFancyNum += KillAddCreateFancyNumOption;
                            FancyButton?.ShowUsesIcon(3, LeftCreateFancy.ToString());
                        }
                    }
                }, this);
                if (PatchManager.FantastMeetingChat)
                {
                    PrivateChatManager.Instance?.Register(MyRole.RoleColor, MyRole.RoleColor, "Fantast" + FantastTeamId, Language.Translate("chat.fantasttext"),this,() => MeetingHud.Instance, (p1, p2) => Fantast.IsFantastTeam(p1, FantastTeamId, true) && Fantast.IsFantastTeam(p2, FantastTeamId, true));
                }
            }

            FantastizedAbility = MyFantastized?.GetAbilityOnRole(MyPlayer, AbilityAssignmentStatus.CanLoadToKillNeutral, StoredFantastizedArgument)?.Register(this);
        }

        void DecorateFancyColor(PlayerDecorateNameEvent ev)
        {
            if (IsSameTeam(GamePlayer.LocalPlayer,true)&&IsSameTeam(ev.Player)) ev.Color = Fantast.MyRole.RoleColor;
        }

        string RuntimeAssignable.DisplayName { get { 
                var name = Fantast.AppendTeamIdIfNecessary(MyFantastized?.GetDisplayName(FantastizedAbility!) ?? (MyRole as DefinedRole).DisplayName, FantastTeamId, NebulaGameManager.Instance?.CanSeeAllInfo ?? false, false);
                return name;
            }
        }
        string RuntimeAssignable.DisplayColoredName => (this as RuntimeAssignable).DisplayName.Color(MyTeam.UnityColor);
        string RuntimeRole.DisplayShort => Fantast.AppendTeamIdIfNecessary(MyFantastized?.GetDisplayShort(FantastizedAbility!) ?? (MyRole as DefinedRole).DisplayShort, FantastTeamId, NebulaGameManager.Instance?.CanSeeAllInfo ?? false, true);

        void PromoteFancy()
        {
            try
            {
                if (MyPlayer.TryGetModifier<HasLove.Instance>(out var haslove)) return;
                var myfancies = NebulaGameManager.Instance!.AllPlayerInfo.Where(p => p.IsAlive && IsMyFancy(p, false, true)).ToList();
                PDebug.Log("promote list count:" + myfancies.Count.ToString());
                if (myfancies.Count <= 0) return;
                else if (myfancies.Count <= WhenleftFancynumAllAwareOption)
                {
                    PDebug.Log("AllAware");
                    using (RPCRouter.CreateSection("Fancy"))
                    {
                        myfancies.Do(p =>
                        {
                            p.SetRole(Fancy.MyRole, [1,..RoleArgumentsForFancy]);
                            p.RemoveModifier(FancyModifier.MyRole);
                        });
                    }
                    return;
                }
                PDebug.Log("SingleAware");
                var promoteFancy = myfancies[UnityEngine.Random.Range(0, myfancies.Count)];
                using (RPCRouter.CreateSection("Fancy"))
                {
                    promoteFancy.SetRole(Fancy.MyRole, [1,..RoleArgumentsForFancy]);
                    promoteFancy.RemoveModifier(FancyModifier.MyRole);
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }

        [OnlyHost]
        void OnPlayerDead(PlayerDieOrDisconnectEvent ev)
        {
            if (NebulaGameManager.Instance!.AllPlayerInfo.Any(p =>IsSameTeam(p, true)&&!p.IsDead))
            {
                return;
            }
            if (guessDeadNoPromote&&ev.Player.PlayerState==PlayerState.Guessed)
            {
                return;
            }
            if (ev.Player.PlayerId==MyPlayer.PlayerId)
            {
                PromoteFancy();
                return;
            }
            if (IsMyFancy(ev.Player, true))
            {
                PromoteFancy();
            }
        }

        bool RuntimeRole.HasImpostorVision => true;
        bool RuntimeRole.IgnoreBlackout => true;
        void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (IsSameTeam(ev.Player,true)&&IsSameTeam(ev.Target)) ev.SetAsCannotKillBasically();
        }

        void OnPlayerDie(PlayerDieEvent ev)
        {
            ModSingleton<IWinningOpportunity>.Instance.SetOpportunity(MyTeam, Nebula.Roles.Impostor.KillerOpportunityHelpers.CalcTeamOpportunity(p => MyPlayer == p, p=>IsMyFancy(p,true)));
        }

        void ClaimFantastRemaining(KillerTeamCallback callback)
        {
            if (callback.ExcludedTeam == Fantast.MyTeam) return;
            if(MyPlayer.IsAlive) callback.MarkRemaining();
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (!Fantast.TeamGuesserOption)
            {
                return;
            }
            GuesserSystem.OnMeetingStart(LeftGuess, () => RpcUpdateFantastTeamLeftGuess.Invoke((MyPlayer,LeftGuess-1)));
        }
        [Local]
        [OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev)
        {
            if (!Fantast.TeamGuesserOption)
            {
                return;
            }
            GuesserSystem.OnDead();
        }
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (!Fantast.TeamGuesserOption)
            {
                return;
            }
            GuesserSystem.OnGameEnd(MyPlayer);
        }
    }
}

public class Fancy : DefinedRoleTemplate, HasCitation, DefinedRole, IAssignableDocument
{
    private Fancy() : base("fancy", Fantast.MyTeam.Color, RoleCategory.NeutralRole, Fantast.MyTeam, [KillCoolDownOption], false, optionHolderPredicate: ()=> (Fantast.MyRole as DefinedRole).IsSpawnable ) {
        ConfigurationHolder?.ScheduleAddRelated(() => [Fantast.MyRole.ConfigurationHolder!]);
        ConfigurationHolder!.Title = ConfigurationHolder.Title.WithComparison("fantast.fancy");
    }
    IEnumerable<DefinedAssignable> DefinedAssignable.AchievementGroups => [Fancy.MyRole, MyRole];
    string DefinedAssignable.InternalName => "fantast.fancy";
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    IEnumerable<DefinedRole> DefinedRole.GetGuessableAbilityRoles() => ((Fantast.MyRole as DefinedRole).IsSpawnable && !Fantast.FantastizedImpostorOption) ? [this] : [];
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments.GetAsBool(0,false), arguments.Get(1, 0),Nebula.Roles.Roles.GetRole(arguments.Get(3,-1)),arguments.Skip(4).ToArray());
    static private IRelativeCooldownConfiguration KillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.fancy.killCoolDown", CoolDownType.Relative, (0f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), -5f, (0.125f, 2f, 0.125f), 1f ,null,() => Fantast.KillCooldown);

    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Fancy.png")!.AsImage();
    static public Fancy MyRole = new Fancy();
    bool IAssignableDocument.HasTips => true;
    bool ISpawnable.IsSpawnable { get => (Fantast.MyRole as DefinedSingleAssignable).IsSpawnable; }
    bool DefinedRole.IsKiller => true;
    IUsurpableAbility? DefinedRole.GetUsurpedAbility(Virial.Game.Player player, int[] arguments)
    {
        var role = Nebula.Roles.Roles.GetRole(arguments.Get(0, -1));
        var ability = role?.GetUsurpedAbility(player, arguments.Skip(1).ToArray());
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
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp => MyFantastized != null ? [MyRole, MyFantastized, .. FantastizedAbility?.SubAssignableOnHelp ?? []] : [MyRole];
        public DefinedRole? MyFantastized { get; private set; }
        public IPlayerAbility? FantastizedAbility { get; private set; } = null;
        private int[] StoredFantastizedArgument { get; set; }

        string RuntimeRole.DisplayIntroBlurb => (MyFantastized ?? MyRole).DisplayIntroBlurb;
        string RuntimeRole.DisplayIntroRoleName => (MyFantastized ?? MyRole).DisplayName;

        IEnumerable<IPlayerAbility?> RuntimeAssignable.MyAbilities => FantastizedAbility != null ? [FantastizedAbility, .. FantastizedAbility.SubAbilities] : [];
        bool RuntimeRole.CanUseVent => true;
        bool RuntimeRole.HasImpostorVision => true;
        bool RuntimeRole.IgnoreBlackout => true;
        string RuntimeAssignable.DisplayName
        {
            get
            {
                var name = Fantast.AppendTeamIdIfNecessary(MyFantastized?.GetDisplayName(FantastizedAbility!) ?? (MyRole as DefinedRole).DisplayName, FantastTeamId, NebulaGameManager.Instance?.CanSeeAllInfo ?? false, false);
                return name;
            }
        }
        string RuntimeAssignable.DisplayColoredName => (this as RuntimeAssignable).DisplayName.Color(MyRole.UnityColor);
        string RuntimeRole.DisplayShort => Fantast.AppendTeamIdIfNecessary(MyFantastized?.GetDisplayShort(FantastizedAbility!) ?? (MyRole as DefinedRole).DisplayShort, FantastTeamId, NebulaGameManager.Instance?.CanSeeAllInfo ?? false, true);

        bool RuntimeRole.CheckGuessAbility(DefinedRole abilityRole) => abilityRole == MyFantastized || abilityRole == MyRole;
        public int FantastTeamId;
        private bool ShouldResolveToFantastTeamId = false; //チームIDの代わりにFantastのプレイヤーIDが与えられる場合、true。割り当て直後にチームIDを解決する必要がある。
        public Instance(GamePlayer player, bool givenTeamId, int FantastId,DefinedRole? fantastizedRole, int[] fantastizedArguments) : base(player)
        {
            ShouldResolveToFantastTeamId = !givenTeamId;
            FantastTeamId = FantastId;
            MyFantastized = fantastizedRole;
            StoredFantastizedArgument = fantastizedArguments;
        }

        int[]? RuntimeAssignable.RoleArguments => [1, FantastTeamId,MyFantastized?.Id??-1,..FantastizedAbility?.AbilityArguments??[]];
        int[]? RuntimeRole.UsurpedAbilityArguments => (FantastizedAbility?.AbilityArguments ?? []).Prepend(MyFantastized?.Id ?? -1).ToArray();
        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == Fantast.FantastWin && NebulaGameManager.Instance!.AllPlayerInfo.Any(p => Fantast.IsFantastLeader(p, FantastTeamId, true)));
        void RuntimeRole.Usurp()
        {
            (FantastizedAbility as IUsurpableAbility)?.Usurp();
        }
        Fantast.Instance GetMyLeader()
        {
            return NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => Fantast.IsFantastLeader(p, FantastTeamId))!.Role as Fantast.Instance;
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (!Fantast.TeamGuesserOption)
            {
                return;
            }
            var myleader = GetMyLeader();
            if (myleader==null)
            {
                return;
            }
            GuesserSystem.OnMeetingStart(myleader.LeftGuess, () => Fantast.RpcUpdateFantastTeamLeftGuess.Invoke((myleader.MyPlayer, myleader.LeftGuess - 1)));
        }
        [Local]
        [OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev)
        {
            if (!Fantast.TeamGuesserOption)
            {
                return;
            }
            GuesserSystem.OnDead();
        }
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (!Fantast.TeamGuesserOption)
            {
                return;
            }
            GuesserSystem.OnGameEnd(MyPlayer);
        }
        void OnCheckTeamSide(PlayerIsSameSideCallback ev)
        {
            if (Fantast.IsFantastTeam(ev.Player1, FantastTeamId,true) && Fantast.IsFantastTeam(ev.Player2,FantastTeamId, true))
            {
                ev.MarkAsSameSide();
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (ShouldResolveToFantastTeamId)
            {
                FantastTeamId = (GamePlayer.GetPlayer((byte)FantastTeamId)?.Role as Fantast.Instance)?.FantastTeamId ?? 0;
                ShouldResolveToFantastTeamId = false;
            }
            if (AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(Fantast.MyRole.RoleColor, 0f, 0.25f, 0.45f);
                var myTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, ObjectTrackers.PlayerlikeLocalKillablePredicate, Fantast.MyRole.UnityColor,Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption);
                var killButton = NebulaAPI.Modules.InteractButtonInner(this, MyPlayer, myTracker, new(false, true, true), true, true, false, Virial.Compat.VirtualKeyInput.Kill, null,
                    KillCoolDownOption.Cooldown, "kill", ModAbilityButton.LabelType.Impostor, null!,
                    (player, button) =>
                    {
                        var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEvent(MyPlayer, player));
                        if (!(cancelable?.IsCanceled ?? false))
                        {
                            MyPlayer.MurderPlayer(player, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                        }
                        if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    }, _ => !MyPlayer.IsDived, _ => MyPlayer.AllowToShowKillButtonByAbilities, null
                    );
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
                if (Fantast.TeamGuesserOption&&MyPlayer.Modifiers.Any(m=>m is GuesserModifier.Instance))
                {
                    MyPlayer.RemoveModifier(GuesserModifier.MyRole);
                }
                if (PatchManager.FantastMeetingChat)
                {
                    PrivateChatManager.Instance?.Register(MyRole.RoleColor, MyRole.RoleColor, "Fantast" + FantastTeamId, Language.Translate("chat.fantasttext"), this,() => MeetingHud.Instance, (p1, p2) => Fantast.IsFantastTeam(p1, FantastTeamId, true) && Fantast.IsFantastTeam(p2, FantastTeamId, true));
                }
            }
            FantastizedAbility = MyFantastized?.GetAbilityOnRole(MyPlayer, AbilityAssignmentStatus.CanLoadToKillNeutral, StoredFantastizedArgument)?.Register(this);
        }
        void ClaimFantastRemaining(KillerTeamCallback callback)
        {
            if (callback.ExcludedTeam == Fantast.MyTeam) return;
            if (MyPlayer.IsAlive) callback.MarkRemaining();
        }
    }
}

public class FancyModifier : DefinedModifierTemplate, HasCitation, DefinedModifier ,IAssignableDocument
{
    static public FancyModifier MyRole = new FancyModifier();
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/FancyModifier.png")!.AsImage();

    bool IAssignableDocument.HasTips => true;
    private FancyModifier() : base("fancy", Fantast.MyTeam.Color, withConfigurationHolder: false) { }
    IEnumerable<DefinedAssignable> DefinedAssignable.AchievementGroups => [Fancy.MyRole, MyRole];
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments.GetAsBool(0, false), arguments.Get(1, 0));
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public int FantastTeamId;
        private bool ShouldResolveToFantastTeamId = false;
        public Instance(GamePlayer player, bool givenTeamId, int FantastId) : base(player)
        {
            ShouldResolveToFantastTeamId = !givenTeamId;
            FantastTeamId = FantastId;
        }

        int[] RuntimeAssignable.RoleArguments => [1, FantastTeamId];
        bool RuntimeAssignable.CanBeAwareAssignment => NebulaGameManager.Instance?.CanSeeAllInfo ?? false;
        void RuntimeAssignable.DecorateNameConstantly(ref string name,bool canSeeAllInfo, bool inEndScene)
        {
            if (canSeeAllInfo||inEndScene) {
                var append = "★";
                append = Fantast.AppendTeamIdIfNecessary(append, FantastTeamId, canSeeAllInfo, true );
                name += append.Color(Fantast.MyRole.UnityColor);
            }
        }

        void RuntimeAssignable.OnActivated()
        {
            if (ShouldResolveToFantastTeamId)
            {
                FantastTeamId = (GamePlayer.GetPlayer((byte)FantastTeamId)?.Role as Fantast.Instance)?.FantastTeamId ?? 0;
                ShouldResolveToFantastTeamId = false;
            }
        }

        //bool RuntimeAssignable.MyCrewmateTaskIsIgnored => true;
    }
}

[NebulaPreprocess(PreprocessPhase.PostBuildNoS)]
internal class FantastCriteria : AbstractModule<IGameModeStandard>, IGameOperator
{
    static FantastCriteria() => DIManager.Instance.RegisterModule(() => new FantastCriteria().RegisterPermanently());

    List<Fantast.Instance> allAliveFantasts = [];
    [OnlyHost]
    void OnUpdate(GameUpdateEvent ev)
    {
        int totalAlive = 0;
        bool leftOtherKillers = GameOperatorManager.Instance?.Run(new KillerTeamCallback(Fantast.MyTeam)).RemainingOtherTeam ?? false; //帆かキラーが残っていても殲滅勝利は可能

        //全生存者数を数え、生存しているジャッカルチームを発見する。
        allAliveFantasts.Clear();
        foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo)
        {
            if (p.Role is Fantast.Instance jRole) allAliveFantasts.Add(jRole);
            if (p.IsDead) continue;
            totalAlive++;
        }



        ulong FantastMask = 0;
        int teamCount = 0;
        ulong winningFantastTeams = 0;
        ulong completeWinningFantastMask = 0;

        //全ジャッカルに対して、各チームごとに勝敗を調べる
        foreach (var Fantast in allAliveFantasts)
        {
            //ジャッカル陣営の数をカウントする
            ulong myMask = 1ul << Fantast!.FantastTeamId;
            if ((FantastMask & myMask) == 0) teamCount++;
            else continue; //既に考慮したチームはスキップしてよい
            FantastMask |= myMask;

            //死亡しておらず、同チーム、かつラバーズでないか相方死亡ラバー。ただし、サイドキックがキル人外でないならジャッカル本人のみ
            int aliveFantasts = NebulaGameManager.Instance!.AllPlayerInfo.Count(p => !p.IsDead && Fantast!.IsSameTeam(p,true) && (!p.TryGetModifier<Lover.Instance>(out var lover) || lover.IsAloneLover));
            if (aliveFantasts<=0)
            {
                teamCount--;
                continue;
            }
            //完全殲滅勝利
            if (aliveFantasts == totalAlive) completeWinningFantastMask |= myMask;
            //キル勝利
            if (aliveFantasts * 2 >= totalAlive && !leftOtherKillers) winningFantastTeams|=myMask;
        }

        //キル勝利のトリガー
        if (teamCount == 1 && winningFantastTeams != 0)
        {
            allAliveFantasts.Do(j => j.IsDefeatedFantast = (winningFantastTeams & (1ul << j.FantastTeamId)) == 0ul);
            NebulaAPI.CurrentGame?.TriggerGameEnd(Fantast.FantastWin, GameEndReason.Situation);
            return;
        }
        //完全殲滅勝利のトリガー
        if (completeWinningFantastMask != 0)
        {
            allAliveFantasts.Do(j => j.IsDefeatedFantast = (completeWinningFantastMask & (1ul << j.FantastTeamId)) == 0ul);
            NebulaAPI.CurrentGame?.TriggerGameEnd(Fantast.FantastWin, GameEndReason.Situation);
        }
    }
};
