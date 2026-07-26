using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Impostor;

namespace Plana.Roles.Modifier;

public class LastImpostor : DefinedModifierTemplate, DefinedAllocatableModifier, HasAssignmentRoutine, RoleFilter, AssignableFilter<DefinedRole>, HasRoleFilter, ICodeName, ISpawnable, IAssignToCategorizedRole, DefinedAssignable, IRoleID, HasCitation, IAssignableDocument
{
    private LastImpostor() : base("lastImpostor", NebulaTeams.ImpostorTeam.Color, [PatchManager.CanGenerateLastImpostor,PatchManager.GenerateLastImpostorIfPlayer,PatchManager.CannotGenerateHasInfected,LastImpostorClockRatioOption],true)
    {
        ConfigurationHolder?.SetDisplayState(() =>
        {
            if (PatchManager.CanGenerateLastImpostor)
            {
                return ConfigurationHolderState.Emphasized;
            }
            return ConfigurationHolderState.Inactivated;
        });
    }
    bool AssignableFilter<DefinedRole>.Test(DefinedRole role)
    {
        return false;
    }

    void AssignableFilter<DefinedRole>.ToggleAndShare(DefinedRole role)
    {
    }

    void AssignableFilter<DefinedRole>.SetAndShare(DefinedRole role, bool val)
    {
    }

    RoleFilter HasRoleFilter.RoleFilter => null;

    bool ISpawnable.IsSpawnable => ConfigurationHolder?.DisplayOption != ConfigurationHolderState.Inactivated;
 
    internal bool CanAssignTo(RoleCategory category)
    {
        return category == RoleCategory.ImpostorRole;
    }

    public bool CanAssignOnThisGameByConfiguration(RoleCategory category)
    {
        return (this as ISpawnable).IsSpawnable&&category == RoleCategory.ImpostorRole;
    }
    void HasAssignmentRoutine.TryAssign(IRoleTable roleTable)
    {
    }
    public void GetAssignProperties(RoleCategory category, out int assign100, out int assignRandom, out int assignChance)
    {
        assign100 = 0;
        assignRandom = 0;
        assignChance = 0;
    }
    string ICodeName.CodeName
    {
        get
        {
            return "LI";
        }
    }
    SpecialAssignment[] DefinedAllocatableModifier.SpecialAssignment => PatchManager.CanGenerateLastImpostor ? [new SpecialAssignment(this,1,100,null,null)] :Array.Empty<SpecialAssignment>();
    int HasAssignmentRoutine.AssignPriority => 0;
    Citation HasCitation.Citation => PCitations.PlanaANDKC;
    public static FloatConfiguration LastImpostorClockRatioOption = NebulaAPI.Configurations.Configuration("options.role.lastimpostor.clockRatio", new ValueTuple<float, float, float>(1f, 5f, 0.125f), 1.5f, FloatConfigurationDecorator.Ratio);
    static public LastImpostor MyRole = new LastImpostor();
    bool IAssignableDocument.HasTips => true;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            GuesserSystem.OnMeetingStart(99, () => { }, false, null, true);
        }
        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            if (NebulaGameManager.Instance!.AllPlayerInfo.Count(p=>!p.IsDead&&p.IsImpostor)>1)
            {
                MyPlayer.RemoveModifier(LastImpostor.MyRole);
                if (leftGuess>0)
                {
                    MyPlayer.AddModifier(GuesserModifier.MyRole, [leftGuess]);
                }
            }
            MyPlayer.GainAttribute(PlayerAttributes.CooldownSpeed, 0.25f,LastImpostorClockRatioOption, false, 100, "LI::CooldownSpeed");
        }
        int leftGuess;
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                leftGuess = 0;
                if (MyPlayer.TryGetModifier<GuesserModifier.Instance>(out var modifier))
                {
                    MyPlayer.RemoveModifier(GuesserModifier.MyRole);
                    leftGuess = modifier.LeftGuess;
                }
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo||inEndScene) name += "LI".Color(MyRole.RoleColor.ToUnityColor());
        }
    }
}
