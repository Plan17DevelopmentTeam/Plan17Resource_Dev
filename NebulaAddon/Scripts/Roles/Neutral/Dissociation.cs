using Nebula.Game.Statistics;
using Nebula.Roles.Complex;
using Nebula.Roles.Crewmate;
using Plana.Roles.Crewmate;
using Plana.Roles.Impostor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using Virial;
using Virial.Assignable;
using Virial.Components;
using Virial.Configuration;
using Virial.DI;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Helpers;

namespace Plana.Roles.Neutral;
[NebulaRPCHolder]
internal class Dissociation : DefinedRoleTemplate, DefinedRole,DefinedAssignable, IAssignableDocument,HasCitation
{
    static readonly public RoleTeam MyTeam = NebulaAPI.Preprocessor!.CreateTeam("teams.dissociation", new(229,126,138), TeamRevealType.OnlyMe);
    int[]? DefinedAssignable.DefaultAssignableArguments => [0, Nebula.Roles.Roles.AllRoles.Where(r => r.Category == RoleCategory.CrewmateRole && !r.IsSystemRole && r.IsSpawnableInSomeForm() && RoleFilterOption.Contains(r)).Shuffle().First().Id];
    private Dissociation() : base("dissociation", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [KillCooldown,AwareNeedAlivePlayerNum,RoleFilterOption])
    {
    }
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    bool DefinedRole.IsKiller => true;
    public static IRelativeCooldownConfiguration KillCooldown = NebulaAPI.Configurations.KillConfiguration("options.role.dissociation.killCooldown", CoolDownType.Relative, (0f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), +2.5f, (0.125f, 2f, 0.125f), 1.125f);
    public static IntegerConfiguration AwareNeedAlivePlayerNum = NebulaAPI.Configurations.Configuration("options.role.dissociation.awareneedaliveplayernum", (1, 24), 12);
    public static readonly SimpleRoleFilterConfiguration RoleFilterOption = new SimpleRoleFilterConfiguration("options.role.dissociation.abilityFilter")
    {
        RolePredicate = (DefinedRole r) => !r.IsSystemRole&&r.IsSpawnableInSomeForm()&&r.Category==RoleCategory.CrewmateRole&&r!=Sheriff.MyRole&&r!=Celestialdog.MyRole&&r!=Knight.MyRole&&r!=Assistant.MyRole&&r!=Dunner.MyRole&&r!=Insomniacs.MyRole&&r!=Shifter.MyRole&&r!=Secret.MyNiceRole&&r!=Agent.MyRole&&r!=Engineer.MyRole&&r!=Madmate.MyRole,
        ScrollerTag = "dissociationFilter",
        InvertOption = true,
        PreviewOnlySpawnableRoles = false
    };

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments.GetAsBool(0, false), Nebula.Roles.Roles.GetRole(arguments.Get(1,-1)));
    public static readonly RemoteProcess<GamePlayer> ShareMyAwareInfo = new("shareMyAwareInfoDissociationRPC", (m, _) =>
    {
        if (m.Role is Dissociation.Instance instance)
        {
            instance.Cognize();
        }
    });
    static public Dissociation MyRole = new();
    bool IAssignableDocument.HasTips => true;
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        public static GameEnd DissociationWin = NebulaAPI.Preprocessor!.CreateEnd("dissociation", MyTeam.Color);
        DefinedRole RuntimeRole.Role => MyRole;
        public DefinedRole myExtRole { get; set; }
        public RuntimeRole ExtRoleInstance { get; set; }
        public Instance(GamePlayer player, bool aware, DefinedRole myExtRole) : base(player)
        {
            amAware = aware;
            if (myExtRole != null)
            {
                this.myExtRole = myExtRole;
            }
            else
            {
                PDebug.Log("Dissociation InputRole is null");
                this.myExtRole = Nebula.Roles.Roles.AllRoles.Where(r => r.Category == RoleCategory.CrewmateRole && !r.IsSystemRole && r.IsSpawnableInSomeForm() && RoleFilterOption.Contains(r)).Shuffle().First();
            }
        }

        int[]? RuntimeAssignable.RoleArguments => [amAware.AsInt(),myExtRole?.Id??-1];
        bool RuntimeRole.CanUseVent => amAware;
        bool RuntimeRole.CanMoveInVent => amAware;
        bool RuntimeRole.HasImpostorVision => !amAware?ExtRoleInstance?.HasImpostorVision??false:amAware;
        bool RuntimeRole.IgnoreBlackout => !amAware?ExtRoleInstance?.IgnoreBlackout??false:amAware;
        bool RuntimeRole.CheckGuessAbility(DefinedRole abilityRole) => abilityRole == myExtRole || abilityRole == MyRole;
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var myTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => amAware&&ObjectTrackers.PlayerlikeLocalKillablePredicate(p), MyTeam.Color.ToUnityColor(), Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption);
                var killButton = NebulaAPI.Modules.PlayerlikeKillButton(this, MyPlayer, new PlayerInteractParameter(false, true, true), true, Virial.Compat.VirtualKeyInput.Kill, null,
                    KillCooldown.Cooldown, "kill", ModAbilityButton.LabelType.Impostor, null!,
                    (target, _) =>
                    {
                        var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, target));
                        if (!(cancelable?.IsCanceled ?? false))
                        {
                            MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                        }
                        if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    },
                    myTracker,
                    _ => !MyPlayer.IsDived,
                    _ => MyPlayer.AllowToShowKillButtonByAbilities&&amAware
                    );
                killButton.CoolDownTimer = new Dissociation.ReserveTimer(KillCooldown.Cooldown).SetAsKillCoolDown().Register(this);
                GameOperatorManager.Instance?.Subscribe<TaskPhaseStartEvent>(ev =>
                {
                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        if (killButton.CoolDownTimer is ReserveTimer timer)
                        {
                            timer.SetTime(timer.Max);
                        }
                    });
                },this,-1);
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
            MyPlayer.FeelBeTrueCrewmate = !amAware;
            ExtRoleInstance = myExtRole.CreateInstance(MyPlayer, myExtRole.DefaultAssignableArguments??Array.Empty<int>());
            ExtRoleInstance.OnActivated();
            ExtRoleInstance.Register(this);
            GameOperatorManager.Instance?.RegisterOnReleased(() =>
            {
                ExtRoleInstance.Inactivate();
            }, this);
        }
        bool scheduled = false;
        [Local]
        void OnUpdate(GameUpdateEvent ev)
        {
            if (!amAware&&!scheduled)
            {
                if (NebulaGameManager.Instance?.AllPlayerInfo.Count(p => p.IsAlive) <= AwareNeedAlivePlayerNum)
                {
                    if (!MeetingHud.Instance.AsBoolFast())
                    {
                        scheduled = true;
                        NebulaGameManager.Instance.Scheduler.Schedule(RPCScheduler.RPCTrigger.PreMeeting, ShareMyAwareInfo.GetInvoker(MyPlayer));
                    }
                    else
                    {
                        ShareMyAwareInfo.Invoke(MyPlayer);
                    }
                }
            }
        }
        void OnMeetingEnd(TaskPhaseRestartEvent ev)
        {
            if (amAware||scheduled)
            {
                return;
            }
            if (NebulaGameManager.Instance?.AllPlayerInfo.Count(p => p.IsAlive) <= AwareNeedAlivePlayerNum)
            {
                ShareMyAwareInfo.Invoke(MyPlayer);
            }
        }
        public void Cognize()
        {
            if (amAware)
            {
                return;
            }
            if (!amAware)
            {
                amAware = true;
                base.MyPlayer.FeelBeTrueCrewmate = false;
            }
            if (AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0.4f, 1f, 0.75f);
            }
        }

        [Local]
        void OnShowIntro(GameShowIntroLocalEvent ev)
        {
            ev.SetTeam(Nebula.Roles.Crewmate.Crewmate.MyTeam);
            ev.SetRole(myExtRole);
        }

        [OnlyMyPlayer]
        void OnCheckWin(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd ==DissociationWin&&!MyPlayer.IsDead);
        }

        void ClaimRemaining(KillerTeamCallback ev)
        {
            if (ev.ExcludedTeam == MyTeam) return;
            if (MyPlayer.IsAlive) ev.MarkRemaining();
        }

        [OnlyHost]
        void CheckKillWin(GameUpdateEvent ev)
        {
            if (MyPlayer.IsDead) return;
            if (!(ev.Game.GameMode?.AllowSpecialGameEnd ?? false)) return;

            //他陣営が生き残っていれば勝利しない
            if (GameOperatorManager.Instance?.Run(new KillerTeamCallback(MyTeam)).RemainingOtherTeam ?? false) return;
            
            int dissociations= 0;
            int totalAlive = 0;

            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo)
            {
                if (p.IsDead) continue;

                totalAlive++;
                if (p.Role.Role == MyRole) dissociations++;
            }

            if (totalAlive <= 2 && dissociations == 1) NebulaAPI.CurrentGame?.TriggerGameEnd(DissociationWin, GameEndReason.Situation, BitMasks.AsPlayer().Add(MyPlayer));
        }
        bool amAware = false;
        bool ShouldShowDissociationRole => amAware;
        bool ShouldShowAnnotatedRole => NebulaGameManager.Instance?.CanSeeAllInfo ?? false;

        string RuntimeAssignable.DisplayName =>
            ShouldShowDissociationRole ? (MyRole as DefinedRole).DisplayName+"("+myExtRole.DisplayName+")" :
            ShouldShowAnnotatedRole ? myExtRole.DisplayName + "(" + (MyRole as DefinedRole).DisplayName + ")" :
            myExtRole.DisplayName;
        string RuntimeAssignable.DisplayColoredName =>
            ShouldShowDissociationRole ? (MyRole as DefinedRole).DisplayColoredName + "(" + myExtRole.DisplayColoredName + ")" :
            ShouldShowAnnotatedRole ? myExtRole.DisplayColoredName + "(" + (MyRole as DefinedRole).DisplayColoredName + ")" :
            myExtRole.DisplayColoredName;

        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp => ShouldShowDissociationRole ? [MyRole,myExtRole] : [myExtRole];
        IEnumerable<IPlayerAbility> RuntimeAssignable.MyAbilities => ExtRoleInstance == null ? Array.Empty<IPlayerAbility>() : ExtRoleInstance.MyAbilities!;
        (Image, Virial.Color?, Virial.Color?)? RuntimeAssignable.OverriddenRoleIcon => (myExtRole.GetRoleIcon()!, myExtRole.Color, null);
        RoleTaskType RuntimeRole.TaskType => amAware ? RoleTaskType.NoTask : RoleTaskType.CrewmateTask;
        bool RuntimeAssignable.MyCrewmateTaskIsIgnored => true;
    }
    public class ReserveTimer : FlexibleLifespan, GameTimer, IGameOperator
    {
        private Func<bool>? predicate = null;
        private bool isActive;
        protected float currentTime;
        protected float min, max;

        public float Max => max;

        /// <summary>
        /// タイマーの進行を強制的に止めます。
        /// IsProgressingはfalseを返します。
        /// </summary>
        /// <returns></returns>
        public ReserveTimer StopForcely() => SetTime(min);

        public ReserveTimer Pause()
        {
            isActive = false;
            return this;
        }
        public virtual ReserveTimer Start(float? time = null)
        {
            isActive = true;
            currentTime = time.HasValue ? time.Value : min;
            return this;
        }
        public ReserveTimer Resume()
        {
            isActive = true;
            return this;
        }
        public ReserveTimer Reset()
        {
            currentTime = max;
            return this;
        }
        public ReserveTimer SetTime(float time)
        {
            currentTime = time;
            return this;
        }
        public ReserveTimer SetRange(float min, float max)
        {
            if (min > max)
            {
                this.max = min;
                this.min = max;
            }
            else
            {
                this.max = max;
                this.min = min;
            }
            return this;
        }
        public ReserveTimer Expand(float time)
        {
            this.max += time;
            return this;
        }

        public float CurrentTime { get => currentTime; }
        public virtual float Percentage { get => max > min ? 1- (currentTime - min) / (max - min) : 0f; }
        public bool IsProgressing => CurrentTime<max;

        void Update(UpdateEvent ev)
        {
            if (isActive && (predicate?.Invoke() ?? true))
            {
                float deltaTime = Time.fixedDeltaTime;
                if (AffectedByCooldownEffect)
                {
                    float coeff = GamePlayer.LocalPlayer?.Unbox().CalcAttributeVal(PlayerAttributes.CooldownSpeed, true) ?? 1f;
                    //if (coeff < 0f) coeff = 0f;
                    deltaTime *= coeff;
                }

                currentTime = Mathn.Clamp(currentTime + deltaTime, min, max);
            }
        }

        public ReserveTimer() : this(0f, 0f) { }
        public ReserveTimer(float max) : this(0f, max) { }

        public ReserveTimer(float min, float max)
        {
            SetRange(min, max);
            Reset();
            Pause();
        }

        GameTimer GameTimer.SetCondition(System.Func<bool> progressWhile) => SetPredicate(progressWhile);
        public ReserveTimer SetPredicate(Func<bool>? predicate)
        {
            this.predicate = predicate;
            return this;
        }

        public Func<bool>? Predicate => this.predicate;
        public bool AffectedByCooldownEffect = false;

        public ReserveTimer SetAsKillCoolDown()
        {
            AffectedByCooldownEffect = true;
            return SetPredicate(() => AmongUsLLImpl.LocalPlayer.IsKillTimerEnabled || AmongUsLLImpl.LocalPlayer.ForceKillTimerContinue);
        }

        public ReserveTimer SetAsAbilityCoolDown()
        {
            AffectedByCooldownEffect = true;
            return SetPredicate(() =>
            {
                if (AmongUsLLImpl.LocalPlayer.CanMove) return true;
                if (AmongUsLLImpl.LocalPlayer.inMovingPlat || AmongUsLLImpl.LocalPlayer.onLadder) return true;

                if (Minigame.Instance &&
                ((bool)Minigame.Instance.MyNormTask
                || Minigame.Instance.TryCast<SwitchMinigame>() != null
                || Minigame.Instance.TryCast<IDoorMinigame>() != null
                || Minigame.Instance.TryCast<VitalsMinigame>() != null
                || Minigame.Instance.TryCast<MultistageMinigame>() != null
                || Minigame.Instance.TryCast<AutoMultistageMinigame>() != null
                )) return true;
                return false;
            });
        }

        GameTimer GameTimer.SetAsKillCoolTimer() => SetAsKillCoolDown();

        GameTimer GameTimer.SetAsAbilityTimer() => SetAsAbilityCoolDown();

        IVisualTimer IVisualTimer.Start(float? time) => Start(time);

        GameTimer GameTimer.Pause() => Pause();

        GameTimer GameTimer.Resume() => Resume();

        GameTimer GameTimer.SetRange(float min, float max) => SetRange(min, max);

        GameTimer GameTimer.SetTime(float time) => SetTime(time);

        GameTimer GameTimer.Expand(float time) => Expand(time);
        string IVisualTimer.TimerText => currentTime <max&&currentTime>0 ? Mathn.CeilToInt(currentTime).ToString() : null;
        internal class TimerCoolDownHelper : IGameOperator
        {
            private ReserveTimer myTimer;
            public TimerCoolDownHelper(ReserveTimer timer)
            {
                this.myTimer = timer;
            }

            void ResetVentCoolDownOnTaskPhaseRestart(TaskPhaseRestartEvent ev) => myTimer?.Start();
            void ResetVentCoolDownOnGameStart(GameStartEvent ev) => myTimer?.Start();
        }

        GameTimer GameTimer.ResetsAtTaskPhase()
        {
            GameOperatorManager.Instance?.Subscribe(new TimerCoolDownHelper(this), this);
            return this;
        }
    }
}