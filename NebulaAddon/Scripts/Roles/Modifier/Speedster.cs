using Plana.Core;
using Virial.Game;
using static Virial.Game.OutfitDefinition;

namespace Plana.Roles.Modifier;

public class Speedster : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier, IAssignableDocument
{
    private Speedster(): base("speedster", "spe", new(198, 17, 17), [speedRatio]) {
    }

    Citation? HasCitation.Citation => PCitations.TownOfUs;

    static private FloatConfiguration speedRatio = NebulaAPI.Configurations.Configuration("options.role.speedster.speed", (1.25f, 2f, 0.25f), 1.5f, FloatConfigurationDecorator.Ratio);

    static public Speedster MyRole = new Speedster();
    bool IAssignableDocument.HasTips => true;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated() {
            
        }
        [Local]
        void OnUpdate(GameUpdateEvent ev)
        {
            var outfit = MyPlayer.Unbox().CurrentOutfit;
            if (!outfit.Tag.ToLower().Contains("camo") && !outfit.Tag.ToLower().Contains("commscfeffect"))
            {
                MyPlayer.GainSpeedAttribute(speedRatio, 0.25f, true, 0, "speedster:speedfast");
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo||inEndScene) name += "S".Color(MyRole.RoleColor.ToUnityColor());
        }
    }
}


