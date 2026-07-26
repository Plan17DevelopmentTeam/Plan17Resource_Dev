using Nebula.Roles.Neutral;
using Nebula.VoiceChat;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Modifier;

namespace Plana.Roles.Neutral;

public class Connoisseur : DefinedRoleTemplate, HasCitation, DefinedRole, IAssignableDocument
{
    public static Team RoleTeam = new Team("teams.connoisseur", new Virial.Color(111,61,177), TeamRevealType.OnlyMe);
    private Connoisseur() : base("connoisseur", RoleTeam.Color, RoleCategory.NeutralRole, RoleTeam, [GoalNum,CanSelectImpBeGoal,CanSelectNeuBeGoal,CanSelectCrewBeGoal,ImpurityNum,CanSelectImpBeImpurity,CanSelectNeuBeImpurity,CanSelectCrewBeImpurity,KillNumOption,KillCooldown])
    {
    }
    Citation? HasCitation.Citation => PCitations.PlanaANDKCANDXiaoming;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public static readonly IntegerConfiguration GoalNum = NebulaAPI.Configurations.Configuration("options.role.conniosseur.goalNum", (1, 5), 1);
    public static readonly BoolConfiguration CanSelectImpBeGoal = NebulaAPI.Configurations.Configuration("options.role.conniosseur.canSelectImpBeGoal", false);
    public static readonly BoolConfiguration CanSelectNeuBeGoal = NebulaAPI.Configurations.Configuration("options.role.conniosseur.canSelectNeuBeGoal", false);
    public static readonly BoolConfiguration CanSelectCrewBeGoal = NebulaAPI.Configurations.Configuration("options.role.conniosseur.canSelectCrewBeGoal", true);
    public static readonly IntegerConfiguration ImpurityNum = NebulaAPI.Configurations.Configuration("options.role.conniosseur.ImpuritylNum", (0, 10), 2);
    public static readonly BoolConfiguration CanSelectImpBeImpurity = NebulaAPI.Configurations.Configuration("options.role.conniosseur.canSelectImpBeImpurity", true);
    public static readonly BoolConfiguration CanSelectNeuBeImpurity= NebulaAPI.Configurations.Configuration("options.role.conniosseur.canSelectNeuBeImpurity", true);
    public static readonly BoolConfiguration CanSelectCrewBeImpurity = NebulaAPI.Configurations.Configuration("options.role.conniosseur.canSelectCrewBeImpurity", true);
    public static readonly IntegerConfiguration KillNumOption = NebulaAPI.Configurations.Configuration("options.role.conniosseur.killNum", (0, 15), 1);
    public static readonly IRelativeCooldownConfiguration KillCooldown = NebulaAPI.Configurations.KillConfiguration("options.role.conniosseur.killCooldown", CoolDownType.Immediate, (0f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), 2.5f, (0.125f, 2f, 0.125f), 1f, null);
    static public Connoisseur MyRole = new Connoisseur();
    bool IAssignableDocument.HasTips => true;
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        public static GameEnd ConnoisseurWin = NebulaAPI.Preprocessor!.CreateEnd("connoisseur", RoleTeam.Color);
        public Instance(GamePlayer player) : base(player)
        {
        }
        DefinedRole RuntimeRole.Role => MyRole;
        [OnlyHost]
        void CheckWin(GameUpdateEvent ev)
        {
            if (!MeetingHud.Instance&&MyPlayer.IsAlive)
            {
                var goals = NebulaGameManager.Instance!.AllPlayerInfo.Where(p => ConnoisseurGoal.IsMyGoal(MyPlayer, p));
                var impurities = NebulaGameManager.Instance!.AllPlayerInfo.Where(p => ConnoisseurImpurity.IsMyImpurity(MyPlayer, p));
                if (goals.Count()>0&&goals.All(p => p.PlayerState == PlayerStates.Exiled)&&impurities.All(p => p.IsDead))
                {
                    NebulaAPI.CurrentGame?.TriggerGameEnd(ConnoisseurWin, GameEndReason.Special, BitMasks.AsPlayer(MyPlayer));
                }
            }
        }
        [Local]
        void OnPlayerDie(PlayerDieEvent ev)
        {
            if (ev is PlayerExiledEvent)
            {
                return;
            }
            if (MyPlayer.IsDead)
            {
                return;
            }
            if (ConnoisseurGoal.IsMyGoal(MyPlayer,ev.Player)&&ev.Player.PlayerState!=PlayerStates.Exiled)
            {
                var allplayers = NebulaAPI.CurrentGame?.GetAllPlayers().Where(p => p != MyPlayer && MyPlayer.CanKill(p) && !p.IsDead && !p.IsDisconnected && !p.TryGetModifier<ConnoisseurGoal.Instance>(out var g) && !p.TryGetModifier<ConnoisseurImpurity.Instance>(out var i));
                var goalCanSelectplayers = allplayers.Where(p => (CanSelectImpBeGoal && p.IsImpostor) || (CanSelectCrewBeGoal && p.IsCrewmate) || (CanSelectNeuBeGoal && p.Role.Role.Category == RoleCategory.NeutralRole)).ToList();
                goalCanSelectplayers.Random().AddModifier(ConnoisseurGoal.MyRole, [MyPlayer.PlayerId]);
            }
        }
        [Local]
        private void DecorateName(PlayerDecorateNameEvent ev)
        {
            if (!NebulaGameManager.Instance!.CanSeeAllInfo)
            {
                if (ConnoisseurGoal.IsMyGoal(MyPlayer, ev.Player) && !ev.Player.AmOwner)
                {
                    ev.Name += " ■".Color(MyRole.RoleColor.ToUnityColor());
                }
            }
        }
        [Local]
        private void ReflectRoleName(PlayerSetFakeRoleNameEvent ev)
        {
            if (!NebulaGameManager.Instance!.CanSeeAllInfo)
            {
                if (ConnoisseurImpurity.IsMyImpurity(MyPlayer,ev.Player) && !ev.Player.AmOwner)
                {
                    var modi= ev.Player.Modifiers.FirstOrDefault(m => m is ConnoisseurImpurity.Instance);
                    ev.Append(modi?.OverrideRoleName(string.Empty,false,true)??string.Empty);
                }
            }
        }
        ModAbilityButton? killButton;
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var allplayers = NebulaAPI.CurrentGame?.GetAllPlayers().Where(p => p!=MyPlayer&&MyPlayer.CanKill(p)&&!p.IsDead && !p.IsDisconnected&& !p.TryGetModifier<ConnoisseurGoal.Instance>(out var g) && !p.TryGetModifier<ConnoisseurImpurity.Instance>(out var i));
                var goalCanSelectplayers = allplayers.Where(p => (CanSelectImpBeGoal && p.IsImpostor) || (CanSelectCrewBeGoal && p.IsCrewmate) || (CanSelectNeuBeGoal && p.Role.Role.Category == RoleCategory.NeutralRole)).ToList();
                while(true)
                {
                    if (NebulaAPI.CurrentGame?.GetAllPlayers().Count(p=>ConnoisseurGoal.IsMyGoal(MyPlayer,p))>=GoalNum)
                    {
                        break;
                    }
                    var player = goalCanSelectplayers.Random();
                    player.AddModifier(ConnoisseurGoal.MyRole, [MyPlayer.PlayerId]);
                }
                var impurityCanSelectplayers = allplayers.Where(p =>(CanSelectImpBeImpurity && p.IsImpostor) || (CanSelectCrewBeImpurity&& p.IsCrewmate) || (CanSelectNeuBeImpurity && p.Role.Role.Category == RoleCategory.NeutralRole)).ToList();
                while (true)
                {
                    if (NebulaAPI.CurrentGame?.GetAllPlayers().Count(p => ConnoisseurImpurity.IsMyImpurity(MyPlayer, p)) >= ImpurityNum)
                    {
                        break;
                    }
                    var player = impurityCanSelectplayers.Random();
                    impurityCanSelectplayers.Remove(player);
                    player.AddModifier(ConnoisseurImpurity.MyRole, [MyPlayer.PlayerId]);
                }
                if (KillNumOption!=0)
                {
                    int leftKill = KillNumOption;
                    var myTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer,ObjectTrackers.PlayerlikeLocalKillablePredicate, null, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption);

                    killButton = NebulaAPI.Modules.PlayerlikeKillButton(this, MyPlayer, new PlayerInteractParameter(false, true, true), true, Virial.Compat.VirtualKeyInput.Kill, null,
                        KillCooldown.Cooldown, "kill", ModAbilityButton.LabelType.Impostor, null!,
                        (target, _) =>
                        {
                            var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, target));
                            if (!(cancelable?.IsCanceled ?? false))
                            {
                                leftKill--;
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill, result =>
                                {
                                    if (result == KillResult.Kill)
                                    {
                                        killButton.UpdateUsesIcon(leftKill.ToString());
                                    }
                                    else
                                    {
                                        leftKill++;
                                    }
                                });
                            }
                            if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                        },
                        myTracker,
                        _ => !MyPlayer.IsDived,
                        _ => MyPlayer.AllowToShowKillButtonByAbilities&&leftKill>0
                        );
                    killButton.ShowUsesIcon(3, leftKill.ToString());
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
                }
            }
        }
    }
}
public class ConnoisseurGoal : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
{
    private ConnoisseurGoal() : base("connoisseurGoal", Connoisseur.RoleTeam.Color, null, true, () => false)
    {
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKCANDXiaoming; } }
    public static bool IsMyGoal(GamePlayer connoisseur, GamePlayer goal)
    {
        return goal.Modifiers.Any(m =>
        {
            if (m is Instance instance)
            {
                return instance.MyConnoisseur?.PlayerId == connoisseur.PlayerId;
            }
            return false;
        });
    }

    static public ConnoisseurGoal MyRole = new ConnoisseurGoal();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, GamePlayer.GetPlayer((byte)arguments.Get(0, -1)));
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {

        bool RuntimeAssignable.CanBeAwareAssignment
        {
            get
            {
                NebulaGameManager instance = NebulaGameManager.Instance!;
                return instance != null && instance.CanSeeAllInfo;
            }
        }

        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public GamePlayer? MyConnoisseur;
        public Instance(GamePlayer player, GamePlayer connoisseur) : base(player)
        {
            MyConnoisseur = connoisseur;
        }
        void RuntimeAssignable.OnActivated()
        {
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (canSeeAllInfo||inEndScene) name += "■".Color(MyRole.RoleColor.ToUnityColor());
        }
    }
}
public class ConnoisseurImpurity : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
{
    private ConnoisseurImpurity() : base("connoisseurImpurity", Connoisseur.RoleTeam.Color, null, true, () => false)
    {
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKCANDXiaoming; } }
    public static bool IsMyImpurity(GamePlayer connoisseur,GamePlayer impurity)
    {
        return impurity.Modifiers.Any(m =>
        {
            if (m is Instance instance)
            {
                return instance.MyConnoisseur?.PlayerId == connoisseur.PlayerId;
            }
            return false;
        });
    }

    static public ConnoisseurImpurity MyRole = new ConnoisseurImpurity();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player,GamePlayer.GetPlayer((byte)arguments.Get(0,-1)));
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {

        bool RuntimeAssignable.CanBeAwareAssignment
        {
            get
            {
                NebulaGameManager instance = NebulaGameManager.Instance!;
                return instance != null && instance.CanSeeAllInfo;
            }
        }

        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public GamePlayer? MyConnoisseur;
        public Instance(GamePlayer player,GamePlayer connoisseur) : base(player)
        {
            MyConnoisseur = connoisseur;
        }
        void RuntimeAssignable.OnActivated()
        {
        }
        string? RuntimeAssignable.OverrideRoleName(string lastRoleName, bool isShort, bool canSeeAllInfo)
        {
            if (canSeeAllInfo)
            {
                return lastRoleName + $" {(this as RuntimeAssignable).DisplayColoredName}";
            }
            return null;
        }
    }
}