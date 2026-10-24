using Plana.Core;
using Plana.Roles.Crewmate;
using Virial.Helpers;

namespace Plana.Roles.Impostor;

public class MarksMan : DefinedSingleAbilityRoleTemplate<MarksMan.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    private MarksMan() : base("marksMan", NebulaTeams.ImpostorTeam.Color,RoleCategory.ImpostorRole,NebulaTeams.ImpostorTeam, [KillCooldown,MaxBullet,UseButtetResetCooldown,LoadButtetCooldown])
    {
    }
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/MarksMan.png")!.AsImage();
    static IRelativeCooldownConfiguration KillCooldown = NebulaAPI.Configurations.KillConfiguration("options.role.marksman.killcooldown",CoolDownType.Relative, (0f, 60f, 2.5f), 27.5f,(-40f,40f,2.5f),5f,(0.125f,2f,0.125f),1.25f);
    static IntegerConfiguration MaxBullet = NebulaAPI.Configurations.Configuration("options.role.marksman.maxbullet", new IntegerSelection([1,2,3,4,5,6,7,8,9,10,15,20,99]),3);
    static FloatConfiguration UseButtetResetCooldown = NebulaAPI.Configurations.Configuration("options.role.marksman.resetcooldown", (0f, 60f, 2.5f), 0f, FloatConfigurationDecorator.Second);
    static FloatConfiguration LoadButtetCooldown = NebulaAPI.Configurations.Configuration("options.role.marksman.loadbulletcooldown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    Citation? HasCitation.Citation { get { return Citations.SuperNewRoles; } }

    static public MarksMan MyRole = new MarksMan();
    bool IAssignableDocument.HasTips => true;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.KillersSide;
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueKillAbility;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new(player, arguments.GetAsBool(0),arguments.Get(1,0));
    public static RemoteProcess<(GamePlayer, int)> UpdateMarksManBulletRPC = new("UpdateMMBulletRPC", (m, _) =>
    {
        if (m.Item1.TryGetAbility<Ability>(out var ab))
        {
            ab.bullet = m.Item2;
        }
    });
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        public static Virial.Media.Image unloadBulletImage=NebulaAPI.AddonAsset.GetResource("marksUnloadBullet.png")!.AsImage(115f)!, loadBulletImage = NebulaAPI.AddonAsset.GetResource("marksLoadBullet.png.png")!.AsImage(115f)!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt(),bullet];
        public Ability(GamePlayer player,bool isUsurped,int bullets) : base(player,isUsurped)
        {
            bullet = bullets;
            if (AmOwner)
            {
                var myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.LocalKillablePredicate, Palette.ImpostorRed, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption, false);
                killButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, true, false, VirtualKeyInput.Kill, null, KillCooldown.Cooldown, "kill", null, (ModAbilityButton _) => myTracker.CurrentTarget != null, (ModAbilityButton _) => !MyPlayer.IsDead, false).SetLabelType(ModAbilityButton.LabelType.Impostor);
                killButton.OnClick = (button) =>
                {
                    var player = myTracker.CurrentTarget;
                    var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEvent(MyPlayer, player));
                    if (!(cancelable?.IsCanceled ?? false))
                    {
                        MyPlayer.MurderPlayer(player, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    }
                    if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton.ShowUsesIcon(0, $"{bullet}/{((int)MaxBullet)}");
                unloadbulletButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, false, false, VirtualKeyInput.SecondaryAbility, null, 0f, "marksman.unloadbullet",unloadBulletImage, (ModAbilityButton _) => bullet<MaxBullet&& !killButton.IsInCooldown, (ModAbilityButton _) => !MyPlayer.IsDead, false).SetLabelType(ModAbilityButton.LabelType.Impostor).SetAsUsurpableButton(this);
                unloadbulletButton.OnClick = (button) =>
                {
                    bullet++;
                    bullet = Mathn.Clamp(bullet, 0, MaxBullet);
                    killButton.UpdateUsesIcon($"{bullet}/{((int)MaxBullet)}");
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    UpdateMarksManBulletRPC.Invoke((MyPlayer, bullet));
                };
                loadbulletButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, false, false, VirtualKeyInput.Ability, null, LoadButtetCooldown, "marksman.loadbullet",loadBulletImage, (ModAbilityButton _) =>
                {
                    var timer = killButton.CoolDownTimer as TimerImpl;
                    return bullet > 0 && timer.CurrentTime>UseButtetResetCooldown&&killButton.IsInCooldown;
                }, (ModAbilityButton _) => !MyPlayer.IsDead, false).SetLabelType(ModAbilityButton.LabelType.Impostor).SetAsUsurpableButton(this);
                loadbulletButton.OnClick = (button) =>
                {
                    bullet--;
                    bullet = Mathn.Clamp(bullet, 0, MaxBullet);
                    killButton.UpdateUsesIcon($"{bullet}/{((int)MaxBullet)}");
                    killButton.CoolDownTimer.Start(UseButtetResetCooldown);
                    UpdateMarksManBulletRPC.Invoke((MyPlayer, bullet));
                };
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }
        ModAbilityButton? killButton, unloadbulletButton, loadbulletButton;
        bool IPlayerAbility.HideKillButton => true;
        public int bullet;
    }
}
