using Nebula.Roles.Complex;
using Virial.Helpers;
using Virial.Runtime;
using static Plana.Roles.Impostor.OutlawLeader;

namespace Plana.Roles.Impostor;

[NebulaPreprocess(PreprocessPhase.PostRoles)]
public class OutlawTeamAllocator : AbstractModule<Game>, IGameOperator
{
    static void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule<Game>(() => new OutlawTeamAllocator());
    }

    private OutlawTeamAllocator()
    {
        this.RegisterPermanently();
    }

    void OnFixAssignmentTable(PreFixAssignmentEvent ev)
    {
        var leaders = ev.RoleTable.GetPlayers(OutlawTeam.MyRole).ToArray();

        for (int i = 0; i < leaders.Length; i++) ev.RoleTable.EditRole(leaders[i], (last) =>
        {
            return (OutlawLeader.MyRole, [leaders[i]]);
        });
    }
}

[NebulaRPCHolder]
internal class OutlawTeam : DefinedRoleTemplate, DefinedRole, IAssignableDocument
{
    static readonly public RoleTeam MyTeam = NebulaAPI.Preprocessor!.CreateTeam("teams.outlaw", Virial.Color.ImpostorColor, TeamRevealType.Teams);
    private OutlawTeam() : base("outlaw", Virial.Color.ImpostorColor, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [
        new GroupConfiguration("options.role.outlaw.assistantOptions",[AssistantShapeshiftCooldown,AssistantShapeshiftDuration,AssistantShapeshiftLeaveEvidence],new(0.7f, 0.2f, 0.2f))
        ,new GroupConfiguration("options.role.outlaw.dunnerOptions",[DunnerCleanCooldown,DunnerSyncKillAndCleanCoolDownOption],new(0.7f, 0.2f, 0.2f))],
        othersAssignments: () =>
        {
            return [new((_, playerId) => (Assistant.MyRole, [0, playerId]), RoleCategory.ImpostorRole), 
                    new((_, playerId) => (Dunner.MyRole, [0, playerId]), RoleCategory.ImpostorRole)];
        })
    {
    }

    static public readonly FloatConfiguration AssistantShapeshiftCooldown = NebulaAPI.Configurations.Configuration("options.role.outlaw.assistant.shapeshiftcooldown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static public readonly FloatConfiguration AssistantShapeshiftDuration = NebulaAPI.Configurations.Configuration("options.role.outlaw.assistant.shapeshiftduration", (0f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static public readonly BoolConfiguration AssistantShapeshiftLeaveEvidence = NebulaAPI.Configurations.Configuration("options.role.outlaw.assistant.shapeshiftleaveEvidence", false);
    static public readonly FloatConfiguration DunnerCleanCooldown = NebulaAPI.Configurations.Configuration("options.role.outlaw.dunner.cleancooldown", (0f, 60f, 2.5f), 25f, FloatConfigurationDecorator.Second);
    static public readonly BoolConfiguration DunnerSyncKillAndCleanCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.outlaw.dunner.syncKillAndCleanCoolDown", false, null, null);

    bool DefinedAssignable.ShowOnFreeplayScreen => false;
    bool IGuessed.CanBeGuess => false;
    DefinedRole[] DefinedRole.AdditionalRoles => [OutlawLeader.MyRole, Assistant.MyRole, Dunner.MyRole];

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);


    static public OutlawTeam MyRole = new();

    public static bool IsOutlaw(GamePlayer player) => player.Role is OutlawLeader.IOutlawTeam;

    public static bool OutlawCanKill(GamePlayer player)
    {
        return player.Role is OutlawLeader.Instance
            || player.Role is Assistant.Instance a && a.CanKill
            || player.Role is Dunner.Instance d && d.CanKill;
    }

    static public RemoteProcess<GamePlayer> RpcSetCanKill = new("RpcSetCanKill", (message, _) =>
    {
        var role = message.Role;
        if (role is Assistant.Instance assistant) assistant.CanKill = true;
        if (role is Dunner.Instance dunner) dunner.CanKill = true;
    });

    public class Instance(GamePlayer player) : RuntimeAssignableTemplate(player), RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.Role => MyRole;

        void RuntimeAssignable.OnActivated()
        {
        }
    }
}

internal class OutlawLeader : DefinedRoleTemplate, DefinedRole, IAssignableDocument, HasCitation
{
    private OutlawLeader() : base("outlawLeader", Virial.Color.ImpostorColor, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [], false, false)
    {
    }

    bool DefinedAssignable.ShowOnHelpScreen => false;
    bool ISpawnable.IsSpawnable => (OutlawTeam.MyRole as ISpawnable).IsSpawnable;
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, (byte)arguments.Get(0, player.PlayerId));


    static public OutlawLeader MyRole = new();

    public interface IOutlawTeam
    {
        int LeaderId { get; }

        OutlawLeader.Instance? MyLeader { get; }
        internal OutlawLeader.Instance? GetMyLeader(ref GamePlayer? cache)
        {
            if (cache != null && cache.Role is OutlawLeader.Instance leader && leader.LeaderId == LeaderId) return leader;
            else cache = null;

            if (cache == null) if (NebulaGameManager.Instance?.AllPlayerInfo.Find(p => p.Role is OutlawLeader.Instance l && l.LeaderId == this.LeaderId, out var found) ?? false) cache = found;
            if (cache == null) return null;
            return cache.Role as OutlawLeader.Instance;
        }
    }

    public class Instance(GamePlayer player, int id) : RuntimeAssignableTemplate(player), IOutlawTeam, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        public int LeaderId { get; internal set; } = id;
        public OutlawLeader.Instance? MyLeader => this;
        DefinedRole RuntimeRole.Role => MyRole;

        int[] RuntimeAssignable.RoleArguments => [LeaderId];

        public bool IsSameTeam(GamePlayer? player)
        {
            if (player == null) return false;
            if (player.Role is OutlawLeader.IOutlawTeam outlaw && outlaw.LeaderId == LeaderId) return true;
            return false;
        }

        void RuntimeAssignable.OnActivated()
        {
        }

        void RuntimeAssignable.OnInactivated()
        {
            var players = NebulaGameManager.Instance?.AllPlayerInfo.Where(p => !p.IsDead && IsSameTeam(p) && p.Role.Role != MyRole).ToList();
            if (players == null) return;
            var random = players[System.Random.Shared.Next(players.Count)];
            OutlawTeam.RpcSetCanKill.Invoke(random);

            if (MyPlayer.Modifiers.Any(r => r is GuesserModifier))
                random.AddModifier(GuesserModifier.MyRole);
        }

        void OnIntro(GameShowIntroLocalEvent ev)
        {
            if (ev.RelatedTeam == OutlawTeam.MyTeam) 
                ev.AddPlayer(MyPlayer);

            if (MyPlayer.AmOwner)
            {
                if (ev.RelatedTeam == NebulaTeams.ImpostorTeam)
                {
                    var players = NebulaGameManager.Instance?.AllPlayerInfo.Where(p => !p.IsDead && IsSameTeam(p) && p.Role.Role != MyRole).ToList();
                    players.Do(p => ev.AddPlayer(p));
                } 
            }
        }

        [OnlyMyPlayer]
        void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (IsSameTeam(ev.Target))
            {
                ev.SetAsCannotKillBasically();
            }
        }
        [Local]
        void DecorateNameColor(PlayerDecorateNameEvent ev)
        {
            if (IsSameTeam(ev.Player))
            {
                ev.Color = NebulaTeams.ImpostorTeam.Color;
            }
        }
        [Local]
        void ShowRoleText(PlayerSetFakeRoleNameEvent ev)
        {
            if (ev.Player.PlayerId != MyPlayer.PlayerId && IsSameTeam(ev.Player))
            {
                ev.Alternate(ev.Player.Role.Role.DisplayColoredName);
            }
        }

        [OnlyHost]
        [OnlyMyPlayer]
        void OnLeaderDie(PlayerDieOrDisconnectEvent ev)
        {
            var players = GamePlayer.AllPlayers.Where(p => !p.IsDead && IsSameTeam(p) && p.Role.Role != MyRole).ToList();
            if (ev is PlayerExiledEvent)
            {
                players.Do(p => p.VanillaPlayer.ModMarkAsExtraVictim(null, PlayerState.Suicide, PlayerState.Suicide));
                return;
            }
            else if (players?.Count > 1)
            {
                var i = System.Random.Shared.Next(players.Count);
                players[i].Suicide(PlayerStates.Suicide, PlayerState.Suicide, KillParameter.RemoteKill);

                players.RemoveAt(i);
            }

            if (players == null) return;
            var random = players[System.Random.Shared.Next(players.Count)];

            OutlawTeam.RpcSetCanKill.Invoke(random);
        }

        string RuntimeAssignable.DisplayName => (MyRole as DefinedRole).DisplayName + " (" + (OutlawTeam.MyRole as DefinedRole).DisplayName + ")";
        string RuntimeAssignable.DisplayColoredName => (MyRole as DefinedRole).DisplayColoredName + " (" + (OutlawTeam.MyRole as DefinedRole).DisplayColoredName + ")";
    }
}

internal class Assistant : DefinedRoleTemplate, DefinedRole, IAssignableDocument, HasCitation
{
    private Assistant() : base("assistant", Virial.Color.ImpostorColor, RoleCategory.ImpostorRole, OutlawTeam.MyTeam, [], false, false)
    {

    }

    bool DefinedAssignable.ShowOnHelpScreen => false;
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    bool ISpawnable.IsSpawnable => (OutlawTeam.MyRole as ISpawnable).IsSpawnable;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments.GetAsBool(0), arguments.Get(1, 0));

    RoleCategory DefinedCategorizedAssignable.Category
    {
        get
        {
            if (GamePlayer.LocalPlayer == null)
            {
                return RoleCategory.ImpostorRole;
            }
            bool cankill = false;
            var myplayer = GamePlayer.AllPlayers.FirstOrDefault(p => p.Role is Assistant.Instance);
            if (myplayer != null && myplayer.Role is Assistant.Instance assistant)
            {
                cankill = assistant.CanKill;
            }
            return cankill ? RoleCategory.ImpostorRole : RoleCategory.CrewmateRole;
        }
    }
    RoleTeam DefinedSingleAssignable.Team
    {
        get
        {
            if (GamePlayer.LocalPlayer == null)
            {
                return OutlawTeam.MyTeam;
            }
            bool cankill = false;
            var myplayer = GamePlayer.AllPlayers.FirstOrDefault(p => p.Role is Assistant.Instance);
            if (myplayer != null && myplayer.Role is Assistant.Instance assistant)
            {
                cankill = assistant.CanKill;
            }
            return cankill ? NebulaTeams.ImpostorTeam : OutlawTeam.MyTeam;
        }
    }

    static public Assistant MyRole = new();

    public class Instance(GamePlayer player, bool canKill, int leaderId) : RuntimeAssignableTemplate(player), OutlawLeader.IOutlawTeam, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.Role => MyRole;
        RoleTaskType RuntimeRole.TaskType => RoleTaskType.NoTask;
        bool RuntimeRole.CanInvokeSabotage => true;
        bool RuntimeRole.CanMoveInVent => true;
        bool RuntimeRole.CanUseVent => true;
        bool RuntimeRole.HasImpostorVision => true;
        bool RuntimeRole.IgnoreBlackout => true;

        public int LeaderId { get; private set; } = leaderId;
        private GamePlayer? myLeaderCache = null;
        public OutlawLeader.Instance? MyLeader => (this as IOutlawTeam).GetMyLeader(ref myLeaderCache);

        public bool CanKill { get; internal set; } = canKill;

        bool RuntimeRole.HasVanillaKillButton => CanKill;
        int[] RuntimeAssignable.RoleArguments => [CanKill.AsInt(), LeaderId];

        ModAbilityButton? ShapeshiftButton;
        public void RevertPlayerToNormal(bool shouldAnimate = true)
        {
            if (MyPlayer.VanillaPlayer.CurrentOutfitType == PlayerOutfitType.Shapeshifted || MeetingHud.Instance.AsBoolFast())
            {
                VanillaRpc.ShapeshiftRpc.Invoke((MyPlayer, MyPlayer, shouldAnimate, OutlawTeam.AssistantShapeshiftLeaveEvidence));
            }
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                ShapeshiftButton = NebulaAPI.Modules.EffectButton(this, base.MyPlayer, VirtualKeyInput.Ability, null, OutlawTeam.AssistantShapeshiftCooldown, OutlawTeam.AssistantShapeshiftDuration, "assistant.shapeshift", new WrapSpriteLoader(() => AmongUsUtil.GetRolePrefab<ShapeshifterRole>()!.Ability.Image), (ModAbilityButton _) => MyPlayer.CanMove, (ModAbilityButton _) => !MyPlayer.IsDead, false, true).SetLabelType(ModAbilityButton.LabelType.Impostor);
                ShapeshiftButton.OnClick = (button) =>
                {
                    if (!button.IsInEffect)
                    {
                        var shapeshift = AmongUsUtil.GetRolePrefab<ShapeshifterRole>();
                        MyPlayer.VanillaPlayer.NetTransform.Halt();
                        ShapeshifterMinigame shapeshifterMinigame = UnityEngine.Object.Instantiate<ShapeshifterMinigame>(shapeshift?.ShapeshifterMenu!);
                        shapeshifterMinigame.transform.SetParent(Camera.main.transform, false);
                        shapeshifterMinigame.transform.localPosition = new UnityEngine.Vector3(0f, 0f, -50f);
                        shapeshifterMinigame.Begin(null);
                    }
                    else
                    {
                        button.InterruptEffect();
                    }
                };
                ShapeshiftButton.OnEffectStart = (button) =>
                {
                    button.SetLabel("almighty.unshapeshift");
                };
                ShapeshiftButton.OnEffectEnd = (button) =>
                {
                    if (MyPlayer.VanillaPlayer.CurrentOutfitType == PlayerOutfitType.Shapeshifted)
                    {
                        MyPlayer.VanillaPlayer.NetTransform.Halt();
                        RevertPlayerToNormal(!MyPlayer.Logic.InVent);
                    }
                    button.StartCoolDown();
                    button.SetLabel("assistant.shapeshift");
                };
                (ShapeshiftButton.EffectTimer as TimerImpl)?.SetPredicate(() =>
                {
                    if (MyPlayer.VanillaPlayer.CurrentOutfitType == PlayerOutfitType.Shapeshifted && (ShapeshiftButton.EffectTimer?.IsProgressing ?? false))
                    {
                        return true;
                    }
                    return false;
                });
                (ShapeshiftButton.CoolDownTimer as TimerImpl)?.SetPredicate(() =>
                {
                    if (MyPlayer.VanillaPlayer.CurrentOutfitType == PlayerOutfitType.Default && (ShapeshiftButton.CoolDownTimer?.IsProgressing ?? false) && AmongUsLLImpl.LocalPlayer.IsKillTimerEnabled)
                    {
                        return true;
                    }
                    return false;
                });
            }
            GameOperatorManager.Instance?.Subscribe<MeetingPreStartEvent>(ev =>
            {
                MyPlayer.Unbox().RemoveOutfit("Shapeshift");
                MyPlayer.VanillaPlayer.CurrentOutfitType = PlayerOutfitType.Default;
                if (MyPlayer.AmOwner)
                {
                    ShapeshiftButton?.InterruptEffect();
                }
            }, this);
        }

        void RuntimeAssignable.OnInactivated()
        {
            if (AmOwner)
            {
                RevertPlayerToNormal(!MyPlayer.Logic.InVent);
            }
        }

        [Local]
        [EventPriority(EventPriority.Low)]
        void OnLeaderDead(PlayerDieOrDisconnectEvent ev)
        {
            if (MyPlayer.IsDead) return;
            NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();

            if (ev.Player.PlayerId == myLeaderCache?.PlayerId && CanKill) 
            {
                if (ev.Player.Modifiers.Any(r => r is GuesserModifier))
                    MyPlayer.AddModifier(GuesserModifier.MyRole);
            }
        }

        [OnlyMyPlayer]
        void BlockWins(PlayerBlockWinEvent ev)
        {
            ev.IsBlocked |= ev.GameEnd == NebulaGameEnd.CrewmateWin;
        }

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == NebulaGameEnd.ImpostorWin);

        [OnlyMyPlayer]
        void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (MyLeader?.IsSameTeam(ev.Target) ?? false) ev.SetAsCannotKillBasically();
        }
        [Local]
        void DecorateNameColor(PlayerDecorateNameEvent ev)
        {
            if (MyLeader?.IsSameTeam(ev.Player) ?? false)
            {
                ev.Color = NebulaTeams.ImpostorTeam.Color;
            }
        }
        [Local]
        void ShowRoleText(PlayerSetFakeRoleNameEvent ev)
        {
            if (ev.Player.PlayerId != MyPlayer.PlayerId && (MyLeader?.IsSameTeam(ev.Player) ?? false))
            {
                ev.Alternate(ev.Player.Role.Role.DisplayColoredName);
            }
        }

        string RuntimeAssignable.DisplayName => (MyRole as DefinedRole).DisplayName + " (" + (OutlawTeam.MyRole as DefinedRole).DisplayName + ")";
        string RuntimeAssignable.DisplayColoredName => (MyRole as DefinedRole).DisplayColoredName + " (" + (OutlawTeam.MyRole as DefinedRole).DisplayColoredName + ")";
    }
}

internal class Dunner : DefinedRoleTemplate, DefinedRole, IAssignableDocument, HasCitation
{
    private Dunner() : base("dunner", Virial.Color.ImpostorColor, RoleCategory.ImpostorRole, OutlawTeam.MyTeam, [], false, false)
    {

    }

    bool DefinedAssignable.ShowOnHelpScreen => false;
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    bool ISpawnable.IsSpawnable => (OutlawTeam.MyRole as ISpawnable).IsSpawnable;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments.GetAsBool(0), arguments.Get(1, 0));

    RoleCategory DefinedCategorizedAssignable.Category
    {
        get
        {
            if (GamePlayer.LocalPlayer == null)
            {
                return RoleCategory.ImpostorRole;
            }
            bool cankill = false;
            var myplayer = GamePlayer.AllPlayers.FirstOrDefault(p => p.Role is Dunner.Instance);
            if (myplayer != null && myplayer.Role is Dunner.Instance dunner)
            {
                cankill = dunner.CanKill;
            }
            return cankill ? RoleCategory.ImpostorRole : RoleCategory.CrewmateRole;
        }
    }
    RoleTeam DefinedSingleAssignable.Team
    {
        get
        {
            if (GamePlayer.LocalPlayer == null)
            {
                return OutlawTeam.MyTeam;
            }
            bool cankill = false;
            var myplayer = GamePlayer.AllPlayers.FirstOrDefault(p => p.Role is Dunner.Instance);
            if (myplayer != null && myplayer.Role is Dunner.Instance dunner)
            {
                cankill = dunner.CanKill;
            }
            return cankill ? NebulaTeams.ImpostorTeam : OutlawTeam.MyTeam;
        }
    }

    static public Dunner MyRole = new();

    public class Instance(GamePlayer player, bool canKill, int leaderId) : RuntimeAssignableTemplate(player), OutlawLeader.IOutlawTeam, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.Role => MyRole;
        RoleTaskType RuntimeRole.TaskType => RoleTaskType.NoTask;
        bool RuntimeRole.CanInvokeSabotage => true;
        bool RuntimeRole.CanMoveInVent => true;
        bool RuntimeRole.CanUseVent => true;
        bool RuntimeRole.HasImpostorVision => true;
        bool RuntimeRole.IgnoreBlackout => true;

        public int LeaderId { get; private set; } = leaderId;
        private GamePlayer? myLeaderCache = null;
        public OutlawLeader.Instance? MyLeader => (this as IOutlawTeam).GetMyLeader(ref myLeaderCache);

        public bool CanKill { get; internal set; } = canKill;

        bool RuntimeRole.HasVanillaKillButton => CanKill;
        int[] RuntimeAssignable.RoleArguments => [CanKill.AsInt(), LeaderId];

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                ObjectTracker<Virial.Game.DeadBody> cleanTracker = ObjectTrackers.ForDeadBody(this, null, base.MyPlayer, d => true, null, null, false);
                ModAbilityButton cleanButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, VirtualKeyInput.Ability, "cleaner.clean", OutlawTeam.DunnerCleanCooldown, "clean", Cleaner.buttonSprite, (ModAbilityButton _) => cleanTracker.CurrentTarget != null, null, false);
                cleanButton.OnClick = delegate (ModAbilityButton button)
                {
                    Virial.Game.DeadBody? currentTarget = cleanTracker.CurrentTarget;

                    if (currentTarget == null) return;

                    NebulaGameManager? instance = NebulaGameManager.Instance;
                    instance?.RpcDoGameAction(MyPlayer, MyPlayer.Position, GameActionTypes.CleanCorpseAction);

                    if (currentTarget.Player.MyKiller == MyPlayer) new StaticAchievementToken("cleaner.common2");

                    AmongUsUtil.RpcCleanDeadBody(currentTarget, MyPlayer.PlayerId, EventDetail.Clean);
                    if (OutlawTeam.DunnerSyncKillAndCleanCoolDownOption)
                    {
                        NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    }
                    cleanButton.StartCoolDown();
                };
                GameOperatorManager.Instance?.Subscribe<PlayerKillPlayerEvent>(delegate (PlayerKillPlayerEvent ev)
                {
                    if (ev.Murderer.AmOwner)
                    {
                        if (OutlawTeam.DunnerSyncKillAndCleanCoolDownOption)
                        {
                            cleanButton.CoolDownTimer?.Start(null);
                            return;
                        }
                        if (cleanButton.CoolDownTimer?.CurrentTime < 5f)
                        {
                            ModAbilityButton cleanButton2 = cleanButton;
                            if (cleanButton2 == null)
                            {
                                return;
                            }
                            IVisualTimer coolDownTimer = cleanButton2.CoolDownTimer;
                            if (coolDownTimer == null)
                            {
                                return;
                            }
                            coolDownTimer.Start(new float?(5f));
                        }
                    }
                }, this, 100);
            }
        }

        [Local]
        [EventPriority(EventPriority.Low)]
        void OnLeaderDead(PlayerDieOrDisconnectEvent ev)
        {
            if (MyPlayer.IsDead) return;

            NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();

            if (ev.Player.PlayerId == myLeaderCache?.PlayerId && CanKill)
            {
                if (ev.Player.Modifiers.Any(r => r is GuesserModifier))
                    MyPlayer.AddModifier(GuesserModifier.MyRole);
            }
        }

        [OnlyMyPlayer]
        void BlockWins(PlayerBlockWinEvent ev)
        {
            ev.IsBlocked |= ev.GameEnd == NebulaGameEnd.CrewmateWin;
        }

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == NebulaGameEnd.ImpostorWin);

        [OnlyMyPlayer]
        void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (MyLeader?.IsSameTeam(ev.Target) ?? false) ev.SetAsCannotKillBasically();
        }
        [Local]
        void DecorateNameColor(PlayerDecorateNameEvent ev)
        {
            if (MyLeader?.IsSameTeam(ev.Player) ?? false)
            {
                ev.Color = NebulaTeams.ImpostorTeam.Color;
            }
        }
        [Local]
        void ShowRoleText(PlayerSetFakeRoleNameEvent ev)
        {
            if (ev.Player.PlayerId != MyPlayer.PlayerId && (MyLeader?.IsSameTeam(ev.Player) ?? false))
            {
                ev.Alternate(ev.Player.Role.Role.DisplayColoredName);
            }
        }

        string RuntimeAssignable.DisplayName => (Dunner.MyRole as DefinedRole).DisplayName + " (" + (OutlawTeam.MyRole as DefinedRole).DisplayName + ")";
        string RuntimeAssignable.DisplayColoredName => (Dunner.MyRole as DefinedRole).DisplayColoredName + " (" + (OutlawTeam.MyRole as DefinedRole).DisplayColoredName + ")";
    }
}