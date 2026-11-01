using Nebula.Patches;
using Plana.Core;
using Plana.Roles.Crewmate;
using Virial.Helpers;

namespace Plana.Roles.Impostor;

public class Almighty : DefinedSingleAbilityRoleTemplate<Almighty.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    private Almighty() : base("almighty", NebulaTeams.ImpostorTeam.Color,RoleCategory.ImpostorRole,NebulaTeams.ImpostorTeam,[RecoverEnergyTo100Time,MaxEnergy,UseVentRequireEnergy,UseKillRequireEnergy,UseViperKillRequireEnergy,DissolveDurationOption,AcidLeavesBoneOption,CanReportBoneOption,ShapeShiftCooldown,ShapeshiftLeaveEvidence,UseShapeshiftEnergy,VanishCooldown,UseVanishEnergy,InEffectSubEnergyPercentageEverySecond])
    {
    }
    public static readonly FloatConfiguration RecoverEnergyTo100Time = NebulaAPI.Configurations.Configuration("options.role.almighty.recoverEnegryTo100Time", (2.5f, 120f, 2.5f), 17.5f, FloatConfigurationDecorator.Second);
    public static readonly IntegerConfiguration MaxEnergy = NebulaAPI.Configurations.Configuration("options.role.almighty.maxEnergy", (0,500,5),200,null,val=>val.ToString()+Language.Translate("options.percentage"));
    public static readonly IntegerConfiguration UseVentRequireEnergy = NebulaAPI.Configurations.Configuration("options.role.almighty.ventEnergy", (0, 150, 5), 25, null, val => val.ToString() + Language.Translate("options.percentage"));
    public static readonly IntegerConfiguration UseKillRequireEnergy = NebulaAPI.Configurations.Configuration("options.role.almighty.killEnergy", (0, 200, 5), 100, null, val => val.ToString() + Language.Translate("options.percentage"));
    public static readonly IntegerConfiguration UseViperKillRequireEnergy = NebulaAPI.Configurations.Configuration("options.role.almighty.viperKillEnergy", (0, 300, 5), 150, null, val => val.ToString() + Language.Translate("options.percentage"));
    private static FloatConfiguration DissolveDurationOption = NebulaAPI.Configurations.Configuration("options.role.almighty.dissolveDuration", (5f, 120f, 5f), 15f, FloatConfigurationDecorator.Second, null, new TranslateTextComponent("options.role.viper.dissolveDuration"));
    private static BoolConfiguration AcidLeavesBoneOption = NebulaAPI.Configurations.Configuration("options.role.almighty.acidLeavesBone", true, null,new TranslateTextComponent("options.role.viper.acidLeavesBone"));
    private static BoolConfiguration CanReportBoneOption = NebulaAPI.Configurations.Configuration("options.role.almighty.canReportBone", true, null, new TranslateTextComponent("options.role.viper.canReportBone"));
    public static readonly FloatConfiguration ShapeShiftCooldown = NebulaAPI.Configurations.Configuration("options.role.almighty.shapeshiftCooldown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    public static readonly IntegerConfiguration UseShapeshiftEnergy = NebulaAPI.Configurations.Configuration("options.role.almighty.shapeshiftEnergy", (0, 200, 5), 50, null, val => val.ToString() + Language.Translate("options.percentage"));
    public static readonly BoolConfiguration ShapeshiftLeaveEvidence = NebulaAPI.Configurations.Configuration("options.role.almighty.shapeshiftLeaveEvidence",true);
    public static readonly FloatConfiguration VanishCooldown = NebulaAPI.Configurations.Configuration("options.role.almighty.vanishCooldown", (0f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    public static readonly IntegerConfiguration UseVanishEnergy = NebulaAPI.Configurations.Configuration("options.role.almighty.vanishEnergy", (0, 200, 5), 40, null, val => val.ToString() + Language.Translate("options.percentage"));
    public static readonly IntegerConfiguration InEffectSubEnergyPercentageEverySecond = NebulaAPI.Configurations.Configuration("options.role.almighty.ineffectSubEnergyPercentageEverySecond", (0, 100, 1), 10, null, val => val.ToString() + Language.Translate("options.percentage"));
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Almighty.png")!.AsImage();
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.AsUniqueKillAbility;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public Almighty MyRole = new Almighty();
    bool IAssignableDocument.HasTips => true;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        public float currentEnergy;
        void SubEnergy(float energy)
        {
            currentEnergy -= energy;
        }
        TextMeshPro? ventEnergyText;
        public Ability(GamePlayer player,bool isUsurped) : base(player,isUsurped)
        {
            currentEnergy = 0f;
            if (AmOwner)
            {
                var myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, p=>ObjectTrackers.LocalKillablePredicate(p)&&(vanishButton==null||!vanishButton.IsInEffect), Palette.ImpostorRed, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption, false);
                {
                    mainkillButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, true, false, VirtualKeyInput.Kill, null, MyPlayer.TeamKillCooldown, "kill", null, (ModAbilityButton _) => myTracker.CurrentTarget != null && currentEnergy >= UseKillRequireEnergy, (ModAbilityButton _) => !MyPlayer.IsDead, false).SetLabelType(ModAbilityButton.LabelType.Impostor);
                    mainkillButton.OnClick = (button) =>
                    {
                        var player = myTracker.CurrentTarget;
                        var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEvent(MyPlayer, player));
                        if (!(cancelable?.IsCanceled ?? false))
                        {
                            MyPlayer.MurderPlayer(player!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                        }
                        if (cancelable?.ResetCooldown ?? false)
                        {
                            NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                            SubEnergy(UseKillRequireEnergy);
                        }
                    };
                    mainkillButton.OnBroken = (button) =>
                    {
                        Snatcher.RewindKillCooldown();
                    };
                    mainkillButton.BindSubKey(VirtualKeyInput.AidAction);
                    mainkillButton.SetAsUsurpableButton(this);
                    mainkillButton.ShowUsesIcon(0, UseKillRequireEnergy.GetValue().ToString() + Language.Translate("options.percentage"));
                }
                {
                    ShapeshiftButton = NebulaAPI.Modules.EffectButton(this, base.MyPlayer, VirtualKeyInput.Ability, null, ShapeShiftCooldown, 10000f, "assistant.shapeshift", new WrapSpriteLoader(() => AmongUsUtil.GetRolePrefab<ShapeshifterRole>()!.Ability.Image), (ModAbilityButton _) => MyPlayer.CanMove && currentEnergy >= UseShapeshiftEnergy, (ModAbilityButton _) => !MyPlayer.IsDead, false).SetLabelType(ModAbilityButton.LabelType.Impostor);
                    ShapeshiftButton.OnClick = (button) =>
                    {
                        if (!button.IsInEffect)
                        {
                            var shapeshift = AmongUsUtil.GetRolePrefab<ShapeshifterRole>();
                            MyPlayer.VanillaPlayer.NetTransform.Halt();
                            ShapeshifterMinigame shapeshifterMinigame = UnityEngine.Object.Instantiate<ShapeshifterMinigame>(shapeshift.ShapeshifterMenu);
                            shapeshifterMinigame.transform.SetParent(Camera.main.transform, false);
                            shapeshifterMinigame.transform.localPosition = new UnityEngine.Vector3(0f, 0f, -50f);
                            shapeshifterMinigame.Begin(null);
                        }
                        else
                        {
                            button.InterruptEffect();
                        }
                    };
                    ShapeshiftButton.OnEffectStart = (button) =>
                    {
                        button.SetLabel("almighty.unshapeshift");
                    };
                    ShapeshiftButton.OnEffectEnd = (button) =>
                    {
                        if (MyPlayer.VanillaPlayer.CurrentOutfitType == PlayerOutfitType.Shapeshifted)
                        {
                            MyPlayer.VanillaPlayer.NetTransform.Halt();
                            RevertPlayerToNormal(!MyPlayer.Logic.InVent);
                        }
                        button.StartCoolDown();
                        button.SetLabel("assistant.shapeshift");
                    };
                    (ShapeshiftButton.EffectTimer as TimerImpl).SetPredicate(() =>
                    {
                        if (MyPlayer.VanillaPlayer.CurrentOutfitType == PlayerOutfitType.Shapeshifted && ShapeshiftButton.EffectTimer.IsProgressing)
                        {
                            return true;
                        }
                        return false;
                    });
                    (ShapeshiftButton.CoolDownTimer as TimerImpl).SetPredicate(() =>
                    {
                        if (MyPlayer.VanillaPlayer.CurrentOutfitType == PlayerOutfitType.Default && ShapeshiftButton.CoolDownTimer.IsProgressing && AmongUsLLImpl.LocalPlayer.IsKillTimerEnabled)
                        {
                            return true;
                        }
                        return false;
                    });
                    ShapeshiftButton.ShowUsesIcon(0, UseShapeshiftEnergy.GetValue().ToString() + Language.Translate("options.percentage"));
                    ShapeshiftButton.OnUpdate = (button) =>
                    {
                        if (currentEnergy < UseShapeshiftEnergy && ShapeshiftButton.IsInEffect)
                        {
                            ShapeshiftButton.InterruptEffect();
                        }
                    };
                    GameOperatorManager.Instance!.Subscribe<GameHudUpdateEvent>(ev=>
                    {
                        try
                        {
                            ModAbilityButtonImpl impl = ShapeshiftButton as ModAbilityButtonImpl;
                            if (ShapeshiftButton.IsInEffect && impl != null)
                            {
                                impl.VanillaButton.SetCooldownFill(0f);
                                impl.VanillaButton.cooldownTimerText.text = "";
                            }
                        }
                        catch (Exception e)
                        {
                            PDebug.Log("Almighty ShapeshiftButtonUpdateError:" + e.ToString());
                        }
                    }, ShapeshiftButton, -1);
                }
                {
                    viperkillButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, false, false, VirtualKeyInput.None, null, MyPlayer.TeamKillCooldown, "acid", new WrapSpriteLoader(() => AmongUsUtil.GetRolePrefab<ViperRole>()!.killSprite), (ModAbilityButton _) => myTracker.CurrentTarget != null && currentEnergy >= UseViperKillRequireEnergy, (ModAbilityButton _) => !MyPlayer.IsDead, false).SetLabelType(ModAbilityButton.LabelType.Impostor);
                    viperkillButton.OnClick = (button) =>
                    {
                        var player = myTracker.CurrentTarget;
                        var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEvent(MyPlayer, player));
                        if (!(cancelable?.IsCanceled ?? false))
                        {
                            MyPlayer.MurderPlayer(player!, PlayerState.Dissolved, null, KillParameter.WithBlink | KillParameter.WithOverlay | KillParameter.WithAssigningGhostRole | KillParameter.WithDeadBody | KillParameter.WithViperDeadBody, null);
                        }
                        if (cancelable?.ResetCooldown ?? false)
                        {
                            button.StartCoolDown();
                            SubEnergy(UseViperKillRequireEnergy);
                        }
                    };
                    viperkillButton.OnBroken = (button) =>
                    {
                        Snatcher.RewindKillCooldown();
                    };
                    viperkillButton.SetAsUsurpableButton(this);
                    viperkillButton.ShowUsesIcon(0, UseViperKillRequireEnergy.GetValue().ToString() + Language.Translate("options.percentage"));
                }
                {
                    vanishButton = NebulaAPI.Modules.EffectButton(this, base.MyPlayer, VirtualKeyInput.SecondaryAbility, null, VanishCooldown, 10000f, "almighty.vanish", new WrapSpriteLoader(() => AmongUsUtil.GetRolePrefab<PhantomRole>()!.Ability.Image), (ModAbilityButton _) => MyPlayer.CanMove && currentEnergy >= UseVanishEnergy, (ModAbilityButton _) => !MyPlayer.IsDead, false, true).SetLabelType(ModAbilityButton.LabelType.Impostor);
                    vanishButton.OnEffectStart = (button) =>
                    {
                        var phantom = AmongUsUtil.GetRolePrefab<PhantomRole>();
                        button.SetImage(new WrapSpriteLoader(()=>phantom!.Ability.SecondImage));
                        button.SetLabel("almighty.appear");
                        MyPlayer.GainAttribute(PlayerAttributes.Invisible,float.MaxValue, false, 0, "almighty:vanish");
                        VanillaRpc.PhantomAnimVanishRpc.Invoke((MyPlayer, true));
                    };
                    vanishButton.OnEffectEnd = (button) =>
                    {
                        var phantom = AmongUsUtil.GetRolePrefab<PhantomRole>();
                        button.SetImage(new WrapSpriteLoader(() => phantom!.Ability.Image));
                        button.SetLabel("almighty.vanish");
                        VanillaRpc.PhantomAnimVanishRpc.Invoke((MyPlayer, false));
                        MyPlayer.RemoveAttributeByTag("almighty:vanish");
                        button.StartCoolDown();
                    };
                    vanishButton.ShowUsesIcon(0, UseVanishEnergy.GetValue().ToString() + Language.Translate("options.percentage"));
                    vanishButton.OnUpdate = (button) =>
                    {
                        if (currentEnergy < UseVanishEnergy && vanishButton.IsInEffect)
                        {
                            vanishButton.InterruptEffect();
                        }
                    };
                    GameOperatorManager.Instance!.Subscribe<GameHudUpdateEvent>(ev=>
                    {
                        try
                        {
                            ModAbilityButtonImpl impl = vanishButton as ModAbilityButtonImpl;
                            if (vanishButton.IsInEffect && impl != null)
                            {
                                impl.VanillaButton.SetCooldownFill(0f);
                                impl.VanillaButton.cooldownTimerText.text = "";
                            }
                        }
                        catch (Exception e)
                        {
                            PDebug.Log("Almighty VanishButtonHudUpdateError" + e.ToString());
                        }
                    }, vanishButton, -1);
                }
                mainkillButton.OnSubAction = (button) =>
                {
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        if (viperkillButton != null)
                        {
                            mainkillButton.ResetKeyBinding();
                            viperkillButton.BindKey(VirtualKeyInput.Kill);
                            viperkillButton.BindSubKey(VirtualKeyInput.AidAction);
                        }
                    });
                };
                viperkillButton.OnSubAction = (button) =>
                {
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        if (mainkillButton != null)
                        {
                            viperkillButton.ResetKeyBinding();
                            mainkillButton.BindKey(VirtualKeyInput.Kill);
                            mainkillButton.BindSubKey(VirtualKeyInput.AidAction);
                        }
                    });
                };
                this.BindGameObject(DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ShowUsesIcon(0, out this.ventEnergyText).gameObject);
                ventEnergyText.text = UseVentRequireEnergy.GetValue().ToString()+Language.Translate("options.percentage");
                try
                {
                    string prefix = Language.Translate("role.almighty.hud.energyProgress") + ": ", postfix = Language.Translate("options.percentage");
                    Helpers.TextHudContent("almightyEnergyProgress", this, tmPro =>
                    {
                        tmPro.text = prefix + Math.Floor(currentEnergy).ToString() + postfix;
                    });
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(mainkillButton.GetKillButtonLike());
            }
            GameOperatorManager.Instance!.Subscribe<MeetingPreStartEvent>(delegate (MeetingPreStartEvent ev)
            {
                MyPlayer.Unbox().RemoveOutfit("Shapeshift");
                MyPlayer.VanillaPlayer.CurrentOutfitType = PlayerOutfitType.Default;
                if (MyPlayer.AmOwner)
                {
                    ShapeshiftButton?.InterruptEffect();
                    vanishButton?.InterruptEffect();
                }
            }, this);
        }
        public void RevertPlayerToNormal(bool shouldAnimate = true)
        {
            if (MyPlayer.VanillaPlayer.CurrentOutfitType == PlayerOutfitType.Shapeshifted || MeetingHud.Instance.AsBoolFast())
            {
                VanillaRpc.ShapeshiftRpc.Invoke((MyPlayer, MyPlayer, shouldAnimate,ShapeshiftLeaveEvidence));
            }
        }
        void OnDeadBodyGenerated(DeadBodyInstantiateEvent ev)
        {
            ModViperDeadBody deadBody;
            if (ev.Player != null && ev.Killer == MyPlayer && ev.DeadState == PlayerState.Dissolved && ev.DeadBody.VanillaDeadBody.gameObject.TryGetComponent<ModViperDeadBody>(out deadBody))
            {
                deadBody.SetUp(DissolveDurationOption, AcidLeavesBoneOption, !AcidLeavesBoneOption || CanReportBoneOption);
            }
        }

        void OnGameStart(GameStartEvent ev)
        {
            currentEnergy = 0f;
        }
        [Local]
        void OnMeetingEnd(TaskPhaseStartEvent ev)
        {
            currentEnergy = 0f;
        }
        [Local]
        void OnEnterVent(PlayerVentEnterEvent ev)
        {
            SubEnergy(UseVentRequireEnergy);
        }
        [Local]
        void OnUpdate(GameUpdateEvent ev)
        {
            if (MyPlayer.Logic.InVent)
            {
                return;
            }
            int ineffectbutton = 0;
            if (ShapeshiftButton?.IsInEffect ?? false)
            {
                ineffectbutton++;
            }
            if (vanishButton?.IsInEffect ?? false)
            {
                ineffectbutton++;
            }
            if (ineffectbutton <= 0)
            {
                currentEnergy += 100f / (float)(int)RecoverEnergyTo100Time * Time.deltaTime;
            }
            else
            {
                currentEnergy -= (float)(int)InEffectSubEnergyPercentageEverySecond*Time.deltaTime*ineffectbutton;
            }
            currentEnergy = Mathn.Clamp(currentEnergy, 0f, MaxEnergy);
        }
        void IGameOperator.OnReleased()
        {
            if (AmOwner)
            {
                RevertPlayerToNormal(!MyPlayer.Logic.InVent);
                if (vanishButton?.IsInEffect??false)
                {
                    VanillaRpc.PhantomAnimVanishRpc.Invoke((MyPlayer, !MyPlayer.Logic.InVent));
                }
            }
        }
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        ModAbilityButton? mainkillButton,viperkillButton,ShapeshiftButton,vanishButton;
        bool IPlayerAbility.HideKillButton=>(mainkillButton!=null&&!mainkillButton.IsBroken)||(viperkillButton!=null&&!viperkillButton.IsBroken);
    }
}
