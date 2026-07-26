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
    public void Register(Color color, Color textfieldColor, string chatid, string localizedname, ILifespan lifespan, Func<bool> useChannelPredicate, Func<GamePlayer, GamePlayer, bool> canReceive, Action<string> onSendMessage = null);
    public void Register(Color color, Color textfieldColor, string chatid, Func<string> localizedname, ILifespan lifespan, Func<bool> useChannelPredicate, Func<GamePlayer, GamePlayer, bool> canReceive, Action<string> onSendMessage = null);
    public void RegisterPublicChannel(Color color, Color textfieldColor, string chatid, string localizedname, ILifespan lifespan, Func<bool> useChannelPredicate, bool donotSendMessage = false, Action<string> onSendMessage = null);
    public void SetPredicate(string chatid, Func<bool> useChannelPredicate, Func<GamePlayer, GamePlayer, bool> canReceive = null);
}
[NebulaPreprocess(PreprocessPhase.PostFixStructure)]
public class PrivateChatManager : AbstractModule<Game>, IPrivateChatManager, IGameOperator
{
    internal static PrivateChatManager Instance { get; private set; }
    public IEnumerable<ChatChannel> AllChannels => channels;
    public PrivateChatManager()
    {
        PDebug.Log("PrivateChatManager Initialized");
        if (Instance != null)
        {
            Instance.channels.Clear();
            Instance.channels = null;
        }
        Instance = this;
        Register(Color.Green, Color.White, "public", Language.Translate("chat.public"), NebulaAPI.CurrentGame, () => (MeetingHud.Instance && !ExileController.Instance) || AmongUsLLImpl.LobbyInstance || NebulaGameManager.Instance.CanBeSpectator || PatchManager.HasFreeChat, (s, p) => true);
        this.RegisterPermanently();
    }
    static void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule<Game>(() => new PrivateChatManager());
        Harmony.CreateAndPatchAll(typeof(PrivateChatPatchs));
    }
    public void Register(ChatChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        channels.Add(channel);
        PDebug.Log("register " + channel.Id + " channel Count:" + channels.Count());
    }
    public void Register(Color color, Color textfieldColor, string chatid, string localizedname, ILifespan lifespan, Func<bool> useChannelPredicate, Func<GamePlayer, GamePlayer, bool> canReceive, Action<string> onSendMessage = null)
    {
        channels.Add(new ChatChannel(localizedname, chatid, canReceive, lifespan, useChannelPredicate, color, textfieldColor));
        PDebug.Log("register " + chatid + " channel Count:" + channels.Count());
    }
    public void Register(Color color, Color textfieldColor, string chatid, Func<string> localizedname, ILifespan lifespan, Func<bool> useChannelPredicate, Func<GamePlayer, GamePlayer, bool> canReceive, Action<string> onSendMessage = null)
    {
        channels.Add(new ChatChannel(localizedname, chatid, canReceive, lifespan, useChannelPredicate, color, textfieldColor));
        PDebug.Log("register " + chatid + " channel Count:" + channels.Count());
    }
    public void RegisterPublicChannel(Color color, Color textfieldColor, string chatid, string localizedname, ILifespan lifespan, Func<bool> useChannelPredicate, bool donotSendMessage = false, Action<string> onSendMessage = null)
    {
        channels.Add(new ChatChannel(true, donotSendMessage, localizedname, chatid, onSendMessage, lifespan, useChannelPredicate, color, textfieldColor));
    }
    public void SetPredicate(string chatid, Func<bool> useChannelPredicate, Func<GamePlayer, GamePlayer, bool> predicate = null)
    {
        var targetChannel = channels.FirstOrDefault(c => c.Id == chatid);
        if (targetChannel == null)
        {
            PDebug.Log("Not found id is " + chatid.ToString() + " channel");
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
        ChangeChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatchs.chatTeamText, channels.FirstOrDefault(c => c.Id is "public"));
    }
    void OnGameEnd(GameEndEvent ev)
    {
        channels.RemoveAll(p => p.Id is not "public");
        ChangeChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatchs.chatTeamText, channels.FirstOrDefault(c => c.Id is "public"));
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
        try
        {
            if (PatchManager.AllPlayerCannotSeeOtherNameColor)
            {
                goto refreshChannel;
            }
            if (ev.Player.AmOwner && PatchManager.ImpostorMeetingChat && !PatchManager.ImpostorCannotSeeOther && ev.Role.Role.Category == RoleCategory.ImpostorRole)
            {
                Instance.Register(NebulaTeams.ImpostorTeam.Color, new(0.7f, 0.2f, 0.2f), "Impostor", Language.Translate("chat.impostortext"), ev.Role, () => MeetingHud.Instance && GamePlayer.LocalPlayer.IsImpostor && NebulaGameManager.Instance.AllPlayerInfo.Any(p => p.IsImpostor && p.TryGetAbility<CrawlerEngineer.Ability>(out var ab)), (p1, p2) => (p1.IsImpostor && p2.IsImpostor));
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    refreshChannel:;
        RefreshChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatchs.chatTeamText);
    }
    void OnSetModifier(PlayerModifierSetEvent ev)
    {
        RefreshChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatchs.chatTeamText);
    }
    void OnRemoveModifier(PlayerModifierRemoveEvent ev)
    {
        RefreshChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatchs.chatTeamText);
    }
    void OnExile(ExileSceneStartEvent ev)
    {
        RefreshChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatchs.chatTeamText);
    }
    void OnRestartTaskPhase(TaskPhaseRestartEvent ev)
    {
        RefreshChannel(AmongUsLLImpl.HudManagerBridge.Chat, PrivateChatPatchs.chatTeamText);
    }
    void RemoveDeadObject()
    {
        channels.RemoveAll(c => c.Lifespan.IsDeadObject);
    }
    public void RefreshChannel(ChatController c, TextMeshPro chatTeamText)
    {
        RemoveDeadObject();
        if (currentChat == null || !currentChat.CanUseChannel || currentChat.Lifespan.IsDeadObject)
        {
            ChatChannel? currentChannel = null;
            try
            {
            setChannel:;
                if (channels.All(c => !c.CanUseChannel))
                {
                    PDebug.Log("No Can use Channel");
                    return;
                }
                currentIndex++;
                if (currentIndex >= channels.Count)
                {
                    currentIndex = 0;
                }
                currentChannel = channels[currentIndex];
                if (currentChannel == null || !currentChannel.CanUseChannel)
                {
                    goto setChannel;
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
            currentChat = currentChannel;
        }
        if (chatTeamText != null)
        {
            chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", currentChat.LocalizedName.Color(currentChat.Color.ToUnityColor()));
        }
        var color = currentChat.TextFieldColor.ToUnityColor();
        c.freeChatField.background.color = color;
        c.quickChatField.background.color = color;
        c.freeChatField.textArea.outputText.color = UnityEngine.Color.black;
    }
    public void ChangeChannel(ChatController c, TextMeshPro chatTeamText, ChatChannel nextchannel = null)
    {
        RemoveDeadObject();
        if (nextchannel == null)
        {
            try
            {
            setChannel:;
                if (channels.All(c => !c.CanUseChannel))
                {
                    PDebug.Log("No Can use Channel");
                    return;
                }
                currentIndex++;
                if (currentIndex >= channels.Count)
                {
                    currentIndex = 0;
                }
                var currentChannel = channels[currentIndex];
                if (currentChannel == null || !currentChannel.CanUseChannel)
                {
                    goto setChannel;
                }
                currentChat = currentChannel;
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        else
        {
            currentChat = nextchannel;
        }
        if (chatTeamText != null)
        {
            chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", currentChat.LocalizedName.Color(currentChat.Color.ToUnityColor()));
        }
        var color = currentChat.TextFieldColor.ToUnityColor();
        c.freeChatField.background.color = color;
        c.quickChatField.background.color = color;
        c.freeChatField.textArea.outputText.color = UnityEngine.Color.black;
    }
    public ChatChannel currentChat;
    public int currentIndex;
    private List<ChatChannel> channels = new();
}
public class ChatChannel
{
    public string Id { get; private set; }
    public Color Color { get; private set; }
    public Color TextFieldColor { get; private set; }
    public string LocalizedName
    {
        get
        {
            return showLocalizedName();
        }
        set
        {
            showLocalizedName = () => value;
        }
    }

    public ILifespan Lifespan => lifespan;

    public bool CanUseChannel => useChannelPredicate();

    public bool CanReceive(GamePlayer sender, Player player)
    {
        return canReceive(sender, player);
    }
    public ChatChannel(string localizedName, string id, Func<GamePlayer, GamePlayer, bool> canHear, ILifespan lifespan, Func<bool> useChannelPredicate, Color color, Color fieldColor, Action<string> onSendMessage = null)
    {
        Id = id;
        //LocalizedName = localizedName;
        showLocalizedName = () => localizedName;
        canReceive = canHear;
        this.lifespan = lifespan;
        Color = color;
        TextFieldColor = fieldColor;
        SendMessageAction = onSendMessage;
        this.useChannelPredicate = useChannelPredicate;
    }
    public ChatChannel(Func<string> localizedName, string id, Func<GamePlayer, GamePlayer, bool> canHear, ILifespan lifespan, Func<bool> useChannelPredicate, Color color, Color fieldColor, Action<string> onSendMessage = null)
    {
        Id = id;
        //LocalizedName = localizedName;
        showLocalizedName = localizedName;
        canReceive = canHear;
        this.lifespan = lifespan;
        Color = color;
        TextFieldColor = fieldColor;
        SendMessageAction = onSendMessage;
        this.useChannelPredicate = useChannelPredicate;
    }
    public ChatChannel(bool ispublic, bool donotSendMessage, string localizedName, string id, Action<string> onSendMessage, ILifespan lifespan, Func<bool> useChannelPredicate, Color color, Color fieldColor)
    {
        Id = id;
        this.donotSendMessage = donotSendMessage;
        showLocalizedName = () => localizedName;
        this.lifespan = lifespan;
        Color = color;
        TextFieldColor = fieldColor;
        isPublicChannel = ispublic;
        SendMessageAction = onSendMessage;
        this.useChannelPredicate = useChannelPredicate;
    }
    internal Func<bool> useChannelPredicate;
    internal Action<string> SendMessageAction;
    public bool donotSendMessage;
    public bool isPublicChannel = false;
    internal Func<GamePlayer, GamePlayer, bool> canReceive;
    private ILifespan lifespan;
    internal Func<string> showLocalizedName;
}
[HarmonyPatch]
[NebulaRPCHolder]
public class PrivateChatPatchs
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
        sender.SetName(name + message.namepostfix);
        AmongUsLLImpl.HudManagerBridge.Chat.AddChat(sender, message.text);
        sender.SetName(name);
    }
});
    public static TextMeshPro? chatTeamText;
    [HarmonyPatch(typeof(ChatController), "Update"), HarmonyPostfix]
    public static void ChatUpdate(ChatController __instance)
    {
        var srmt = __instance.sendRateMessageText;
        if (AmongUsLLImpl.LobbyInstance || (AmongUsLLImpl.LocalPlayer?.Data.IsDead ?? true))
        {
            chatTeamText?.gameObject.SetActive(false);
            if (srmt != null)
            {
                srmt.transform.localPosition = new Vector3(-3.3719f, -1.5046f, -5f);
            }
            return;
        }
        try
        {
            if (PrivateChatManager.Instance?.AllChannels.Count(c => c.CanUseChannel) <= 1 && PrivateChatManager.Instance.AllChannels.Any(c => c.CanUseChannel && c.Id is "public"))
            {
                chatTeamText?.gameObject?.SetActive(false);
                if (srmt != null)
                {
                    srmt.transform.localPosition = new Vector3(-3.3719f, -1.5046f, -5f);
                }
                return;
            }
            if (chatTeamText == null)
            {
                TextMeshPro countText = UnityEngine.Object.Instantiate<TextMeshPro>(AmongUsLLImpl.HudManagerInstance.MeetingPrefab.TimerText, AmongUsLLImpl.HudManagerBridge.Chat.freeChatField.transform);
                countText.gameObject.SetActive(true);
                countText.gameObject.GetComponent<TextTranslatorTMP>().enabled = false;
                countText.alignment = TextAlignmentOptions.Left;
                countText.transform.localPosition = new UnityEngine.Vector3(-0.14f, 0.49f);
                countText.color = Palette.White;
                countText.rectTransform.sizeDelta = new(6f, 0.4496f);
                countText.text = Language.Translate("chat.current").Replace("%CHAT%", (PrivateChatManager.Instance.currentChat == null ? "" : PrivateChatManager.Instance?.currentChat?.LocalizedName.Color(PrivateChatManager.Instance.currentChat.Color.ToUnityColor())));
                chatTeamText = countText;
            }
            chatTeamText?.gameObject.SetActive(true);
            srmt?.transform.localPosition = new Vector3(-3.3719f, -1.22f, -5f);
            if (Input.GetMouseButtonDown(1))
            {
                PrivateChatManager.Instance?.ChangeChannel(__instance, chatTeamText);
            }
            if (PrivateChatManager.Instance?.currentChat?.Id is not "public")
            {
                var c = PrivateChatManager.Instance?.currentChat.Color.ToUnityColor();
                var c2 = PrivateChatManager.Instance?.currentChat.TextFieldColor.ToUnityColor();
                __instance.freeChatField.background.color = c2 ?? UnityEngine.Color.white;
                __instance.quickChatField.background.color = c2 ?? UnityEngine.Color.white;
                chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", PrivateChatManager.Instance?.currentChat?.LocalizedName.Color(c ?? UnityEngine.Color.white));
            }
        }
        catch (Exception)
        {
            //PDebug.Log(e.ToString());
        }
    }
    [HarmonyPatch(typeof(ChatController), "SendChat"), HarmonyPrefix]
    public static bool BlockSendChat(ChatController __instance)
    {
        string text = __instance.freeChatField.textArea.text;
        if (AmongUsLLImpl.LobbyInstance.AsBoolFast() || AmongUsLLImpl.LocalPlayer.Data.IsDead)
        {
            return true;
        }
        var chatmanager = PrivateChatManager.Instance;
        var currentChannel = chatmanager.currentChat;
        if (currentChannel != null && currentChannel.Id is not "public")
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            if (!currentChannel.CanUseChannel)
            {
                PDebug.Log("Cannot Use " + currentChannel.Id + " Channel,RefreshChannel");
                chatmanager.RefreshChannel(__instance, chatTeamText);
                return false;
            }
            if (currentChannel.Lifespan.IsDeadObject)
            {
                PDebug.Log(currentChannel.Id + " Channel Lifespan Is DeadObject,RefreshChannel");
                chatmanager.RefreshChannel(__instance, chatTeamText);
                return false;
            }
            __instance.freeChatField.Clear();
            int bitReceivePlayers = 0;
            foreach (var p in NebulaGameManager.Instance.AllPlayerInfo)
            {
                if (currentChannel?.isPublicChannel ?? false)
                {
                    bitReceivePlayers |= (1 << (int)p.PlayerId);
                    continue;
                }
                if (currentChannel?.CanReceive(GamePlayer.LocalPlayer, p) ?? false)
                {
                    bitReceivePlayers |= (1 << (int)p.PlayerId);
                }
            }
            currentChannel.SendMessageAction?.Invoke(text);
            if (currentChannel.donotSendMessage)
            {
                return false;
            }
            RpcSendPrivateChat.Invoke((AmongUsLLImpl.LocalPlayer.PlayerId, bitReceivePlayers, $"[{currentChannel.LocalizedName}]".Color(currentChannel.Color.ToUnityColor()), text));
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
                PrivateChatManager.Instance.Register(Jackal.MyRole.RoleColor, Jackal.MyRole.RoleColor, "Jackal" + __instance.JackalTeamId, Language.Translate("chat.jackaltext"),
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
                PrivateChatManager.Instance.Register(Jackal.MyRole.RoleColor, Jackal.MyRole.RoleColor, "Jackal" + __instance.JackalTeamId, Language.Translate("chat.jackaltext"),
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
                PrivateChatManager.Instance.Register(Jackal.MyRole.RoleColor, Jackal.MyRole.RoleColor, "Jackal" + __instance.JackalTeamId, Language.Translate("chat.jackaltext"),
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
                PrivateChatManager.Instance.Register(color, color, "Lovers" + __instance.LoversId, () => (Language.Translate("chat.lovertext") + ("(" + __instance.MyLover.Get().PlayerName + ")").Color(Lover.Colors[loverCount - 1])),
                    __instance, () =>
                    {
                        if (MeetingHud.Instance && PatchManager.LoverMeetingChat)
                        {
                            return true;
                        }
                        if (!MeetingHud.Instance && PatchManager.ChatOption)
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
    public static void ScaletOnAddLover(RuntimeVentRoleTemplate __instance, GamePlayer player, bool isFavorite)
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
                PrivateChatManager.Instance.Register(Scarlet.MyRole.RoleColor, Scarlet.MyRole.RoleColor, "Scarlet", () => (Language.Translate("chat.scarlettext") + ("(" + player.ColoredName + ")").Color(Scarlet.MyRole.UnityColor)),
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
                PDebug.Log("register scarlet lover chat");
                PrivateChatManager.Instance.Register(Scarlet.MyRole.RoleColor, Scarlet.MyRole.RoleColor, "Scarlet", () => (Language.Translate("chat.scarlettext") + ("(" + __instance.MyScarlet.MyPlayer.ColoredName + ")").Color(Scarlet.MyRole.UnityColor)),
                    __instance, () => MeetingHud.Instance, (p1, p2) => __instance.IsMyScarlet(p2));
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
}