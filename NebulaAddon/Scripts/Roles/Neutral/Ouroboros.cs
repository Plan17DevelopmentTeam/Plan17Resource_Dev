using Epic.OnlineServices.Presence;
using Nebula.Game.Statistics;
using Nebula.Roles.Complex;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Plana.Roles.Modifier;
using Plana.Roles.Neutral;
using System.ComponentModel;
using Virial;
using Virial.Assignable;
using Virial.Components;
using Virial.Configuration;
using Virial.DI;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Events.Role;
using Virial.Game;
using Virial.Runtime;

namespace Plana.Roles.Neutral;

[NebulaPreprocess(PreprocessPhase.BuildAssignmentTypes)]
internal static class OuroborosAssignmentType
{
    static public Virial.Color Color = new(130,184,164);
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.RegisterAssignmentType(() => Neutral.Ouroboros.MyRole, (lastArgs, role) => Ouroboros.GenerateArgument(lastArgs.Get(0, 0), role), "ouroborosized", Color, (status, role) => status.HasFlag(AbilityAssignmentStatus.CanLoadToKillNeutral), () => (Ouroboros.MyRole as ISpawnable).CanSpawnInCurrentGame && Ouroboros.OuroborosizedImpostorOption);
    }
}

public class Ouroboros : DefinedRoleTemplate, HasCitation, DefinedRole, IAssignableDocument
{
    static readonly public RoleTeam MyTeam = new Team("teams.ouroboros", OuroborosAssignmentType.Color, TeamRevealType.OnlyMe, () => KillCooldown);
    private Ouroboros() : base("ouroboros", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [KillCoolDownOption,NumOfEcdyses,TeamGuesserOption,TeamGuessNum, OuroborosizedImpostorOption])
    {
    }
    bool AssignableFilterHolder.CanLoadDefault(DefinedAssignable assignable)
    {
        return base.CanLoadDefaultTemplate(assignable) && (!TeamGuesserOption || !(assignable is Nebula.Roles.Complex.GuesserModifier));
    }
    IEnumerable<DefinedRole> DefinedRole.GetGuessableAbilityRoles() => ((this as DefinedRole).IsSpawnable && !OuroborosizedImpostorOption) ? [this] : [];
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Ouroboros.png")!.AsImage();
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments.Get(0, player.PlayerId), arguments.Get(1, 0),arguments.Get(2,TeamGuessNum), Nebula.Roles.Roles.GetRole(arguments.Get(3, -1)), arguments.Skip(4).ToArray());
    static public int[] GenerateArgument(int ouroborosTeamId, DefinedRole? ouroborosized) => [ouroborosTeamId, 0, TeamGuessNum, ouroborosized?.Id ?? -1, ..(ouroborosized?.DefaultAssignableArguments ?? [])];
    static private IRelativeCooldownConfiguration KillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.ouroboros.killCoolDown", CoolDownType.Relative, (0f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), -5f, (0.125f, 2f, 0.125f), 1f);
    static public IntegerConfiguration NumOfEcdyses = NebulaAPI.Configurations.Configuration("options.role.ouroboros.numOfEcdyses", (0, 15), 0, null, val => val == 0 ? Language.Translate("options.noLimit") : val.ToString());
    static public BoolConfiguration TeamGuesserOption = NebulaAPI.Configurations.Configuration("options.role.ouroboros.teamguesser", true);
    static public IntegerConfiguration TeamGuessNum = NebulaAPI.Configurations.Configuration("options.role.ouroboros.teamguessnum", (1, 15), 5);
    static public BoolConfiguration OuroborosizedImpostorOption = NebulaAPI.Configurations.Configuration("options.role.ouroboros.ouroborosizedImpostor", false);

    static public float KillCooldown => KillCoolDownOption.Cooldown;

    static public Ouroboros MyRole = new Ouroboros();
    bool IAssignableDocument.HasTips => true;
    bool DefinedRole.IsKiller => true;

    public static bool IsOuroborosTeam(GamePlayer player, int teamId,bool excludedDefeated=false)
    {
        if (player.Role is Instance j) return j.OuroborosTeamId == teamId&&(!excludedDefeated||!j.IsDefeatedOuroboros);
        return false;
    }
    public static Ouroboros.Instance GetFirstOuroboro(int teamId)
    {
        var ours = NebulaGameManager.Instance!.AllPlayerInfo.Where(p => IsOuroborosTeam(p, teamId));
        var firstOur=ours.FirstOrDefault(r => r.Role is Ouroboros.Instance ins && ins.inherited == 0);
        if (firstOur==null)
        {
            return null;
        }
        return firstOur!.Role as Ouroboros.Instance;
    }

    static public bool ShouldShowTeamId(int id, bool canSeeAllInfo)
    {
        canSeeAllInfo |= !AmongUsLLImpl.AmongUsClientInstance || AmongUsLLImpl.AmongUsClientInstance.GameState == InnerNet.InnerNetClient.GameStates.Ended;
        if (!canSeeAllInfo) return false;
        if (id > 0) return true;
        bool moreThanOne = ((Ouroboros.MyRole as DefinedSingleAssignable)?.AllocationParameters?.RoleCountSum ?? 0) > 1;
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
    public static readonly RemoteProcess<(GamePlayer, int)> RpcUpdateOuroborosTeamLeftGuess = new("RpcUpdateOuroborosTeamLeftGuess", (m, _) =>
    {
        if (m.Item1?.Role is Instance instance)
        {
            instance.LeftGuess = m.Item2;
        }
    });
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        public static GameEnd OuroborosWin = NebulaAPI.Preprocessor!.CreateEnd("ouroboros", OuroborosAssignmentType.Color);
        DefinedRole RuntimeRole.Role => MyRole;
        public int LeftGuess;
        public int OuroborosTeamId;
        public int inherited = 0;//継承回数
        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp => MyOuroborosized != null ? [MyRole, MyOuroborosized, ..(OuroborosizedAbility?.SubAssignableOnHelp ?? [])] : [MyRole];
        public DefinedRole? MyOuroborosized { get; private set; }
        public IPlayerAbility? OuroborosizedAbility { get; private set; } = null;
        private int[] StoredOuroborosizedArgument { get; set; }

        string RuntimeRole.DisplayIntroBlurb => (MyOuroborosized ?? MyRole).DisplayIntroBlurb;
        string RuntimeRole.DisplayIntroRoleName => (MyOuroborosized ?? MyRole).DisplayName;
       
        IEnumerable<IPlayerAbility?> RuntimeAssignable.MyAbilities => OuroborosizedAbility != null ? [OuroborosizedAbility, ..OuroborosizedAbility.SubAbilities] : [];
        public Instance(GamePlayer player, int ouroborosTeamId, int inherited,int leftGuess, DefinedRole? ouroborosized, int[] ouroborosizedArgument) : base(player)
        {
            OuroborosTeamId = ouroborosTeamId;
            LeftGuess = leftGuess;
            MyOuroborosized = ouroborosized;
            StoredOuroborosizedArgument = ouroborosizedArgument;
            this.inherited = inherited;
        }
        bool RuntimeRole.CanUseVent => true;
        //RoleAssignmentでもこの配置を前提とした部分がある点に注意(ジャッカル化の割り当てのため)
        int[] OuroborosArguments => [OuroborosTeamId,  inherited,LeftGuess, MyOuroborosized?.Id ?? -1];
        int[]? RuntimeAssignable.RoleArguments => OuroborosArguments.Concat(OuroborosizedAbility?.AbilityArguments ?? []).ToArray();
        int[]? RuntimeRole.UsurpedAbilityArguments => (OuroborosizedAbility?.AbilityArguments ?? []).Prepend(MyOuroborosized?.Id ?? -1).ToArray();
        bool RuntimeRole.CheckGuessAbility(DefinedRole abilityRole) => abilityRole == MyOuroborosized || abilityRole == MyRole;
        public int[] RoleArgumentsForNextOuroboros
        {
            get
            {
                var arg = (this as RuntimeAssignable).RoleArguments;
                arg[1]++;
                return arg;
            }
        }
        public bool IsSameTeam(GamePlayer? player)
        {
            if (player?.Role is Instance ouroboros && ouroboros.OuroborosTeamId == OuroborosTeamId) return true;
            return false;
        }
        internal bool IsDefeatedOuroboros = false;

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd == OuroborosWin && NebulaGameManager.Instance!.AllPlayerInfo.Any(p => !p.IsDead && Ouroboros.IsOuroborosTeam(p, OuroborosTeamId, true)));
        }

        void RuntimeRole.Usurp()
        {
            (OuroborosizedAbility as IUsurpableAbility)?.Usurp();
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var myKillTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeLocalKillablePredicate(p), null, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption);
                var killButton = NebulaAPI.Modules.KillButton(this, MyPlayer, true, Virial.Compat.VirtualKeyInput.Kill,
                    KillCooldown, "kill", ModAbilityButton.LabelType.Impostor, null!,
                    (target, _) =>
                    {
                        var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, target));
                        if (!(cancelable?.IsCanceled ?? false))
                        {
                            MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                        }                        
                        if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    },
                    null,
                    _ => myKillTracker.CurrentTarget != null && !MyPlayer.IsDived,
                    _ => MyPlayer.AllowToShowKillButtonByAbilities
                    );
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
            OuroborosizedAbility = MyOuroborosized?.GetAbilityOnRole(MyPlayer, AbilityAssignmentStatus.CanLoadToKillNeutral, StoredOuroborosizedArgument)?.Register(this);
        }

        void DecorateOuroborosColor(PlayerDecorateNameEvent ev)
        {
            if (IsSameTeam(GamePlayer.LocalPlayer) && IsSameTeam(ev.Player)) ev.Color = Ouroboros.MyRole.RoleColor;
        }

        string RuntimeAssignable.DisplayName { get { 
                var name = Ouroboros.AppendTeamIdIfNecessary(MyOuroborosized?.GetDisplayName(OuroborosizedAbility!) ?? (MyRole as DefinedRole).DisplayName, OuroborosTeamId, NebulaGameManager.Instance?.CanSeeAllInfo ?? false, false);
                return name;
            }
        }
        string RuntimeAssignable.DisplayColoredName => (this as RuntimeAssignable).DisplayName.Color(MyTeam.UnityColor);
        string RuntimeRole.DisplayShort => Ouroboros.AppendTeamIdIfNecessary(MyOuroborosized?.GetDisplayShort(OuroborosizedAbility!) ?? (MyRole as DefinedRole).DisplayShort, OuroborosTeamId, NebulaGameManager.Instance?.CanSeeAllInfo ?? false, true);
        [OnlyMyPlayer]
        void ShowMyLeftEcdysesNum(PlayerSetFakeRoleNameEvent ev)
        {
            if (AmOwner || NebulaGameManager.Instance.CanSeeAllInfo)
            {
                ev.Append(NumOfEcdyses == 0 ? "" : $"({NumOfEcdyses - inherited})".Color(MyRole.UnityColor));
            }
        }
        [OnlyHost, OnlyMyPlayer]
        void OnDead(PlayerMurderedEvent ev)
        {
            if (ev.Dead.PlayerId==ev.Murderer.PlayerId)
            {
                return;
            }
            if (ev.Dead.PlayerState==PlayerState.Cursed||ev.Dead.PlayerState==PlayerState.Dead||ev.Dead.PlayerState==PlayerState.Guessed||ev.Dead.PlayerState==PlayerState.Dissolved)
            {
                if (inherited<(NumOfEcdyses==0?1000:NumOfEcdyses))
                {
                    using (RPCRouter.CreateSection("OuroborosEcdyses"))
                    {
                        ev.Murderer.SetRole(Ouroboros.MyRole, RoleArgumentsForNextOuroboros);
                        foreach (var killerModifier in ev.Murderer.Modifiers)
                        {
                            if (killerModifier.Modifier is ScarletLover||killerModifier.Modifier is Lover||killerModifier.Modifier is KnightedTarget||killerModifier.Modifier is PMini)
                            {
                                continue;
                            }    
                            ev.Murderer.RemoveModifier(killerModifier.Modifier);
                        }
                        foreach (var modifier in ev.Dead.Modifiers)
                        {
                            if (TeamGuesserOption&&modifier.Modifier is GuesserModifier)
                            {
                                continue;
                            }
                            if (modifier.Modifier is ScarletLover||modifier.Modifier is Lover)
                            {
                               continue;
                            }
                            ev.Murderer.AddModifier(modifier.Modifier);
                        }
                    }
                }
            }
        }

        bool RuntimeRole.HasImpostorVision => true;
        bool RuntimeRole.IgnoreBlackout => true;


        [OnlyMyPlayer]
        void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (IsSameTeam(ev.Target)) ev.SetAsCannotKillBasically();
        }
        void OnPlayerDie(PlayerDieEvent ev)
        {
            ModSingleton<IWinningOpportunity>.Instance.SetOpportunity(MyTeam, Nebula.Roles.Impostor.KillerOpportunityHelpers.CalcTeamOpportunity((p)=>IsOuroborosTeam(p,OuroborosTeamId),(p)=>false));
        }

        void ClaimOuroborosRemaining(KillerTeamCallback ev)
        {
            if (ev.ExcludedTeam == Ouroboros.MyTeam) return;
            if(MyPlayer.IsAlive) ev.MarkRemaining();
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (!TeamGuesserOption)
            {
                return;
            }
            var firstOuroboro = GetFirstOuroboro(OuroborosTeamId);
            if (firstOuroboro==null)
            {
                PDebug.Log("Not Found First Ouroboros,Cannot use Ouroboros Team Guesser");
                return;
            }
            GuesserSystem.OnMeetingStart(firstOuroboro.LeftGuess, () => RpcUpdateOuroborosTeamLeftGuess.Invoke((firstOuroboro.MyPlayer, LeftGuess - 1)));
        }
        [Local]
        [OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev)
        {
            if (!TeamGuesserOption)
            {
                return;
            }
            GuesserSystem.OnDead();
        }
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (!TeamGuesserOption)
            {
                return;
            }
            GuesserSystem.OnGameEnd(MyPlayer);
        }
    }
}
[NebulaPreprocess(PreprocessPhase.PostBuildNoS)]
internal class OuroborosCriteria : AbstractModule<IGameModeStandard>, IGameOperator
{
    static OuroborosCriteria() => DIManager.Instance.RegisterModule(() => new OuroborosCriteria().Register(NebulaAPI.CurrentGame!));

    List<Ouroboros.Instance> allAliveOuroboross = [];
    [OnlyHost]
    void OnUpdate(GameUpdateEvent ev)
    {
        bool sidekickShouldBeCountKillers = Sidekick.SidekickShouldBeCountAsKillers;

        int totalAlive = 0;
        bool leftOtherKillers = GameOperatorManager.Instance?.Run(new KillerTeamCallback(Ouroboros.MyTeam)).RemainingOtherTeam ?? false; //帆かキラーが残っていても殲滅勝利は可能

        //全生存者数を数え、生存しているジャッカルチームを発見する。
        allAliveOuroboross.Clear();
        foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo)
        {
            if (p.IsDead) continue;
            if (p.Role is Ouroboros.Instance jRole) allAliveOuroboross.Add(jRole);
            totalAlive++;
        }



        ulong ouroborosMask = 0;
        int teamCount = 0;
        ulong completeWinningOuroborosMask = 0;
        foreach (var ouroboros in allAliveOuroboross)
        {
            //ジャッカル陣営の数をカウントする
            ulong myMask = 1ul << ouroboros!.OuroborosTeamId;
            if ((ouroborosMask & myMask) == 0) teamCount++;
            else continue; //既に考慮したチームはスキップしてよい
            ouroborosMask |= myMask;
            int aliveOuroboross = NebulaGameManager.Instance!.AllPlayerInfo.Count(p => !p.IsDead && ouroboros!.IsSameTeam(p) && (!p.TryGetModifier<Lover.Instance>(out var lover) || lover.IsAloneLover) && !p.IsMadmate);
            if (aliveOuroboross == totalAlive) completeWinningOuroborosMask |= myMask;
        }
        if (completeWinningOuroborosMask != 0)
        {
            allAliveOuroboross.Do(j => j.IsDefeatedOuroboros = (completeWinningOuroborosMask & (1ul << j.OuroborosTeamId)) == 0ul);
            NebulaAPI.CurrentGame?.TriggerGameEnd(Ouroboros.Instance.OuroborosWin, GameEndReason.Situation);
        }
    }
};

[NebulaPreprocess(PreprocessPhase.PostRoles)]
internal class OuroborosTeamAllocator : AbstractModule<Virial.Game.Game>, IGameOperator
{
    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule(() => new OuroborosTeamAllocator());
    }
    private OuroborosTeamAllocator()
    {
        this.RegisterPermanently();
    }

    void OnFixAssignmentTable(PreFixAssignmentEvent ev)
    {
        var ouroboross = ev.RoleTable.GetPlayers(Ouroboros.MyRole).ToArray();
        //ジャッカルIDの割り振り
        for (int i = 0; i < ouroboross.Length; i++) ev.RoleTable.EditRole(ouroboross[i], (last) =>
        {
            if(last.argument.Length < 1) return (last.role, [i]);
            last.argument[0] = i;
            return (last.role, last.argument);
        });
    }
}