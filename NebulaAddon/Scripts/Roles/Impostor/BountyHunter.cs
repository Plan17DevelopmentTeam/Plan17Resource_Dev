using Nebula.Game.Statistics;
using Nebula.Roles.Modifier;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Game;
using Virial.Helpers;
using Vector3 = UnityEngine.Vector3;
using Color = UnityEngine.Color;

namespace Plana.Roles.Impostor;

public class BountyHunter : DefinedSingleAbilityRoleTemplate<BountyHunter.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    private BountyHunter() : base("bountyHunterMod", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole,NebulaTeams.ImpostorTeam, [BountyKillCoolDownOption, OthersKillCoolDownOption,NoResetBountyTarget,MaxBountyTarget,AddBountyTargetTime, ChangeBountyIntervalOption, ShowBountyArrowOption,ChangeBountySkillOption,ChangeBountySkillCooldownOption, ArrowUpdateIntervalOption]) { }
    Citation? HasCitation.Citation => NoResetBountyTarget?PCitations.TownOfUsMira:Citations.TheOtherRoles;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new(player, arguments.GetAsBool(0));

    static readonly FloatSelection CoolDownSelection = new FloatSelection(ArrayHelper.Selection(0f,10f,0.5f).Concat(ArrayHelper.Selection(10f,60f,2.5f).AsEnumerable()).Distinct().ToArray());
    static private readonly IRelativeCooldownConfiguration BountyKillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.bountyHunterMod.bountyKillCoolDown", CoolDownType.Ratio, CoolDownSelection, 10f, (-30f, 30f, 2.5f), -10f, (0.125f, 2f, 0.125f), 0.5f);
    static private readonly IRelativeCooldownConfiguration OthersKillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.bountyHunterMod.othersKillCoolDown", CoolDownType.Ratio, CoolDownSelection, 40f, (-30f, 30f, 2.5f), 20f, (0.125f, 2f, 0.125f), 2f);
    static private readonly BoolConfiguration ShowBountyArrowOption = NebulaAPI.Configurations.Configuration("options.role.bountyHunterMod.showBountyArrow", true);
    static private readonly FloatConfiguration ArrowUpdateIntervalOption = NebulaAPI.Configurations.Configuration("options.role.bountyHunterMod.arrowUpdateInterval", (0f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second, () => ShowBountyArrowOption);
    static private readonly BoolConfiguration NoResetBountyTarget = NebulaAPI.Configurations.Configuration("options.role.bountyHunterMod.noResetBounty", false);
    static private readonly IntegerConfiguration MaxBountyTarget = NebulaAPI.Configurations.Configuration("options.role.bountyHunterMod.maxBountyTarget", (0, 15), 5, () => NoResetBountyTarget, val => val == 0 ?Language.Translate("options.noLimit") : val.ToString());
    static private readonly FloatConfiguration AddBountyTargetTime = NebulaAPI.Configurations.Configuration("options.role.bountyHunterMod.addBountyTargetTime", (0f,120f,2.5f),45f,FloatConfigurationDecorator.Second, () => NoResetBountyTarget);
    static private readonly FloatConfiguration ChangeBountyIntervalOption = NebulaAPI.Configurations.Configuration("options.role.bountyHunterMod.changeBountyInterval", (5f, 120f, 5f), 45f, FloatConfigurationDecorator.Second,()=>!NoResetBountyTarget);
    static private readonly BoolConfiguration ChangeBountySkillOption = NebulaAPI.Configurations.Configuration("options.role.bountyHunterMod.changeBountySkill", false,()=>!NoResetBountyTarget);
    static private readonly FloatConfiguration ChangeBountySkillCooldownOption = NebulaAPI.Configurations.Configuration("options.role.bountyHunterMod.changeBountySkillcooldown", (0f,60f,2.5f),30f,FloatConfigurationDecorator.Second,()=>ChangeBountySkillOption);
    static public readonly BountyHunter MyRole = new();
    bool IAssignableDocument.HasTips => true;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.KillersSide;
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueKillAbility;

    static private readonly GameStatsEntry StatsBountyKill = NebulaAPI.CreateStatsEntry("stats.bountyHunter.bountyKill", GameStatsCategory.Roles, MyRole);
    static private readonly GameStatsEntry StatsOthersKill = NebulaAPI.CreateStatsEntry("stats.bountyHunter.othersKill", GameStatsCategory.Roles, MyRole);
    float MaxKillCoolDown => Mathn.Max(BountyKillCoolDownOption.Cooldown, OthersKillCoolDownOption.Cooldown, AmongUsLLImpl.Instance.VanillaKillCooldown);
    IEnumerable<DefinedAssignable> DefinedAssignable.AchievementGroups => [Nebula.Roles.Impostor.BountyHunter.MyRole, Nebula.Roles.Impostor.BountyHunter.MyRole, MyRole];
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        private ModAbilityButtonImpl? killButton = null,skillButton;
        bool IPlayerAbility.HideKillButton => true;
        Image changeBountyImage = NebulaResourceTextureLoader.FromResource("Nebula.Resources.Buttons.SpectatorExitButton.png", 115f);
        private AchievementToken<bool>? acTokenKillBounty;
        private AchievementToken<bool>? acTokenKillNonBounty;
        private AchievementToken<(bool cleared,Queue<float> history)>? acTokenChallenge;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                acTokenKillBounty = new("bountyHunter.common1", false, (val, _) => val);
                acTokenKillNonBounty = new("bountyHunter.another1", false, (val, _) => val);
                acTokenChallenge = new("bountyHunter.challenge", (false, new()), (val, _) => val.cleared);

                bountyTimer = new TimerImpl(NoResetBountyTarget?AddBountyTargetTime:ChangeBountyIntervalOption).Start().Register(this);
                arrowTimer = new TimerImpl(ArrowUpdateIntervalOption).Start().Register(this);

                var killTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, ObjectTrackers.PlayerlikeKillablePredicate(MyPlayer), Palette.ImpostorRed, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption);

                killButton = new ModAbilityButtonImpl(false, true).KeyBind(Virial.Compat.VirtualKeyInput.Kill).Register(this);
                killButton.Availability = (button) => killTracker.CurrentTarget != null && MyPlayer.CanMove;
                killButton.Visibility = (button) => !MyPlayer.IsDead;
                killButton.OnClick = (button) => {
                    var target = killTracker.CurrentTarget;
                    var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, target));
                    if (!(cancelable?.IsCanceled ?? false))
                    {
                        MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    }
                    else
                    {
                        if (cancelable?.ResetCooldown ?? false)
                        {
                            button.CoolDownTimer.Start(MyPlayer.TeamKillCooldown);
                        }
                        return;
                    }

                    if (IsUsurped)
                    {
                        button.CoolDownTimer!.Start(MyPlayer.TeamKillCooldown);
                    }
                    else
                    {
                        if (target!.RealPlayer.PlayerId == currentBounty || currentTargets.Any(p => p.RealPlayer.PlayerId == target.RealPlayer.PlayerId))
                        {
                            if (!NoResetBountyTarget)
                            {
                                ChangeBounty(killTracker.CurrentTarget.RealPlayer);
                            }
                            else
                            {
                                currentTargets.Remove(target.RealPlayer);
                                TargetArrows.Do(arrow =>
                                {
                                    if (arrow.MyPlayer.PlayerId == target.RealPlayer.PlayerId)
                                    {
                                        arrow.Release();
                                    }
                                });
                                TargetArrows.RemoveAll(p => p.IsDeadObject);
                            }
                            var cooldown = BountyKillCoolDownOption.Cooldown;
                            (button.CoolDownTimer as AdvancedTimer).SetVisualMax(cooldown);
                            button.CoolDownTimer!.Start(cooldown);
                            acTokenKillBounty.Value = true;
                            StatsBountyKill.Progress();
                        }
                        else
                        {
                            var cd = OthersKillCoolDownOption.Cooldown;
                            (button.CoolDownTimer as AdvancedTimer).SetVisualMax(cd);
                            button.CoolDownTimer!.Start(cd);
                            acTokenKillNonBounty.Value = true;
                            StatsOthersKill.Progress();
                        }
                    }

                    acTokenChallenge.Value.history.Enqueue(Time.time);
                    if (acTokenChallenge.Value.history.Count >= 3)
                    {
                        float first = acTokenChallenge.Value.history.DequeAt(3);
                        float last = Time.time;
                        if (last - first < 30f) acTokenChallenge.Value.cleared = true;
                    }
                };
                killButton.OnStartTaskPhase = (button) =>
                {
                    (button.CoolDownTimer as AdvancedTimer).SetVisualMax(AmongUsLLImpl.Instance.VanillaKillCooldown);
                };
                killButton.CoolDownTimer = new AdvancedTimer(10f, MyRole.MaxKillCoolDown).SetDefault(AmongUsLLImpl.Instance.VanillaKillCooldown).SetAsKillCoolDown().Start(10f).Register(this);
                killButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                killButton.SetLabel("kill");
                if (ChangeBountySkillOption&&!NoResetBountyTarget)
                {
                    skillButton = new ModAbilityButtonImpl(false, false).KeyBind(Virial.Compat.VirtualKeyInput.Ability).Register(this);
                    (skillButton as ModAbilityButton).SetImage(changeBountyImage);
                    skillButton.Availability = (button) => MyPlayer.CanMove;
                    skillButton.Visibility = (button) => !MyPlayer.IsDead;
                    skillButton.OnClick = (button) => {
                        if (IsUsurped)
                        {
                            return;
                        }
                        ChangeBounty(GamePlayer.GetPlayer(currentBounty));
                        button.StartCoolDown();
                    };
                    skillButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, ChangeBountySkillCooldownOption).SetAsAbilityTimer().Start();
                    skillButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                    skillButton.SetLabel("bountyhunter.changetarget");
                    (skillButton as ModAbilityButton).SetAsUsurpableButton(this);
                }
                iconHolder = HudContent.InstantiateContent("BountyHolder", true);
                this.BindGameObject(iconHolder.gameObject);
                if (!NoResetBountyTarget)
                {
                    bountyIcon = AmongUsUtil.GetPlayerIcon(MyPlayer.Unbox().DefaultOutfit.Outfit.outfit, iconHolder.transform, Vector3.zero, Vector3.one * 0.5f);
                    bountyIcon.ToggleName(true);
                    bountyIcon.SetName("", Vector3.one * 4f, UnityEngine.Color.white, -1f);
                    if (ShowBountyArrowOption) bountyArrow = new Arrow().SetColor(NebulaTeams.ImpostorTeam.Color).Register(this);
                    ChangeBounty();
                }
                else
                {
                    TargetIcons = new();
                    currentTargets = new();
                    TargetArrows = new();
                    bountyTimer.SetPredicate(() => !MeetingHud.Instance&&!ExileController.Instance&&(MaxBountyTarget==0||currentTargets.Count<MaxBountyTarget));
                    var button = new ModAbilityButtonImpl(false, false).Register(this);
                    (button as ModAbilityButton).SetImage(changeBountyImage);
                    button.Availability = (button) => false;
                    button.Visibility = (button) => !MyPlayer.IsDead;
                    var timer = new TimerImpl(AddBountyTargetTime).Register(this);
                    button.CoolDownTimer = timer;
                    button.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                    button.SetLabel("bountyhunter.nextTargetTime");
                    (button as ModAbilityButton).SetAsUsurpableButton(this);
                    GameOperatorManager.Instance.Subscribe<GameUpdateEvent>(ev =>
                    {
                        timer.SetTime(bountyTimer.CurrentTime);
                    }, bountyTimer);
                    GameOperatorManager.Instance.Subscribe<GameStartEvent>(ev =>
                    {
                        if (GeneralConfigurations.UseShortenCooldownAtGameStartOption)
                        {
                            bountyTimer.SetTime(GeneralConfigurations.ShortenCooldownAtGameStartOption+2.5f);
                        }
                    }, this);
                }
            }
        }
        List<GamePlayer> currentTargets = new();
        HudContent? iconHolder;
        private byte currentBounty = 0;
        PoolablePlayer bountyIcon = null!;
        List<PoolablePlayer> TargetIcons = new List<PoolablePlayer>();
        List<TrackingArrowAbility> TargetArrows = new List<TrackingArrowAbility>();
        TimerImpl bountyTimer = null!;
        TimerImpl arrowTimer = null!;
        Arrow bountyArrow = null!;
        
        void ChangeBounty(GamePlayer? excluded = null)
        {
            var arr = PlayerControl.AllPlayerControls.GetFastEnumerator().Where(p => !p.AmOwner && p.PlayerId != excluded?.PlayerId && !p.Data.IsDead && MyPlayer.CanKill(p.GetModInfo()!)).ToArray();
            if (arr.Length == 0) currentBounty = byte.MaxValue;
            else currentBounty = arr[System.Random.Shared.Next(arr.Length)].PlayerId;

            if (currentBounty == byte.MaxValue)
                bountyIcon.gameObject.SetActive(false);
            else
            {
                bountyIcon.gameObject.SetActive(true);
                bountyIcon.UpdateFromPlayerOutfit(GamePlayer.GetPlayer(currentBounty)!.Unbox().DefaultOutfit.Outfit.outfit, PlayerMaterial.MaskType.None, false, true);
            }

            if (ShowBountyArrowOption) UpdateArrow();

            bountyTimer.Start();
        }
        void AddBounty(List<GamePlayer> excludes)
        {
            var nextBounty = byte.MaxValue;
            var arr = PlayerControl.AllPlayerControls.GetFastEnumerator().Where(p => !p.AmOwner && !excludes.Any(player=>p.PlayerId==player.PlayerId) && !p.Data.IsDead && MyPlayer.CanKill(p.GetModInfo()!)).ToArray();
            if (arr.Length == 0) return;
            else nextBounty = arr[System.Random.Shared.Next(arr.Length)].PlayerId;

            if (nextBounty == byte.MaxValue)
                return;
            else
            {
                var target = GamePlayer.GetPlayer(nextBounty);
                /*var icon = AmongUsUtil.GetPlayerIcon(target.Unbox().DefaultOutfit.Outfit.outfit, iconHolder?.transform, Vector3.zero, Vector3.one * 0.5f);
                icon.ToggleName(false);
                icon.gameObject.SetActive(true);
                TargetIcons.Add(icon);*/
                if (ShowBountyArrowOption) TargetArrows.Add(new TrackingArrowAbility(target,ArrowUpdateIntervalOption,NebulaTeams.ImpostorTeam.Color,false).Register(this));
                currentTargets.Add(target);
                HideAndSeekDeathPopup hideAndSeekDeathPopup = UnityEngine.Object.Instantiate<HideAndSeekDeathPopup>(GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab, DestroyableSingleton<HudManager>.Instance.transform.parent);
                hideAndSeekDeathPopup.Show(target.VanillaPlayer, 0);
                TextTranslatorTMP tmp;
                if (hideAndSeekDeathPopup.text.transform.TryGetComponent<TextTranslatorTMP>(out tmp))
                {
                    tmp.defaultStr = Language.Translate("role.bountyHunterMod.nextTargetis");
                    tmp.TargetText = StringNames.None;
                    tmp.ResetText();
                }
            }
            bountyTimer.Start();
        }
        
        void UpdateArrow()
        {
            var target = NebulaGameManager.Instance?.GetPlayer(currentBounty);
            if (target==null)
            {
                bountyArrow.IsActive= false;
            }
            else
            {
                bountyArrow.IsActive= true;
                bountyArrow.TargetPos = target.VanillaPlayer.transform.localPosition;
            }

            arrowTimer.Start();
        }

        void UpdateTimer()
        {
            if (NoResetBountyTarget)
            {
                if (!bountyTimer.IsProgressing)
                {
                    AddBounty(currentTargets);
                }
            }
            else
            {
                if (!bountyTimer.IsProgressing)
                {
                    ChangeBounty();
                }
                bountyIcon.SetName(Mathn.CeilToInt(bountyTimer.CurrentTime).ToString());

                if (ShowBountyArrowOption && !arrowTimer.IsProgressing)
                {
                    UpdateArrow();
                }
            }
        }

        [Local]
        void LocalUpdate(GameUpdateEvent ev) => UpdateTimer();
        

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            //死亡しているプレイヤーであれば切り替える
            if (!NoResetBountyTarget)
            {
                if (GamePlayer.GetPlayer(currentBounty)?.IsDead ?? true && !NoResetBountyTarget) ChangeBounty();
            }
            else
            {
                currentTargets.RemoveAll(p => p.IsDead);
                TargetArrows.Do(p =>
                {
                    if (p.MyPlayer.IsDead)
                    {
                        p.Release();
                    }
                });
                TargetArrows.RemoveAll(p => p.IsDeadObject);
            }
        }
    }
}
