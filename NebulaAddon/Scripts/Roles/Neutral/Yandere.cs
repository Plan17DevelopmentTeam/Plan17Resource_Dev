using Nebula.Utilities;
using Plana.Core;
using Virial.Runtime;

namespace Plana.Roles.Neutral;
[NebulaPreprocess(PreprocessPhase.BuildAssignmentTypes)]
public static class YandereAssignmentUp
{
    static public Virial.Color Color = new(227, 0, 136);
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        try
        {
            PDebug.Log("Add Yandere Assignment Type");
            preprocessor.RegisterAssignmentType(() => Yandere.MyRole, (int[] lastArgs, DefinedRole role) => Yandere.GenerateArgument(lastArgs.Get(0,0), role), "yandereExRole", Color, (AbilityAssignmentStatus status, DefinedRole role) => status.HasFlag(AbilityAssignmentStatus.CanLoadToKillNeutral)||status.HasFlag(AbilityAssignmentStatus.CanLoadToMadmate), () => (Yandere.MyRole as ISpawnable).CanSpawnInCurrentGame && Yandere.YandereRoleOption);
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
}
[NebulaPreprocess(PreprocessPhase.PostFixStructure)]
public class YandereEndCriteria
{
    static YandereEndCriteria()
    {
        DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() => new YandereCriteria().RegisterPermanently());
    }
    public class YandereCriteria : IModule, IGameOperator
    {
        List<Yandere.Instance> allYanderes = [];
        [OnlyHost]
        void OnUpdate(GameUpdateEvent ev)
        {
            int totalAlive = 0;
            allYanderes.Clear();
            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo)
            {
                if (p.Role is Yandere.Instance yRole) allYanderes.Add(yRole);
                if (p.IsDead) continue;
                totalAlive++;
            }
            ulong YandereMask = 0;
            int teamCount = 0;
            ulong completeWinningFantastMask = 0;
            foreach (var yandere in allYanderes.Where(p=>!p.MyPlayer.IsDead))
            {
                ulong myMask = 1ul << yandere!.YandereTeamId;
                if ((YandereMask & myMask) == 0) teamCount++;
                else continue;
                YandereMask |= myMask;
                int aliveYanderes = NebulaGameManager.Instance!.AllPlayerInfo.Count(p => !p.IsDead && (yandere.isSameTeam(p) || !yandere.MyPlayer.CanKill(p)));
                if (aliveYanderes == totalAlive) completeWinningFantastMask |= myMask;
            }
            if (completeWinningFantastMask != 0)
            {
                allYanderes.Do(y => y.isDefeatedYandere = (completeWinningFantastMask & (1ul << y.YandereTeamId)) == 0ul);
                NebulaAPI.CurrentGame?.TriggerGameEnd(Yandere.Instance.YandereTeamWin, GameEndReason.Situation);
            }
        }
    }
}
[NebulaPreprocess(PreprocessPhase.PostRoles)]
internal class YandereTeamAllocator : AbstractModule<Virial.Game.Game>, IGameOperator
{
    private static void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule<Virial.Game.Game>(() => new YandereTeamAllocator());
    }

    private YandereTeamAllocator()
    {
        this.RegisterPermanently();
    }

    private void OnFixAssignmentTable(PreFixAssignmentEvent ev)
    {
        byte[] yanderes = ev.RoleTable.GetPlayers(Yandere.MyRole).ToArray<byte>();
        int i;
        for (i = 0; i < yanderes.Length; i++)
        {
            ev.RoleTable.EditRole(yanderes[i], last =>
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
[NebulaRPCHolder]
public class Yandere : DefinedRoleTemplate, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    public static Team MyTeam = new Team("teams.yandere", YandereAssignmentUp.Color, TeamRevealType.OnlyMe);
    public static FloatConfiguration AddDistance = NebulaAPI.Configurations.Configuration("options.role.yandere.addTargetDistance", new ValueTuple<float, float, float>(0f, 20f, 0.25f), 1.5f, FloatConfigurationDecorator.Ratio);
    private Yandere() : base("yandere", YandereAssignmentUp.Color, RoleCategory.NeutralRole, MyTeam, [BaseKillCD, MaxKillCD, MinKillCD, TargetDeadKillCD, AddDistance, AddDuration, AddCoolDown, killCorrectSubCD, killWrongAddCD, ActiveTargetArrow, TargetArrowUpdateTime, ActiveKillTargetArrow, KillTargetArrowUpdateTime, StayKillCD, CanUseVent, HasImpostorVision, YandereRoleOption])
    {
        IConfigurationHolder configurationHolder = base.ConfigurationHolder!;
        if (configurationHolder != null)
        {
            configurationHolder.AddTags(new ConfigurationTag[] { Nebula.Configuration.ConfigurationTags.TagFunny });
            configurationHolder.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImages/Yandere.png")!.AsImage(115f)!;
        }
        MetaAbility.RegisterCircle(new EffectCircleInfo("role.yandere.targetdistanceRange", () => AddDistance, () => null, base.RoleColor));
    }
    IEnumerable<DefinedRole> DefinedRole.GetGuessableAbilityRoles()
    {
        if (!((ISpawnable)this).IsSpawnable || YandereRoleOption)
        {
            return Array.Empty<DefinedRole>();
        }
        return [this];
    }
    public static int[] GenerateArgument(int yandereTeamId,DefinedRole role)
    {
        return [yandereTeamId,role?.Id ?? -1, .. (role?.DefaultAssignableArguments ?? [])];
    }
    bool DefinedRole.IsKiller => true;
    Citation? HasCitation.Citation { get { return PCitations.ExtremeRoles; } }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player,arguments.Get(0,player.PlayerId), Nebula.Roles.Roles.GetRole(arguments.Get(1, -1)), arguments.Skip(2).ToArray());
    public static FloatConfiguration BaseKillCD = NebulaAPI.Configurations.Configuration("options.role.yandere.killcd", new ValueTuple<float, float, float>(10f, 60f, 2.5f), 25f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration MinKillCD = NebulaAPI.Configurations.Configuration("options.role.yandere.minKillCD", new ValueTuple<float, float, float>(0f, 60f, 2.5f), 0f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration MaxKillCD = NebulaAPI.Configurations.Configuration("options.role.yandere.maxKillCD", new ValueTuple<float, float, float>(0f, 60f, 2.5f), 45f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration TargetDeadKillCD = NebulaAPI.Configurations.Configuration("options.role.yandere.Prokillcd", new ValueTuple<float, float, float>(10f, 60f, 2.5f), 17.5f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration AddDuration = NebulaAPI.Configurations.Configuration("options.role.yandere.addTargetDuration", new ValueTuple<float, float, float>(0f, 60f, 1f), 5f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration AddCoolDown = NebulaAPI.Configurations.Configuration("options.role.yandere.addcooldown", new ValueTuple<float, float, float>(5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration killCorrectSubCD = NebulaAPI.Configurations.Configuration("options.role.yandere.killcorrectAddCD", new ValueTuple<float, float, float>(0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration killWrongAddCD = NebulaAPI.Configurations.Configuration("options.role.yandere.killwrongAddCD", new ValueTuple<float, float, float>(0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    public static BoolConfiguration ActiveTargetArrow = NebulaAPI.Configurations.Configuration("options.role.yandere.activetargetarrow", true);
    public static FloatConfiguration TargetArrowUpdateTime = NebulaAPI.Configurations.Configuration("options.role.yandere.targetupdatetime", new ValueTuple<float, float, float>(0f, 30f, 2.5f), 0f, FloatConfigurationDecorator.Second);
    public static BoolConfiguration ActiveKillTargetArrow = NebulaAPI.Configurations.Configuration("options.role.yandere.activekilltargetarrow", true);
    public static BoolConfiguration StayKillCD = NebulaAPI.Configurations.Configuration("options.role.yandere.aftermeetingstaycd", false);
    public static BoolConfiguration CanUseVent = NebulaAPI.Configurations.Configuration("options.role.yandere.canusevent", true);
    public static BoolConfiguration HasImpostorVision = NebulaAPI.Configurations.Configuration("options.role.yandere.hasImpostorVision", true);
    public static FloatConfiguration KillTargetArrowUpdateTime = NebulaAPI.Configurations.Configuration("options.role.yandere.killtargetupdatetime", new ValueTuple<float, float, float>(0f, 30f, 2.5f), 0f, FloatConfigurationDecorator.Second);
    public static BoolConfiguration YandereRoleOption = NebulaAPI.Configurations.Configuration("options.role.yandere.yandererole", false);
    static public Yandere MyRole = new Yandere();
    bool IAssignableDocument.HasTips => true;
    static public bool ShouldShowTeamId(int id, bool canSeeAllInfo)
    {
        canSeeAllInfo |= !AmongUsLLImpl.AmongUsClientInstance || AmongUsLLImpl.AmongUsClientInstance.GameState == InnerNet.InnerNetClient.GameStates.Ended;
        if (!canSeeAllInfo) return false;
        if (id > 0) return true;
        bool moreThanOne = ((MyRole as DefinedSingleAssignable)?.AllocationParameters?.RoleCountSum ?? 0) > 1;
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

    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public DefinedRole? MyExRole { get; set; }
        public IPlayerAbility? ExAbility { get; private set; } = null;
        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp => MyExRole != null ? [MyRole, MyExRole,..ExAbility?.SubAssignableOnHelp??Array.Empty<DefinedAssignable>()] : [MyRole];
        private int[] StoredExArgument { get; set; }
        IEnumerable<IPlayerAbility?> RuntimeAssignable.MyAbilities => ExAbility != null ? [ExAbility, .. ExAbility.SubAbilities] : [];
        ModAbilityButton? killButton;
        GamePlayer? targetPlayer;
        public int YandereTeamId;
        public bool isDefeatedYandere;
        public Instance(GamePlayer player, int yandereTeamId,DefinedRole? yandereRole, int[] yandereRoleArgument): base(player)
        {
            YandereTeamId = yandereTeamId;
            MyExRole = yandereRole;
            StoredExArgument = yandereRoleArgument;
        }
        int[]? RuntimeAssignable.RoleArguments 
        {
            get
            {
                return [YandereTeamId, MyExRole?.Id??-1, .. ExAbility?.AbilityArguments ?? []];
            }
        }
        int[]? RuntimeRole.UsurpedAbilityArguments => (ExAbility?.AbilityArguments ?? []).Prepend(MyExRole?.Id ?? -1).ToArray();
        string RuntimeRole.DisplayIntroRoleName
        {
            get
            {
                return ((RuntimeAssignable)this).DisplayName;
            }
        }

        string RuntimeRole.DisplayIntroBlurb
        {
            get
            {
                string text = Language.Translate("role.yandere.InGameblurb")+ " ♥";
                var player = targetPlayer;
                if (MyExRole!=null)
                {
                    text = MyExRole.DisplayIntroBlurb +Environment.NewLine+ text;
                }
                return text.Replace("%NAME%", (((player != null) ? player.Name : null) ?? "ERROR").Color(MyRole.RoleColor.ToUnityColor()));
            }
        }
        string RuntimeAssignable.DisplayColoredName
        {
            get
            {
                return ((RuntimeAssignable)this).DisplayName.Color(MyRole.UnityColor);
            }
        }

        string RuntimeAssignable.DisplayName
        {
            get
            {

                var roleName=MyExRole != null ? Language.Translate("role.yandere.prefix") + MyExRole!.GetDisplayName(ExAbility) : (MyRole as DefinedAssignable).DisplayName;
                return AppendTeamIdIfNecessary(roleName, YandereTeamId, NebulaGameManager.Instance?.CanSeeAllInfo ?? false, false);
            }
        }
        List<GamePlayer> killplayer = new List<GamePlayer>();
        private TrackingArrowAbility arrowAbility;
        List<TrackingArrowAbility> killTargetArrow = new List<TrackingArrowAbility>();
        Dictionary<byte, float> targetDic = new Dictionary<byte, float>();
        public static GameEnd YandereTeamWin = NebulaAPI.Preprocessor!.CreateEnd("yandere", MyRole.RoleColor);
        bool RuntimeRole.CheckGuessAbility(DefinedRole abilityRole)
        {
            return abilityRole == MyExRole || abilityRole == Yandere.MyRole;
        }
        void OnPlayerDieEvent(PlayerDieEvent ev)
        {
            ModSingleton<IWinningOpportunity>.Instance.SetOpportunity(MyTeam, KillerOpportunityHelpers.CalcTeamOpportunity((Player p) => base.MyPlayer == p, isSameTeam, 0.2f), false);
        }
        void ClaimYandereRemaining(KillerTeamCallback callback)
        {
            if (callback.ExcludedTeam != MyTeam)
            {
                if (Player.AllPlayers.Any((Player p) => p.Role is Yandere.Instance && p.IsAlive))
                {
                    callback.MarkRemaining();
                }
            }
        }
        public bool isSameTeam(GamePlayer p)
        {
            if (p == null)
            {
                return false;
            }
            if (YandereLover.IsMyLover(MyPlayer,p,YandereTeamId))
            {
                return true;
            }
            if (p.Role is Yandere.Instance instance&&instance.YandereTeamId==YandereTeamId)
            {
                return true;
            }
            return false;
        }
        private void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd == YandereTeamWin && isSameTeam(ev.Player)&&!isDefeatedYandere);
        }
        [Local]
        private void DecoratetargetColor(PlayerDecorateNameEvent ev)
        {
            if (!NebulaGameManager.Instance?.CanSeeAllInfo??true)
            {
                if (YandereLover.IsMyLover(MyPlayer,ev.Player,YandereTeamId) && !ev.Player.AmOwner)
                {
                    ev.Name += "♥".Color(MyRole.RoleColor.ToUnityColor());
                }
            }
        }

        float cooldown = AddCoolDown;
        [Local]
        private void OnMeetingStart(MeetingStartEvent ev)
        {
            cooldown = AddCoolDown;
        }
        bool RuntimeRole.CanMoveInVent
        {
            get
            {
                return CanUseVent;
            }
        }

        bool RuntimeRole.CanUseVent
        {
            get
            {
                return CanUseVent;
            }
        }

        bool RuntimeRole.HasImpostorVision
        {
            get
            {
                return HasImpostorVision;
            }
        }

        bool RuntimeRole.IgnoreBlackout
        {
            get
            {
                return HasImpostorVision;
            }
        }
        bool meetingstart;
        bool loverDeadEarly;
        void OnGameEnd(GameEndEvent ev)
        {
            if (ev.EndState.EndCondition == YandereTeamWin&&ev.EndState.Winners.Test(MyPlayer)&&MyPlayer.AmOwner)
            {
                new StaticAchievementToken("yandere.challenge2");
                if (loverDeadEarly)
                {
                    new StaticAchievementToken("yandere.another3");
                }
                var target = NebulaGameManager.Instance?.AllPlayerInfo?.FirstOrDefault(p => p.TryGetModifier<YandereLover.Instance>(out var m));
                if (target!=null&&ev.EndState.Winners.Test(target))
                {
                    new StaticAchievementToken("yandere.challenge3");
                }
                if (Allkillplayers.Count>=5)
                {
                    new StaticAchievementToken("yandere.challenge1");
                }
                if (target.MyKiller!=null&&!Allkillplayers.Any(p=>p.PlayerId==target.MyKiller.PlayerId))
                {
                    new StaticAchievementToken("yandere.another2");
                }
            }
        }
        [Local]
        void OnPlayerDie(PlayerDieEvent ev)
        {
            if (!meetingstart&&YandereLover.IsMyLover(MyPlayer,ev.Player,YandereTeamId))
            {
                loverDeadEarly = true;
            }
        }
        [Local]
        void MeetingStart(MeetingStartEvent ev)
        {
            meetingstart = false;
        }
        [Local]
        private void Update(GameUpdateEvent ev)
        {
            try
            {
                if (cooldown > 0f)
                {
                    if (!MeetingHud.Instance && !ExileController.Instance)
                    {
                        cooldown -= Time.deltaTime;
                    }
                    return;
                }
                foreach (var playerInfo in GamePlayer.AllPlayers)
                {
                    byte playerId = playerInfo.PlayerId;
                    float playerProgress;
                    if (targetDic.TryGetValue(playerId, out playerProgress))
                    {
                        if (!playerInfo.IsDisconnected && !playerInfo.IsDead && MyPlayer.PlayerId != playerId && targetPlayer.PlayerId != playerId && !playerInfo.Logic.InVent)
                        {
                            var pos = targetPlayer.TruePosition;
                            Virial.Compat.Vector2 vector = playerInfo.TruePosition - pos;
                            float magnitude = vector.Magnitude;
                            if (magnitude <= AddDistance && !PhysicsHelpers.AnyNonTriggersBetween(pos, vector.Normalized, magnitude, Constants.ShipAndObjectsMask))
                            {
                                playerProgress += Time.deltaTime;
                            }
                            else
                            {
                                playerProgress = 0f;
                            }
                        }
                        else
                        {
                            playerProgress = 0f;
                        }
                        if (playerProgress >= AddDuration && !killplayer.Contains(GamePlayer.GetPlayer(playerId)))
                        {
                            try
                            {
                                killplayer.Add(GamePlayer.GetPlayer(playerId));
                                killTargetArrow.Add(new TrackingArrowAbility(GamePlayer.GetPlayer(playerId), TargetArrowUpdateTime, Virial.Color.Red,false).Register(this));
                                targetDic.Remove(playerId);
                            }
                            catch (Exception e)
                            {
                                PDebug.Log(e);
                            }
                        }
                        else
                        {
                            targetDic[playerId] = playerProgress;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        int correctkillnum, wrongkillnum;
        List<GamePlayer> Allkillplayers;
        [Local]
        void EditMyKillCooldown(ResetKillCooldownLocalEvent ev)
        {
            IEnumerable<IKillButtonLike> buttons =NebulaAPI.CurrentGame?.KillButtonLikeHandler.KillButtonLike;
            var max = 0f;
            if (targetPlayer?.IsDead ?? true)
            {
                max=TargetDeadKillCD;
            }
            else
            {
                float num = BaseKillCD + correctkillnum * -killCorrectSubCD + wrongkillnum * killWrongAddCD;
                num = Mathn.Clamp(num, MinKillCD, MaxKillCD);
                max = num;
            }
            foreach (var button in buttons)
            {
                if (button is ModKillButtonHandler handler)
                {
                    handler.button.CoolDownTimer = NebulaAPI.Modules.Timer(this,max).SetAsKillCoolTimer().Start(null);
                }
            }
        }
        [Local]
        void OnTaskStart(TaskPhaseRestartEvent ev)
        {
            NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
        }
        [OnlyMyPlayer]
        private void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (YandereLover.IsMyLover(MyPlayer,ev.Target,YandereTeamId))
            {
                ev.SetAsCannotKillBasically();
            }
        }
        private void OnMeetingEnd(MeetingPreEndEvent ev)
        {
            PDebug.Log("OnMeetingEnd");
            if (!StayKillCD)
            {
                wrongkillnum = 0;
                correctkillnum = 0;
            }
            killplayer = new List<GamePlayer>();
            targetDic = new Dictionary<byte, float>();
            foreach (var player in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
            {
                targetDic.Add(player.PlayerId, 0f);
            }
            foreach (var arrow in killTargetArrow)
            {
                arrow.Release();
            }
            killTargetArrow = new List<TrackingArrowAbility>();
            NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
        }
        void RuntimeRole.Usurp()
        {
            (ExAbility as IUsurpableAbility)?.Usurp();
        }
        void RuntimeAssignable.OnActivated()
        {
            try
            {
                if (AmOwner)
                {

                    loverDeadEarly = false;
                    meetingstart = false;
                    targetPlayer = null;
                    if (targetPlayer == null)
                    {
                        List<GamePlayer> RandomSelectTarget = new List<GamePlayer>();
                        GamePlayer lover = null;
                        foreach (var p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                        {
                            if (p.PlayerId == MyPlayer.PlayerId)
                            {
                                continue;
                            }
                            if (YandereLover.IsMyLover(MyPlayer,p,YandereTeamId))
                            {
                                lover = p;
                                break;
                            }
                            RandomSelectTarget.Add(p);
                        }
                        targetPlayer = lover != null ? lover : RandomSelectTarget[UnityEngine.Random.Range(0, RandomSelectTarget.Count)];
                        if (lover == null)
                        {
                            targetPlayer?.AddModifier(YandereLover.MyRole, [MyPlayer.PlayerId,YandereTeamId]);
                        }
                    }
                    cooldown = AddCoolDown;
                    wrongkillnum = 0;
                    correctkillnum = 0;
                    killplayer = new List<GamePlayer>();
                    targetDic = new Dictionary<byte, float>();
                    foreach (var player in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                    {
                        targetDic.Add(player.PlayerId, 0f);
                    }
                    if (ActiveTargetArrow && targetPlayer != null && !targetPlayer.IsDead)
                    {
                        arrowAbility = new TrackingArrowAbility(targetPlayer, TargetArrowUpdateTime, MyRole.RoleColor,false).Register(this);
                    }
                    var myTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeLocalKillablePredicate(p), Palette.ImpostorRed, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption);

                    var killButton = NebulaAPI.Modules.PlayerlikeKillButton(this, MyPlayer, new PlayerInteractParameter(false,true,true),true, Virial.Compat.VirtualKeyInput.Kill,null,
                        BaseKillCD, "kill", ModAbilityButton.LabelType.Impostor, null!,
                        (target, _) =>
                        {
                            var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, target));
                            if (!(cancelable?.IsCanceled ?? false))
                            {
                                //キャンセルされなければキルを実行する
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                            }

                            //クールダウンをリセットする
                            if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                        },
                        myTracker,
                        _ => !MyPlayer.IsDived,
                        _ => MyPlayer.AllowToShowKillButtonByAbilities
                        );
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
                    GameOperatorManager.Instance!.Subscribe<GameEndEvent>(ev =>
                        {
                            var target = NebulaGameManager.Instance?.AllPlayerInfo?.FirstOrDefault(p => YandereLover.IsMyLover(MyPlayer,targetPlayer,YandereTeamId));
                            if (ev.EndState.EndCondition != YandereTeamWin && ev.EndState.Winners.Test(target) && ev.EndState.Winners.Test(MyPlayer))
                            {
                                new StaticAchievementToken("yandere.another1");
                            }
                        }, NebulaAPI.CurrentGame!);
                    GameOperatorManager.Instance!.Subscribe<PlayerKillPlayerEvent>(ev =>
                    {
                        if (ev.Murderer.AmOwner)
                        {
                            if (ev.Dead.PlayerState == PlayerState.Guessed)
                            {
                                return;
                            }
                            if (!targetPlayer?.IsDead ?? false)
                            {
                                if (killplayer.Contains(ev.Dead))
                                {
                                    correctkillnum++;
                                    new StaticAchievementToken("yandere.common1");
                                }
                                else
                                {
                                    wrongkillnum++;
                                }
                            }
                            Allkillplayers.Add(ev.Dead);
                        }
                    }, this);
                }
                var ability = MyExRole?.GetAbilityOnRole(MyPlayer, AbilityAssignmentStatus.KillersSide, StoredExArgument);
                if (ability != null)
                {
                    ExAbility = ability?.Register(this);
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
    }
}
public class YandereLover : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>,HasCitation
{
    private YandereLover() : base("yandereLover", YandereAssignmentUp.Color, null,true,()=>false)
    {
    }
    Citation? HasCitation.Citation { get { return PCitations.ExtremeRoles; } }
    static public YandereLover MyRole = new YandereLover();
    public static bool IsMyLover(GamePlayer yandere, GamePlayer lover,int YandereTeamId)
    {
        return lover.Modifiers.Any(m =>
        {
            if (m is Instance instance)
            {
                return instance.MyYandere?.PlayerId == yandere.PlayerId&&instance.YandereTeamId==YandereTeamId;
            }
            return false;
        });
    }
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player,GamePlayer.GetPlayer((byte)arguments.Get(0,-1)),arguments.Get(1,arguments.Get(0,-1)));
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        bool RuntimeAssignable.CanBeAwareAssignment => NebulaGameManager.Instance!=null ? NebulaGameManager.Instance.CanSeeAllInfo:false;

        public Instance(GamePlayer player,GamePlayer yandere,int yandereTeamId) : base(player)
        {
            MyYandere = yandere;
            YandereTeamId = yandereTeamId;
        }
        public int YandereTeamId;
        public GamePlayer? MyYandere;
        void RuntimeAssignable.OnActivated()
        {
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (canSeeAllInfo || inEndScene)
            {
                name += ("♥"+ Yandere.AppendTeamIdIfNecessary("",YandereTeamId, canSeeAllInfo, true)).Color(MyRole.RoleColor.ToUnityColor());
            }
        }
    }
}