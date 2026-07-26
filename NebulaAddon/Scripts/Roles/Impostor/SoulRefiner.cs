using Plana.Core;

namespace Plana.Roles.Impostor;

public class SoulRefiner : DefinedRoleTemplate, DefinedRole, HasCitation, IAssignableDocument
{
    private SoulRefiner() : base("soulrefiner", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [DistanceOption, KillAddDistanceOption,MaxDistanceOption, skillCDOption, skilltimeOption, killOption,SkillKillIsRemoteOption,LockSKillOption,UnlockSkillKillNumOption, CanKillImpostorOption,ShareCooldownOption])
    {
        MetaAbility.RegisterCircle(new EffectCircleInfo("role.soulrefiner.origindistance", () => DistanceOption, () => null, base.RoleColor));
        MetaAbility.RegisterCircle(new EffectCircleInfo("role.soulrefiner.adddistance", () => KillAddDistanceOption, () => null, base.RoleColor));
    }
    Citation HasCitation.Citation => PCitations.PlanaANDKCANDYunDuan;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    static private FloatConfiguration KillAddDistanceOption = NebulaAPI.Configurations.Configuration("options.role.soulrefiner.killadddistance", (0f, 10f, 0.25f), 0.25f,FloatConfigurationDecorator.Ratio);
    static private FloatConfiguration MaxDistanceOption = NebulaAPI.Configurations.Configuration("options.role.soulrefiner.maxdistance", (0f, 20f, 0.25f), 10f, FloatConfigurationDecorator.Ratio);
    static private FloatConfiguration DistanceOption = NebulaAPI.Configurations.Configuration("options.role.soulrefiner.distance", (0f, 10f, 0.25f), 1.25f, FloatConfigurationDecorator.Ratio);
    static private FloatConfiguration skillCDOption = NebulaAPI.Configurations.Configuration("options.role.soulrefiner.skillCooldown", (0f, 60f, 2.5f), 27.5f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration skilltimeOption = NebulaAPI.Configurations.Configuration("options.role.soulrefiner.skilltime", (0f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration killOption = NebulaAPI.Configurations.Configuration("options.role.soulrefiner.killtime", (0f, 60f, 0.5f), 7.5f, FloatConfigurationDecorator.Second);
    static private BoolConfiguration LockSKillOption = NebulaAPI.Configurations.Configuration("options.role.soulrefiner.lockSkill", true);
    static private IntegerConfiguration UnlockSkillKillNumOption = NebulaAPI.Configurations.Configuration("options.role.soulrefiner.unlockKillnum", (1, 16), 2,()=>LockSKillOption);
    static private BoolConfiguration CanKillImpostorOption = NebulaAPI.Configurations.Configuration("options.role.soulrefiner.canKillImpostor", false);
    static private BoolConfiguration SkillKillIsRemoteOption = NebulaAPI.Configurations.Configuration("options.role.soulrefiner.SkillKillIsRemote", true);
    static private BoolConfiguration ShareCooldownOption = NebulaAPI.Configurations.Configuration("options.role.soulrefiner.shareCooldown", true);
    static public SoulRefiner MyRole = new SoulRefiner();
    bool IAssignableDocument.HasTips => true;
    public class Formation : EquipableAbility, IGameOperator
    {
        protected override void HudUpdate(GameHudUpdateEvent ev)
        {
            return;
        }
        void IGameOperator.OnReleased()
        {
            if (circle)
            {
                UnityEngine.Object.Destroy(circle.gameObject);
            }
            if (Renderer)
            {
                UnityEngine.Object.Destroy(Renderer);
            }
            circle = null;
        }
        public Dictionary<byte, float> targetDic;
        public Formation(GamePlayer owner, Dictionary<byte, float> dic,List<GamePlayer> killPlayers)
            : base(owner, true, "SoulRefiner")
        {
            base.Renderer.sprite = soulRefinerSkill.GetSprite();
            targetDic = dic;
            circle = EffectCircle.SpawnEffectCircle(AmongUsLLImpl.LocalPlayer.transform, UnityEngine.Vector3.zero, NebulaTeams.ImpostorTeam.Color, new float?(DistanceOption + (KillAddDistanceOption * Instance.GetKillNum(Owner))), null, true);
            circle.gameObject.AddComponent<ScriptBehaviour>().UpdateHandler += delegate ()
            {
                if (MeetingHud.Instance&&circle)
                {
                    if (circle)
                    {
                        UnityEngine.Object.Destroy(circle.gameObject);
                    }
                    if (Renderer)
                    {
                        UnityEngine.Object.Destroy(Renderer);
                    }
                    circle = null;
                    return;
                }
                float num = DistanceOption + (KillAddDistanceOption * Instance.GetKillNum(Owner));
                circle.OuterRadius = () => num;
                Renderer.transform.Rotate(new UnityEngine.Vector3(0f, 0f, 0.4f));
                try
                {
                    foreach (var playerInfo in GamePlayer.AllPlayers)
                    {
                        byte playerId = playerInfo.PlayerId;
                        float playerProgress;
                        if (targetDic.TryGetValue(playerId, out playerProgress))
                        {
                            if (!playerInfo.IsDisconnected && !playerInfo.IsDead && Owner.PlayerId != playerId && !playerInfo.Logic.InVent)
                            {
                                var pos = Owner.TruePosition;
                                Virial.Compat.Vector2 vector = playerInfo.TruePosition - pos;
                                float magnitude = vector.Magnitude;
                                if (magnitude <= num && !PhysicsHelpers.AnyNonTriggersBetween(pos, vector.Normalized, magnitude, Constants.ShipAndObjectsMask))
                                {
                                    playerProgress += Time.deltaTime;
                                }
                                else
                                {
                                    playerProgress = 0f;
                                }
                            }
                            else
                            {
                                playerProgress = 0f;
                            }
                            if (playerProgress >= killOption)
                            {
                                try
                                {
                                    var p = GamePlayer.GetPlayer(playerId);
                                    if ((CanKillImpostorOption && p.IsImpostor) || !p.IsImpostor)
                                    {
                                        new StaticAchievementToken("soulrefiner.common1");
                                        Owner.MurderPlayer(p, PatchManager.Refiner, EventDetails.Kill, SkillKillIsRemoteOption ? KillParameter.NormalKill : KillParameter.RemoteKill);
                                    }
                                    if (killPlayers.Count((GamePlayer p) => p.PlayerState == PatchManager.Refiner) >= 2)
                                    {
                                        new StaticAchievementToken("soulrefiner.common2");
                                    }
                                    if (killPlayers.Count((GamePlayer p) => p.PlayerState == PatchManager.Refiner && (p.IsImpostor || p.Role.Role.Category == RoleCategory.NeutralRole)) >= 2)
                                    {
                                        new StaticAchievementToken("soulrefiner.another1");
                                    }
                                    targetDic.Remove(playerId);
                                }
                                catch (Exception e)
                                {
                                    PDebug.Log(e);
                                }
                            }
                            else
                            {
                                targetDic[playerId] = playerProgress;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            };
        }
        EffectCircle circle;
        static private Virial.Media.Image soulRefinerSkill = NebulaAPI.AddonAsset.GetResource("soulRefinerskillImage.png").AsImage(100f);
    }
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        private ModAbilityButton? skillButton = null;
        static private Virial.Media.Image bimage = NebulaAPI.AddonAsset.GetResource("soulrefinerbutton.png").AsImage(100f);
        public Instance(GamePlayer player) : base(player)
        {
        }
        float distance;
        public static int GetKillNum(GamePlayer refiner)
        {
            return NebulaGameManager.Instance.AllPlayerInfo.Count(c => c.MyKiller != null && c.MyKiller == refiner);
        }
        [OnlyMyPlayer]
        [Local]
        private void OnKillPlayer(PlayerKillPlayerEvent ev)
        {
            if (LockSKillOption)
            {
                if (UnlockSkillKillNumOption - GetKillNum(MyPlayer) <= 0)
                {
                    if (lockSprite != null)
                    {
                        skillButton!.HideUsesIcon();
                        UnityEngine.Object.Destroy(lockSprite);
                        lockSprite = null;
                    }
                }
                else
                {
                    skillButton!.ShowUsesIcon(0, (Mathn.Max(0, UnlockSkillKillNumOption - GetKillNum(MyPlayer))).ToString());
                }
            }
            distance = DistanceOption + (KillAddDistanceOption * GetKillNum(MyPlayer));
            KillPlayers.Add(ev.Dead);
            if (KillPlayers.Count >= 5)
            {
                new StaticAchievementToken("soulrefiner.challenge1");
            }
        }
        SpriteRenderer lockSprite;
        Formation form;
        Dictionary<byte, float> dic = new Dictionary<byte, float>();
        List<GamePlayer> KillPlayers = new List<GamePlayer>();
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                dic = new Dictionary<byte, float>();
                foreach (var player in GamePlayer.AllPlayers)
                {
                    dic.TryAdd(player.PlayerId, 0f);
                }
                distance = DistanceOption;
                skillButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Ability);
                if (LockSKillOption)
                {
                    lockSprite = (skillButton as ModAbilityButtonImpl)!.VanillaButton.AddLockedOverlay();
                    skillButton.ShowUsesIcon(0, ((int)UnlockSkillKillNumOption).ToString());
                }
                skillButton.Availability = (button) => MyPlayer.CanMove&&(!LockSKillOption||(LockSKillOption&&lockSprite==null));
                skillButton.Visibility = (button) => !MyPlayer.IsDead;
                skillButton.SetLabel("refiner");
                skillButton.SetImage(bimage);
                skillButton.OnClick = (button) =>
                {
                    button.StartEffect();
                    if (ShareCooldownOption)
                    {
                        NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    }
                };
                skillButton.OnEffectStart = (button) =>
                {
                    form = new Formation(MyPlayer, dic,KillPlayers).Register(this);
                };
                skillButton.OnEffectEnd = (button) =>
                {
                    if (!ShareCooldownOption)
                    {
                        button.StartCoolDown();
                    }
                    else
                    {
                        NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    }
                    if (form != null)
                    {
                        dic = form.targetDic;
                        form.Release();
                    }
                    form = null;
                    if (KillPlayers.Count>=5)
                    {
                        new StaticAchievementToken("soulrefiner.challenge1");
                    }
                };
                GameOperatorManager.Instance?.Subscribe<MeetingPreStartEvent>(ev =>
                {
                    if (form != null)
                    {
                        form.Release();
                    }
                    form = null;
                    if (KillPlayers.Count >= 5)
                    {
                        new StaticAchievementToken("soulrefiner.challenge1");
                    }
                },this, 100);
                GameOperatorManager.Instance?.Subscribe<PlayerKillPlayerEvent>(ev =>
                {
                    if (ev.Murderer.AmOwner&&ShareCooldownOption)
                    {
                        NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    }
                }, this);
                GameOperatorManager.Instance?.RegisterOnReleased(() =>
                {
                    if (skillButton.IsInEffect)
                    {
                        if (form != null)
                        {
                            form.Release();
                        }
                        form = null;
                    }
                }, skillButton);
                skillButton.EffectTimer = NebulaAPI.Modules.Timer(this, skilltimeOption);
                skillButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, skillCDOption).SetAsAbilityTimer().Start();
                skillButton.StartCoolDown();
                skillButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                if (ShareCooldownOption)
                {
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(skillButton.GetKillButtonLike());
                }
            }
        }
    }
}
