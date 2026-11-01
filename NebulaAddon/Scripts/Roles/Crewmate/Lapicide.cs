using static UnityEngine.GraphicsBuffer;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Plana.Roles.Crewmate;

[NebulaRPCHolder]
public class Lapicide : DefinedSingleAbilityRoleTemplate<Lapicide.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    private Lapicide() : base("lapicide", new(110, 132, 109), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [MeetingSkillCount, EveryMeetingSkillCount]) { }
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    static IntegerConfiguration MeetingSkillCount = NebulaAPI.Configurations.Configuration("options.role.lapicide.meetingskillcount", (5, 15,1), 6);
    static IntegerConfiguration EveryMeetingSkillCount = NebulaAPI.Configurations.Configuration("options.role.lapicide.everymeetingskillcount", (2, 6,1),2);
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0), arguments.Get(1, MeetingSkillCount));
    static public Lapicide MyRole = new Lapicide();
    bool IAssignableDocument.HasTips => true;
    public static RemoteProcess<(GamePlayer, int)> UpdateCWArguments = new("UpdateCWArgumentsRPC", (m, _) =>
    {
        if (m.Item1.TryGetAbility<Ability>(out var a))
        {
            a.initialSkillCount = m.Item2;
        }
    });
    public static void LoadPatch(Harmony harmony)
    {
        PDebug.Log("Lapicide Patch");
        harmony.Patch(typeof(PlayerVoteArea).GetMethod(nameof(PlayerVoteArea.CanBeHighlighted)), null, new HarmonyMethod(typeof(Lapicide).GetMethod("LapicideCanBeHighlightedPatch")));
        harmony.Patch(typeof(MeetingHudExtension).GetMethod("CanVoteFor", new Type[] { typeof(byte) }), null, new HarmonyMethod(typeof(Lapicide).GetMethod("LapicideCanUseSkillFor")));
        PDebug.Log("Done");
    }
    public static void LapicideCanUseSkillFor(byte playerId, ref bool __result)
    {
        if (NebulaAPI.CurrentGame != null && GamePlayer.LocalPlayer!.Role.GetAbility<Lapicide.Ability>() != null && !MeetingHudExtension.CanVote)
        {
            if (!__result)
            {
                Player player = Player.GetPlayer(playerId);
                if (player != null && MeetingHudExtension.CanUseAbilityForByMask(playerId))
                {
                    __result = true;
                }
            }
        }
    }
    public static void LapicideCanBeHighlightedPatch(PlayerVoteArea __instance, ref bool __result)
    {
        if (NebulaAPI.CurrentGame != null && GamePlayer.LocalPlayer!.Role.GetAbility<Lapicide.Ability>() != null && !MeetingHudExtension.CanVote)
        {
            if (!__result && __instance.AmDead && !AmongUsLLImpl.LocalPlayer.Data.IsDead && __instance.Parent != null && __instance.Parent.CurrentState != MeetingHud.MeetingStates.Discussion)
            {
                __result = true;
            }
        }
    }
    [NebulaRPCHolder]
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static Virial.Media.Image? UnSkillButton = NebulaAPI.AddonAsset.GetResource("CLapicideUnSkill.png")!.AsImage(115f)!;
        static Virial.Media.Image? InSkillButton = NebulaAPI.AddonAsset.GetResource("CLapicideInSkill.png")!.AsImage(115f)!;
        static Virial.Media.Image? MeetingInfoIcon = NebulaAPI.AddonAsset.GetResource("LapicideMeetingInfo.png")!.AsImage(135f)!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt(), initialSkillCount];
        public static RemoteProcess<(string, string, string)> RegisterOverlay = new("LapicideReceiveOverlayRPC", (m, _) =>
        {
            try
            {
                var sb = new System.Text.StringBuilder().Append(Language.Translate("role.lapicide.message.deadPlayer")).Append(":").Append(m.Item1).Append(Environment.NewLine).Append(Language.Translate("role.lapicide.message.playerRole")).Append(":").Append(m.Item2).Append(Environment.NewLine).Append(Language.Translate("role.lapicide.message.deadReason")).Append(":").Append(m.Item3);
                NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaAPI.GUI.VerticalHolder(GUIAlignment.Left, [
                NebulaAPI.GUI.LocalizedText(GUIAlignment.Left,NebulaAPI.GUI.GetAttribute(AttributeAsset.OverlayTitle),"role.lapicide.message.header"),
                                    NebulaAPI.GUI.RawText(GUIAlignment.Center,NebulaAPI.GUI.GetAttribute(AttributeAsset.OverlayContent),sb.ToString())
                ]), MeetingInfoIcon, MyRole.RoleColor, false);
            }
            catch (Exception e)
            {
                PDebug.Log("LapicideReceiveOverlayError:" + e);
            }
        });
        public Ability(GamePlayer player, bool isUsurped, int initialskillCount) : base(player, isUsurped)
        {
            initialSkillCount = initialskillCount;
            if (AmOwner)
            {
                GameOperatorManager.Instance?.RegisterOnReleased(() =>
                {
                    MeetingHudExtension.CanVote = true;
                }, this);
                string prefix = Language.Translate("role.Lapicide.leftskillprefix");
                Helpers.TextHudContent("LapicideText", this, delegate (TextMeshPro tmPro)
                {
                    tmPro.text = prefix + ": " + initialSkillCount;
                }, true);
            }
        }
        GamePlayer? selectedDeadPlayer;
        public int initialSkillCount;
        public int meetingCount;
        FlexibleLifespan lifespan;
        Dictionary<byte, SpriteRenderer> highlights;
        private List<byte> deadplayers = new List<byte>();
        private List<byte> confirmedplayers = new List<byte>();
        private List<byte>knownplayers= new List<byte>();
        [Local]
        void ReflectRoleName(PlayerSetFakeRoleNameEvent ev)
        {
            if (deadplayers.Contains(ev.Player.PlayerId))
            {
                ev.Append(ev.Player.Role.DisplayColoredName);
            }
        }
        static readonly RemoteProcess<ValueTuple<GamePlayer, byte>> RpcUpdateStatus = new RemoteProcess<ValueTuple<GamePlayer, byte>>("UpdateLapicide", (m, _) =>
        {
            if (m.Item1.TryGetAbility<Lapicide.Ability>(out var ability))
            {
                ability.knownplayers.Add(m.Item2);
            }
        }, true);
        [Local, OnlyMyPlayer]
        void OnGuess(PlayerKillPlayerEvent ev)
        {
            if (ev.Dead.PlayerState == PlayerState.Guessed && !deadplayers.Contains(ev.Dead.PlayerId))
            {
                deadplayers.Add(ev.Dead.PlayerId);
                RpcUpdateStatus.Invoke((MyPlayer, ev.Dead.PlayerId));
            }
        }
        [Local]
        void OnExile(PlayerExiledEvent ev)
        {
            if (ev.Player.PlayerState == PlayerState.Exiled && !deadplayers.Contains(ev.Player.PlayerId))
            {
                deadplayers.Add(ev.Player.PlayerId); 
                RpcUpdateStatus.Invoke((MyPlayer, ev.Player.PlayerId));
            }
        }
        GUIWidget IPlayerAbility.ProgressWidget
        {
            get
            {
                GUIWidget[] array = new GUIWidget[2];
                array[0] = ProgressGUI.OneLineText([Language.Translate("role.lapicide.gui.results")]);
                if (knownplayers.Count > 0)
                {
                    array[1] = ProgressGUI.Holder(from p in Player.AllOrderedPlayers
                                                  where knownplayers.Contains(p.PlayerId)
                                                  select ProgressGUI.OneLineText([p.ColoredName + ": " + p.Role.DisplayColoredName]));
                }
                else
                {
                    array[1] = ProgressGUI.OneLineText([Language.Translate("role.lapicide.gui.none")]);
                }
                return ProgressGUI.Holder(array);
            }
        }
        [Local]
        void OnUpdate(GameUpdateEvent ev)
        {
            if (MyPlayer.IsDead)
            {
                deadplayers.Clear();
                return;
            }
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            meetingCount = EveryMeetingSkillCount;
            selectedDeadPlayer = null;
            if (MyPlayer.IsDead)
            {
                deadplayers.Clear();
                return;
            }
            if (initialSkillCount <= 0)
            {
                return;
            }
            ev.CanVote = true;
            if (lifespan != null && !lifespan.IsDeadObject)
            {
                lifespan.Release();
            }
            lifespan = new FlexibleLifespan(this);
            highlights = new Dictionary<byte, SpriteRenderer>();
            foreach (var pva in MeetingHud.Instance.playerStates)
            {
                var highlight = GameObject.Instantiate(pva.HighlightedFX, pva.transform);
                highlight.enabled = false;
                highlight.color = UnityEngine.Color.red;
                highlight.transform.localPosition = new(-0.0155f, 0.0127f, -2.8f);
                highlights[pva.PlayerId] = highlight;
                PDebug.Log("AddLapicideHightlight:" + pva.PlayerId);
            }
            void UpdateHighlight(byte player, GamePlayer nPlayer)
            {
                PDebug.Log(nPlayer.Name+nPlayer.IsDead+nPlayer.PlayerState);
                try
                {
                    PDebug.Log("LapicideUpdateHightlight");
                    if (selectedDeadPlayer == null)
                    {
                        PDebug.Log("DPlayer select");
                        if (nPlayer.IsDead&&!confirmedplayers.Contains(nPlayer.PlayerId))
                        {
                            selectedDeadPlayer  = nPlayer;
                            var highlight = highlights[player];
                            highlight.color = UnityEngine.Color.red;
                            highlight.enabled = true;
                        }
                    }
                    else if (selectedDeadPlayer.PlayerId == player)
                    {
                        PDebug.Log("Unselect dplayer");
                        selectedDeadPlayer = null;
                        highlights[player].enabled = false;
                        highlights.Do(entry => entry.Value.enabled = false);
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log("LapicideUpdateHighlightError:" + e);
                }
            }
            var skipButton = MeetingHud.Instance.SkipVoteButton;
            GameObject binder = UnityHelper.CreateObject("LapicideButton", MeetingHud.Instance.SkipVoteButton.transform.parent, MeetingHud.Instance.SkipVoteButton.transform.localPosition, null);
            GameOperatorManager.Instance?.Subscribe<GameUpdateEvent>(delegate (GameUpdateEvent ev)
                    {
                        if (binder == null)
                        {
                            return;
                        }
                        binder.gameObject.SetActive(!this.MyPlayer.IsDead && MeetingHud.Instance.CurrentState == MeetingHud.MeetingStates.NotVoted && binder&&initialSkillCount>0&&meetingCount>0);
                    }, lifespan, 100);
            GameOperatorManager.Instance?.Subscribe<InvokeVoteAlternateEvent>(ev =>
            {
                if (ev.Player == MyPlayer)
                {
                    return;
                }
                if (IsUsurped)
                {
                    MeetingHudExtension.CanVote = true;
                    UnityEngine.Object.Destroy(binder);
                    highlights.Do(entry => entry.Value.gameObject.SetActive(false));
                    return;
                }
                UpdateHighlight(ev.Player.PlayerId, ev.Player);
                VanillaAsset.PlaySelectSE();
            }, lifespan);
            SpriteRenderer leftRenderer = UnityHelper.CreateObject<SpriteRenderer>("Lapicide-UseSkill", binder.transform, new Vector3(2.1f, 0.15f), null);
            leftRenderer.sprite = UnSkillButton.GetSprite();
            PassiveButton passiveButton = leftRenderer.gameObject.SetUpButton(true, new SpriteRenderer[0], null, null);
            passiveButton.OnMouseOver.AddListener(delegate
            {
                leftRenderer.color = new Color32(146, 164, 204, 255);
            });
            passiveButton.OnMouseOut.AddListener(delegate
            {
                leftRenderer.color = UnityEngine.Color.white;
            });
            passiveButton.OnClick.AddListener(delegate
            {
                if (IsUsurped)
                {
                    MeetingHudExtension.CanVote = true;
                    UnityEngine.Object.Destroy(binder);
                    highlights.Do(entry => entry.Value.gameObject.SetActive(false));
                    return;
                }
                if (selectedDeadPlayer != null)
                {
                    if (confirmedplayers.Contains(selectedDeadPlayer.PlayerId))
                    {
                        return;
                    }
                    initialSkillCount--;
                    meetingCount--;
                    UpdateCWArguments.Invoke((MyPlayer, initialSkillCount));
                    if (!deadplayers.Contains(selectedDeadPlayer.PlayerId))
                    {
                        deadplayers.Add(selectedDeadPlayer.PlayerId);
                        RpcUpdateStatus.Invoke((MyPlayer, selectedDeadPlayer.PlayerId));
                        var ev = new PlayerSetFakeRoleNameEvent(selectedDeadPlayer, true);
                        ReflectRoleName(ev);
                    }
                    else
                    {
                        if (!confirmedplayers.Contains(selectedDeadPlayer.PlayerId))
                        {
                            confirmedplayers.Add(selectedDeadPlayer.PlayerId);
                            RegisterOverlay.Invoke((selectedDeadPlayer.ColoredName, selectedDeadPlayer.Role.DisplayColoredName, selectedDeadPlayer.PlayerState.Text));
                        }
                    }
                    selectedDeadPlayer = null;
                    MeetingHudExtension.CanVote = true;
                    highlights.Do(entry => entry.Value.enabled = false);
                    leftRenderer.sprite = UnSkillButton?.GetSprite();
                    MeetingHudExtension.ReflectVotingMask();
                    if (initialSkillCount <= 0 || meetingCount <= 0)
                    {
                        MeetingHudExtension.CanVote = true;
                        UnityEngine.Object.Destroy(binder);
                        highlights.Do(entry => entry.Value.gameObject.SetActive(false));
                        MeetingHudExtension.ReflectVotingMask();
                    }
                }
                else
                {
                    try
                    {
                        MeetingHudExtension.CanVote = !MeetingHudExtension.CanVote;
                        if (MeetingHudExtension.CanVote)
                        {
                            leftRenderer.sprite = UnSkillButton?.GetSprite();
                            highlights.Do(entry => entry.Value.gameObject.SetActive(false));
                        }
                        else
                        {
                            leftRenderer.sprite = InSkillButton?.GetSprite();
                            highlights.Do(entry => entry.Value.gameObject.SetActive(true));
                        }
                    }
                    catch (Exception e)
                    {
                        PDebug.Log(e);
                    }
                    MeetingHudExtension.ReflectVotingMask();
                }
            });
            leftRenderer.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.6f);
        }
    }
}
