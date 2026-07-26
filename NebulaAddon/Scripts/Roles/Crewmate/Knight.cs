using Plana.Core;
using Plana.Roles.Modifier;
using static UnityEngine.GraphicsBuffer;

namespace Plana.Roles.Crewmate;

public class Knight : DefinedSingleAbilityRoleTemplate<Knight.Ability>, DefinedRole, HasCitation, IAssignableDocument
{
    private Knight() : base("knight", Virial.Color.Gray, RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfKILLOption, KILLCooldownOption, CanKillHidingPlayerOption, SealAbilityUntilReportingDeadBodiesOption, OnPlayerDeadUnlockKill])
    {
        IConfigurationHolder configurationHolder = base.ConfigurationHolder;
        if (configurationHolder != null)
        {
            configurationHolder.AddTags(new ConfigurationTag[] { ConfigurationTags.TagBeginner });
        }
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    Citation? HasCitation.Citation { get { return PCitations.TOHE; } }
    public static IntegerConfiguration NumOfKILLOption = NebulaAPI.Configurations.Configuration("options.role.knight.op1", (1, 15), 1);
    public static FloatConfiguration KILLCooldownOption = NebulaAPI.Configurations.Configuration("options.role.knight.op2", (0f, 45f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    public static BoolConfiguration CanKillHidingPlayerOption = NebulaAPI.Configurations.Configuration("options.role.knight.op3", false);
    public static BoolConfiguration SealAbilityUntilReportingDeadBodiesOption = NebulaAPI.Configurations.Configuration("options.role.knight.op4", true);
    public static BoolConfiguration OnPlayerDeadUnlockKill = NebulaAPI.Configurations.Configuration("options.role.knight.op5", false);
    static public Knight MyRole = new Knight();
    bool IAssignableDocument.HasTips => true;
    public override Knight.Ability CreateAbility(GamePlayer player, int[] arguments)
    {
        return new Knight.Ability(player, arguments.GetAsBool(0), arguments.Get<int>(1, NumOfKILLOption));
    }
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility, IBindPlayer, IGameOperator, ILifespan
    {
        ModAbilityButton killButton;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt(), leftKill];

        static private Virial.Media.Image KillImage = NebulaAPI.AddonAsset.GetResource("KILL.png").AsImage(100f);
        int leftKill;
        public Ability(GamePlayer player, bool isUsurped, int shots)
            : base(player, isUsurped)
        {
            if (AmOwner)
            {
                leftKill = shots;
                SpriteRenderer lockSprite = null;
                ObjectTracker<GamePlayer> killTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.KillablePredicate(base.MyPlayer), null, CanKillHidingPlayerOption, false);
                killButton = NebulaAPI.Modules.AbilityButton(this, false, true, 0, false).BindKey(base.MyPlayer.IsCrewmate ? Virial.Compat.VirtualKeyInput.Kill : Virial.Compat.VirtualKeyInput.Ability, null).SetAsUsurpableButton(this);
                killButton.Availability = (button) => killTracker.CurrentTarget != null && this.MyPlayer.CanMove && lockSprite == null;
                killButton.Visibility = (button) => !base.MyPlayer.IsDead && leftKill > 0;
                killButton.SetImage(KillImage);
                killButton.ShowUsesIcon(3, leftKill.ToString());
                try
                {
                    if (SealAbilityUntilReportingDeadBodiesOption)
                    {
                        if (!NebulaGameManager.Instance!.AllPlayerInfo.Any(p=> p.IsDead))
                        {
                            lockSprite = (killButton as ModAbilityButtonImpl)!.VanillaButton.AddLockedOverlay();
                        }
                        if (OnPlayerDeadUnlockKill)
                        {
                            GameOperatorManager.Instance?.Subscribe<GameUpdateEvent>(op =>
                            {
                                if (lockSprite != null && DestroyableSingleton<HudManager>.InstanceExists && DestroyableSingleton<HudManager>.Instance.ReportButton.canInteract)
                                {
                                    (killButton as ModAbilityButtonImpl).PlayFlashOnce();
                                    UnityEngine.Object.Destroy(lockSprite.gameObject);
                                    lockSprite = null;
                                }
                            }, this);
                        }
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log("SetLockError : " + e.ToString());
                }
                killButton.OnClick = button=>
                {
                    leftKill--;
                    var target = killTracker.CurrentTarget;
                    var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, target));
                    if (!(cancelable?.IsCanceled ?? false))
                    {
                        MyPlayer.MurderPlayer(killTracker.CurrentTarget, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill, (result) =>
                        {
                            if (result == KillResult.Kill)
                            {
                                new StaticAchievementToken("knight.common1");
                                if (killTracker.CurrentTarget.Role.Role.Category != RoleCategory.CrewmateRole)
                                {
                                    new StaticAchievementToken("knight.common2");
                                }
                                if (killTracker.CurrentTarget.Role is Sheriff.Ability)
                                {
                                    new StaticAchievementToken("knight.another1");
                                }
                                if (killTracker.CurrentTarget.Role is Mayor.Ability)
                                {
                                    new StaticAchievementToken("knight.another2");
                                }
                                if (killTracker.CurrentTarget.Role is Sidekick.Instance || killTracker.CurrentTarget.Modifiers.Any((RuntimeModifier m) => m is SidekickModifier))
                                {
                                    new StaticAchievementToken("knight.challenge1");
                                }
                                killButton.UpdateUsesIcon(leftKill.ToString());
                            }
                            else
                            {
                                leftKill++;
                            }
                        });
                    }
                    if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, KILLCooldownOption).SetAsKillCoolTimer().Start(null);
                killButton.StartCoolDown();
                killButton.SetLabel("kill");
                (killButton as ModAbilityButtonImpl)!.OnMeeting = (button) =>
                {
                    if (lockSprite!=null)
                    {
                        if (NebulaGameManager.Instance!.AllPlayerInfo.Any((Player p) => p.IsDead))
                        {
                            UnityEngine.Object.Destroy(lockSprite.gameObject);
                            lockSprite = null;
                        }
                    }
                };
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }
    }
}