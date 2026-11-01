using Plana.Roles.Crewmate;
using Plana.Roles.Impostor;
using Virial.Helpers;
using Virial.Runtime;
using BindingFlags = System.Reflection.BindingFlags;
using Color = Virial.Color;
using IEnumerator = System.Collections.IEnumerator;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Plana.Core.API;

public interface IPrivateChatManager
{
    public IEnumerable<ChatChannel> AllChannels
    {
        get;
    }
    public void Register(ChatChannel channel);
    public void Register(Color color, Color textfieldColor, string chatid, string localizedname, ILifespan lifespan, Func<bool> useChannelPredicate, Func<GamePlayer, GamePlayer, bool> canReceive, Action<string>? onSendMessage = null);
    public void Register(Color color, Color textfieldColor, string chatid, Func<string> localizedname, ILifespan lifespan, Func<bool> useChannelPredicate, Func<GamePlayer, GamePlayer, bool> canReceive, Action<string>? onSendMessage = null);
    public void RegisterPublicChannel(Color color, Color textfieldColor, string chatid, string localizedname, ILifespan lifespan, Func<bool> useChannelPredicate, bool donotSendMessage = false, Action<string>? onSendMessage = null);
    public void SetPredicate(string chatId, Func<bool> useChannelPredicate, Func<GamePlayer, GamePlayer, bool>? canReceive = null);
}

[NebulaPreprocess(PreprocessPhase.PostFixStructure)]
public class PrivateChatManager : AbstractModule<Game>, IPrivateChatManager, IGameOperator
{
    internal static PrivateChatManager? Instance { get; private set; }

    public const int NormalGroup = 0;
    public const int SpecialGroup = 1;

    public ChatChannel? CurrentChat { get; private set; }
    public int CurrentGroup { get; private set; }
    
    public IEnumerable<ChatChannel> AllChannels => channels;

    public bool isForces;
    public int currentIndex;
    private List<ChatChannel> channels = [];
    
    private PrivateChatManager()
    {
        PDebug.Log("PrivateChatManager Initialized");
        if (Instance != null)
        {
            Instance.channels.Clear();
        }
        Instance = this;
        Register(Color.Green, Color.White, "public", Language.Translate("chat.public"), NebulaAPI.CurrentGame, () => (MeetingHud.Instance && !ExileController.Instance) || AmongUsLLImpl.LobbyInstance || NebulaGameManager.Instance.CanBeSpectator || PatchManager.HasFreeChat, (s, p) => true);
        this.RegisterPermanently();
    }
    static void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule<Game>(() => new PrivateChatManager());
        Harmony.CreateAndPatchAll(typeof(PrivateChatPatches));
    }
    public void Register(ChatChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        channels.Add(channel);
        PDebug.Log("register " + channel.Id + " channel Count:" + channels.Count());
    }
    public void Register(Color color, Color textfieldColor, string chatid, string localizedname, ILifespan lifespan, Func<bool> useChannelPredicate, Func<GamePlayer, GamePlayer, bool> canReceive, Action<string>? onSendMessage = null)
    {
        channels.Add(new ChatChannel(localizedname, chatid, canReceive, lifespan, useChannelPredicate, color, textfieldColor));
        PDebug.Log("register " + chatid + " channel Count:" + channels.Count());
    }
    public void Register(Color color, Color textfieldColor, string chatid, Func<string> localizedname, ILifespan lifespan, Func<bool> useChannelPredicate, Func<GamePlayer, GamePlayer, bool> canReceive, Action<string>? onSendMessage = null)
    {
        channels.Add(new ChatChannel(localizedname, chatid, canReceive, lifespan, useChannelPredicate, color, textfieldColor));
        PDebug.Log("register " + chatid + " channel Count:" + channels.Count());
    }
    public void RegisterPublicChannel(Color color, Color textfieldColor, string chatid, string localizedname, ILifespan lifespan, Func<bool> useChannelPredicate, bool donotSendMessage = false, Action<string>? onSendMessage = null)
    {
        RegisterPublicChannel(color, textfieldColor, chatid, localizedname, lifespan, useChannelPredicate, donotSendMessage, onSendMessage, NormalGroup);
    }

    public void RegisterPublicChannel(Color color, Color textfieldColor, string chatid, string localizedname, ILifespan lifespan, Func<bool> useChannelPredicate, bool donotSendMessage, Action<string>? onSendMessage, int group)
    {
        var channel = new ChatChannel(true, donotSendMessage, localizedname, chatid, onSendMessage, lifespan, useChannelPredicate, color, textfieldColor);
        channel.Group = group;
        channels.Add(channel);
    }

    public void SetPredicate(string chatId, Func<bool> useChannelPredicate, Func<GamePlayer, GamePlayer, bool>? predicate = null)
    {
        var targetChannel = channels.FirstOrDefault(c => c.Id == chatId);
        if (targetChannel == null)
        {
            PDebug.Log("Not found id is " + chatId.ToString() + " channel");
            return;
        }

        targetChannel.useChannelPredicate = useChannelPredicate;
        if (predicate != null)
        {
            targetChannel.canReceive = predicate;
        }
    }
    void OnMeetingStart(MeetingPreStartEvent ev)
    {
        ChangeChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatches.chatTeamText, channels.FirstOrDefault(c => c.Id is "public"));
    }
    void OnGameEnd(GameEndEvent ev)
    {
        channels.RemoveAll(p => p.Id is not "public");
        ChangeChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatches.chatTeamText, channels.FirstOrDefault(c => c.Id is "public"));
        currentIndex = 0;
    }
    
    void OnUpdate(GameHudUpdateEvent ev)
    {
        if (channels.Any(c => c.CanUseChannel))
        {
            if (!AmongUsLLImpl.HudManagerBridge.Chat.isActiveAndEnabled)
            {
                AmongUsLLImpl.HudManagerBridge.Chat.SetVisible(true);
            }
        }
        else
        {
            if (AmongUsLLImpl.HudManagerBridge.Chat.isActiveAndEnabled)
            {
                AmongUsLLImpl.HudManagerBridge.Chat.SetVisible(false);
            }
        }
    }
    
    void OnSetRole(PlayerRoleSetEvent ev)
    {
        if (ev.Player.AmOwner)
        {
            try
            {
                if (PatchManager.AllPlayerCannotSeeOtherNameColor)
                {
                    goto refreshChannel;
                }
                if (PatchManager.ImpostorMeetingChat && !PatchManager.ImpostorCannotSeeOther && ev.Role.Role.Category == RoleCategory.ImpostorRole)
                {
                    Instance?.Register(NebulaTeams.ImpostorTeam.Color, new(0.7f, 0.2f, 0.2f), "Impostor", Language.Translate("chat.impostortext"), ev.Role, () => MeetingHud.Instance && GamePlayer.LocalPlayer.IsImpostor && NebulaGameManager.Instance.AllPlayerInfo.Any(p => p.IsImpostor && p.TryGetAbility<CrawlerEngineer.Ability>(out var ab)), (p1, p2) => (p1.IsImpostor && p2.IsImpostor));
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
            refreshChannel:;
            RefreshChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatches.chatTeamText);
        }
    }
    
    void OnSetModifier(PlayerModifierSetEvent ev)
    {
        RefreshChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatches.chatTeamText);
    }
    void OnRemoveModifier(PlayerModifierRemoveEvent ev)
    {
        RefreshChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatches.chatTeamText);
    }
    void OnExile(ExileSceneStartEvent ev)
    {
        RefreshChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatches.chatTeamText);
    }
    void OnRestartTaskPhase(TaskPhaseRestartEvent ev)
    {
        RefreshChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatches.chatTeamText);
    }

    public void SetForces(string? id)
    {
        var channel = channels.FirstOrDefault(c => c.Id == id);
        SetForces(channel);
    }
    
    public void SetForces(ChatChannel? channel)
    {
        if (channel == null)
        {
            isForces = false;
            return;
        }
        if (!channels.Contains(channel) || channel.Lifespan.IsDeadObject) return;
        
        CurrentChat = channel;
        isForces = true;
        ChangeChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatches.chatTeamText, channel);
    }
    
    void RemoveDeadObject()
    {
        channels.RemoveAll(c => c.Lifespan.IsDeadObject);
        if (isForces && (CurrentChat == null || !channels.Contains(CurrentChat)))
        {
            SetForces(channel: null);
        }
    }
    
    private ChatChannel? FindUsableChannel(int group)
    {
        return channels.FirstOrDefault(c => c.Group == group && c.CanUseChannel);
    }

    private void ApplyChannelVisuals(ChatController c, TextMeshPro? chatTeamText)
    {
        if (CurrentChat == null) return;
        PrivateChatPatches.SetChannelText(chatTeamText, Language.Translate("chat.current").Replace("%CHAT%", CurrentChat.LocalizedName.Color(CurrentChat.Color)));
        var darkPublic = PatchManager.DarkThemeEnabled && CurrentChat.Id is "public";
        var color = darkPublic ? PatchManager.DarkChatFieldColor : CurrentChat.TextFieldColor.ToUnityColor();
        c.freeChatField.background.color = color;
        c.quickChatField.background.color = color;
        c.freeChatField.textArea.outputText.color = darkPublic ? UnityEngine.Color.white : UnityEngine.Color.black;
    }

    public bool ToggleSpecialGroup(ChatController c, TextMeshPro? chatTeamText)
    {
        RemoveDeadObject();
        if (CurrentGroup == SpecialGroup)
        {
            CurrentGroup = NormalGroup;
            var next = FindUsableChannel(NormalGroup);
            if (next == null)
            {
                CurrentGroup = SpecialGroup;
                return false;
            }
            isForces = false;
            CurrentChat = next;
        }
        else
        {
            CurrentGroup = SpecialGroup;
            var next = FindUsableChannel(SpecialGroup);
            if (next == null)
            {
                CurrentGroup = NormalGroup;
                CurrentChat = FindUsableChannel(NormalGroup) ?? CurrentChat;
                return false;
            }
            isForces = false;
            CurrentChat = next;
        }
        ApplyChannelVisuals(c, chatTeamText);
        return true;
    }

    public void RefreshChannel(ChatController c, TextMeshPro? chatTeamText)
    {
        RemoveDeadObject();
        if (CurrentChat == null || !CurrentChat.CanUseChannel || CurrentChat.Lifespan.IsDeadObject || CurrentChat.Group != CurrentGroup)
        {
            var next = FindUsableChannel(CurrentGroup);
            if (next == null && CurrentGroup != NormalGroup)
            {
                CurrentGroup = NormalGroup;
                next = FindUsableChannel(NormalGroup);
            }
            if (next == null)
            {
                PDebug.Log("No Can use Channel");
                return;
            }
            CurrentChat = next;
        }

        ApplyChannelVisuals(c, chatTeamText);
    }
    
    public void ChangeChannel(ChatController c, TextMeshPro chatTeamText, ChatChannel? nextChannel = null)
    {
        RemoveDeadObject();
        
        if (nextChannel == null)
        {
            var groupChannels = channels.Where(ch => ch.Group == CurrentGroup && ch.CanUseChannel).ToList();
            if (groupChannels.Count == 0) return;
            if (isForces && CurrentChat != null && groupChannels.Contains(CurrentChat)) return;
            var index = CurrentChat == null ? -1 : groupChannels.IndexOf(CurrentChat);
            nextChannel = groupChannels[(index + 1) % groupChannels.Count];
        }
        else
        {
            CurrentGroup = nextChannel.Group;
        }

        CurrentChat = nextChannel;
        ApplyChannelVisuals(c, chatTeamText);
    }
}

public class ChatChannel
{
    public string Id { get; private set; }
    public int Group { get; internal set; }
    public Color Color { get; private set; }
    public Color TextFieldColor { get; private set; }
    public string LocalizedName
    {
        get => showLocalizedName();
        set
        {
            showLocalizedName = () => value;
        }
    }

    public ILifespan Lifespan { get; }

    public bool CanUseChannel => useChannelPredicate();

    public bool CanReceive(GamePlayer sender, Player player)
    {
        return canReceive(sender, player);
    }
    
    internal Func<bool> useChannelPredicate;
    internal Action<string>? SendMessageAction;
    public bool donotSendMessage;
    public bool isPublicChannel;
    internal Func<GamePlayer, GamePlayer, bool> canReceive;
    internal Func<string> showLocalizedName;
    
    public ChatChannel(string localizedName, string id, Func<GamePlayer, GamePlayer, bool> canHear, ILifespan lifespan, Func<bool> useChannelPredicate, Color color, Color fieldColor, Action<string>? onSendMessage = null)
    {
        Id = id;

        showLocalizedName = () => localizedName;
        canReceive = canHear;
        this.Lifespan = lifespan;
        Color = color;
        TextFieldColor = fieldColor;
        SendMessageAction = onSendMessage;
        this.useChannelPredicate = useChannelPredicate;
    }
    
    public ChatChannel(Func<string> localizedName, string id, Func<GamePlayer, GamePlayer, bool> canHear, ILifespan lifespan, Func<bool> useChannelPredicate, Color color, Color fieldColor, Action<string>? onSendMessage = null)
    {
        Id = id;

        showLocalizedName = localizedName;
        canReceive = canHear;
        this.Lifespan = lifespan;
        Color = color;
        TextFieldColor = fieldColor;
        SendMessageAction = onSendMessage;
        this.useChannelPredicate = useChannelPredicate;
    }
    
    public ChatChannel(bool isPublic, bool donotSendMessage, string localizedName, string id, Action<string> onSendMessage, ILifespan lifespan, Func<bool> useChannelPredicate, Color color, Color fieldColor)
    {
        Id = id;
        
        this.donotSendMessage = donotSendMessage;
        showLocalizedName = () => localizedName;
        this.Lifespan = lifespan;
        Color = color;
        TextFieldColor = fieldColor;
        isPublicChannel = isPublic;
        SendMessageAction = onSendMessage;
        this.useChannelPredicate = useChannelPredicate;
    }
}

[HarmonyPatch]
[NebulaRPCHolder]
public class PrivateChatPatches
{
    static RemoteProcess<(byte sender, int receivePlayer, string namepostfix, string text)> RpcSendPrivateChat = new("SendPrivateChatRpc",
(message, _) =>
{
    var sender = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault(p => p.PlayerId == message.sender);
    if (sender == null)
    {
        return;
    }

    var mask = BitMasks.AsPlayer((uint)message.receivePlayer);
    if (mask.Test(GamePlayer.LocalPlayer) || AmongUsLLImpl.LocalPlayer.PlayerId == sender.PlayerId || NebulaGameManager.Instance.CanSeeAllInfo)
    {
        var name = sender.name;
        sender.SetName(sender.name+ message.namepostfix);
        AmongUsLLImpl.HudManagerBridge.Chat.AddChat(sender, message.text);
        sender.SetName(name);
        sender?.ToNebulaPlayer()?.Unbox()?.playerNameText?.lastText = null;
    }
});
    public static TextMeshPro? chatTeamText;

    public static void SetChannelText(TextMeshPro? text, string channelText)
    {
        if (text == null) return;
        text.text = channelText + PatchManager.ChatHelpHintSuffix;
    }

    public static bool ShouldShowChannelBar()
    {
        try
        {
            var manager = PrivateChatManager.Instance;
            if (manager == null) return false;
            if (AmongUsLLImpl.LobbyInstance.AsBoolFast())
            {
                if (manager.CurrentGroup != PrivateChatManager.SpecialGroup) return false;
            }
            else if (manager.CurrentGroup != PrivateChatManager.SpecialGroup && (AmongUsLLImpl.LocalPlayer?.Data?.IsDead ?? true))
            {
                return false;
            }
            var usable = manager.AllChannels.Where(c => c.Group == manager.CurrentGroup && c.CanUseChannel).ToList();
            return usable.Count > 1 || (usable.Count == 1 && usable[0].Id is not "public");
        }
        catch (Exception e)
        {
            PDebug.Log(e);
            return false;
        }
    }

    [HarmonyPatch(typeof(ChatController), "Update"), HarmonyPostfix, HarmonyPriority(HarmonyLib.Priority.Low)]
    public static void ChatUpdate(ChatController __instance)
    {
        var srmt = __instance.sendRateMessageText;
        var manager = PrivateChatManager.Instance;
        if (!ShouldShowChannelBar())
        {
            chatTeamText.GetIfNotNull()?.gameObject.SetActive(false);

            srmt.GetIfNotNull()?.transform.localPosition = new Vector3(-3.3719f, -1.5046f, -5f);
            return;
        }
        try
        {
            if (chatTeamText == null)
            {
                TextMeshPro countText = UnityEngine.Object.Instantiate<TextMeshPro>(AmongUsLLImpl.HudManagerInstance.MeetingPrefab.TimerText, AmongUsLLImpl.HudManagerBridge.Chat.freeChatField.transform);
                countText.gameObject.SetActive(true);
                countText.gameObject.GetComponent<TextTranslatorTMP>().enabled = false;
                countText.alignment = TextAlignmentOptions.Left;
                countText.transform.localPosition = new Vector3(-0.14f, 0.49f);
                countText.color = Palette.White;
                countText.rectTransform.sizeDelta = new(6f, 0.4496f);
                chatTeamText = countText;
            }
            chatTeamText.GetIfNotNull()?.gameObject.SetActive(true);
            srmt.GetIfNotNull()?.transform.localPosition = new Vector3(-3.3719f, -1.22f, -5f);
            if (Input.GetMouseButtonDown(1))
            {
                manager?.ChangeChannel(__instance, chatTeamText!);
            }
            if (manager?.CurrentChat != null)
            {
                var darkPublic = PatchManager.DarkThemeEnabled && manager.CurrentChat.Id is "public";
                var color = darkPublic ? PatchManager.DarkChatFieldColor : manager.CurrentChat.TextFieldColor.ToUnityColor();
                __instance.freeChatField.background.color = color;
                __instance.quickChatField.background.color = color;
                __instance.freeChatField.textArea.outputText.color = darkPublic ? UnityEngine.Color.white : UnityEngine.Color.black;
                SetChannelText(chatTeamText, Language.Translate("chat.current").Replace("%CHAT%", manager.CurrentChat.LocalizedName.Color(manager.CurrentChat.Color)));
            }
        }
        catch (Exception ex)
        {
            PDebug.Log(ex);
        }
    }

    [HarmonyPatch(typeof(ChatController), "SendChat"), HarmonyPrefix,HarmonyPriority(100)]
    public static bool BlockSendChat(ChatController __instance)
    {
        string text = __instance.freeChatField.textArea.text;
        if (text.TrimStart().StartsWith("/"))
        {
            return true;
        }
        var chatManager = PrivateChatManager.Instance;
        if (chatManager == null) return true;
        
        var currentChannel = chatManager.CurrentChat;
        var specialChannel = currentChannel != null && currentChannel.Group == PrivateChatManager.SpecialGroup;
        if (AmongUsLLImpl.LobbyInstance.AsBoolFast())
        {
            if (!specialChannel) return true;
        }
        else if (AmongUsLLImpl.LocalPlayer.Data.IsDead && !specialChannel)
        {
            return true;
        }
        if (currentChannel != null && currentChannel.Id is not "public")
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            if (!currentChannel.CanUseChannel)
            {
                PDebug.Log("Cannot Use " + currentChannel.Id + " Channel,RefreshChannel");
                chatManager.RefreshChannel(__instance, chatTeamText);
                return false;
            }
            if (currentChannel.Lifespan.IsDeadObject)
            {
                PDebug.Log(currentChannel.Id + " Channel Lifespan Is DeadObject,RefreshChannel");
                chatManager.RefreshChannel(__instance, chatTeamText);
                return false;
            }
            __instance.freeChatField.Clear();
            int bitReceivePlayers = 0;
            if (!currentChannel.donotSendMessage)
            {
                foreach (var p in NebulaGameManager.Instance.AllPlayerInfo)
                {
                    if (currentChannel.isPublicChannel)
                    {
                        bitReceivePlayers |= (1 << (int)p.PlayerId);
                        continue;
                    }
                    if (currentChannel.CanReceive(GamePlayer.LocalPlayer, p))
                    {
                        bitReceivePlayers |= (1 << (int)p.PlayerId);
                    }
                }
            }
            currentChannel.SendMessageAction?.Invoke(text);
            
            if (currentChannel.donotSendMessage)
            {
                return false;
            }
            RpcSendPrivateChat.Invoke((AmongUsLLImpl.LocalPlayer.PlayerId, bitReceivePlayers, $"[{currentChannel.LocalizedName}]".Color(currentChannel.Color), text));
            return false;
        }
        return true;
    }
    [HarmonyPatch(typeof(Jackal.Instance), "Virial.Assignable.RuntimeAssignable.OnActivated"), HarmonyPostfix]
    public static void JackalOnActivated(Jackal.Instance __instance)
    {
        if (!__instance.AmOwner)
        {
            return;
        }
        try
        {
            if (PatchManager.JackalMeetingChat)
            {
                PrivateChatManager.Instance?.Register(Jackal.MyRole.RoleColor, Jackal.MyRole.RoleColor, "Jackal" + __instance.JackalTeamId, Language.Translate("chat.jackaltext"),
                    __instance, () => MeetingHud.Instance, (p1, p2) => Jackal.IsJackalTeam(p1, __instance.JackalTeamId) && Jackal.IsJackalTeam(p2, __instance.JackalTeamId));
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    //[HarmonyPatch(typeof(Sidekick.Instance), "Virial.Assignable.RuntimeAssignable.OnActivated"), HarmonyPostfix]
    public static void SidekickOnActivated(Sidekick.Instance __instance)
    {
        if (!__instance.AmOwner)
        {
            return;
        }
        try
        {
            if (PatchManager.JackalMeetingChat)
            {
                PrivateChatManager.Instance?.Register(Jackal.MyRole.RoleColor, Jackal.MyRole.RoleColor, "Jackal" + __instance.JackalTeamId, Language.Translate("chat.jackaltext"),
                    __instance, () => MeetingHud.Instance, (p1, p2) => Jackal.IsJackalTeam(p1, __instance.JackalTeamId) && Jackal.IsJackalTeam(p2, __instance.JackalTeamId));
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    //[HarmonyPatch(typeof(SidekickModifier.Instance), "Virial.Assignable.RuntimeAssignable.OnActivated"), HarmonyPostfix]
    public static void SidekickMOnActivated(SidekickModifier.Instance __instance)
    {
        if (!__instance.AmOwner)
        {
            return;
        }
        try
        {
            if (PatchManager.JackalMeetingChat)
            {
                PrivateChatManager.Instance?.Register(Jackal.MyRole.RoleColor, Jackal.MyRole.RoleColor, "Jackal" + __instance.JackalTeamId, Language.Translate("chat.jackaltext"),
                    __instance, () => MeetingHud.Instance, (p1, p2) => Jackal.IsJackalTeam(p1, __instance.JackalTeamId) && Jackal.IsJackalTeam(p2, __instance.JackalTeamId));
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    [HarmonyPatch(typeof(Lover.Instance), "Virial.Assignable.RuntimeAssignable.OnActivated"), HarmonyPostfix]
    public static void LoverOnActivated(Lover.Instance __instance)
    {
        if (!__instance.AmOwner)
        {
            return;
        }
        try
        {
            if (PatchManager.LoverMeetingChat || PatchManager.ChatOption)
            {
                PDebug.Log("register lover chat");
                int loverCount = __instance.MyPlayer.GetModifiers<Lover.Instance>()?.Count() ?? 0;
                var color = Lover.Colors[loverCount - 1];
                PrivateChatManager.Instance?.Register(color, color, "Lovers" + __instance.LoversId, () => (Language.Translate("chat.lovertext") + ("(" + __instance.MyLover.Get().PlayerName + ")").Color(Lover.Colors[loverCount - 1])),
                    __instance, () =>
                    {
                        if (MeetingHud.Instance.AsBoolFast() && PatchManager.LoverMeetingChat)
                        {
                            return true;
                        }
                        if (!MeetingHud.Instance.AsBoolFast() && PatchManager.ChatOption)
                        {
                            return true;
                        }
                        return false;
                    }, (p1, p2) => __instance.MyLover.Get() == p2);
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    [HarmonyPatch(typeof(Scarlet.Instance), "OnAddLover"), HarmonyPostfix]
    public static void ScarletOnAddLover(RuntimeVentRoleTemplate __instance, GamePlayer player, bool isFavorite)
    {
        if (!__instance.AmOwner)
        {
            return;
        }
        try
        {
            if (PatchManager.ScarletPrivateMeetingChat)
            {
                PDebug.Log("register scarlet lover chat");
                PrivateChatManager.Instance?.Register(Scarlet.MyRole.RoleColor, Scarlet.MyRole.RoleColor, "Scarlet", () => (Language.Translate("chat.scarlettext") + ("(" + player.ColoredName + ")").Color(Scarlet.MyRole.UnityColor)),
                    __instance, () => MeetingHud.Instance, (p1, p2) => player == p2);
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    [HarmonyPatch(typeof(ScarletLover.Instance), "Virial.Assignable.RuntimeAssignable.OnActivated"), HarmonyPostfix]
    public static void ScarletLoverOnActivated(ScarletLover.Instance __instance)
    {
        if (!__instance.AmOwner)
        {
            return;
        }
        try
        {
            if (PatchManager.ScarletPrivateMeetingChat)
            {
                PDebug.Log("Register scarlet lover chat");
                PrivateChatManager.Instance?.Register(Scarlet.MyRole.RoleColor, Scarlet.MyRole.RoleColor, "Scarlet", () => (Language.Translate("chat.scarlettext") + ("(" + __instance.MyScarlet.MyPlayer.ColoredName + ")").Color(Scarlet.MyRole.UnityColor)),
                    __instance, () => MeetingHud.Instance, (p1, p2) => __instance.IsMyScarlet(p2));
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
}
