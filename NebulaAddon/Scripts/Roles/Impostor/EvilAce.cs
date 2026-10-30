using Plana.Core;
using Plana.Roles.Crewmate;

namespace Plana.Roles.Impostor;

public class EvilAce : DefinedSingleAbilityRoleTemplate<EvilAce.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    private EvilAce() : base("evilAce", NebulaTeams.ImpostorTeam.Color,RoleCategory.ImpostorRole,NebulaTeams.ImpostorTeam, [KillCoolDown,MurderSubCoolDown,MinCoolDown,IsLastImpostorMurderSubCooldown])
    {
    }
    static private IRelativeCooldownConfiguration KillCoolDown = NebulaAPI.Configurations.KillConfiguration("options.role.evilAce.killcooldown",CoolDownType.Relative, (0f, 60f, 2.5f), 25f,(-40f,40f,2.5f),+5f,(0.125f,2f,0.125f),1.25f);
    static private FloatConfiguration MurderSubCoolDown = NebulaAPI.Configurations.Configuration("options.role.evilAce.murdersub", (0f, 60f, 2.5f), 2.5f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration MinCoolDown = NebulaAPI.Configurations.Configuration("options.role.evilAce.mincd", (0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    static private BoolConfiguration IsLastImpostorMurderSubCooldown = NebulaAPI.Configurations.Configuration("options.role.evilAce.isLIMurderSubCooldown", false);
    Citation? HasCitation.Citation { get { return Citations.NebulaOnTheShip; } }
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/EvilAce.png")!.AsImage();
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueKillAbility;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public EvilAce MyRole = new EvilAce();
    bool IAssignableDocument.HasTips => true;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        public Ability(GamePlayer player,bool isUsurped) : base(player,isUsurped)
        {
            if (AmOwner)
            {
                killplayers = new List<byte>();
                var myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.LocalKillablePredicate, Palette.ImpostorRed, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption, false);
                killButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, true, false, VirtualKeyInput.Kill, null, KillCoolDown.Cooldown, "kill", null, (ModAbilityButton _) => myTracker.CurrentTarget != null, (ModAbilityButton _) =>!MyPlayer.IsDead, false).SetLabelType(ModAbilityButton.LabelType.Impostor);
                killButton.OnClick = (button) =>
                {
                    var player = myTracker.CurrentTarget;
                    var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEvent(MyPlayer, player));
                    if (!(cancelable?.IsCanceled ?? false))
                    {
                        //キャンセルされなければキルを実行する
                        MyPlayer.MurderPlayer(player!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill, result =>
                        {
                            if (result == KillResult.Kill)
                            {
                                if (killplayers != null)
                                {
                                    killplayers.Add(player!.PlayerId);
                                    if (killplayers.Count >= 5)
                                    {
                                        new StaticAchievementToken("evilAce.challenge2");
                                    }
                                }
                                if (!IsLastImpostorMurderSubCooldown||NebulaGameManager.Instance!.AllPlayerInfo.Count(p=>p!=MyPlayer&&!MyPlayer.CanKill(p))<=0)
                                {
                                    button.CoolDownTimer = NebulaAPI.Modules.Timer(this, Mathn.Max(MinCoolDown, KillCoolDown.Cooldown - MurderSubCoolDown * killplayers!.Count)).SetAsKillCoolTimer().Start();
                                }
                                NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                            }
                        });
                    }
                    if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton.OnBroken = (button)=>
                {
                    Snatcher.RewindKillCooldown();
                };
                killButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                killButton.SetAsUsurpableButton(this);
                GameOperatorManager.Instance?.Subscribe<PlayerMurderedEvent>(ev =>
                {
                    if (ev.Murderer.AmOwner && ev.Dead.PlayerState == PlayerState.Guessed && killplayers.Count >= 3)
                    {
                        new StaticAchievementToken("evilAce.common1");
                    }
                }, this);
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        ModAbilityButton? killButton;
        List<byte> killplayers = new List<byte>();
        bool IPlayerAbility.HideKillButton=>killButton!=null&&!killButton.IsBroken;
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (killplayers!=null&&killplayers.Count>=5&&ev.EndState.Winners.Test(MyPlayer))
            {
                new StaticAchievementToken("evilAce.challenge1");
            }
        }
        [Local]
        void ReflectRoleName(PlayerSetFakeRoleNameEvent ev)
        {
            if (killplayers==null)
            {
                killplayers = new List<byte>();
            }
            if (killplayers.Contains(ev.Player.PlayerId))
            {
                ev.Append(ev.Player.Role.DisplayColoredName);
            }
        }
        [Local,OnlyMyPlayer]
        void OnKill(PlayerKillPlayerEvent ev)
        {
            if (ev.Dead.PlayerState==PlayerState.Guessed&&!killplayers.Contains(ev.Dead.PlayerId))
            {
                killplayers.Add(ev.Dead!.PlayerId);
            }
        }
    }
}
