using Plana.Roles.Neutral;
using UnityEngine.UIElements;

namespace Nebula.Roles.Crewmate;

[NebulaRPCHolder]
public class Cavalier : DefinedRoleTemplate, DefinedRole, HasCitation ,IAssignableDocument
{
    public static Team MyTeam = new Team("teams.cavalier", new Virial.Color(255, 178, 70), TeamRevealType.OnlyMe);
    private Cavalier() : base("cavalier", new(255, 178, 70), RoleCategory.NeutralRole, MyTeam, [NumOfKillOption, KillCooldownOption, CanKillMaster, MasterCanKillCavalier])
    {
        ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImages/Cavalier.png")!.AsImage(115f)!;
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public static IntegerConfiguration NumOfKillOption = NebulaAPI.Configurations.Configuration("options.role.cavalier.numOfkill", new ValueTuple<int, int>(1, 15), 1, null, null, null);
    public static FloatConfiguration KillCooldownOption = NebulaAPI.Configurations.Configuration("options.role.cavalier.killCooldown", new ValueTuple<float,float,float>(10f,45f,2.5f),25f, FloatConfigurationDecorator.Second, null, null);
    public static BoolConfiguration CanKillMaster= NebulaAPI.Configurations.Configuration("options.role.cavalier.canKillMaster", false);
    public static BoolConfiguration MasterCanKillCavalier = NebulaAPI.Configurations.Configuration("options.role.cavalier.masterCanKillCavalier", false);

    static public Cavalier MyRole = new Cavalier();
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(Instance.selectImage!, "role.cavalier.ability.Select");
    }
    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%NUM%", NumOfKillOption.GetValue().ToString());
    }
    public class Instance(GamePlayer player) : RuntimeAssignableTemplate(player), RuntimeRole, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;
        ModAbilityButton? killButton, selectbutton;
        GamePlayer? secondSetColorPlayer;
        [Local]
        private void DecorateKnightedTargetColor(PlayerDecorateNameEvent ev)
        {
            try
            {
                if (AmOwner)
                {
                    if (ev.Player != null && selectPlayer != null && selectPlayer.PlayerId == ev.Player.PlayerId)
                    {
                        ev.Color = new Virial.Color?(MyRole.RoleColor);
                        if (ev.Player.Modifiers.Any((RuntimeModifier m) => m is Lover.Instance))
                        {
                            Lover.Instance lover = ev.Player.Modifiers.FirstOrDefault((RuntimeModifier mod) => mod is Lover.Instance) as Lover.Instance;
                            secondSetColorPlayer = lover?.MyLover.Get();
                        }
                    }
                    if (ev.Player != null && ev.Player.PlayerId == MyPlayer.PlayerId)
                    {
                        ev.Color = new Virial.Color?(MyRole.RoleColor);
                    }
                    if (secondSetColorPlayer != null && ev.Player.PlayerId == secondSetColorPlayer.PlayerId)
                    {
                        ev.Color = new Virial.Color?(MyRole.RoleColor);
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        static internal Virial.Media.Image? selectImage = NebulaAPI.AddonAsset.GetResource("Select.png")?.AsImage(115f);
        [OnlyMyPlayer]
        void OnCheckExtraWin(PlayerCheckExtraWinEvent ev)
        {
            if (selectPlayer == null)
            {
                return;
            }
            if (ev.WinnersMask.Test(selectPlayer) && !MyPlayer.TryGetModifier<SkinnerDog.Instance>(out var m))
            {
                ev.SetWin(true);
                ev.ExtraWinMask.Add(PatchManager.cavalierExtra);
            }
        }
        private void OnGameEnd(GameEndEvent ev)
        {
            if (selectPlayer == null)
            {
                return;
            }
            if (ev.EndState.Winners.Test(selectPlayer))
            {
                if (MyPlayer.AmOwner)
                {
                    if (selectPlayer.IsImpostor)
                    {
                        new StaticAchievementToken("cavalier.common2");
                    }
                    if (selectPlayer.TryGetModifier<Lover.Instance>(out var lover))
                    {
                        new StaticAchievementToken("cavalier.common3");
                    }
                    if (selectPlayerEarlyDead&&!MyPlayer.IsDead)
                    {
                        new StaticAchievementToken("cavalier.common4");
                    }
                    if (killSelectPlayer)
                    {
                        new StaticAchievementToken("cavalier.another1");
                    }
                    if (selectPlayer.Role.Role.Category == RoleCategory.NeutralRole)
                    {
                        new StaticAchievementToken("cavalier.challenge1");
                    }
                }
            }
        }
        int meetingnum;
        bool selectPlayerEarlyDead;
        bool killSelectPlayer;
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            meetingnum++;
            if (meetingnum<=2 && selectPlayer!=null&&selectPlayer.IsDead)
            {
                selectPlayerEarlyDead = true;
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                selectPlayer = null;

                if (NebulaGameManager.Instance!=null)
                {
                    var p = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.TryGetModifier<Master.Instance>(out var m));
                    if (p!=null)
                    {
                        selectPlayer = p;
                    }
                }

                int leftKill = NumOfKillOption;
                SpriteRenderer? lockSprite = null;
                ObjectTracker<GamePlayer> killTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.KillablePredicate(base.MyPlayer), null, false, false);
                killButton = NebulaAPI.Modules.AbilityButton(this, false, true, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Kill, null);
                killButton.Availability = (button) => killTracker.CurrentTarget != null && selectPlayer != null &&(CanKillMaster||killTracker.CurrentTarget != selectPlayer) && this.MyPlayer.CanMove && lockSprite == null;
                killButton.Visibility = (button) => !base.MyPlayer.IsDead && leftKill > 0;
                killButton.ShowUsesIcon(2, leftKill.ToString());
                try
                {
                    if (selectPlayer == null)
                    {
                        lockSprite = (killButton as ModAbilityButtonImpl).VanillaButton.AddLockedOverlay();
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log("SetLockError : " + e.ToString());
                }
                this.killButton.OnClick = delegate (ModAbilityButton button)
                {
                    if (selectPlayer != null && killTracker.CurrentTarget.PlayerId == selectPlayer.PlayerId)
                    {
                        killSelectPlayer = true;
                    }
                    leftKill--;
                    var player = killTracker.CurrentTarget;
                    var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEvent(MyPlayer, player));
                    if (!(cancelable?.IsCanceled ?? false))
                    {
                        MyPlayer.MurderPlayer(player, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill, (result) =>
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
                    if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, KillCooldownOption).SetAsKillCoolTimer().Start(null);
                killButton.StartCoolDown();
                killButton.SetLabel("kill");
                selectbutton = NebulaAPI.Modules.AbilityButton(this,false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Ability, null);
                selectbutton.Availability = (button) => killTracker.CurrentTarget != null && this.MyPlayer.CanMove;
                selectbutton.Visibility = (button) => !base.MyPlayer.IsDead && selectPlayer == null;
                (selectbutton as Virial.Components.ModAbilityButton).SetImage(selectImage!);
                selectbutton.OnClick = button =>
                {
                    if (killTracker.CurrentTarget == null) return;

                    new StaticAchievementToken("cavalier.common1");
                    SetPlayer.Invoke((MyPlayer, killTracker.CurrentTarget));
                    selectPlayer?.AddModifier(Master.MyRole, null);
                    if (lockSprite)
                    {
                        UnityEngine.Object.Destroy(lockSprite);
                        lockSprite = null;
                    }
                };
                selectbutton.SetLabel("knightedSelect");
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }

        public GamePlayer? selectPlayer;

        static RemoteProcess<(GamePlayer, GamePlayer)> SetPlayer = new("KnightedSetPlayer", (message, _) =>
        {
            if (message.Item1.Role is Instance instance)
            {
                instance.selectPlayer = message.Item2;
            }
        });
    }
}
public class Master : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>,HasCitation, IAssignableDocument
{
    private Master() : base("master",  new(255, 178, 70),null,true,()=>false)
    {
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }

    static public Master MyRole = new Master();
    bool IAssignableDocument.HasTips => true;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (base.AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0f, 0.25f, 0.4f, 0f);
            }
        }

        [OnlyMyPlayer]
        private void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (!Cavalier.MasterCanKillCavalier && ev.Target.Role is Cavalier.Instance instance && instance.selectPlayer == MyPlayer)
            {
                ev.SetAsCannotKillBasically();
            }
        }

        [Local]
        private void DecorateKnightedTargetColor(PlayerDecorateNameEvent ev)
        {
            if (AmOwner)
            {
                if (ev.Player != null && ev.Player.Role is Cavalier.Instance)
                {
                    ev.Color = new Virial.Color?(MyRole.RoleColor);
                }
                if (ev.Player != null && ev.Player.PlayerId == MyPlayer.PlayerId)
                {
                    ev.Color = new Virial.Color?(MyRole.RoleColor);
                }
            }
        }
        [OnlyMyPlayer]
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo||inEndScene) name += "¡".Color(MyRole.RoleColor.ToUnityColor());
        }
    }
}