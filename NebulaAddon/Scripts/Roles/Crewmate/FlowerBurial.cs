
namespace Plana.Roles.Crewmate;

public class FlowerBurial : DefinedSingleAbilityRoleTemplate<FlowerBurial.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    private FlowerBurial() : base("flowerburial", new(123,177,234), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [flowersPlaceNum, PlaceCooldown, CheckCooldown,CheckDuration, FlowerRatio,NeutralCannotSeeRole,ImpostorCannotSeeRole])
    {
        MetaAbility.RegisterCircle(new EffectCircleInfo("role.flowerburial.flowerRatio", () => FlowerRatio, () => null, base.RoleColor));
    }
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    public static readonly IntegerConfiguration flowersPlaceNum = NebulaAPI.Configurations.Configuration("options.role.flowerburial.flowersPlaceNum", (1, 20), 3);
    public static readonly FloatConfiguration PlaceCooldown = NebulaAPI.Configurations.Configuration("options.role.flowerburial.placeCooldown", (0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    public static readonly FloatConfiguration CheckCooldown = NebulaAPI.Configurations.Configuration("options.role.flowerburial.checkCooldown", (0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    public static readonly FloatConfiguration CheckDuration = NebulaAPI.Configurations.Configuration("options.role.flowerburial.checkDuration", (0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    public static readonly FloatConfiguration FlowerRatio = NebulaAPI.Configurations.Configuration("options.role.flowerburial.flowerRatio", (0.25f, 20f, 0.25f), 4f, FloatConfigurationDecorator.Ratio);
    public static readonly BoolConfiguration NeutralCannotSeeRole = NebulaAPI.Configurations.Configuration("options.role.flowerburial.neutralCannotSeeRole", false);
    public static readonly BoolConfiguration ImpostorCannotSeeRole = NebulaAPI.Configurations.Configuration("options.role.flowerburial.impostorCannotSeeRole", false);
    static public FlowerBurial MyRole = new FlowerBurial();
    bool IAssignableDocument.HasTips => true;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static Image PlaceIcon = NebulaAPI.AddonAsset.GetResource("flowerBurialPlaceFlower.png")!.AsImage(115f)!;
        static Image CheckIcon = NebulaAPI.AddonAsset.GetResource("flowerBurialCheckFlowers.png")!.AsImage(115f)!;
        static Image MeetingInfoIcon = NebulaAPI.AddonAsset.GetResource("flowerBurialMeetingInfo.png")!.AsImage(135f)!;
        Dictionary<DeadBody,string> CheckedDeadbodies = new();
        List<Flower> flowers = new();
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                flowers = new();
                ModAbilityButton placeButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.Ability, PlaceCooldown, "place", PlaceIcon, _ => MyPlayer.CanMove, _ => !MyPlayer.IsDead && flowersPlaceNum - flowers.Count > 0);
                placeButton.SetAsUsurpableButton(this).SetLabelType(ModAbilityButton.LabelType.Crewmate).ShowUsesIcon(3, flowersPlaceNum.GetValue().ToString());
                placeButton.OnClick = _ =>
                {
                    flowers.Add(Flower.PlaceFlower(MyPlayer.TruePosition));
                    placeButton.UpdateUsesIcon((flowersPlaceNum - flowers.Count).ToString());
                    placeButton.StartCoolDown();
                    EffectCircle.SpawnEffectCircle(AmongUsLLImpl.ShipStatusInstance.transform, MyPlayer.TruePosition.ToUnityVector(), MyRole.RoleColor, new float?(FlowerRatio), null, true);
                };
                /*(placeButton as ModAbilityButtonImpl)!.OnMeeting = _ =>
                {
                    flowers.Do(f => f.ReflectInstantiationGlobally());
                };*/
                /*ModAbilityButton checkButton = NebulaAPI.Modules.EffectButton(this, MyPlayer, VirtualKeyInput.SecondaryAbility, null, CheckCooldown, CheckDuration, "flowerburial.check", CheckIcon, _ => MyPlayer.CanMove && flowers.Count > 0, _ => !MyPlayer.IsDead);
                checkButton.SetAsUsurpableButton(this).SetLabelType(ModAbilityButton.LabelType.Crewmate);
                checkButton.OnEffectStart = _ =>
                {
                    flowers.Do(obj =>
                    {
                        using (RPCRouter.CreateSection("CheckFlower"))
                        {
                            Flower.RpcCheck.Invoke((obj.ObjectId, true));
                        }
                    });
                };
                checkButton.OnEffectEnd = _ =>
                {
                    flowers.Do(obj =>
                    {
                        using (RPCRouter.CreateSection("CheckFlower"))
                        {
                            Flower.RpcCheck.Invoke((obj.ObjectId, false));
                        }
                    });
                    checkButton.StartCoolDown();
                };
                checkButton.OnUpdate = _ =>
                {
                    if (!checkButton.IsInEffect)
                    {
                        return;
                    }
                    foreach (var d in Helpers.AllDeadBodies())
                    {
                        if (!CheckedDeadbodies.ContainsKey(d) && MathHelpers.Distance(MyPlayer.TruePosition, d.TruePosition) <= FlowerRatio)
                        {
                            CheckedDeadbodies.Add(d, AmongUsUtil.GetRoomName(d.TruePosition));
                        }
                    }
                };*/
            }
        }
        [Local]
        void OnUpdate(GameUpdateEvent ev)
        {
            foreach (var burial in flowers)
            {
                foreach (var d in Helpers.AllDeadBodies())
                {
                    if (!CheckedDeadbodies.ContainsKey(d) && MathHelpers.Distance(burial.Position, d.TruePosition) <= FlowerRatio)
                    {
                        CheckedDeadbodies.Add(d, AmongUsUtil.GetRoomName(d.TruePosition));
                        if (!burial.Shared)
                        {
                            burial.Shared = true;
                            NebulaGameManager.Instance.Scheduler.Schedule(RPCScheduler.RPCTrigger.AfterMeeting, NebulaSyncObject.RpcInstantiateDef.GetInvoker((burial.ObjectId, burial.TagHash, burial.argumentsCache, true)));
                        }
                    }
                }
            }
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            foreach (var d in CheckedDeadbodies)
            {
                var targetPlayer = GamePlayer.GetPlayer(d.Key.ParentId);
                if (targetPlayer == null || targetPlayer.IsAlive) continue;
                var sb = new StringBuilder(Language.Translate("role.flowerburial.message.deadPlayer")).Append(":").Append(targetPlayer.ColoredName).Append(Environment.NewLine).Append(Language.Translate("role.flowerburial.message.deadroom")).Append(":").Append(d.Value);
                if (NeutralCannotSeeRole&&targetPlayer.Role.Role.Category==RoleCategory.NeutralRole)
                {
                    goto IL_1;
                }
                if (ImpostorCannotSeeRole&& targetPlayer.IsImpostor)
                {
                    goto IL_1;
                }
                sb.Append(Environment.NewLine).Append(Language.Translate("role.flowerburial.message.role")).Append(":").Append(targetPlayer.Role.DisplayColoredName);
            IL_1:;
                sb.Append(Environment.NewLine).Append(Language.Translate("role.flowerburial.message.deadReason")).Append(":").Append(targetPlayer.PlayerState.Text);
                NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaAPI.GUI.VerticalHolder(GUIAlignment.Left, [
                    NebulaAPI.GUI.LocalizedText(GUIAlignment.Left,NebulaAPI.GUI.GetAttribute(AttributeAsset.OverlayTitle),"role.flowerburial.message.header"),
                    NebulaAPI.GUI.RawText(GUIAlignment.Center,NebulaAPI.GUI.GetAttribute(AttributeAsset.OverlayContent),sb.ToString())
                    ]), MeetingInfoIcon, MyRole.RoleColor, false);
            }
            CheckedDeadbodies.Clear();
        }
    }
    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    [NebulaRPCHolder]
    public class Flower : NebulaSyncStandardObject
    {
        /*public void Check(bool isCheck)
        {
            MyRenderer.sprite = isCheck ? CheckingImage.GetSprite() : NoCheckImage.GetSprite();
        }*/
        public Flower(Virial.Compat.Vector2 pos)
            : base(pos, NebulaSyncStandardObject.ZOption.Back, false, Flower.NoCheckImage.GetSprite(), false)
        {
            if (MyRenderer)
            {
                MyRenderer.transform.localScale = UnityEngine.Vector2.one * FlowerRatio / 5;
            }
        }
        static Flower()
        {
            NebulaSyncObject.RegisterInstantiater(MyTag, (float[] args) => new Flower(new Virial.Compat.Vector2(args[0], args[1])));
        }
        public static Flower? PlaceFlower(Virial.Compat.Vector2 pos)
        {
            return NebulaSyncObject.LocalInstantiate(MyTag, new float[] { pos.x, pos.y }).SyncObject as Flower;
        }
        public bool Shared=false;
        public static string MyTag = "FBurialFlower";

        public static Image NoCheckImage = NebulaAPI.AddonAsset.GetResource("flowerBurialFlowers.png")!.AsImage(125f)!;
        //public static Image CheckingImage = NebulaAPI.AddonAsset.GetResource("flowerBurialFlowersChecking.png")!.AsImage(125f)!;
    }
}