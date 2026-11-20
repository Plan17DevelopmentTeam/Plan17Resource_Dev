using Virial.Helpers;
using Color = Virial.Color;

namespace Plana.Roles.Crewmate;

[NebulaRPCHolder]
public class Trainee : DefinedRoleTemplate, DefinedRole, DefinedAssignable, IAssignableDocument, HasCitation
{
    private Trainee() : base("trainee", new(255, 225, 123), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [LearningOfOption, LearningCoolDownOption, LearningNeedRoundOption, RoleFilterOption, LearnAliveCrewOption]) { }
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Trainee.png")!.AsImage();
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, Nebula.Roles.Roles.GetRole(arguments.Get(0, -1)), arguments.Skip(1).ToArray());
    static private readonly ValueConfiguration<int> LearningOfOption = NebulaAPI.Configurations.Configuration("options.role.trainee.learningOf", ["options.role.trainee.learningOf.afterSeconds", "options.role.trainee.learningOf.afterRounds", "options.role.trainee.learningOf.both"], 2);
    static private readonly FloatConfiguration LearningCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.trainee.cooldown", (30f, 120f, 10f), 40f, FloatConfigurationDecorator.Second, () => LearningOfOption.GetValue() == 0 || LearningOfOption.GetValue() == 2);
    static private readonly IntegerConfiguration LearningNeedRoundOption = NebulaAPI.Configurations.Configuration("options.role.trainee.needRound", (1, 8, 1), 1, () => LearningOfOption.GetValue() == 1 || LearningOfOption.GetValue() == 2);
    public static readonly SimpleRoleFilterConfiguration RoleFilterOption = new SimpleRoleFilterConfiguration("options.role.trainee.abilityFilter")
    {
        RolePredicate = (DefinedRole r) => !r.IsSystemRole && r.IsSpawnableInSomeForm() && !r.IsMadmate && r != Insomniacs.MyRole && r != Aide.MyRole && r != MyRole && r.Category == RoleCategory.CrewmateRole,
        ScrollerTag = "badTraineeFilter",
        InvertOption = true,
        PreviewOnlySpawnableRoles = true
    };
    internal static BoolConfiguration LearnAliveCrewOption = NebulaAPI.Configurations.Configuration("options.role.trainee.learnAliveCrew", true);

    static public Trainee MyRole = new Trainee();
    bool IAssignableDocument.HasTips => true;
    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;
        static internal Virial.Media.Image? buttonImage = NebulaAPI.AddonAsset.GetResource("TraineeSkill.png")?.AsImage(115f);
        ModAbilityButton? learnButton;
        public byte managerId;
        public bool canDecorateName = false;
        public bool becomeAideRole = false;
        [Local]
        void DecorateName(PlayerDecorateNameEvent ev)
        {
            try
            {
                if (!AmOwner || ev.Player == null||canDecorateName == false) return;
                if (ev.Player.PlayerId == MyPlayer.PlayerId || ev.Player.PlayerId == managerId)
                {
                    ev.Color = Aide.MyRole.RoleColor;
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        (Virial.Media.Image image, Color? maincolor, Color? subColor)? RuntimeAssignable.OverriddenRoleIcon => MyExtendedRole != null ? (Aide.MyRole.GetRoleIcon()!, Aide.MyRole.Color, null) : null;
        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp => MyExtendedRole == null ? [MyRole] : [Aide.MyRole, MyExtendedRole];
        IEnumerable<IPlayerAbility?> RuntimeAssignable.MyAbilities => ExtendedRoleInstance?.MyAbilities ?? [];
        
        public DefinedRole? MyExtendedRole { get; set; }
        public RuntimeRole? ExtendedRoleInstance { get; set; }
        
        int[] RuntimeAssignable.RoleArguments => [MyExtendedRole?.Id ?? -1, .. ExtendedRoleInstance?.RoleArguments ?? []];
        
        public Instance(GamePlayer player, DefinedRole? role, int[] arguments) : base(player)
        {
            if (role != null)
            {
                MyExtendedRole = role;
                ExtendedRoleInstance = MyExtendedRole.CreateInstance(MyPlayer, arguments ?? role.DefaultAssignableArguments ?? []);
                ExtendedRoleInstance.OnActivated();
                ExtendedRoleInstance.Register(this);
            }
        }
        
        void IGameOperator.OnReleased()
        {
            ExtendedRoleInstance?.Inactivate();
            ExtendedRoleInstance = null;
        }

        bool RuntimeRole.CheckGuessAbility(DefinedRole abilityRole)
        {
            return MyExtendedRole == null ? abilityRole == MyRole : abilityRole == MyExtendedRole;
        }
        string? RuntimeAssignable.OverrideRoleName(string lastRoleName, bool isShort, bool canSeeAllInfo)
        {
            if ((AmOwner || canSeeAllInfo) && MyExtendedRole != null && ExtendedRoleInstance != null)
            {
                var prefix = isShort ? $"{ExtendedRoleInstance.GetRoleIconTagSmall()}{ExtendedRoleInstance.DisplayColoredShort}" : ExtendedRoleInstance.DisplayColoredName;
                return $"{prefix} {(Aide.MyRole as DefinedAssignable).DisplayColoredName}";
            }
            return lastRoleName;
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner && MyExtendedRole == null)
            {
                try
                {
                    GameObject? lockSprite = null;
                    bool used = false;

                    ObjectTracker<GamePlayer> myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, p => ObjectTrackers.StandardPredicate(p) && MyExtendedRole == null, MyRole.RoleColor.ToUnityColor(), false, false);
                    learnButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Ability, null);
                    learnButton.SetImage(buttonImage);
                    (learnButton as ModAbilityButtonImpl)?.UseCoolDownSupport = false;
                    learnButton.Availability = (ModAbilityButton button) => myTracker.CurrentTarget != null && this.MyPlayer.CanMove && !lockSprite.AsBoolFast();
                    learnButton.Visibility = (ModAbilityButton button) => !this.MyPlayer.IsDead && !used;

                    int learnOf = LearningOfOption.GetValue();
                    var cooldown = LearningCoolDownOption.GetValue();
                    if (learnOf == 0 || learnOf == 2)
                    {
                        learnButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, LearningCoolDownOption).SetAsAbilityTimer().Start();
                    }
                    
                    if (learnOf >= 1)
                    {
                        lockSprite = learnButton.AddLockedOverlay();
                        var num = 0;
                        learnButton.OnMeeting = (button) =>
                        {
                            num++;
                            if (num >= LearningNeedRoundOption)
                            {
                                lockSprite.Destroy();
                                lockSprite = null;
                            }
                        };
                    }
                    
                    learnButton.SetLabel("trainee.learn");
                    learnButton.OnClick = delegate (ModAbilityButton button)
                    {
                        var player = myTracker.CurrentTarget;
                        if (player == null) return;
                        managerId = player.PlayerId;
                        used = true;
                        DefinedRole? learnedRole = null;
                        
                        var targrtRoleCategory = player.Role.Role.Category;
                        new StaticAchievementToken("trainee.common1");
                        if (targrtRoleCategory == RoleCategory.ImpostorRole)
                        {
                            new StaticAchievementToken("trainee.common2");
                        }
                        if (targrtRoleCategory == RoleCategory.NeutralRole)
                        {
                            new StaticAchievementToken("trainee.common3");
                        }
                        if (targrtRoleCategory == RoleCategory.CrewmateRole)
                        {
                            new StaticAchievementToken("trainee.common4");
                        }
                        if (MyPlayer.IsTrueCrewmate && targrtRoleCategory == RoleCategory.CrewmateRole &&
                            !player.IsMadmate && player.Role.Role != Insomniacs.MyRole && player.Role.Role != MyRole)
                        {
                            canDecorateName = true;
                            learnedRole = player.Role.Role;
                            var nextArgs = player.Role.RoleArguments;
                            RegisterRoleInstance(MyPlayer, learnedRole, nextArgs ?? []);
                            player.AddModifier(Manager.MyRole, [MyPlayer.PlayerId]);
                        }
                        else
                        {
                            var players = GamePlayer.AllPlayers
                                .Where(p => p.Role.Role.Category == RoleCategory.CrewmateRole &&
                                            p.Role.Role != Insomniacs.MyRole&&!p.IsMadmate).Except([player, MyPlayer]).ToList();
                            
                            if (LearnAliveCrewOption)
                            {
                                players.RemoveAll(p => p.IsDead);
                            }
                            
                            List<DefinedRole> crewRoles = players.Select(p => p.Role.Role).Except([MyRole, Aide.MyRole]).ToList();
                            
                            if (!MyPlayer.IsTrueCrewmate)
                            {
                                crewRoles.RemoveAll(r => !RoleFilterOption.Contains(r) || r == player.Role.Role);
                            }
                            
                            if (crewRoles.Count > 0)
                            {
                                canDecorateName = true;
                                learnedRole = crewRoles[UnityEngine.Random.Range(0, crewRoles.Count)];
                                RegisterRoleInstance(MyPlayer, learnedRole, []);
                                player.AddModifier(Manager.MyRole, [MyPlayer.PlayerId]);
                            }
                            else
                            {
                                MyPlayer.SetRole(Aide.MyRole);
                                becomeAideRole = true;
                            }
                        }
                    };
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            }
        }

        [NebulaRPC]
        public static void RegisterRoleInstance(GamePlayer myPlayer, DefinedRole role, int[] arguments)
        {
            try
            {
                Trainee.Instance? instance = null;
                instance = GameOperatorManager.Instance?.AllOperators.OfType<Instance>().FirstOrDefault(t => t.MyPlayer == myPlayer);
                if (instance == null)
                {
                    PDebug.Log("Not Found Trainee Instance");
                    return;
                }
                
                instance.MyExtendedRole = role;
                instance.ExtendedRoleInstance = instance.MyExtendedRole.CreateInstance(instance.MyPlayer, arguments.Length==0 ? (role.DefaultAssignableArguments ?? []) : arguments);
                instance.ExtendedRoleInstance.OnActivated();
                instance.ExtendedRoleInstance.Register(instance);
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        [NebulaRPC]
        void ManagerAchievement(GamePlayer manager)
        {
            if (manager.AmOwner && manager != null && manager.Role.Role.Category == RoleCategory.CrewmateRole && !manager.IsMadmate)
            {
                new StaticAchievementToken("manager.common1");
            }
        }
        void OnPlayerKillPlayer(PlayerKillPlayerEvent ev)
        {
            var manager = GamePlayer.GetPlayer(managerId);
            if (ev.Murderer == manager && (ev.Murderer.Role.Role.Category != RoleCategory.CrewmateRole || ev.Murderer.IsMadmate) && ev.Dead == MyPlayer)
            {
                if (MyPlayer.AmOwner)
                {
                    new StaticAchievementToken("trainee.common5");
                }
            }
            if (ev.Murderer == MyPlayer && ev.Dead == manager && manager.Role.Role.Category == RoleCategory.ImpostorRole)
            {
                if (MyPlayer.AmOwner)
                {
                    new StaticAchievementToken("trainee.challenge1");
                }
                if (manager.AmOwner)
                {
                    new StaticAchievementToken("manager.challenge3");
                }
            }
        }
        void OnExile(PlayerExiledEvent ev)
        {
            var manager = GamePlayer.GetPlayer(managerId);
            var voters = MeetingHudExtension.GetLastSwappedVotedFor()
                .Where(entry => entry.voteTo == managerId)
                    .Select(entry => NebulaGameManager.Instance!.GetPlayer(entry.voter))
                        .Where(p => p != null && !p.IsDead).ToArray();
            if (ev.Player == manager && manager.Role.Role.Category == RoleCategory.ImpostorRole && voters.Contains(MyPlayer))
            {
                if (MyPlayer.AmOwner)
                {
                    new StaticAchievementToken("trainee.challenge1");
                }
                if (manager.AmOwner)
                {
                    new StaticAchievementToken("manager.challenge3");
                }
            }
        }
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            bool won = ev.CheckWin(MyPlayer);
            var manager = GamePlayer.GetPlayer(managerId);
            bool managerwon = false;
            bool managerIsAlive = false;
            bool crewWinEnd = ev.EndState.EndCondition == NebulaGameEnds.CrewmateGameEnd.Get();
            if (manager != null)
            {
                managerwon = ev.CheckWin(manager);
                managerIsAlive = manager.IsAlive;
                if (won && managerwon && crewWinEnd && MyPlayer.IsAlive && managerIsAlive && MyExtendedRole == manager.Role.Role)
                {
                    new StaticAchievementToken("trainee.challenge2");
                }
            }
            if (won && becomeAideRole && crewWinEnd)
            {
                new StaticAchievementToken("trainee.challenge3");
            }
        }
    }
}

public class Manager : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID,
    RuntimeAssignableGenerator<RuntimeModifier>, HasCitation, IAssignableDocument
{
    bool DefinedAssignable.ShowOnHelpScreen => true;
    bool DefinedAssignable.ShowOnFreeplayScreen => false;
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Manager.png")!.AsImage();

    private Manager() : base("manager", new(255, 225, 123), null, true, () => false)
    {
    }

    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    static public Manager MyRole = new Manager();
    bool IAssignableDocument.HasTips => true;

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) =>
        new Instance(player, GamePlayer.GetPlayer((byte)arguments.Get(0, 255)));

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public GamePlayer? aide;
        
        public Instance(GamePlayer player, GamePlayer? aide) : base(player)
        {
            this.aide = aide;
        }
        [Local]
        void OnDecoratePlayerName(PlayerDecorateNameEvent ev)
        {
            if (aide == null||ev.Player == null) return;
            if (aide.Role is not Trainee.Instance) return;
            if (MyPlayer.AmOwner)
            {
                if(ev.Player.PlayerId == MyPlayer.PlayerId || ev.Player.PlayerId == aide.PlayerId)
                {
                    ev.Color = Manager.MyRole.RoleColor;
                }
            }
            if(aide.AmOwner)
            {
                if (ev.Player.PlayerId == aide.PlayerId || ev.Player.PlayerId == MyPlayer.PlayerId)
                {
                    ev.Color = Aide.MyRole.RoleColor;
                }
            }
        }

        void RuntimeAssignable.OnActivated()
        {
            if (base.AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0f, 0.25f, 0.4f, 0f);
            }
        }
        
        [OnlyMyPlayer]
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo || inEndScene) name += "◇".Color(MyRole.RoleColor);
        }
        [Local]
        void OnPlayerKillPlayer(PlayerKillPlayerEvent ev)
        {
            if (ev.Murderer == MyPlayer && ev.Murderer.Role.Role.Category == RoleCategory.CrewmateRole && !ev.Murderer.IsMadmate && ev.Dead == aide && (ev.Dead.Role.Role.Category != RoleCategory.CrewmateRole || ev.Dead.IsMadmate))
            {
                new StaticAchievementToken("manager.challenge1");
            }
            if (ev.Murderer == aide && ev.Dead == MyPlayer && (ev.Dead.Role.Role.Category != RoleCategory.CrewmateRole || ev.Dead.IsMadmate))
            {
                new StaticAchievementToken("manager.challenge3");
            }
        }
        [Local]
        void OnExile(PlayerExiledEvent ev)
        {
            if (aide != null)
            {
                var voters = MeetingHudExtension.GetLastSwappedVotedFor()
                     .Where(entry => entry.voteTo == aide.PlayerId)
                         .Select(entry => NebulaGameManager.Instance!.GetPlayer(entry.voter))
                                .Where(p => p != null && !p.IsDead).ToArray();
                if (ev.Player == aide && (aide.Role.Role.Category != RoleCategory.CrewmateRole || aide.IsMadmate) && voters.Contains(MyPlayer))
                {
                    new StaticAchievementToken("manager.challenge1");
                }
            }
        }
        void OnGameEnd(GameEndEvent ev)
        {
            bool managerwon = ev.CheckWin(MyPlayer);
            bool crewWinEnd = ev.EndState.EndCondition == NebulaGameEnds.CrewmateGameEnd.Get();
            if (aide != null)
            {
                bool aidewon = ev.CheckWin(aide);
                if (managerwon && aidewon && !crewWinEnd&&aide.IsSameSideOf(MyPlayer))
                {
                    if (aide.AmOwner)
                    {
                        new StaticAchievementToken("trainee.challenge4");
                    }
                }
                if (managerwon && aidewon && crewWinEnd && aide.IsDead)
                {
                    if (MyPlayer.AmOwner)
                    {
                        new StaticAchievementToken("manager.common2");
                    }
                }
                if (managerwon && aidewon && !crewWinEnd && MyPlayer.IsSameSideOf(aide))
                {
                    if (MyPlayer.AmOwner)
                    {
                        new StaticAchievementToken("manager.challenge2");
                    }
                }
            }
        }
    }
}

public class Aide : DefinedRoleTemplate, DefinedRole, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeRole>, HasCitation, IAssignableDocument
{
    private Aide() : base("aide", new(255, 225, 123), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, null, false, false, () => false)
    {
    }

    bool IGuessed.CanBeGuess => false;
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Aide.png")!.AsImage();
    bool DefinedAssignable.ShowOnFreeplayScreen => false;
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    static public Aide MyRole = new Aide();
    bool IAssignableDocument.HasTips => true;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(GamePlayer player) : base(player)
        {
        }
        
        void RuntimeAssignable.OnActivated()
        {
        }
    }
}
