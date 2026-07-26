using Hazel;
using Nebula.Roles.Complex;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Neutral;
using Rewired.Utils.Platforms.Windows;
using TMPro;
using UnityEngine;
using Virial.Assignable;
using Virial.Events.Role;
using Virial.Helpers;
using Virial.Utilities;
using static Rewired.Glyphs.UnityUI.UnityUITextMeshProGlyphHelper.Tag;
using Color = UnityEngine.Color;
using IEnumerator = System.Collections.IEnumerator;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Nebula.Roles.Assignment;

[HarmonyPatch]
[NebulaRPCHolder]
internal class RoleDraft
{
    static internal BoolConfiguration UseRoleDraftOption = new BoolConfigurationImpl("options.assignment.useRoleDraft", false);
    static internal IntegerConfiguration DraftAmountOfChoices = NebulaAPI.Configurations.Configuration("options.assignment.roledraft.amountofchoices", (1, 15), 3, () => UseRoleDraftOption);
    static internal IntegerConfiguration DraftTimeToChoose = NebulaAPI.Configurations.Configuration("options.assignment.roledraft.timetochoose", (1, 20), 5, () => UseRoleDraftOption);
    static internal BoolConfiguration DraftShowRole = NebulaAPI.Configurations.Configuration("options.assignment.roledraft.showrole", true, () => UseRoleDraftOption);
    static internal BoolConfiguration DraftHideImpRole = NebulaAPI.Configurations.Configuration("options.assignment.roledraft.hideimp", true, () => UseRoleDraftOption && DraftShowRole);
    static internal BoolConfiguration DraftHideNeuRole = NebulaAPI.Configurations.Configuration("options.assignment.roledraft.hideneu", true, () => UseRoleDraftOption && DraftShowRole);
    static internal BoolConfiguration DraftHideCrewRole = NebulaAPI.Configurations.Configuration("options.assignment.roledraft.hidecrew", true, () => UseRoleDraftOption && DraftShowRole);
    static internal GroupConfiguration DraftOptions = new GroupConfiguration("options.assignment.roleDraft", [UseRoleDraftOption, DraftAmountOfChoices, DraftTimeToChoose, DraftShowRole, DraftHideImpRole, DraftHideNeuRole, DraftHideCrewRole], GroupConfigurationColor.Gray);
    public static bool isEnabled => false; //UseRoleDraftOption;
    public static bool isRunning;

    public static List<byte> pickOrder = new();
    public static bool picked;
    public static float timer;
    private static List<GameObject> buttons = new();
    private static TextMeshPro feedText;
    public static List<DefinedRole> alreadyPicked = new();
    private static Dictionary<byte, DefinedRole> playerRoles = new();
    static IRoleTable AssignRoleTable;
    static MetaScreen SelectInfoScreen;
    static TextMeshPro selectInfoText;
    public record DraftRoleChance(DefinedRole role, AllocationParameters? param = null)
    {
        public int count = (param ?? role.AllocationParameters)?.RoleCountSum ?? 0;
        public int left = (param ?? role.AllocationParameters)?.RoleCountSum ?? 0;
        public int cost = (param ?? role.AllocationParameters)?.TeamCost ?? 1;
        public int otherCost = (param ?? role.AllocationParameters)?.OtherCost ?? 1;
        public AllocationParameters? Param => param ?? role.AllocationParameters;
    }
    public static void CreateRolePool(out List<DraftRoleChance> impostorRoles, out List<DraftRoleChance> crewmateRoles, out List<DraftRoleChance> neutralRoles, out List<DraftRoleChance>[] customChances, out List<DraftRoleChance>[] allRoles)
    {
        customChances = AssignmentType.AllTypes.Select((AssignmentType t) => new List<DraftRoleChance>(from r in Nebula.Roles.Roles.AllRoles.Where(delegate (DefinedRole r)
        {
            if (t.Predicate(r.AssignmentStatus, r))
            {
                AllocationParameters customAllocationParameters = r.GetCustomAllocationParameters(t);
                return ((customAllocationParameters != null) ? customAllocationParameters.RoleCountSum : 0) > 0;
            }
            return false;
        })
                                                                                                        select new DraftRoleChance(r, r.GetCustomAllocationParameters(t))
                                                                                                        {
                                                                                                            cost = 1,
                                                                                                            otherCost = 0
                                                                                                        })).ToArray<List<DraftRoleChance>>();

        List<DraftRoleChance> GetRolePool(RoleCategory category)
        {
            return new List<DraftRoleChance>(from r in Nebula.Roles.Roles.AllRoles.Where(delegate (DefinedRole r)
            {
                if (r.Category == category)
                {
                    AllocationParameters allocationParameters = r.AllocationParameters;
                    return ((allocationParameters != null) ? allocationParameters.RoleCountSum : 0) > 0;
                }
                return false;
            })
                                              select new DraftRoleChance(r, null)
                                              {
                                                  cost = 1,
                                                  otherCost = 0
                                              });
        }
        crewmateRoles = GetRolePool(RoleCategory.CrewmateRole);
        impostorRoles = GetRolePool(RoleCategory.ImpostorRole);
        neutralRoles = GetRolePool(RoleCategory.NeutralRole);
        List<List<DraftRoleChance>> list = new List<List<DraftRoleChance>>();
        list.Add(crewmateRoles);
        list.Add(impostorRoles);
        list.Add(neutralRoles);
        list.AddRange(customChances);
        allRoles = list.ToArray();
        foreach (IExclusiveAssignmentRule exclusiveOption in IExclusiveAssignmentRule.AllRules)
        {
            List<DefinedRole> cand = new List<DefinedRole>();
            List<DraftRoleChance>[] array = allRoles;
            for (int i = 0; i < array.Length; i++)
            {
                foreach (DraftRoleChance chance2 in array[i])
                {
                    if (exclusiveOption.Contains(chance2.role) && !cand.Contains(chance2.role))
                    {
                        cand.Add(chance2.role);
                    }
                }
            }
            if (cand.Count > 1)
            {
                cand.RemoveRandomOne<DefinedRole>();
                foreach (List<DraftRoleChance> list2 in allRoles)
                {
                    list2.RemoveAll(chance => cand.Contains(chance.role));
                }
            }
        }
    }
    public static void CategoryAssign(IRoleTable rtable, int left, List<byte> main, List<byte> others, List<DraftRoleChance> rolePool, List<DraftRoleChance>[] allRolePool, Action<DefinedRole, byte>? onSelected = null)
    {
        RoleTable table = (rtable as RoleTable)!;
        if (left < 0) left = 15;
        void OnSelected(DraftRoleChance selected)
        {
            if (onSelected != null)
            {
                onSelected.Invoke(selected.role, main[0]);
                main.RemoveAt(0);
            }
            else
            {
                var player = main[0];
                table.SetRole(main[0], selected.role, selected.role.DefaultAssignableArguments);
                main.RemoveAt(0);

                selected.Param?.TeamAssignment?.Do(assignment =>
                {
                    var param = assignment.Assigner.Invoke(selected.role, player);
                    table.SetRole(main[0], param.role, param.argument);
                    main.RemoveAt(0);
                });

                selected.Param?.OthersAssignment?.Do(assignment =>
                {
                    var param = assignment.Assigner.Invoke(selected.role, player);
                    table.SetRole(others[0], param.role, param.argument);
                    others.RemoveAt(0);
                });
            }

            left -= selected.cost;

            //割り当て済み役職を排除
            selected.left--;
            if (selected.left == 0) rolePool.Remove(selected);
        }

        bool left100Roles = true;
        while (main.Count > 0 && left > 0 && rolePool.Count > 0)
        {
            //コスト超過によって割り当てられない役職を弾く
            rolePool.RemoveAll(c =>
            {
                if (main == others)
                    return c.cost + c.otherCost > left && c.cost + c.otherCost > main.Count;
                else
                    return c.cost > left && c.cost > main.Count && c.otherCost > others.Count;
            });

            //100%割り当て役職が残っている場合
            if (left100Roles)
            {
                var roles100 = rolePool.Where(r => r.Param!.GetRoleChance(r.count - r.left + 1) == 100f);
                if (roles100.Any(r => true))
                {
                    //役職を選択する
                    OnSelected(roles100.ToArray().Random());
                    continue;
                }
                else
                {
                    left100Roles = false;
                }
            }

            //100%役職がもう残っていない場合
            var sum = rolePool.Sum(r => r.Param!.GetRoleChance(r.count - r.left + 1));
            var random = System.Random.Shared.NextSingle() * sum;
            foreach (var r in rolePool)
            {
                random -= r.Param!.GetRoleChance(r.count - r.left + 1);
                if (random < 0f)
                {
                    //役職を選択する
                    OnSelected(r);
                    break;
                }
            }
        }
    }
    public static IEnumerator CoSelectRoles(IntroCutscene __instance)
    {
        isRunning = true;
        Clear();
        // SoundEffectsManager.play("draft",  volume: 1f, true, true);
        bool playedAlert = false;
        /*feedText = UnityEngine.Object.Instantiate(__instance.TeamTitle, __instance.transform);
        var aspectPosition = feedText.gameObject.AddComponent<AspectPosition>();
        aspectPosition.Alignment = AspectPosition.EdgeAlignments.LeftTop;
        aspectPosition.DistanceFromEdge = new Vector2(1.62f, 1.2f);
        aspectPosition.AdjustPosition();
        feedText.transform.localScale = new Vector3(0.6f, 0.6f, 1);
        feedText.transform.position += new Vector3(0f, 0.6f);
        feedText.text = Language.Translate("roleDraft.FeedText");
        feedText.alignment = TextAlignmentOptions.TopLeft;
        feedText.autoSizeTextContainer = true;
        feedText.fontSize = 3f;
        feedText.enableAutoSizing = false;*/
        
        __instance.TeamTitle.transform.localPosition = __instance.TeamTitle.transform.localPosition + new Vector3(1f, 0f);
        __instance.TeamTitle.text = "Currently Picking:";
        __instance.BackgroundBar.enabled = false;
        __instance.TeamTitle.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
        __instance.TeamTitle.autoSizeTextContainer = true;
        __instance.TeamTitle.enableAutoSizing = false;
        __instance.TeamTitle.fontSize = 5;
        __instance.TeamTitle.alignment = TextAlignmentOptions.Top;
        __instance.ImpostorText.gameObject.SetActive(false);
        __instance.Foreground.gameObject.SetActive(false);
        GameObject.Find("BackgroundLayer")?.SetActive(false);
        foreach (var player in UnityEngine.Object.FindObjectsOfType<PoolablePlayer>())
        {
            if (player.name.Contains("Dummy"))
            {
                player.gameObject.SetActive(false);
            }
        }
        __instance.FrontMost.gameObject.SetActive(false);
        //__instance.Foreground.gameObject.SetActive(false);
        if (AmongUsLLImpl.AmongUsClientInstance.AmHost)
        {
            sendPickOrder();
            AssignRoleTable = new RoleTable();
        }

        while (pickOrder.Count == 0)
        {
            yield return null;
        }
        CreateRolePool(out var impostorRoles, out var crewmateRoles, out var neutralRoles,out var customChances, out var allRoles);
        try
        {
            PDebug.Log("Build Select Info Window");
            SelectInfoScreen = MetaScreen.GenerateWindow(new(3.3f, 4.4f), AmongUsLLImpl.HudManagerInstance.transform, Vector3.left*3,false,false,false,BackgroundSetting.Modern,false);
            UnityEngine.Object.Destroy(SelectInfoScreen.transform.parent.gameObject.transform.FindChild("ClickGuard").gameObject);
            MetaWidgetOld widget = new MetaWidgetOld(Array.Empty<IMetaWidgetOld>());
            widget.Append(new MetaWidgetOld.Text(TextAttributeOld.BoldAttr)
            {
                RawText = Language.Translate("RoleDraft.FeedText")
            });
            widget.Append(new MetaWidgetOld.VerticalMargin(0.1f));
            MetaWidgetOld inner = new MetaWidgetOld(Array.Empty<IMetaWidgetOld>());
            inner.Append(new MetaWidgetOld.Text(TextAttributeOld.BoldAttrLeft)
            {
                PostBuilder = (tmp) =>
                {
                    selectInfoText = tmp;
                }
            });
            MetaWidgetOld.ScrollView scroller = new MetaWidgetOld.ScrollView(new Vector2(3.5f, 3.8f), inner, true)
            {
                Alignment = IMetaWidgetOld.AlignmentOption.Center
            };
            widget.Append(scroller);
            SelectInfoScreen.SetWidget(widget);
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
        while (pickOrder.Count > 0)
        {
            picked = false;
            timer = 0;
            float maxTimer = DraftTimeToChoose;
            string playerText = "";
            while (timer < maxTimer || !picked)
            {
                if (pickOrder.Count == 0)
                    break;

                // wait for pick
                timer += Time.deltaTime;
                if (AmongUsLLImpl.AmongUsClientInstance.AmHost)
                {
                    if (timer >= maxTimer)
                    {
                        List<DefinedRole> availableRoles = new();
                        IEnumerable<DraftRoleChance> imps100 = impostorRoles.Where(r => r.Param.GetRoleChance(r.count - r.left + 1) == 100);
                        IEnumerable<DraftRoleChance> neutrals100 = neutralRoles.Where(r => r.Param.GetRoleChance(r.count - r.left + 1) == 100);
                        IEnumerable<DraftRoleChance> crew100 = crewmateRoles.Where(r => r.Param.GetRoleChance(r.count - r.left + 1) == 100);
                        int impsMax = GeneralConfigurations.AssignmentImpostorOption, neutralsMax = GeneralConfigurations.AssignmentNeutralOption, crewmateMax = GeneralConfigurations.AssignmentCrewmateOption;
                        if (impsMax < 0)
                        {
                            impsMax = 24;
                        }
                        if (crewmateMax < 0)
                        {
                            crewmateMax = 24;
                        }
                        int impsPicked = alreadyPicked.Count(x => x.Category == RoleCategory.ImpostorRole);
                        int neutralsPicked = alreadyPicked.Count(x => x.Category == RoleCategory.NeutralRole);
                        int crewPicked = alreadyPicked.Count(x => x.Category == RoleCategory.CrewmateRole);
                        foreach (var role in Nebula.Roles.Roles.AllRoles.Where(r => !r.IsSystemRole))
                        {
                            // 修复：移除错误的continue，改为判断角色归属（原逻辑是想保留在三类列表中的角色）
                            var crewmateRole = crewmateRoles.FirstOrDefault(r => r.role == role);
                            var neutralRole = neutralRoles.FirstOrDefault(r => r.role == role);
                            var impostorRole = impostorRoles.FirstOrDefault(r => r.role == role);

                            // 只处理属于船员、中立、内鬼的角色
                            bool isNeutral = neutralRole != null;
                            bool isCrewmate = crewmateRole != null;
                            bool isImpostor = impostorRole != null;
                            if (!isNeutral && !isCrewmate && !isImpostor) continue;

                            if (AmongUsLLImpl.LocalPlayer.Data.RoleType == AmongUs.GameOptions.RoleTypes.Impostor && !isImpostor) continue;
                            if (AmongUsLLImpl.LocalPlayer.Data.RoleType == AmongUs.GameOptions.RoleTypes.Crewmate && isImpostor) continue;
                            // 排除已选角色
                            if (alreadyPicked.Contains(role)) continue;

                            bool passCheck = false;
                            if (isImpostor)
                            {
                                // 内鬼：优先100%概率角色
                                int imps100Left = imps100.Count(x => !alreadyPicked.Contains(x.role)); // 剩余100%概率内鬼

                                // 若100%概率内鬼未用完，仅保留100%概率的内鬼
                                if (imps100Left > 0)
                                {
                                    passCheck = imps100.Any(r => r.role == role);
                                }
                                else
                                {
                                    // 100%概率内鬼用完，补充普通内鬼，直到达到上限
                                    passCheck = impsPicked < impsMax;
                                }
                            }
                            else if (isNeutral)
                            {
                                // 中立：优先100%概率角色
                                int neutral100Left = neutrals100.Count(x => !alreadyPicked.Contains(x.role)); // 剩余100%概率中立

                                if (neutral100Left > 0)
                                {
                                    passCheck = neutrals100.Any(r => r.role == role);
                                }
                                else
                                {
                                    passCheck = neutralsPicked < neutralsMax;
                                }
                            }
                            else if (isCrewmate)
                            {
                                // 船员：优先100%概率角色
                                int crew100Left = crew100.Count(x => !alreadyPicked.Contains(x.role)); // 剩余100%概率船员

                                if (crew100Left > 0)
                                {
                                    passCheck = crew100.Any(r => r.role == role);
                                }
                                else
                                {
                                    passCheck = crewPicked < crewmateMax;
                                }
                            }
                            // 未通过分类校验，跳过
                            if (!passCheck)
                            {
                                // 调试用日志：PDebug.Log($"角色 {role.Name} 未通过优先100%校验，跳过");
                                continue;
                            }
                            // 中立/船员角色数量校验
                            int neutrals100Picked = alreadyPicked.Count(x =>
                            {
                                var chance = neutralRoles.FirstOrDefault(r => r.role == x);
                                return chance != null && chance.Param.GetRoleChance(chance.count - chance.left + 1) == 100;
                            });
                            int crew100Picked = alreadyPicked.Count(x =>
                            {
                                var chance = crewmateRoles.FirstOrDefault(r => r.role == x);
                                return chance != null && chance.Param.GetRoleChance(chance.count - chance.left + 1) == 100;
                            });

                            // 超过最大数量则跳过
                            if ((isNeutral && neutralsPicked >= neutralsMax) ||
                                (isCrewmate && crewPicked >= crewmateMax))
                            {
                                continue;
                            }

                            // 修复：正确获取当前角色的概率配置
                            DraftRoleChance myChance = null;
                            try
                            {
                                // 根据角色类型获取对应的概率配置
                                if (isNeutral) myChance = neutralRole;
                                else if (isCrewmate) myChance = crewmateRole;
                                else if (isImpostor) myChance = impostorRole;
                            }
                            catch (Exception e)
                            {
                                PDebug.Log("search my chance error:" + e.ToString());
                            }

                            // 空值保护
                            if (myChance == null) continue;
                            int neuChance = 0, crewChance = 0;
                            // 修复：修正Min判断逻辑（判断100%概率角色数量是否超过剩余可分配数量）
                            if (isNeutral)
                            {
                                int remainingNeutralSlots = neutralsMax - neutralsPicked;
                                int available100Neutrals = neutrals100.Count(x => !alreadyPicked.Contains(x.role));

                                neuChance = myChance.Param.GetRoleChance(myChance.count - myChance.left + 1);
                                // 有未分配的100%概率中立角色时，只保留100%概率的角色
                                if (available100Neutrals > 0 && neuChance != 100) continue;
                            }
                            else if (isCrewmate)
                            {
                                int remainingCrewSlots = crewmateMax - crewPicked;
                                int available100Crews = crew100.Count(x => !alreadyPicked.Contains(x.role));

                                crewChance = myChance.Param.GetRoleChance(myChance.count - myChance.left + 1);
                                // 修复：替换错误的neutralsMax为crewmateMax
                                if (available100Crews > 0 && crewChance != 100) continue;
                            }

                            // 修复：100%概率角色充足时，过滤低概率角色（<10
                            if (isNeutral)
                            {
                                int available100Neutrals = neutrals100.Count(x => !alreadyPicked.Contains(x.role));
                                int neededNeutrals = neutralsMax - alreadyPicked.Count(r => neutralRoles.Any(p => p.role == r));
                                if (available100Neutrals >= neededNeutrals && neuChance < 100) continue;
                            }
                            if (isCrewmate)
                            {
                                int available100Crews = crew100.Count(x => !alreadyPicked.Contains(x.role));
                                int neededCrews = crewmateMax - alreadyPicked.Count(r => crewmateRoles.Any(p => p.role == r));
                                if (available100Crews >= neededCrews && crewChance < 100) continue;
                            }

                            // 避免重复添加
                            if (!availableRoles.Contains(role))
                            {
                                availableRoles.Add(role);
                            }
                        }
                        if (availableRoles.Count == 0)
                        {
                            availableRoles.Add(AmongUsLLImpl.LocalPlayer.Data.RoleType == AmongUs.GameOptions.RoleTypes.Impostor
                                ? Impostor.Impostor.MyRole
                                : Crewmate.Crewmate.MyRole);
                        }
                        //PDebug.Log("select available roles");
                        // remove some roles, so that you can't always get the same roles:
                        if (availableRoles.Count > DraftAmountOfChoices)
                        {
                            int countToRemove = availableRoles.Count - DraftAmountOfChoices;
                            while (countToRemove-- > 0)
                            {
                                var toRemove = availableRoles.Random();
                                availableRoles.Remove(toRemove);
                            }
                        }
                        var originalAvailable = availableRoles.Shuffle().ToArray();
                        sendPick((byte)originalAvailable.Random().Id, (byte)SelectFlags.Random, pickOrder[0]);
                    }
                }
                if (AmongUsLLImpl.LocalPlayer.PlayerId == pickOrder[0])
                {
                    if (!playedAlert)
                    {
                        playedAlert = true;
                        AmongUsLLImpl.SoundManagerInstance.PlaySound(AmongUsLLImpl.ShipStatusInstance.SabotageSound, false, 1f, null);
                    }
                    // Animate beginning of choice, by changing background color
                    float min = 50 / 255f;
                    float max = 200 / 255f;
                    Color backGroundColor = new Color(min, min, min, 1);
                    if (timer < 1)
                    {
                        if (timer < 0.5f)
                        { // White flash                              
                            float p = timer / 0.5f;
                            float value = (float)Math.Pow(p, 2f) * max;
                            backGroundColor = new Color(value, value, value, 1f);
                        }
                        else
                        {
                            float p = (1 - timer) / 0.5f;
                            float value = ((float)Math.Pow(p, 2f) * max) + ((1 - (float)Math.Pow(p, 2f)) * min);
                            backGroundColor = new Color(value, value, value, 1);
                        }

                    }
                    AmongUsLLImpl.HudManagerInstance.FullScreen.color = backGroundColor;
                    GameObject.Find("BackgroundLayer")?.SetActive(false);

                    // enable pick, wait for pick
                    Color youColor = timer - (int)timer > 0.5 ? Color.red : Color.yellow;
                    playerText = Language.Translate("RoleDraft.You").Color(youColor);
                    // Available Roles:
                    List<DefinedRole> availableRoles = new();
                    IEnumerable<DraftRoleChance> imps100 = impostorRoles.Where(r => r.Param.GetRoleChance(r.count - r.left + 1) == 100);
                    IEnumerable<DraftRoleChance> neutrals100 = neutralRoles.Where(r => r.Param.GetRoleChance(r.count - r.left + 1) == 100);
                    IEnumerable<DraftRoleChance> crew100 = crewmateRoles.Where(r => r.Param.GetRoleChance(r.count - r.left + 1) == 100);
                    int impsMax = GeneralConfigurations.AssignmentImpostorOption, neutralsMax = GeneralConfigurations.AssignmentNeutralOption, crewmateMax = GeneralConfigurations.AssignmentCrewmateOption;
                    if (impsMax<0)
                    {
                        impsMax = 24;
                    }
                    if (crewmateMax<0)
                    {
                        crewmateMax = 24;
                    }
                    int impsPicked = alreadyPicked.Count(x => x.Category == RoleCategory.ImpostorRole);
                    int neutralsPicked = alreadyPicked.Count(x => x.Category == RoleCategory.NeutralRole);
                    int crewPicked = alreadyPicked.Count(x => x.Category == RoleCategory.CrewmateRole);
                    foreach (var role in Nebula.Roles.Roles.AllRoles.Where(r => !r.IsSystemRole))
                    {
                        // 修复：移除错误的continue，改为判断角色归属（原逻辑是想保留在三类列表中的角色）
                        var crewmateRole = crewmateRoles.FirstOrDefault(r => r.role == role);
                        var neutralRole = neutralRoles.FirstOrDefault(r => r.role == role);
                        var impostorRole = impostorRoles.FirstOrDefault(r => r.role == role);

                        // 只处理属于船员、中立、内鬼的角色
                        bool isNeutral = neutralRole != null;
                        bool isCrewmate = crewmateRole != null;
                        bool isImpostor = impostorRole != null;
                        if (!isNeutral && !isCrewmate && !isImpostor) continue;

                        if (AmongUsLLImpl.LocalPlayer.Data.RoleType == AmongUs.GameOptions.RoleTypes.Impostor && !isImpostor) continue;
                        if (AmongUsLLImpl.LocalPlayer.Data.RoleType == AmongUs.GameOptions.RoleTypes.Crewmate && isImpostor) continue;
                        // 排除已选角色
                        if (alreadyPicked.Contains(role)) continue;

                        bool passCheck = false;
                        if (isImpostor)
                        {
                            // 内鬼：优先100%概率角色
                            int imps100Left = imps100.Count(x => !alreadyPicked.Contains(x.role)); // 剩余100%概率内鬼

                            // 若100%概率内鬼未用完，仅保留100%概率的内鬼
                            if (imps100Left > 0)
                            {
                                passCheck = imps100.Any(r => r.role == role);
                            }
                            else
                            {
                                // 100%概率内鬼用完，补充普通内鬼，直到达到上限
                                passCheck = impsPicked < impsMax;
                            }
                        }
                        else if (isNeutral)
                        {
                            // 中立：优先100%概率角色
                            int neutral100Left = neutrals100.Count(x => !alreadyPicked.Contains(x.role)); // 剩余100%概率中立

                            if (neutral100Left > 0)
                            {
                                passCheck = neutrals100.Any(r => r.role == role);
                            }
                            else
                            {
                                passCheck = neutralsPicked < neutralsMax;
                            }
                        }
                        else if (isCrewmate)
                        {
                            // 船员：优先100%概率角色
                            int crew100Left = crew100.Count(x => !alreadyPicked.Contains(x.role)); // 剩余100%概率船员

                            if (crew100Left > 0)
                            {
                                passCheck = crew100.Any(r => r.role == role);
                            }
                            else
                            {
                                passCheck = crewPicked < crewmateMax;
                            }
                        }
                        // 未通过分类校验，跳过
                        if (!passCheck)
                        {
                            // 调试用日志：PDebug.Log($"角色 {role.Name} 未通过优先100%校验，跳过");
                            continue;
                        }
                        // 中立/船员角色数量校验
                        int neutrals100Picked = alreadyPicked.Count(x =>
                        {
                            var chance = neutralRoles.FirstOrDefault(r => r.role == x);
                            return chance != null && chance.Param.GetRoleChance(chance.count - chance.left + 1) == 100;
                        });
                        int crew100Picked = alreadyPicked.Count(x =>
                        {
                            var chance = crewmateRoles.FirstOrDefault(r => r.role == x);
                            return chance != null && chance.Param.GetRoleChance(chance.count - chance.left + 1) == 100;
                        });

                        // 超过最大数量则跳过
                        if ((isNeutral && neutralsPicked >= neutralsMax) ||
                            (isCrewmate && crewPicked >= crewmateMax))
                        {
                            continue;
                        }

                        // 修复：正确获取当前角色的概率配置
                        DraftRoleChance myChance = null;
                        try
                        {
                            // 根据角色类型获取对应的概率配置
                            if (isNeutral) myChance = neutralRole;
                            else if (isCrewmate) myChance = crewmateRole;
                            else if (isImpostor) myChance = impostorRole;
                        }
                        catch (Exception e)
                        {
                            PDebug.Log("search my chance error:" + e.ToString());
                        }

                        // 空值保护
                        if (myChance == null) continue;
                        int neuChance = 0, crewChance = 0;
                        // 修复：修正Min判断逻辑（判断100%概率角色数量是否超过剩余可分配数量）
                        if (isNeutral)
                        {
                            int remainingNeutralSlots = neutralsMax - neutralsPicked;
                            int available100Neutrals = neutrals100.Count(x => !alreadyPicked.Contains(x.role));

                            neuChance = myChance.Param.GetRoleChance(myChance.count - myChance.left + 1);
                            // 有未分配的100%概率中立角色时，只保留100%概率的角色
                            if (available100Neutrals > 0 && neuChance != 100) continue;
                        }
                        else if (isCrewmate)
                        {
                            int remainingCrewSlots = crewmateMax - crewPicked;
                            int available100Crews = crew100.Count(x => !alreadyPicked.Contains(x.role));

                            crewChance = myChance.Param.GetRoleChance(myChance.count - myChance.left + 1);
                            // 修复：替换错误的neutralsMax为crewmateMax
                            if (available100Crews > 0 && crewChance != 100) continue;
                        }

                        // 修复：100%概率角色充足时，过滤低概率角色（<10
                        if (isNeutral)
                        {
                            int available100Neutrals = neutrals100.Count(x => !alreadyPicked.Contains(x.role));
                            int neededNeutrals = neutralsMax - alreadyPicked.Count(r => neutralRoles.Any(p => p.role == r));
                            if (available100Neutrals >= neededNeutrals && neuChance < 100) continue;
                        }
                        if (isCrewmate)
                        {
                            int available100Crews = crew100.Count(x => !alreadyPicked.Contains(x.role));
                            int neededCrews = crewmateMax - alreadyPicked.Count(r => crewmateRoles.Any(p => p.role == r));
                            if (available100Crews >= neededCrews && crewChance < 100) continue;
                        }

                        // 避免重复添加
                        if (!availableRoles.Contains(role))
                        {
                            availableRoles.Add(role);
                        }
                    }
                    if (availableRoles.Count == 0)
                    {
                        availableRoles.Add(AmongUsLLImpl.LocalPlayer.Data.RoleType==AmongUs.GameOptions.RoleTypes.Impostor
                            ? Impostor.Impostor.MyRole
                            : Crewmate.Crewmate.MyRole);
                    }
                    //PDebug.Log("select available roles");
                    // remove some roles, so that you can't always get the same roles:
                    if (availableRoles.Count > DraftAmountOfChoices)
                    {
                        int countToRemove = availableRoles.Count - DraftAmountOfChoices;
                        while (countToRemove-- > 0)
                        {
                            var toRemove = availableRoles.Random();
                            availableRoles.Remove(toRemove);
                        }
                    }
                    var originalAvailable = availableRoles.Shuffle().ToArray();
                    if (timer >= maxTimer)
                    {
                        sendPick((byte)originalAvailable.Random().Id, (byte)SelectFlags.Random);
                    }

                    if (!GameObject.Find("RoleButton"))
                    {
                        PDebug.Log(availableRoles.Count);
                        availableRoles.Do(r => PDebug.Log(r.DisplayName));
                        int totalButtons = availableRoles.Count + 1;
                        int buttonsPerRow = 4;
                        int lastRow = totalButtons / buttonsPerRow;
                        int buttonsInLastRow = totalButtons % buttonsPerRow;

                        for (var t = 0; t < totalButtons; t++)
                        {
                            float row = t / buttonsPerRow;
                            float col = t % buttonsPerRow;
                            if (buttonsInLastRow != 0 && row == lastRow)
                            {
                                col += (buttonsPerRow - buttonsInLastRow) / 2f;
                            }

                            // planned rows: maximum of 4, hence the following calculation for rows as well:
                            row += (4 - lastRow - 1) / 2f;

                            var pos = new UnityEngine.Vector3(-6.4f + (col * 4.5f), -6.8f - (row * 5.33f),__instance.TeamTitle.transform.localPosition.z+1);
                            GameObject obj = null;
                            if (t == availableRoles.Count)
                            {
                                obj = NebulaAPI.GUI.RawButton(GUIAlignment.Center, NebulaAPI.GUI.GetAttribute(AttributeAsset.MetaRoleButton), Language.Translate("RoleDraft.Random").Color(UnityEngine.Color.green), (b) =>
                                {
                                    picked = true;
                                    sendPick((byte)originalAvailable.Random().Id, (byte)SelectFlags.Random);
                                }).Instantiate(new Anchor(new Virial.Compat.Vector2(0.5f, 0.5f), new Virial.Compat.Vector3(0f, 0f, 0f)), new Size(100f, 100f), out var size);
                            }
                            else
                            {
                                var roleInfo = availableRoles[t];
                                obj = NebulaAPI.GUI.RawButton(GUIAlignment.Center, NebulaAPI.GUI.GetAttribute(AttributeAsset.MetaRoleButton), roleInfo.DisplayColoredName, (b) =>
                                {
                                    picked = true;
                                    sendPick((byte)roleInfo.Id, (byte)SelectFlags.Random);
                                }).Instantiate(new Anchor(new Virial.Compat.Vector2(0.5f, 0.5f), new Virial.Compat.Vector3(0f, 0f, 0f)), new Size(100f, 100f), out var size);
                            }
                            obj.name = "RoleButton";
                            obj.transform.SetParent(__instance.TeamTitle.transform);
                            obj.layer = __instance.TeamTitle.gameObject.layer;
                            obj.transform.localPosition = pos;
                            obj.transform.localScale = new Vector3(1.85f, 1.85f);
                            /*var renderer = UnityHelper.CreateObject<SpriteRenderer>("RoleButton", __instance.TeamTitle.transform, pos);
                            renderer.transform.localScale = new Vector3(1.85f, 1.85f);
                                var textHolder = new GameObject("TextHolder");
                            textHolder.AddComponent<BoxCollider2D>().size = new Vector2(3f, 3f);
                            var text = textHolder.AddComponent<TextMeshPro>();
                            text.horizontalAlignment = HorizontalAlignmentOptions.Center;
                            text.fontSize = 4f;
                            text.fontStyle = FontStyles.Bold;
                            text.outlineWidth = 0.1f;
                            text.outlineColor = Color.black;
                            textHolder.layer = renderer.gameObject.layer;
                            textHolder.transform.SetParent(renderer.transform, false);
                            if (t == availableRoles.Count)
                            {
                                text.text = Language.Translate("RoleDraft.Random");
                                text.color = Color.green;
                                //textHolder.transform.localPosition = new Vector3(0, text.text.Contains('\n') ? -1.975f : -3.16f, -1);

                                var button = textHolder.gameObject.SetUpButton();
                                button.OnClick.AddListener(() =>
                                {
                                    picked = true;
                                    sendPick((byte)originalAvailable.Random().Id, (byte)SelectFlags.Random);
                                });

                            }
                            else
                            {
                                var roleInfo = availableRoles[t];
                                text.text = roleInfo.DisplayColoredName;
                                //textHolder.transform.localPosition = new Vector3(0, text.text.Contains('\n') ? -1.975f : -3.16f, -1);
                                var button = textHolder.gameObject.SetUpButton();
                                button.OnClick.AddListener(() =>
                                {
                                    picked = true;
                                    sendPick((byte)roleInfo.Id);
                                });
                            }*/
                            buttons.Add(obj);
                        }
                    }
                }
                else
                {
                    int currentPick = PlayerControl.AllPlayerControls.Count - pickOrder.Count + 1;
                    playerText = string.Format(Language.Translate("RoleDraft.PlayerText"), currentPick);
                    AmongUsLLImpl.HudManagerInstance.FullScreen.color = Color.black;
                }
                __instance.TeamTitle.text = string.Format(Language.Translate("RoleDraft.TeamTitle1"), playerText);
                int waitMore = pickOrder.IndexOf(AmongUsLLImpl.LocalPlayer.PlayerId);

                if (waitMore > 0)
                {
                    __instance.TeamTitle.text += string.Format(Language.Translate("RoleDraft.TeamTitle2"), waitMore);
                }

                __instance.TeamTitle.text += string.Format(Language.Translate("RoleDraft.TeamTitle3"), (int)(maxTimer + 1 - timer));
                yield return null;
            }
        }

        AmongUsLLImpl.HudManagerInstance.FullScreen.color = Color.black;
        __instance.FrontMost.gameObject.SetActive(true);
        GameObject.Find("BackgroundLayer")?.SetActive(true);
        if (AmongUsLLImpl.AmongUsClientInstance.AmHost)
        {
            void AssignCustomAbilities(RoleCategory category,IRoleTable rt)
            {
                foreach (var t in AssignmentType.AllTypes)
                {
                    if (!t.IsActive) continue;
                    if (t.Category == category)
                    {
                        var targetRole = t.RelatedRole;
                        RoleTable table= rt as RoleTable;
                        var players = table.roles.Where(r => r.role == targetRole).Select(r => r.playerId).ToList();
                        CategoryAssign(table, players.Count, players, players, customChances[t.Id], allRoles, (role, player) => table.EditRole(player, last => (last.role, t.EditArguments(last.argument, role))));
                    }
                }
            }
            playerRoles.Do(dic => AssignRoleTable.SetRole(dic.Key, dic.Value));
            var list = PlayerControl.AllPlayerControls.GetFastEnumerator().Select(p => p.PlayerId).ToList();
            list.RemoveAll(p => (AssignRoleTable as RoleTable)!.roles.Any(r => r.playerId == p));
            list.Do(p => AssignRoleTable.SetRole(p, Crewmate.Crewmate.MyRole));
            AssignCustomAbilities(RoleCategory.ImpostorRole, AssignRoleTable);
            AssignCustomAbilities(RoleCategory.NeutralRole, AssignRoleTable);
            AssignCustomAbilities(RoleCategory.CrewmateRole, AssignRoleTable);
            foreach (var m in Nebula.Roles.Roles.AllAllocatableModifiers().OrderBy(im => im.AssignPriority)) m.TryAssign(AssignRoleTable);
            GameOperatorManager.Instance?.Run<PreFixAssignmentEvent>(new PreFixAssignmentEvent(AssignRoleTable), false,false);
            PDebug.Log("AssignDraftRole");
            AssignRoleTable.Determine();
        }

        float myTimer = 0f;
        while (myTimer < 3f)
        {
            myTimer += Time.deltaTime;
            Color c = new Color(0, 0, 0, myTimer / 3.0f);
            __instance.FrontMost.color = c;
            yield return null;
        }

        // SoundEffectsManager.stop("draft"); 
        Clear();
        __instance.Foreground.gameObject.SetActive(true);
        isRunning = false;
        yield break;
    }

    public static void receivePick(byte playerId, byte roleId, byte flag = 0)
    {
        if (!isEnabled) return;
        PDebug.Log("receivePick");
        var roleInfo = Nebula.Roles.Roles.GetRole(roleId)??Crewmate.Crewmate.MyRole;
        if (roleInfo==null)
        {
            //NebulaPlugin.Log.Print("Cannot Draft Receive Pick,Role is null");
            return;
        }
        alreadyPicked.Add(roleInfo);
        playerRoles.TryAdd(playerId, roleInfo);
        var isRandom = flag > 0;
        var reasons = ((SelectFlags)flag).ToString();

        try
        {
            pickOrder.Remove(playerId);
            timer = 0;
            picked = true;
            var isLocalPlayer = playerId == AmongUsLLImpl.LocalPlayer.PlayerId;
            var reasonString = isRandom ? $" ({Language.Translate("RoleDraft.Random")})" : "";
            var roleString = isLocalPlayer
                ? isRandom ? $"{(roleInfo.DisplayName + reasonString).Color(roleInfo.Color.ToUnityColor())}" : $"{roleInfo.DisplayColoredName}"
                : BuildRoleString(roleInfo, isRandom, reasons);

            string prefix = playerId == AmongUsLLImpl.LocalPlayer.PlayerId ? Language.Translate("RoleDraft.You") : alreadyPicked.Count.ToString();

            selectInfoText.text+=Environment.NewLine+prefix+roleString;

            //feedText.text = Language.Translate("RoleDraft.FeedText") + _pickTable.ToString();
        }
        catch (Exception e) { /*NebulaPlugin.Log.Print(e.ToString());*/ }

        static string BuildRoleString(DefinedRole roleInfo, bool isRandom, string reason)
        {
            if (isRandom) return Language.Translate($"RoleDraft.{reason}").Color(Color.green);

            if (!DraftShowRole)
                return Language.Translate("RoleDraft.UnknownRole");

            return roleInfo.Category switch
            {
                RoleCategory.ImpostorRole when DraftHideImpRole =>
                    Language.Translate("RoleDraft.ImpostorRole").Color(Palette.ImpostorRed),
                RoleCategory.NeutralRole when DraftHideNeuRole =>
                    Language.Translate("RoleDraft.NeutralRole").Color(Color.white),
                RoleCategory.CrewmateRole when DraftHideCrewRole =>
                    Language.Translate("RoleDraft.CrewmateRole").Color(Palette.CrewmateBlue),
                _ => roleInfo.DisplayColoredName
            };
        }
    }
    [NebulaRPC]
    public static void sendPick(byte RoleId, byte fl = 0,byte pid=255)
    {
        PDebug.Log("sendPick");
        if (playerRoles.TryGetValue(pid==255?AmongUsLLImpl.LocalPlayer.PlayerId:pid, out _)) return;
        var flag = (SelectFlags)fl;
        receivePick(AmongUsLLImpl.LocalPlayer.PlayerId, RoleId, (byte)flag);

        try
        {
            // destroy all the buttons:
            foreach (var button in buttons)
            {
                GameObject.Destroy(button?.gameObject);
            }
            buttons = new();
        }
        catch (Exception e) { /*NebulaPlugin.Log.Print(e.ToString());*/ }
    }
    [NebulaRPC]
    public static void sendPickOrder()
    {
        pickOrder =/*[AmongUsLLImpl.LocalPlayer.PlayerId];*/PlayerControl.AllPlayerControls.ToArray().Where(r => !r.isDummy).Select(x => x.PlayerId).OrderBy(_ => Guid.NewGuid()).ToList();
    }
    public static void Clear()
    {
        isRunning = false;
        alreadyPicked = new();
        playerRoles = new();
        foreach (var button in buttons)
        {
            if (button)
            {
                UnityEngine.Object.Destroy(button);
            }
        }
        buttons = new();
        if (SelectInfoScreen != null) SelectInfoScreen.CloseScreen();
        if (selectInfoText)
        {
            UnityEngine.Object.Destroy(selectInfoText);
        }
        selectInfoText = null;
    }

    public enum SelectFlags : byte
    {
        Normal,
        Disconnect,
        Random
    }
}
