global using Plana.Core;
namespace Plana.Roles.Crewmate;

[NebulaRPCHolder]
public class Engineer : DefinedRoleTemplate, DefinedRole, HasCitation, IAssignableDocument
{
    private Engineer() : base("engineer", new(53,73,255), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [CooldownOption,CanUseRepair,RepairNum])
    {
    }
    Citation? HasCitation.Citation { get { return CanUseRepair?Citations.TheOtherRoles:Citations.AmongUs; } }
    internal static IVentConfiguration CooldownOption = NebulaAPI.Configurations.VentConfiguration("options.role.engineer.cooldown", false, null, -1, (0f, 60f, 2.5f), 10f, (0f,60f,2.5f), 0f);
    static BoolConfiguration CanUseRepair = NebulaAPI.Configurations.Configuration("options.role.engineer.canuserepair", false);
    static IntegerConfiguration RepairNum = NebulaAPI.Configurations.Configuration("options.role.engineer.repairnum", (1, 16), 2,()=>CanUseRepair);
    static public Engineer MyRole = new Engineer();
    bool IAssignableDocument.HasTips => true;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player,arguments.Get(0,RepairNum));
    [NebulaRPCHolder]
    public class Instance : RuntimeVentRoleTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        public override DefinedRole Role
        {
            get
            {
                return Engineer.MyRole;
            }
        }
        int[] RuntimeAssignable.RoleArguments => [RepairNum];
        Virial.Media.Image? repairIcon = NebulaAPI.AddonAsset.GetResource("engineerRepair.png")!.AsImage(115f);
        public Instance(GamePlayer player,int leftrepair) : base(player,CooldownOption)
        {
            leftRepair = leftrepair;
        }
        bool RuntimeRole.CanMoveInVent => true;
        bool RuntimeRole.CanUseVent => true;
        Sprite? ImpventButton;
        public override void OnActivated()
        {
            if (AmOwner)
            {
                ImpventButton = AmongUsLLImpl.HudManagerBridge.ImpostorVentButton.graphic.sprite;
                var engineer = AmongUsUtil.GetRolePrefab<EngineerRole>();
                AmongUsLLImpl.HudManagerBridge.ImpostorVentButton.graphic.sprite=engineer?.Ability.Image;
                AmongUsLLImpl.HudManagerBridge.ImpostorVentButton.buttonLabelText.fontSharedMaterial = engineer?.Ability.FontMaterial;
                if (CanUseRepair)
                {
                    var repairButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.Ability, 0f, "repair", repairIcon, button => AmongUsUtil.InAnySab && MyPlayer.CanMove, button => !MyPlayer.IsDead && leftRepair > 0).SetLabelType(ModAbilityButton.LabelType.Crewmate);
                    repairButton.ShowUsesIcon(3, leftRepair.ToString());
                    repairButton.OnClick = (button) =>
                    {
                        leftRepair--;
                        if (leftRepair > 0)
                        {
                            repairButton.UpdateUsesIcon(leftRepair.ToString());
                        }
                        else
                        {
                            repairButton.HideUsesIcon();
                        }
                        EngineerFix();
                        UpdateLeftRepairNum(MyPlayer, leftRepair);
                    };
                }
            }
        }
        void RuntimeAssignable.OnInactivated()
        {
            if (AmOwner)
            {
                AmongUsLLImpl.HudManagerBridge.ImpostorVentButton.graphic.sprite = ImpventButton;
                AmongUsLLImpl.HudManagerBridge.ImpostorVentButton.buttonLabelText.fontSharedMaterial= AmongUsUtil.GetRolePrefab<ShapeshifterRole>()?.Ability.FontMaterial;
            }
        }
        public void EngineerFix()
        {
            switch (AmongUsLLImpl.Instance.MapId)
            {
                case 0:
                case 3:
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>().IsActive)
                    {
                        FixComms();
                    }
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>().IsActive)
                    {
                        FixReactor(SystemTypes.Reactor);
                    }
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>().IsActive)
                    {
                        FixOxygen();
                    }
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                    {
                        RpcFix(MyPlayer);
                        return;
                    }
                    break;
                case 1:
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Comms].Cast<HqHudSystemType>().IsActive)
                    {
                        FixMiraComms();
                    }
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>().IsActive)
                    {
                        FixReactor(SystemTypes.Reactor);
                    }
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>().IsActive)
                    {
                        FixOxygen();
                    }
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                    {
                        RpcFix(MyPlayer);
                        return;
                    }
                    break;
                case 2:
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>().IsActive)
                    {
                        FixComms();
                    }
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Laboratory].Cast<ReactorSystemType>().IsActive)
                    {
                        FixReactor(SystemTypes.Laboratory);
                    }
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>().IsActive)
                    {
                        FixOxygen();
                    }
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                    {
                        RpcFix(MyPlayer);
                        return;
                    }
                    break;
                case 4:
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>().IsActive)
                    {
                        FixComms();
                    }
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.HeliSabotage].Cast<HeliSabotageSystem>().IsActive)
                    {
                        FixAirshipReactor();
                    }
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                    {
                        RpcFix(MyPlayer);
                        return;
                    }
                    break;
                case 5:
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>().IsActive)
                    {
                        FixReactor(SystemTypes.Reactor);
                    }
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Comms].Cast<HqHudSystemType>().IsActive)
                    {
                        FixMiraComms();
                    }
                    if (AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                    {
                        RpcFix(MyPlayer);
                        return;
                    }
                    break;
                default:
                    return;
            }
        }

        private static void FixComms()
        {
            AmongUsLLImpl.ShipStatusInstance.RpcUpdateSystem(SystemTypes.Comms, 0);
        }

        private static void FixMiraComms()
        {
            AmongUsLLImpl.ShipStatusInstance.RpcUpdateSystem(SystemTypes.Comms, 16);
            AmongUsLLImpl.ShipStatusInstance.RpcUpdateSystem(SystemTypes.Comms, 17);
        }

        private static void FixAirshipReactor()
        {
            AmongUsLLImpl.ShipStatusInstance.RpcUpdateSystem(SystemTypes.HeliSabotage, 16);
            AmongUsLLImpl.ShipStatusInstance.RpcUpdateSystem(SystemTypes.HeliSabotage, 17);
        }

        private static void FixReactor(SystemTypes system)
        {
            AmongUsLLImpl.ShipStatusInstance.RpcUpdateSystem(system, 16);
        }

        private static void FixOxygen()
        {
            AmongUsLLImpl.ShipStatusInstance.RpcUpdateSystem(SystemTypes.LifeSupp, 16);
        }
        [NebulaRPC]
        public static void RpcFix(GamePlayer engineer)
        {
            if (engineer.Role is Engineer.Instance)
            {
                SwitchSystem switchSystem = AmongUsLLImpl.ShipStatusInstance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                switchSystem.ActualSwitches = switchSystem.ExpectedSwitches;
            }
        }
        [NebulaRPC]
        public static void UpdateLeftRepairNum(GamePlayer engineer,int leftnum)
        {
            if (!engineer.AmOwner&&engineer.Role is Engineer.Instance role)
            {
                role.leftRepair = leftnum;
            }
        }
        public int leftRepair;
        public static void EngineerVentTextPatch()
        {
            if (!AmongUsLLImpl.LocalPlayer)
            {
                return;
            }
            var localModInfo = GamePlayer.LocalPlayer?.Unbox();
            if (localModInfo != null)
            {
                if (!(localModInfo.Role is Engineer.Instance))
                {
                    return;
                }
                var ventTimer = AmongUsLLImpl.LocalPlayer.inVent ? localModInfo.Role?.VentDuration : localModInfo.Role?.VentCoolDown;
                string ventText = "";
                float ventPercentage = 0f;
                if (ventTimer != null && ventTimer.IsProgressing)
                {
                    ventText = Mathn.CeilToInt(ventTimer.CurrentTime).ToString();
                    ventPercentage = ventTimer.Percentage;
                }
                if ((localModInfo.Role?.VentDuration?.Max ??-2.5f)< 0f&&AmongUsLLImpl.LocalPlayer.inVent)
                {
                    ventText = "∞";
                }    
                if (ventTimer != null && !ventTimer.IsProgressing && AmongUsLLImpl.LocalPlayer.inVent)
                {
                    Vent.currentVent.SetButtons(false);
                    var exitVent = Vent.currentVent.GetValidVent();
                    if (exitVent != null) AmongUsLLImpl.LocalPlayer.MyPhysics.RpcExitVent(exitVent.Id);
                }

                var ventButton = AmongUsLLImpl.HudManagerBridge.ImpostorVentButton;
                ventButton.SetCooldownFill(ventPercentage);
                CooldownHelpers.SetCooldownNormalizedUvs(ventButton.graphic);
                ventButton.cooldownTimerText.text = ventText;
                ventButton.cooldownTimerText.color = AmongUsLLImpl.LocalPlayer.inVent ? Engineer.MyRole.UnityColor : UnityEngine.Color.white;
            }
        }
    }
}
