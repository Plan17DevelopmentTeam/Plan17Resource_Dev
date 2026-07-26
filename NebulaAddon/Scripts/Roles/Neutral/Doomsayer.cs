using Nebula.Roles.Complex;
using Plana.Core;

namespace Plana.Roles.Neutral;

public class Doomsayer : DefinedRoleTemplate, HasCitation, DefinedRole, IAssignableDocument
{
    private static Team RoleTeam = new Team("teams.doomsayer", new Virial.Color(0f, 1f, 0.5f), TeamRevealType.OnlyMe);
    private Doomsayer() : base("doomsayer", new Virial.Color(0f,1f,0.5f), RoleCategory.NeutralRole, RoleTeam, [guessCorrectNumToWin, guessCanKillPlayer, GuessShowKillAnim,MeetingCanGuessNum,GuessFailSuicide,GuessFailCannotGuess, PatchManager.GuesserCannotGuessNoSpawnableRole,
    new GroupConfiguration("role.doomsayer.oracleGroup",[CanUseOracle,OracleCooldown,OracleGetRoleNum,GetRoleIsSpawnable,BeforeMeetingStartNoSecondSkill],new(0f,1f,0.5f))]) 
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Guesser.MyNiceRole.ConfigurationHolder, Guesser.MyEvilRole.ConfigurationHolder,GuesserModifier.MyRole.ConfigurationHolder]);
        Guesser.MyNiceRole.ConfigurationHolder?.ScheduleAddRelated(() => [ConfigurationHolder]);
        Guesser.MyEvilRole.ConfigurationHolder?.ScheduleAddRelated(() => [ConfigurationHolder]);
        GuesserModifier.MyRole.ConfigurationHolder?.ScheduleAddRelated(() => [ConfigurationHolder]);
    }
    bool AssignableFilterHolder.CanLoadDefault(DefinedAssignable assignable)
    {
        return base.CanLoadDefaultTemplate(assignable) && !(assignable is Nebula.Roles.Complex.GuesserModifier);
    }
    Citation? HasCitation.Citation => PCitations.TownOfUs;

    static private IntegerConfiguration guessCorrectNumToWin = NebulaAPI.Configurations.Configuration("options.role.doomsayer.guessnum", (1, 15), 3);
    static private BoolConfiguration guessCanKillPlayer = NebulaAPI.Configurations.Configuration("options.role.doomsayer.guesscankillPlayer",true);
    static public BoolConfiguration GuessShowKillAnim = NebulaAPI.Configurations.Configuration("options.role.doomsayer.GuessKillShowAnim", true);
    static public BoolConfiguration GuessFailSuicide = NebulaAPI.Configurations.Configuration("options.role.doomsayer.Guessfailsuicide", false);
    static public BoolConfiguration GuessFailCannotGuess = NebulaAPI.Configurations.Configuration("options.role.doomsayer.guessfailcannotguess", true, () => !GuessFailSuicide);
    static public IntegerConfiguration MeetingCanGuessNum = NebulaAPI.Configurations.Configuration("options.role.doomsayer.meetingcanguessnum", (1, 16), 15);
    static public BoolConfiguration CanUseOracle = NebulaAPI.Configurations.Configuration("options.role.doomsayer.canuseoracle", false);
    static public BoolConfiguration BeforeMeetingStartNoSecondSkill = NebulaAPI.Configurations.Configuration("options.role.doomsayer.beforemeetingstartnosecondskill", true,()=>CanUseOracle);
    static public FloatConfiguration OracleCooldown = NebulaAPI.Configurations.Configuration("options.role.doomsayer.oracleCooldown",(0f,60f,2.5f),20f,FloatConfigurationDecorator.Second,()=>CanUseOracle);
    static public IntegerConfiguration OracleGetRoleNum = NebulaAPI.Configurations.Configuration("options.role.doomsayer.OracleGetRoleNum", (1,20),8,()=>CanUseOracle);
    static public BoolConfiguration GetRoleIsSpawnable = NebulaAPI.Configurations.Configuration("options.role.doomsayer.getroleisspawnable", true, () => CanUseOracle);
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static public Doomsayer MyRole = new Doomsayer();
    bool IAssignableDocument.HasTips => true;
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        public static GameEnd DoomsayerTeamWin = NebulaAPI.Preprocessor!.CreateEnd("doomsayer", MyRole.RoleColor);
        DefinedRole RuntimeRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        public static Action? afterGuess;
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            GameObject? binder = UnityHelper.CreateObject("DoomsayerWinCheck", MeetingHud.Instance.SkipVoteButton.transform.parent, MeetingHud.Instance.SkipVoteButton.transform.localPosition, null);
            GameOperatorManager? instance = GameOperatorManager.Instance;

            instance?.Subscribe<GameUpdateEvent>(delegate (GameUpdateEvent ev)
                {
                    binder.gameObject.SetActive(!this.MyPlayer.IsDead && MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.NotVoted);
                }, new GameObjectLifespan(binder));

            TextMeshPro? countText = UnityEngine.Object.Instantiate<TextMeshPro>(MeetingHud.Instance.TitleText, binder.transform);
            countText.gameObject.SetActive(true);
            countText.gameObject.GetComponent<TextTranslatorTMP>().enabled = false;
            countText.alignment = TextAlignmentOptions.Center;
            countText.transform.localPosition = new UnityEngine.Vector3(2.59f, 0f);
            countText.color = Palette.White;
            countText.transform.localScale *= 0.8f;
            countText.text = Language.Translate("role.doomsayer.winleftguess") + ":" + leftguess;
            GuesserSystem.DoomsayerOnMeetingStart(delegate
            {
                leftguess--;
                countText.text = Language.Translate("role.doomsayer.winleftguess") + ":" + Mathn.Max(leftguess, 0);
                if (leftguess <= 0)
                {
                    NebulaGameManager? instance = NebulaGameManager.Instance;
                    if (instance == null)
                    {
                        return;
                    }
                    new StaticAchievementToken("doomsayer.common1");
                    instance.RpcInvokeSpecialWin(DoomsayerTeamWin, 1 << (int)this.MyPlayer.PlayerId);
                }
                var winchance = 0f;
                switch (leftguess)
                {
                    case 3:
                        winchance = 0.2f;
                        break;
                    case 2:
                        winchance = 0.5f;
                        break;
                    case 1:
                        winchance = 0.8f;
                        break;
                }
                ModSingleton<IWinningOpportunity>.Instance?.RpcSetOpportunity(RoleTeam, winchance);
            },guessCanKillPlayer,GuessShowKillAnim,GuessFailSuicide,GuessFailCannotGuess,MeetingCanGuessNum);
        }
        int leftguess;

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd == DoomsayerTeamWin);
        }

        GamePlayer? oracleTarget;
        List<byte> useskilltargets = new List<byte>();
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                useskilltargets = new List<byte>();
                PoolablePlayer? playericon = null;
                GuesserSystem.doomsayerGuessKillPlayers = new List<GamePlayer>();
                leftguess = guessCorrectNumToWin;
                if (CanUseOracle)
                {
                    var playerTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeStandardPredicate(p)&&!useskilltargets.Contains(p.RealPlayer.PlayerId));

                    var oracleButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                        OracleCooldown, "doomsayer.oracle", Oracle.buttonImage,
                        _ => playerTracker.CurrentTarget != null, null);
                    oracleButton.OnClick = (button) =>
                    {
                        var player = playerTracker.CurrentTarget?.RealPlayer;
                        var result = player?.Role.Role;
                        if (BeforeMeetingStartNoSecondSkill)
                        {
                            oracleTarget = player;
                            if (playericon != null)
                            {
                                UnityEngine.Object.Destroy(playericon?.gameObject);
                            }
                            playericon = (button as ModAbilityButtonImpl)?.GeneratePlayerIcon(player);
                        }
                        else
                        {
                            HashSet<DefinedRole> list = new HashSet<DefinedRole>();
                            HashSet<DefinedRole> spwanableRoles = new HashSet<DefinedRole>();
                            var rolelist = new List<DefinedRole>(Nebula.Roles.Roles.AllRoles);
                            rolelist.Remove(Doomsayer.MyRole);
                            rolelist.Remove(Nebula.Roles.Crewmate.Crewmate.MyRole);
                            rolelist.Remove(Nebula.Roles.Impostor.Impostor.MyRole);
                            rolelist.RemoveAll(r => !r.CanBeGuess);
                            rolelist.Do(p =>
                            {
                                foreach (var r in p.GetGuessableAbilityRoles())
                                {
                                    spwanableRoles.Add(r);
                                }
                                foreach (AssignmentType type in AssignmentType.AllTypes)
                                {
                                    if (type.CanGuessAsAbility&&type.IsActive)
                                    {
                                        if (type.Predicate(p.AssignmentStatus,p))
                                        {
                                            AllocationParameters? customAllocationParameters = p.GetCustomAllocationParameters(type);
                                            if (customAllocationParameters != null && customAllocationParameters.RoleCountSum > 0)
                                            {
                                                spwanableRoles.Add(p);
                                            }
                                        }
                                    }
                                }
                            });
                            for (int i = 0; list.Count< OracleGetRoleNum - 1; i++)
                            {
                                var r = rolelist.Random();
                                if (r == player?.Role.Role)
                                {
                                    rolelist.Remove(r);
                                    continue;
                                }
                                if (!list.Contains(r))
                                {
                                    if (!GetRoleIsSpawnable || spwanableRoles.Contains(r))
                                    {
                                        list.Add(r);
                                        rolelist.Remove(r);
                                    }
                                }
                            }
                            var resultList = list.ToList();
                            DefinedRole? playerRole;
                            if (player.Role.Role.GetGuessableAbilityRoles().Any(r=>r ==player.Role.Role))
                            {
                                playerRole = player.Role.Role;
                            }
                            else
                            {
                                playerRole = spwanableRoles.FirstOrDefault(r => player.Role.CheckGuessAbility(r) && r != player.Role.Role);
                            }
                            if (playerRole==null)
                            {
                                playerRole = player.Role.Role;
                            }
                            resultList.Insert(UnityEngine.Random.Range(0, list.Count), playerRole);
                            var text = Language.Translate("role.doomsayer.oracletext").Replace("%NAME%", player.PlayerName).Replace("%ROLE%", string.Join(",", resultList.Select(r => r.DisplayName)));
                            PatchManager.RpcSendSkillMessage.Invoke((MyPlayer, text));
                            useskilltargets.Add(player.PlayerId);
                        }
                        button.StartCoolDown();
                    };
                    oracleButton.Visibility = (button) => !MyPlayer.IsDead;
                    GameOperatorManager.Instance?.Subscribe<MeetingPreStartEvent>(ev =>
                    {
                        if (playericon!=null)
                        {
                            UnityEngine.Object.Destroy(playericon?.gameObject);
                        }
                        if (CanUseOracle && BeforeMeetingStartNoSecondSkill && oracleTarget != null)
                        {
                            var player = oracleTarget;
                            HashSet<DefinedRole> list = new HashSet<DefinedRole>();
                            HashSet<DefinedRole> spwanableRoles = new HashSet<DefinedRole>();
                            var rolelist = new List<DefinedRole>(Nebula.Roles.Roles.AllRoles);
                            rolelist.Remove(Doomsayer.MyRole);
                            rolelist.Remove(Nebula.Roles.Crewmate.Crewmate.MyRole);
                            rolelist.Remove(Nebula.Roles.Impostor.Impostor.MyRole);
                            rolelist.RemoveAll(r => !r.CanBeGuess);
                            rolelist.Do(p =>
                            {
                                foreach (var r in p.GetGuessableAbilityRoles())
                                {
                                    spwanableRoles.Add(r);
                                }
                                foreach (AssignmentType type in AssignmentType.AllTypes)
                                {
                                    if (type.CanGuessAsAbility&&type.IsActive)
                                    {
                                        if (type.Predicate(p.AssignmentStatus, p))
                                        {
                                            AllocationParameters? customAllocationParameters = p.GetCustomAllocationParameters(type);
                                            if (customAllocationParameters != null && customAllocationParameters.RoleCountSum > 0)
                                            {
                                                spwanableRoles.Add(p);
                                            }
                                        }
                                    }
                                }
                            });
                            for (int i = 0; list.Count < OracleGetRoleNum - 1; i++)
                            {
                                var r = rolelist.Random();
                                if (r == player.Role.Role)
                                {
                                    rolelist.Remove(r);
                                    continue;
                                }
                                if (!list.Contains(r))
                                {
                                    if (!GetRoleIsSpawnable || spwanableRoles.Contains(r))
                                    {
                                        list.Add(r);
                                        rolelist.Remove(r);
                                    }
                                }
                            }
                            var resultList = list.ToList();
                            DefinedRole? playerRole;
                            if (player.Role.Role.GetGuessableAbilityRoles().Any(r => r == player.Role.Role))
                            {
                                playerRole = player.Role.Role;
                            }
                            else
                            {
                                playerRole = spwanableRoles.FirstOrDefault(r => player.Role.CheckGuessAbility(r) && r != player.Role.Role);
                            }
                            if (playerRole == null)
                            {
                                playerRole = player.Role.Role;
                            }
                            resultList.Insert(UnityEngine.Random.Range(0, list.Count), playerRole);
                            var text = Language.Translate("role.doomsayer.oracletext").Replace("%NAME%", oracleTarget.PlayerName).Replace("%ROLE%", string.Join(",", resultList.Select(r => r.DisplayName)));
                            PatchManager.RpcSendSkillMessage.Invoke((MyPlayer, text));
                            useskilltargets.Add(oracleTarget.PlayerId);
                            oracleTarget = null;
                        }
                    }, this);
                }
            }
        }
    }
}
