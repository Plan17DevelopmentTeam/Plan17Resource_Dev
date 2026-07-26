using Plana.Core;

namespace Plana.Roles.Modifier;

public class ButtonBarry : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier, IAssignableDocument
{
    private ButtonBarry() : base("buttonbarry", "butbar", new(191, 107, 51), [CallMeetingCooldown, InSabotageCanCallMeeting, DonotCallMeetingRequireMeetingnum])
    {
    }
    Citation? HasCitation.Citation { get { return PCitations.TownOfUs; } }
    public static FloatConfiguration CallMeetingCooldown = NebulaAPI.Configurations.Configuration("options.role.buttonbarry.callmeetingCooldown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    public static BoolConfiguration InSabotageCanCallMeeting = NebulaAPI.Configurations.Configuration("options.role.buttonbarry.inSabotageCanCallMeeting", false);
    public static IntegerConfiguration DonotCallMeetingRequireMeetingnum = NebulaAPI.Configurations.Configuration("options.role.buttonbarry.notcallmeetingnum", (0, 16), 3,null,val=>val==0?Language.Translate("options.noLimit"):val.ToString());
    static public ButtonBarry MyRole = new ButtonBarry();
    bool IAssignableDocument.HasTips => true;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        Virial.Media.Image? CallMeetingImage = NebulaAPI.AddonAsset.GetResource("buttonbarrySkill.png")!.AsImage(115f);
        public Instance(GamePlayer player) : base(player)
        {
        }
        int totalMeeting;
        [Local]
        void OnMeetingStart(MeetingPreStartEvent ev)
        {
            totalMeeting++;
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                bool used = false;
                var button = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, false, true, VirtualKeyInput.None, null, CallMeetingCooldown, "buttonbarry.callmeeting", CallMeetingImage, (ModAbilityButton _) => MyPlayer.CanMove && (InSabotageCanCallMeeting || !AmongUsUtil.InAnySab), (ModAbilityButton _) => !MyPlayer.IsDead && totalMeeting < (DonotCallMeetingRequireMeetingnum == 0 ? 999 : DonotCallMeetingRequireMeetingnum) && !used, false);
                button.OnClick = button =>
                {
                    used = true;
                    MeetingHudExtension.ModCmdReportDeadBody(GamePlayer.LocalPlayer, null, MeetingHudExtension.ReportType.EmergencyMeeting, InSabotageCanCallMeeting, false);
                };
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo ||inEndScene) name += "●".Color(MyRole.RoleColor.ToUnityColor());
        }
    }
}
