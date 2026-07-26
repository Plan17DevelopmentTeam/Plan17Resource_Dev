using Plana.Core;

namespace Plana.Roles.Modifier;

public class Nocturnal : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier, IAssignableDocument
{
private Nocturnal(): base("nocturnal", "nct", NebulaTeams.ImpostorTeam.Color, [MaxVisionOption],false,true,false) {
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    public static readonly FloatConfiguration MaxVisionOption = NebulaAPI.Configurations.Configuration("options.role.nocturnal.maxvision", (0.25f, 5f, 0.25f), 3f, FloatConfigurationDecorator.Ratio);
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Nocturnal.png")!.AsImage();
    static public Nocturnal MyRole = new Nocturnal();
    bool IAssignableDocument.HasTips => true;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier,RuntimeAssignable
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        IEnumerable<IPlayerAbility> RuntimeAssignable.MyAbilities => [MyVisionAbility!];
        IPlayerAbility? MyVisionAbility;
        public Instance(GamePlayer player) : base(player)
        {
        }
        [Local]
        void HudUpdate(GameHudUpdateEvent ev)
        {
            try
            {
                if (NebulaGameManager.Instance?.CanSeeAllInfo ?? false)
                {
                    return;
                }
                if (PlayerTask.PlayerHasTaskOfType<ElectricTask>(MyPlayer.VanillaPlayer))
                {
                    if (!NebulaInput.SomeUiIsActive)
                    {
                        float axis = Input.GetAxis("Mouse ScrollWheel");
                        float rate = NebulaGameManager.Instance!.WideCamera.TargetRate;
                        if (axis < 0f)
                        {
                            rate -= 0.1f;
                        }
                        if (axis > 0f)
                        {
                            rate += 0.1f;
                        }
                        var max = MaxVisionOption > 1f ? MaxVisionOption : 1f;
                        var min = MaxVisionOption <= 1f ? MaxVisionOption : 1f;
                        rate = Mathn.Clamp(rate, min, max / 2);
                        NebulaGameManager.Instance.WideCamera.TargetRate = rate;
                    }
                }
                else
                {
                    NebulaGameManager.Instance!.WideCamera.TargetRate = 1f;
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        void RuntimeAssignable.OnActivated() 
        {
            MyVisionAbility = new IgnoreWallVisionAbility(MyPlayer).Register(this);
        }
        string? RuntimeAssignable.OverrideRoleName(string lastRoleName,bool isShort,bool canSeeAllInfo)
        {
            return (this as RuntimeAssignable).DisplayColoredName + " "+lastRoleName;
        }
    }
    public class IgnoreWallVisionAbility : AbstractPlayerAbility,IPlayerAbility
    {
        public IgnoreWallVisionAbility(GamePlayer player):base(player)
        {
        }
        bool IPlayerAbility.EyesightIgnoreWalls => PlayerTask.PlayerHasTaskOfType<ElectricTask>(MyPlayer.VanillaPlayer);
    }
}
