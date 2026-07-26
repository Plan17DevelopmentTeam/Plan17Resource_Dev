using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Image = Virial.Media.Image;

namespace Plana.Roles.Neutral;

public class Spinner : DefinedRoleTemplate, HasCitation, DefinedRole, IAssignableDocument
{
    public static Team RoleTeam = new Team("teams.spinner", new Virial.Color(255, 129, 56), TeamRevealType.OnlyMe);
    private Spinner() : base("spinner", RoleTeam.Color, RoleCategory.NeutralRole, RoleTeam, [DrawCooldown, SpinCooldown, DrawCooldownAddratio, CanSpinMinPlayerNum, WinNeedEndOfFateNum])
    {
        //ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImages/Spinner.png")!.AsImage(115f)!;
    }
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public static TranslatableTag EndofFate = new TranslatableTag("state.endoffate");
    static IntegerConfiguration CanSpinMinPlayerNum = NebulaAPI.Configurations.Configuration("options.role.spinner.canspinminplayernum", (2, 24), 3);
    static FloatConfiguration DrawCooldown = NebulaAPI.Configurations.Configuration("options.role.spinner.drawcooldown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static FloatConfiguration SpinCooldown = NebulaAPI.Configurations.Configuration("options.role.spinner.Spincooldown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static FloatConfiguration DrawCooldownAddratio = NebulaAPI.Configurations.Configuration("options.role.spinner.drawcooldownaddratio", (0f, 60f, 2.5f), 2.5f, FloatConfigurationDecorator.Second);
    static IntegerConfiguration WinNeedEndOfFateNum = NebulaAPI.Configurations.Configuration("options.role.spinner.winneedendoffatenum", (1, 24), 4);
    static public Spinner MyRole = new();
    //bool IAssignableDocument.HasTips => true;

    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        public static readonly GameEnd SpinnerWin = NebulaAPI.Preprocessor!.CreateEnd("spinner", MyRole.RoleColor);
        DefinedRole RuntimeRole.Role => MyRole;
        static Image? markImage = null;//暂无
        static Image? spinImage = null;//暂无
        ModAbilityButton? drawButton, spinButton;
        List<GamePlayer> unSpinDrawedPlayers = new();
        List<SpinString> allStrings = new();
        public Instance(GamePlayer player) : base(player)
        {
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                ObjectTracker<GamePlayer> myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, p => ObjectTrackers.StandardPredicate(p) && !unSpinDrawedPlayers.Contains(p), null, false, false);
                drawButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Ability, null);
                drawButton.Availability = (button) => myTracker.CurrentTarget != null && this.MyPlayer.CanMove;
                drawButton.Visibility = (button) => !base.MyPlayer.IsDead;
                //drawButton.SetImage(markImage);
                drawButton.OnClick = delegate (ModAbilityButton button)
                {
                    unSpinDrawedPlayers.Add(myTracker.CurrentTarget);
                    drawButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, DrawCooldown + DrawCooldownAddratio * unSpinDrawedPlayers.Count).SetAsAbilityTimer().Start();
                    drawButton.StartCoolDown();
                };
                drawButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, DrawCooldown).SetAsAbilityTimer().Start();
                drawButton.SetLabel("spinner.draw");
                spinButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.SecondaryAbility, null);
                spinButton.Availability = (button) => this.MyPlayer.CanMove && unSpinDrawedPlayers.Count >= CanSpinMinPlayerNum;
                spinButton.Visibility = (button) => !base.MyPlayer.IsDead;
                //spinButton.SetImage(spinImage);
                spinButton.OnClick = delegate (ModAbilityButton button)
                {
                    allStrings.Add(new SpinString(unSpinDrawedPlayers, this));
                    CoShowString(unSpinDrawedPlayers.ToArray(), MyPlayer);
                    unSpinDrawedPlayers.Do(p => p.AddModifier(Spined.MyRole));
                    unSpinDrawedPlayers.Clear();
                    spinButton.StartCoolDown();
                };
                spinButton.SetLabel("spinner.spin");
                spinButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, SpinCooldown).SetAsAbilityTimer().Start(null);
                //spinButton.EffectTimer = NebulaAPI.Modules.Timer(this, 5f).Start(null);
                spinButton.StartCoolDown();
            }
        }
        [NebulaRPC]
        void CoShowString(GamePlayer[] players, GamePlayer invoker)
        {
            if (invoker.Role is Spinner.Instance ins)
            {
                allStrings.Add(new SpinString(players.ToList(), ins));
            }
        }
        [OnlyMyPlayer]
        void CheckWin(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd == SpinnerWin);
        }
        [OnlyHost]
        void OnUpdate(GameUpdateEvent ev)
        {
            if (NebulaGameManager.Instance.AllPlayerInfo.Count(p => p.PlayerState == EndofFate) >= WinNeedEndOfFateNum)
            {
                NebulaAPI.CurrentGame.TriggerGameEnd(SpinnerWin, GameEndReason.Situation);
            }
        }
    }
    public class SpinString : FlexibleLifespan, IGameOperator
    {
        private List<GamePlayer> stringPlayers;
        Spinner.Instance instance;
        public SpinString(List<GamePlayer> players, Spinner.Instance instance)
        {
            stringPlayers = players;
            this.instance = instance;
            Bind(instance);
            this.RegisterSelf();
        }

        bool initialized = false;
        Dictionary<ValueTuple<GamePlayer, GamePlayer>, LineRenderer> Lines = new();
        void CreateLine(GamePlayer p1, GamePlayer p2)
        {
            var lr = UnityHelper.CreateObject<LineRenderer>($"Line_{p1.PlayerName}_{p2.PlayerName}", null, UnityEngine.Vector3.zero);
            lr.material = new(NebulaAsset.MultiplyShader);
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
            lr.startColor = GroupConfigurationColor.ImpostorRed.ToUnityColor();
            lr.endColor = GroupConfigurationColor.Gray.ToUnityColor();
            lr.positionCount = 2;
            lr.SetPositions(new Il2CppStructArray<UnityEngine.Vector3>([p1.Position.ToUnityVector(), p2.Position.ToUnityVector()]));
            Lines.TryAdd(ValueTuple.Create(p1, p2), lr);
        }
        void OnFirstUpdate()
        {
            if ((instance != null && instance.AmOwner || stringPlayers.Any(p => p.AmOwner)) || NebulaGameManager.Instance.CanSeeAllInfo)
            {
                for (int i = 0; i < stringPlayers.Count; i++)
                {
                    for (int j = i + 1; j < stringPlayers.Count; j++)
                    {
                        CreateLine(stringPlayers[i], stringPlayers[j]);
                    }
                }
            }
            initialized = true;
        }
        void OnDead(PlayerDieOrDisconnectEvent ev)
        {
            if (Lines.Count > 0)
            {
                if (ev.Player == instance.MyPlayer)
                {
                    Inactivate();
                }
            }
        }
        void OnDie(PlayerDieEvent ev)
        {
            if (!instance.AmOwner)
            {
                return;
            }
            if (Lines.Count > 0 && ev.Player != instance.MyPlayer)
            {
                if (stringPlayers.Any(p => p == ev.Player))
                {
                    if (ev is PlayerExiledEvent)
                    {
                        stringPlayers.Except([ev.Player]).Do(p => p.VanillaPlayer.ModMarkAsExtraVictim(ev.Player.VanillaPlayer, EndofFate, EventDetail.Kill));
                    }
                    else
                    {
                        stringPlayers.Except([ev.Player]).Do(p => p.Suicide(EndofFate, EventDetail.Kill, KillParameter.NormalKill));
                    }
                }
            }
        }
        void OnDisconnected(PlayerDisconnectEvent ev)
        {
            if (Lines.Count > 0 && ev.Player != instance.MyPlayer)
            {
                var keysToRemove = new List<ValueTuple<GamePlayer, GamePlayer>>();
                var disconnectedLines = Lines.Where(pair => pair.Key.Item1 == ev.Player || pair.Key.Item2 == ev.Player);
                disconnectedLines.Do(pair =>
                {
                    keysToRemove.Add(pair.Key);
                    GameObject.Destroy(pair.Value);
                });
                keysToRemove.Do(k => Lines.Remove(k));
                if (stringPlayers.Contains(ev.Player))
                {
                    stringPlayers.Remove(ev.Player);
                }
            }
        }

        void OnHudUpdate(GameHudUpdateEvent ev)
        {
            if (!initialized) OnFirstUpdate();
            if (Lines.Count <= 0)
            {
                return;
            }
            if (stringPlayers.Any(p => p.IsDead))
            {
                Release();
                return;
            }
            if (IsDeadObject)
            {
                Inactivate();
                return;
            }
            Lines.Do(pair => pair.Value.SetPositions(new Il2CppStructArray<UnityEngine.Vector3>([pair.Key.Item1.Position.ToUnityVector(), pair.Key.Item2.Position.ToUnityVector()])));
        }

        public void Inactivate()
        {
            Lines.Do(pair => GameObject.Destroy(pair.Value));
            Lines.Clear();
        }

        void IGameOperator.OnReleased()
        {
            Inactivate();
        }

    }
}
public class Spined : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation, IAssignableDocument
{
    private Spined() : base("spined", new Virial.Color(255, 129, 56), null, true, () => false) { }

    Citation? HasCitation.Citation => PCitations.PlanaANDKC;

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static public Spined MyRole = new Spined();
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        //bool RuntimeAssignable.CanBeAwareAssignment => NebulaGameManager.Instance.CanSeeAllInfo;
        public Instance(GamePlayer player) : base(player)
        {
        }
        [OnlyMyPlayer]
        void CheckExtraWin(PlayerCheckExtraWinEvent ev)
        {
            if (ev.Player.PlayerState == Spinner.EndofFate && ev.GameEnd == Spinner.Instance.SpinnerWin)
            {
                ev.SetWin(true);
            }
        }
        void RuntimeAssignable.OnActivated()
        {
        }
    }
}
