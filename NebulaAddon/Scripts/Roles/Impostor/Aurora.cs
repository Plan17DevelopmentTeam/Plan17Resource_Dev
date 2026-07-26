using Il2CppInterop.Runtime.Injection;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Neutral;
using UnityEngine;
using UnityEngine.Rendering;
using static Sentry.MeasurementUnit;
using static UnityEngine.UI.GridLayoutGroup;

namespace Plana.Roles.Impostor;

public class Aurora : DefinedSingleAbilityRoleTemplate<Aurora.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    private Aurora() : base("aurora", NebulaTeams.ImpostorTeam.Color,RoleCategory.ImpostorRole,NebulaTeams.ImpostorTeam, [SkillCoolDown,SkillBuffDuration,DeadBodyEffectDuration])
    {
    ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImages/Aurora.png")!.AsImage(115f)!;
    }
    static private FloatConfiguration SkillCoolDown = NebulaAPI.Configurations.Configuration("options.role.aurora.skillcooldown", (0f, 120f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration SkillBuffDuration = NebulaAPI.Configurations.Configuration("options.role.aurora.skillduration", (0f, 120f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration DeadBodyEffectDuration = NebulaAPI.Configurations.Configuration("options.role.aurora.deadbodyEffectduration", (0f, 120f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKCANDExe; } }

    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Aurora.png")!.AsImage();
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.Allowed;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public Aurora MyRole = new Aurora();
    bool IAssignableDocument.HasTips => true;
    static public IPlayerAttribute AuroralAttribute = new PlayerAttributeImpl(0, "$auroral", "auroral") { Image = NebulaAPI.AddonAsset.GetResource("AuroralBuffIcon.png")!.AsImage()! };
    public static Image buttonIcon = NebulaAPI.AddonAsset.GetResource("AuroraSkill.png")!.AsImage(115f)!;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility

    {
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var playerTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, p => ObjectTrackers.PlayerlikeStandardPredicate(p) && !MyPlayer.CanKill(p.RealPlayer), Palette.ImpostorRed);

                var skillButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    SkillCoolDown, "aurora.passauroral", buttonIcon,
                    _ => playerTracker.CurrentTarget != null && MyPlayer.CanMove, _ => !MyPlayer.IsDead).SetAsUsurpableButton(this);
                skillButton.OnClick = button =>
                {
                    CreateAbilityRPC.Invoke((MyPlayer, playerTracker.CurrentTarget!.RealPlayer));
                    button.StartCoolDown();
                };
                skillButton.StartCoolDown();
                skillButton.SetLabelType(ModAbilityButton.LabelType.Crewmate);
            }
            var ability = new KillPlayerCannotReportAbility(MyPlayer, this, float.MaxValue, true).RegisterSelf();
        }
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
    }
    static readonly RemoteProcess<(GamePlayer rolePlayer,GamePlayer targetPlayer)> CreateAbilityRPC = new("AuroraCreateAbilityRPC", (m, _) =>
    {
        new KillPlayerCannotReportAbility(m.targetPlayer, m.rolePlayer.Role, SkillBuffDuration).RegisterSelf();
    });
    public static MultiImage AuroralAnimation = NebulaAPI.AddonAsset.GetResource("AuroralBuffAnimation.png")!.AsMultiImage(4, 2)!;
    public class KillPlayerCannotReportAbility : EquipableAbility,IGameOperator
    {
        public GamePlayer MyPlayer { get; private set; }
        int animIndex = 0;
        float leftTime;
        bool startUpdate;
        protected override float Size => 0.68f;
        protected override float Distance => 0.7f;
        public KillPlayerCannotReportAbility(GamePlayer myPlayer, ILifespan lifespan, float duration, bool withoutEffect = false) : base(myPlayer, false, "auroralAbility")
        {
            MyPlayer = myPlayer;
            MyPlayer.GainAttribute(AuroralAttribute, duration, false, 100, "aurora:auroralbuff");
            NebulaManager.Instance.ScheduleDelayAction(() => startUpdate = true);
            Bind(lifespan);
            leftTime = duration;
            Renderer.gameObject.SetActive(false);
            Renderer.transform.localPosition = new(-0.1f, 0.4f);
            if (!withoutEffect)
            {
                NebulaManager.Instance.StartCoroutine(CoRunAnimation());
            }
        }
        protected override void HudUpdate(GameHudUpdateEvent ev)
        {
        }
        System.Collections.IEnumerator CoRunAnimation()
        {
            while (true)
            {
                if (IsDeadObject)
                {
                    break;
                }
                if (!GamePlayer.LocalPlayer!.CanKill(MyPlayer) && !MyPlayer.CanKill(GamePlayer.LocalPlayer) || NebulaGameManager.Instance!.CanSeeAllInfo)
                {
                    Renderer.gameObject.SetActive(true);
                    Renderer.sprite = AuroralAnimation.GetSprite(animIndex);
                    animIndex++;
                    if (animIndex >= 8)
                    {
                        animIndex = 0;
                    }
                    yield return ManagedEffects.Wait(0.4f);
                }
                else
                {
                    break;
                }
            }
            yield break;
        }
        void IGameOperator.OnReleased()
        {
            MyPlayer.RemoveAttribute(AuroralAttribute);
            if (Renderer)
            {
                UnityEngine.Object.Destroy(Renderer);
            }
        }
        void OnUpdate(GameUpdateEvent ev)
        {
            if (!startUpdate) return;
            if (Renderer&&!Renderer.enabled)
            {
                return;
            }
            if (!MyPlayer.HasAttribute(AuroralAttribute))
            {
                this.Release();
            }
            else if (leftTime>=0f)
            {
                leftTime -= ev.DeltaTime;
                if (leftTime<=10f)
                {
                    float t = 10f - leftTime;
                    if (leftTime<=0f)
                    {
                        Release();
                    }
                    else if (t<= 10f)
                    {
                        Renderer.color= UnityEngine.Color.Lerp(UnityEngine.Color.white, new(1f,1f,1f,0f), t / 10f);
                    }

                }
            }
        }
        void OnInstantiateDeadBody(DeadBodyInstantiateEvent ev)
        {
            if (ev.Killer?.PlayerId==MyPlayer?.PlayerId)
            {
                ev.DeadBody.VanillaDeadBody.gameObject.AddComponent<CannotReportComponent>();
            }
        }
    }
    public class CannotReportComponent : MonoBehaviour
    {
        static CannotReportComponent()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CannotReportComponent>();
        }
        public float disableTime;
        bool originReported;
        SpriteRenderer renderer;
        int animIndex;
        void Awake()
        {
            disableTime = DeadBodyEffectDuration;
            animIndex = 0;
            var deadbody = gameObject.GetComponent<DeadBody>();
            if (deadbody != null)
            {
                originReported = deadbody.Reported;
            }
            renderer = UnityHelper.CreateObject<SpriteRenderer>("cannotReportComponentAnim", transform, new(-0.5f,0.5f,-1f));
            renderer.transform.localScale = new(0.65f, 0.65f);
            this.StartCoroutine(CoRunAnimation().WrapToIl2Cpp());
        }
        System.Collections.IEnumerator CoRunAnimation()
        {
            while (true)
            {
                renderer.sprite = AuroralAnimation.GetSprite(animIndex);
                animIndex++;
                if (animIndex >= 8)
                {
                    animIndex = 0;
                }
                yield return ManagedEffects.Wait(0.4f);
            }
            yield break;
        }
        void Update()
        {
            var deadbody = gameObject.GetComponent<DeadBody>();
            if (deadbody != null)
            {
                deadbody.enabled = false;
                deadbody.Reported = true;
            }
            disableTime -= Time.deltaTime;
            if (disableTime<=10f)
            {
                float t = 10f - disableTime;
                if (disableTime <= 0f)
                {
                    UnityEngine.Object.Destroy(this);
                }
                else if (t <= 10f)
                {
                    renderer.color = UnityEngine.Color.Lerp(UnityEngine.Color.white, new(1f,1f,1f,0f), t / 10f);
                }
            }
        }
        void OnDestroy()
        {
            this.StopAllCoroutines();
            var deadbody = gameObject.GetComponent<DeadBody>();
            if (deadbody != null)
            {
                deadbody.enabled = true;
                deadbody.Reported = originReported;
            }
            UnityEngine.Object.Destroy(renderer);
        }
    }
    [HarmonyPatch]
    public class CannotReportDeadBodyPatchs
    {

    }
}
