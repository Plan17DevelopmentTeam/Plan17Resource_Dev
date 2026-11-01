using Plana.Core;
using Plana.Roles.Impostor;
using Plana.Roles.Neutral;
using Rewired.Demos.CustomPlatform;
using Virial.Helpers;
using static Plana.Roles.Impostor.CrawlerEngineer;

namespace Plana.Roles.Crewmate;

public class Celestialdog : DefinedRoleTemplate, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    private Celestialdog() : base("celestialdog", new(255,228,174), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam,[SkillCooldownOption,SkillDurationOption,KillCooldownOption,KillNumOption,CanKillStateOption,SkillEndCanUseTeleport]) 
    {
    }
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Celestialdog.png")!.AsImage();
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player,arguments.Get(0,0),arguments.GetAsBool(1));
    public static readonly FloatConfiguration SkillCooldownOption = NebulaAPI.Configurations.Configuration("options.role.celestialdog.skillCoolDown", (2.5f,60f,2.5f),20f,FloatConfigurationDecorator.Second);
    public static readonly FloatConfiguration SkillDurationOption = NebulaAPI.Configurations.Configuration("options.role.celestialdog.skillDuration", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    public static readonly FloatConfiguration KillCooldownOption = NebulaAPI.Configurations.Configuration("options.role.celestialdog.killCooldown", (2.5f, 60f, 2.5f), 22.5f, FloatConfigurationDecorator.Second);
    public static readonly IntegerConfiguration KillNumOption = NebulaAPI.Configurations.Configuration("options.role.celestialdog.killnum", (1,16), 2);
    public static readonly ValueConfiguration<int> CanKillStateOption = NebulaAPI.Configurations.Configuration("options.role.celestialdog.cankillstate", ["options.role.celestialdog.state.InEffect", "options.role.celestialdog.state.none"], 0);
    public static readonly BoolConfiguration SkillEndCanUseTeleport = NebulaAPI.Configurations.Configuration("options.role.celestialdog.endskillcanusetp", true);

    static public Celestialdog MyRole = new Celestialdog();
    bool IAssignableDocument.HasTips => true;
    public static RemoteProcess<(GamePlayer, int,bool)> UpdateCdogArgumentRPC = new("UpdateCdogArgumentRPC", (m, _) =>
    {
        if (m.Item1.Role is Celestialdog.Instance ins)
        {
            ins.leftKill = m.Item2;
            ins.addedKill = m.Item3;
        }
    });
    public class Instance(GamePlayer player, int leftkill, bool addedkill) : RuntimeAssignableTemplate(player), RuntimeRole, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;
        int[]? RuntimeAssignable.RoleArguments => [leftKill, addedKill.AsInt()];
        public bool addedKill = addedkill;
        static Virial.Media.Image skillImage = NebulaAPI.AddonAsset.GetResource("DogSkill.png")!.AsImage(115f)!;
        static Virial.Media.Image KillImage = NebulaAPI.AddonAsset.GetResource("DogKill.png")!.AsImage(100f)!;
        static Virial.Media.Image TeleportImage = NebulaAPI.AddonAsset.GetResource("DogTeleport.png")!.AsImage(115f)!;
        public ModAbilityButton? skillButton, killButton, teleportButton;
        RoleTaskType RuntimeRole.TaskType
        {
            get
            {
                if (skillButton == null)
                {
                    return RoleTaskType.CrewmateTask;
                }
                return skillButton.IsInEffect ? RoleTaskType.NoTask : RoleTaskType.CrewmateTask;
            }
        }
        public int leftKill = leftkill;
        void AddKill()
        {
            if (killButton == null)
            {
                ObjectTracker<GamePlayer> killTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.KillablePredicate(base.MyPlayer), null, false, false);
                killButton = NebulaAPI.Modules.AbilityButton(this, false, true, 0, false).BindKey(base.MyPlayer.IsCrewmate ? Virial.Compat.VirtualKeyInput.Kill : Virial.Compat.VirtualKeyInput.SecondaryAbility, null);
                killButton.Availability = (button) => killTracker.CurrentTarget != null && this.MyPlayer.CanMove&&(CanKillStateOption.GetValue()==1||skillButton.IsInEffect);
                killButton.Visibility = (button) => !base.MyPlayer.IsDead && leftKill > 0;
                killButton.SetImage(KillImage);
                killButton.ShowUsesIcon(3, leftKill.ToString());
                this.killButton.OnClick = delegate (ModAbilityButton button)
                {
                    leftKill--;
                    var target = killTracker.CurrentTarget;
                    var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEvent(MyPlayer, target));
                    if (!(cancelable?.IsCanceled ?? false))
                    {
                        MyPlayer.MurderPlayer(killTracker.CurrentTarget, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill, (result) =>
                        {
                            if (result == KillResult.Kill)
                            {
                                killButton.UpdateUsesIcon(leftKill.ToString());
                            }
                            else
                            {
                                leftKill++;
                            }
                        });
                    }
                    //クールダウンをリセットする
                    if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, KillCooldownOption).SetAsKillCoolTimer().Start(null);
                killButton.SetLabel("kill");
                killButton.StartCoolDown();
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
            killButton.UpdateUsesIcon(leftKill.ToString());
        }
        bool usedTeleport;
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                skillButton = NebulaAPI.Modules.EffectButton(this, base.MyPlayer, VirtualKeyInput.Ability, null, SkillCooldownOption, SkillDurationOption, "celestialdog.skill", skillImage, (ModAbilityButton _) => MyPlayer.CanMove, (ModAbilityButton _) => !MyPlayer.IsDead, false, false).SetLabelType(ModAbilityButton.LabelType.Crewmate);
                skillButton.OnEffectStart = (button) =>
                {
                    usedTeleport = false;
                    MyPlayer.VanillaPlayer.Visible = false;
                    MyPlayer.GainAttribute(PlayerAttributes.Invisible, SkillDurationOption - 0.8f, false, 100,"celestialdog::invisible");
                    RpcEquip.Invoke(new ValueTuple<byte, bool>(MyPlayer.PlayerId, true));
                };
                skillButton.OnEffectEnd = (button) =>
                {
                    MyPlayer.VanillaPlayer.Visible = true;
                    RpcEquip.Invoke(new ValueTuple<byte, bool>(MyPlayer.PlayerId, false));
                    button.StartCoolDown();
                };
                if (leftKill > 0)
                {
                    AddKill();
                }
                if (SkillEndCanUseTeleport)
                {
                    teleportButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.SecondaryAbility, 0f, "celestialdog.teleport", TeleportImage);
                    teleportButton.Visibility = (button) => skillButton.IsInEffect && skillButton.EffectTimer?.Percentage <= 0.25f && !usedTeleport;
                    teleportButton.PlayFlashWhile = (button) => true;
                    teleportButton.OnClick = (button) =>
                    {
                        usedTeleport = true;
                        byte mapId = AmongUsLLImpl.Instance.MapId;
                        NebulaPreSpawnLocation[] cand = NebulaPreSpawnLocation.Locations[(int)mapId];
                        if (cand.Length == 0)
                        {
                            cand = NebulaPreSpawnLocation.Locations[(int)mapId].Where((NebulaPreSpawnLocation l) => l.VanillaIndex != null).ToArray<NebulaPreSpawnLocation>();
                        }
                        var list = new List<UnityEngine.Vector2>();
                        cand.Do(p => list.Add(p.Position!.Value));
                        MyPlayer.VanillaPlayer.NetTransform.RpcSnapTo(list[UnityEngine.Random.Range(0,list.Count)]);
                    };
                }
                GameOperatorManager instance = GameOperatorManager.Instance!;
                if (instance != null)
                {
                    instance.Subscribe<GameUpdateEvent>(op =>
                    {
                        if (addedKill)
                        {
                            return;
                        }
                        if (!skillButton.IsInEffect)
                        {
                            return;
                        }
                        if (MeetingHud.Instance.AsBoolFast())
                        {
                            return;
                        }
                        if (DestroyableSingleton<HudManager>.InstanceExists && DestroyableSingleton<HudManager>.Instance.ReportButton.canInteract)
                            {
                            addedKill = true;
                            leftKill = KillNumOption;
                            UpdateCdogArgumentRPC.Invoke((MyPlayer, leftKill, addedKill));
                            AddKill();
                        }

                    }, this);
                    instance.Subscribe<MeetingStartEvent>(delegate (MeetingStartEvent ev)
                    {
                        if (MyState != null)
                        {
                            RpcEquip.Invoke(new ValueTuple<byte, bool>(this.MyPlayer.PlayerId, false));
                        }
                    }, this);
                    instance.Subscribe<PlayerDieEvent>(ev =>
                    {
                        if (ev.Player.AmOwner)
                        {
                            if (MyState != null)
                            {
                                RpcEquip.Invoke(new ValueTuple<byte, bool>(this.MyPlayer.PlayerId, false));
                            }
                        }
                    }, this);
                    instance.RegisterOnReleased(() =>
                    {
                        if (MyState != null)
                        {
                            RpcEquip.Invoke(new ValueTuple<byte, bool>(this.MyPlayer.PlayerId, false));
                        }
                    }, skillButton);
                }

            }
        }
        public void EnterDogState()
        {
            MyState = new DogState(base.MyPlayer).Register(this);
        }

        public void ExitDogState()
        {
            if (MyState != null)
            {
                MyState.Release();
            }
            MyState = null!;
        }
        public DogState MyState
        {
            get; private set;
        } = null!;
        static readonly RemoteProcess<(byte playerId, bool enter)> RpcEquip = new(
        "CelestialDogStateRPC",
        (message, _) =>
        {
            var player = GamePlayer.GetPlayer(message.playerId);
            if (player == null) return;

            var role = player.Role;
            if (role is Celestialdog.Instance instance)
            {
                if (message.enter)
                    instance.EnterDogState();
                else
                    instance.ExitDogState();
            }
        }
        );
        public class DogState : EquipableAbility,IGameOperator
        {
            Virial.Media.MultiImage DogNormal = NebulaAPI.AddonAsset.GetResource("DogNormal.png")!.AsMultiImage(4, 2, 100f)!;
            Virial.Media.MultiImage DogRun = NebulaAPI.AddonAsset.GetResource("DogRun.png")!.AsMultiImage(4, 2, 100f)!;
            protected override float Size => 0.6f;
            protected override float Distance => 0.3f;
            public DogState(GamePlayer owner) : base(owner, false, "Dogstate")
            {
                spriteIndex = 0;
                owner.VanillaPlayer.Visible = false;
            }
            void IGameOperator.OnReleased()
            {
                if (Renderer) GameObject.Destroy(Renderer.gameObject);
                Renderer = null!;
                Owner.VanillaPlayer.Visible = true;
                Owner.RemoveAttributeByTag("celestialdog::invisible");
            }
            protected override void HudUpdate(GameHudUpdateEvent ev)
            {
                return;
            }
            int spriteIndex;
            bool isrunning;
            float updateSpriteTime;
            void OnUpdate(GameUpdateEvent ev)
            {
                var control = Owner.VanillaPlayer;
                control.Visible = false;
                if (control.MyPhysics.body.velocity.sqrMagnitude >= 0.05f)
                {
                    isrunning = true;
                }
                else
                {
                    isrunning = false;
                }
                Renderer.flipX = Owner.VanillaCosmetics.FlipX;
                updateSpriteTime += Time.deltaTime;
                if (updateSpriteTime >= 0.1f)
                {
                    Renderer.sprite = (isrunning ? DogRun : DogNormal).AsLoader(spriteIndex).GetSprite();
                    spriteIndex++;
                    if (spriteIndex >= 8)
                    {
                        spriteIndex = 0;
                    }
                    updateSpriteTime = 0f;
                }
            }
        }
    }
}
