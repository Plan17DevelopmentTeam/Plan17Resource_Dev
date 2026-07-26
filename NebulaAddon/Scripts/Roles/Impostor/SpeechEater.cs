using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Modifier;

namespace Plana.Roles.Impostor;

public class SpeechEater : DefinedSingleAbilityRoleTemplate<SpeechEater.Ability>, DefinedRole, HasCitation, IAssignableDocument
{
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.KillersSide;
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.Allowed;
    bool AssignableFilterHolder.CanLoadDefault(DefinedAssignable assignable)
    {
        return base.CanLoadDefaultTemplate(assignable) && (BlockMyGuesserOption||!(assignable is Nebula.Roles.Complex.GuesserModifier));
    }
    private SpeechEater() : base("speechEater", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [NumOfEatOption, EatCooldownOption,CrewmateCanAddChatOption,NeutralCanAddChatOption,ImpostorCanAddChatOption,BlockMyGuesserOption]) { }
    public override SpeechEater.Ability CreateAbility(GamePlayer player, int[] arguments)
    {
        return new SpeechEater.Ability(player, arguments.GetAsBool(0),arguments.Get<int>(1,NumOfEatOption),arguments.Get(2,NumOfGuessOption));
    }
    internal static IntegerConfiguration NumOfEatOption = NebulaAPI.Configurations.Configuration("options.role.speecheater.numOfEat", (1, 15), 3);
    internal static FloatConfiguration EatCooldownOption = NebulaAPI.Configurations.Configuration("options.role.speecheater.cooldown", (5f, 45f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    internal static BoolConfiguration CrewmateCanAddChatOption = NebulaAPI.Configurations.Configuration("options.role.speecheater.crewmate", false);
    internal static BoolConfiguration NeutralCanAddChatOption = NebulaAPI.Configurations.Configuration("options.role.speecheater.neutral", true);
    internal static BoolConfiguration ImpostorCanAddChatOption = NebulaAPI.Configurations.Configuration("options.role.speecheater.impostor", true);
    internal static BoolConfiguration BlockMyGuesserOption = NebulaAPI.Configurations.Configuration("options.role.speecheater.blockmyguesser", false);
    internal static IConfiguration GuessableFilterEditorOption = NebulaAPI.Configurations.Configuration(() => null, () => NebulaAPI.GUI.LocalizedButton(0, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OptionsTitleHalf), "role.guesser.guessableFilter", delegate (GUIClickable _)
    {
        OpenFilterEditor();
    }), null);
    static internal IntegerConfiguration NumOfGuessOption = NebulaAPI.Configurations.Configuration("options.role.guesser.numOfGuess", (1, 15), 3);
    static internal IntegerConfiguration NumOfGuessPerMeetingOption = NebulaAPI.Configurations.Configuration("options.role.guesser.numOfGuessPerMeeting", (1, 15), 1);
    static public SpeechEater MyRole = new SpeechEater();
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(Ability.vanishImage, "role.SpeechEater.ability.eaterR");
    }
    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%C%", Language.Translate(CrewmateCanAddChatOption ? "role.SpeechEater.ability.crewmate" : "role.SpeechEater.ability.nocrewmate"));
        yield return new("%N%", Language.Translate(NeutralCanAddChatOption ? "role.SpeechEater.ability.neutral" : "role.SpeechEater.ability.noneutral"));
        yield return new("%I%", Language.Translate(ImpostorCanAddChatOption ? "role.SpeechEater.ability.impostor" : "role.SpeechEater.ability.noimpostor"));
        yield return new("%NUM%", NumOfEatOption.GetValue().ToString());
    }
    private static void OpenFilterEditor()
    {
        Nebula.Roles.RoleOptionHelper.OpenFilterScreen<DefinedRole>("guessableFilter", Nebula.Roles.Roles.AllRoles.Where(r => r.CanBeGuessDefault), r => r.CanBeGuess, (r, val) => r.CanBeGuessVariable!.CurrentValue = val, r => r.CanBeGuessVariable!.CurrentValue = !r.CanBeGuess);
    }
    public static void LoadPatch(Harmony harmony)
    {
        PDebug.Log("SpeechEater Patch");
        harmony.Patch(typeof(TextBoxTMP).GetMethod("SetText"), new HarmonyMethod(typeof(SpeechEater.Ability).GetMethod("StopChat")), null);
        PDebug.Log("Patch1");
        harmony.Patch(typeof(MeetingHud).GetMethod("Update"), null, new HarmonyMethod(typeof(SpeechEater.Ability).GetMethod("MeetingUpdate")));
        PDebug.Log("Patch2");
        harmony.Patch(typeof(ChatController).GetMethod("AddChat"), new HarmonyMethod(typeof(SpeechEater.Ability).GetMethod("StopAddChat"), 0), null);
        PDebug.Log("Patch3");
        harmony.Patch(typeof(ChatController).GetMethod("SendChat"), new HarmonyMethod(typeof(SpeechEater.Ability).GetMethod("CanChat")));
        PDebug.Log("Patch4");
        harmony.Patch(typeof(ChatController).GetMethod("SendFreeChat"), new HarmonyMethod(typeof(SpeechEater.Ability).GetMethod("CanChat")));
        PDebug.Log("Patch5");
        harmony.Patch(typeof(ChatController).GetMethod("SendQuickChat"), new HarmonyMethod(typeof(SpeechEater.Ability).GetMethod("CanChat")));
        PDebug.Log("Patch6");
        harmony.Patch(typeof(ChatController).GetMethod("UpdateChatMode"), null, new HarmonyMethod(typeof(SpeechEater.Ability).GetMethod("ChatUpdatePostfix")));
        PDebug.Log("Done");
    }
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility, IBindPlayer, IGameOperator, ILifespan
    {

        private ModAbilityButton? vanishButton = null;
        static internal Virial.Media.Image vanishImage = NebulaAPI.AddonAsset.GetResource("Silent.png")!.AsImage(115f)!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt(),leftskill,LeftGuess];
        int leftskill;
        public Ability(GamePlayer player, bool isUsurped, int leftskill,int leftguess)
            : base(player, isUsurped)
        {
            try
            {
                if (AmOwner)
                {
                    this.leftskill = leftskill;
                    LeftGuess = leftguess;
                    if (MyPlayer.Modifiers.Any(m=>m is Infected.Instance))
                    {
                        LeftGuess = Infected.InfectedGuessNum;
                    }
                    if (!BlockMyGuesserOption&&MyPlayer.Modifiers.Any(g=>g is GuesserModifier.Instance))
                    {
                        MyPlayer.RemoveModifier(GuesserModifier.MyRole);
                    }
                    GuessNoDead = true;
                    swaped = false;
                    ObjectTracker<GamePlayer> myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, (GamePlayer p) => ObjectTrackers.StandardPredicate(p) && (LastSelect == null || (LastSelect != null && LastSelect.PlayerId != p.PlayerId)), null, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption, false);
                    if (vanishImage == null)
                    {
                        PDebug.Log("image is null");
                        return;
                    }
                    vanishButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).SetAsUsurpableButton(this);
                    vanishButton.BindKey(Virial.Compat.VirtualKeyInput.Ability);
                    (vanishButton as Virial.Components.ModAbilityButton).SetImage(vanishImage);
                    vanishButton.Availability = (ModAbilityButton button) => myTracker.CurrentTarget != null && this.MyPlayer.CanMove;
                    vanishButton.Visibility = (ModAbilityButton button) => !this.MyPlayer.IsDead && leftskill > 0;
                    vanishButton.ShowUsesIcon(0, leftskill.ToString());
                    vanishButton.OnClick = (button) =>
                    {
                        if (button.IsBroken)
                        {
                            return;
                        }
                        var player = myTracker.CurrentTarget;
                        new StaticAchievementToken("SpeechEater.common1");
                        if (player!.IsImpostor)
                        {
                            new StaticAchievementToken("SpeechEater.common2");
                        }
                        SelectPlayer = player;
                        leftskill -= 1;
                        vanishButton.UpdateUsesIcon(leftskill.ToString());
                        button.StartCoolDown();
                    };
                    vanishButton.SetAsUsurpableButton(this);
                    vanishButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, EatCooldownOption).SetAsAbilityTimer().Start(null);
                    vanishButton.SetLabel("eaterR");
                    vanishButton.StartCoolDown();
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        public GamePlayer? SelectPlayer,LastSelect;
        private void OnGameStarted(GameStartEvent ev)
        {
            SelectPlayer = null;
            GameOperatorManager.Instance!.Subscribe<GameEndEvent>(ev =>
            {
                SelectPlayer = null;
            }, NebulaAPI.CurrentGame!);
        }
           public bool swaped;
        public static Sprite? PrevOverlay=null;
        public static Sprite? Overlay=null;
        public static void ChatUpdatePostfix(ChatController __instance)
        {
            if (NebulaAPI.CurrentGame != null && NebulaGameManager.Instance!.AllPlayerInfo.Any(p => p.TryGetAbility<SpeechEater.Ability>(out var ability) && ability.SelectPlayer != null && ability.SelectPlayer.PlayerId == AmongUsLLImpl.LocalPlayer.PlayerId) && !AmongUsLLImpl.LocalPlayer.Data.IsDead)
            {
                __instance.freeChatField.SetVisible(false);
                __instance.quickChatField.SetVisible(false);
            }
        }
        public static void MeetingUpdate(MeetingHud __instance)
        {
            try {
                var speechEaters = NebulaGameManager.Instance!.AllPlayerInfo.Where(p => p.TryGetAbility<SpeechEater.Ability>(out var a));
                if (speechEaters.Count() <= 0 || speechEaters.All(p => p.Role.GetAbility<SpeechEater.Ability>()!.SelectPlayer == null))
                {
                    return;
                }
                if (true)
                {
                    var playerStates = __instance!.playerStates!.Where((PlayerVoteArea x) => speechEaters.Any(p=>p.TryGetAbility<SpeechEater.Ability>(out var ability)&&ability.SelectPlayer!=null&&x.TargetPlayerId == ability.SelectPlayer.PlayerId))!;
                    if (playerStates.Count()<=0)
                    {
                        return;
                    }
                    playerStates!.Do(p=>p.Overlay.gameObject.SetActive(true));

                    if (PrevOverlay == null)
                    {
                        PrevOverlay = playerStates.First().Overlay.sprite;
                    }
                    if (Overlay == null)
                    {
                        Texture2D texture = new Texture2D(347, 62);
                        UnityEngine.Color[] pixels = new UnityEngine.Color[347 * 62];
                        for (int i = 0; i < pixels.Length; i++)
                        {
                            pixels[i] = new Color32(255, 0, 0, 110);
                        }
                        texture.SetPixels(pixels);
                        texture.Apply();
                        Overlay = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), UnityEngine.Vector2.one / 2);
                    }
                    playerStates.Do(p=>p.Overlay.sprite = Overlay);
                    playerStates.Do(state =>
                    {
                        var mySpeechEater = speechEaters.FirstOrDefault(p => p.TryGetAbility<SpeechEater.Ability>(out var ability) && ability.SelectPlayer != null && ability.SelectPlayer.PlayerId == state.TargetPlayerId);
                        if (mySpeechEater==null)
                        {
                            return;
                        }
                        var ability = mySpeechEater.Role.GetAbility<SpeechEater.Ability>();
                        if (__instance.state != MeetingHud.VoteStates.Animating && !ability.swaped)
                        {
                            ability.swaped = true;
                            __instance.StartCoroutine(Effects.SwayX(state.transform, 0.75f, 0.25f).WrapToManaged());
                        }
                    });
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        public static bool StopChat(TextBoxTMP __instance)
        {
            if (MeetingHud.Instance && NebulaAPI.CurrentGame != null && NebulaGameManager.Instance!.AllPlayerInfo.Any(p => p.TryGetAbility<SpeechEater.Ability>(out var ability) && ability.SelectPlayer != null && ability.SelectPlayer.PlayerId == AmongUsLLImpl.LocalPlayer.PlayerId) && !AmongUsLLImpl.LocalPlayer.Data.IsDead)
            {
                return false;
            }
            return true;
        }
        public static bool StopAddChat(ChatController __instance)
        {
            bool flag = false;
            var speechEaters = NebulaGameManager.Instance!.AllPlayerInfo.Where(p => p.TryGetAbility<SpeechEater.Ability>(out var a));
            if (speechEaters.Count() <= 0 || speechEaters.All(p => p.Role.GetAbility<SpeechEater.Ability>()!.SelectPlayer == null))
            {
                return true;
            }
            flag = speechEaters.Any(p =>
            {
                p.TryGetAbility<SpeechEater.Ability>(out var ability);
                if (ability==null||ability!=null&&ability.SelectPlayer == null)
                {
                    return false;
                }
                var targetRoleCategory = ability.SelectPlayer.Role.Role.Category;
                if ((!CrewmateCanAddChatOption && targetRoleCategory == RoleCategory.CrewmateRole) ||
                    (!NeutralCanAddChatOption && targetRoleCategory == RoleCategory.NeutralRole) ||
                    (!ImpostorCanAddChatOption && targetRoleCategory == RoleCategory.ImpostorRole))
                {
                    return true;
                }
                return false;
            });
            if (flag&& speechEaters.Any(p => p.TryGetAbility<SpeechEater.Ability>(out var ability) && ability.SelectPlayer != null && ability.SelectPlayer.PlayerId == AmongUsLLImpl.LocalPlayer.PlayerId) && !AmongUsLLImpl.LocalPlayer.Data.IsDead)
            {
                return false;
            }
            return true;
        }
        public static bool CanChat(ChatController __instance)
        {
            if (NebulaAPI.CurrentGame != null && NebulaGameManager.Instance!.AllPlayerInfo.Any(p => p.TryGetAbility<SpeechEater.Ability>(out var ability) && ability.SelectPlayer != null && ability.SelectPlayer.PlayerId == AmongUsLLImpl.LocalPlayer.PlayerId) && !AmongUsLLImpl.LocalPlayer.Data.IsDead)
            {
                return false;
            }
            return true;
        }
        [Local]
        private void OnPreMeetingStart(MeetingPreStartEvent ev)
        {
            try
            {
                PDebug.Log("MeetingStart");
                LastSelect = null;
                if (SelectPlayer != null && !SelectPlayer.IsDead && !SelectPlayer.IsDisconnected)
                {
                    RpcSilent.Invoke((MyPlayer, SelectPlayer));
                }
            }
            catch (Exception ex)
            {
                PDebug.Log(ex);
            }
        }
        public bool GuessNoDead = true;
        public int LeftGuess = NumOfGuessOption;
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            PDebug.Log("GuessUIGenerated");
            if (!BlockMyGuesserOption)
            {
                GuesserSystem.OnMeetingStart(LeftGuess, () => LeftGuess--, true);
            }
        }
        private void OnMeetingEnd(MeetingEndEvent ev)
        {
            PDebug.Log("OnMeetingEnd");
                swaped = false;
            LastSelect = SelectPlayer;
            SelectPlayer = null;
        }
        static readonly RemoteProcess<ValueTuple<GamePlayer, GamePlayer>> RpcSilent = new("SilentTarget",
    (ValueTuple<GamePlayer,GamePlayer> message, bool _) =>
    {
        if (message.Item1.TryGetAbility<SpeechEater.Ability>(out var ability))
        {
            ability.swaped = false;
            ability.SelectPlayer = message.Item2;
        }
    });
    }
}
