using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Impostor;
using Virial.Configuration;
using Virial.Helpers;
using Vector3 = UnityEngine.Vector3;

namespace Plana.Roles.Modifier;

public class Infected : DefinedModifierTemplate, DefinedAllocatableModifier, HasAssignmentRoutine, RoleFilter, AssignableFilter<DefinedRole>, HasRoleFilter, ICodeName, ISpawnable, IAssignToCategorizedRole, DefinedAssignable, IRoleID,HasCitation, IAssignableDocument
{
    private Infected() : base("infected",NebulaTeams.ImpostorTeam.Color,[PatchManager.CanGenerateInfected,PatchManager.GenerateIfPlayer,InfectedHasGuesser,InfectedGuessNum,InfectedSelectRoleNum,InfectedSetRoleImmediately,ModifierFilterOptionEditor],true)
    {
        ConfigurationHolder?.SetDisplayState(() =>
        {
            if (PatchManager.CanGenerateInfected)
            {
                return ConfigurationHolderState.Emphasized;
            }
            return ConfigurationHolderState.Inactivated;
        });
    }
    bool AssignableFilter<DefinedRole>.Test(DefinedRole role)
    {
        ModifierFilter modifierFilter = role.ModifierFilter;
        return modifierFilter != null && modifierFilter.Test(this as DefinedModifier);
    }

    void AssignableFilter<DefinedRole>.ToggleAndShare(DefinedRole role)
    {
        ModifierFilter modifierFilter = role.ModifierFilter;
        if (modifierFilter == null)
        {
            return;
        }
        modifierFilter.ToggleAndShare(this as DefinedModifier);
    }

    void AssignableFilter<DefinedRole>.SetAndShare(DefinedRole role, bool val)
    {
        ModifierFilter modifierFilter = role.ModifierFilter;
        if (modifierFilter == null)
        {
            return;
        }
        modifierFilter.SetAndShare(this as DefinedModifier, val);
    }

    RoleFilter HasRoleFilter.RoleFilter => this;

    bool ISpawnable.IsSpawnable => ConfigurationHolder?.DisplayOption != ConfigurationHolderState.Inactivated;
    internal bool CanAssignTo(RoleCategory category)
    {
        return category == RoleCategory.CrewmateRole;
    }

    public bool CanAssignOnThisGameByConfiguration(RoleCategory category)
    {
        return (this as ISpawnable).IsSpawnable&&category == RoleCategory.CrewmateRole;
    }
    void HasAssignmentRoutine.TryAssign(IRoleTable roleTable)
    {
    }
    public void GetAssignProperties(RoleCategory category, out int assign100, out int assignRandom, out int assignChance)
    {
        assign100 = 0;
        assignRandom = 0;
        assignChance = 0;
    }
    string ICodeName.CodeName
    {
        get
        {
            return "IFCD";
        }
    }
    SpecialAssignment[] DefinedAllocatableModifier.SpecialAssignment => PatchManager.CanGenerateInfected ? [new SpecialAssignment(this, 1, 100, null, null)] : Array.Empty<SpecialAssignment>();
    int HasAssignmentRoutine.AssignPriority => 0;

    Citation HasCitation.Citation => PCitations.PlanaANDKC;
    bool DefinedModifier.IsMadmate => true;
    public static BoolConfiguration InfectedHasGuesser = NebulaAPI.Configurations.Configuration("options.assignment.InfectedHasGuesser", true);
    public static IntegerConfiguration InfectedGuessNum = NebulaAPI.Configurations.Configuration("options.assignment.InfectedGuessNum", (1, 16), 2);
    public static IntegerConfiguration InfectedSelectRoleNum = NebulaAPI.Configurations.Configuration("options.assignment.infectedselectRoleNum", (0, 20), 4,null,val=> val == 0 ? Language.Translate("options.noLimit") : val.ToString());
    public static BoolConfiguration InfectedSetRoleImmediately = NebulaAPI.Configurations.Configuration("options.assignment.InfectedSetRoleImmediately", false);
    public static ModifierFilter modifierFilter = NebulaAPI.Configurations.ModifierFilter("role.infected.modifierFilter");
    private static IConfiguration ModifierFilterOptionEditor = NebulaAPI.Configurations.Configuration(() =>
    {
        List<DefinedAllocatableModifier> assignable = new(), nonAssignable = new();
        foreach (var r in Nebula.Roles.Roles.AllAllocatableModifiers().Where(r => r != MyRole))
        {
            //ヘルプ画面に出現しない役職はスルー
            if (!r.ShowOnHelpScreen) continue;
            (modifierFilter.Test(r) ? assignable : nonAssignable).Add(r);
        }

        int allAssignableSum = assignable.Count;
        int allNonAssignableSum = nonAssignable.Count;

        if (allAssignableSum == 0)
        {
            //出現なし
            return Language.Translate("document.canAssignTo") + ":" + Language.Translate("roleFilter.none");
        }
        if (allNonAssignableSum == 0)
        {
            //割り当て不可能なし
            return Language.Translate("document.canAssignTo") + ":" + Language.Translate("roleFilter.allPattern.all");
        }
        if (allAssignableSum <= 8)
        {
            //割当先が比較的少ない場合
            return Language.Translate("document.canAssignTo") + ":" + string.Join(Language.Translate("roleFilter.separator"), assignable.Select(r => r.DisplayColoredName));
        }
        if (allNonAssignableSum <= 8)
        {
            //割当不可能な対象が比較的少ない場合
            string roles = string.Join(Language.Translate("roleFilter.separator"), nonAssignable.Select(r => r.DisplayColoredName));
            return Language.Translate("document.canAssignTo") + ":" + Language.Translate("roleFilter.exceptPattern.all").Replace("%ROLES%", roles);
        }
        //どうしようもない場合は略称で表示
        if (allAssignableSum <= allNonAssignableSum)
        {
            return Language.Translate("document.canAssignTo") + ":" + string.Join(Language.Translate("roleFilter.separator"), assignable.Select(r => r.DisplayColoredName));
        }
        else
        {
            return Language.Translate("document.canAssignTo") + ":" + string.Join(Language.Translate("roleFilter.separator"), assignable.Select(r => r.DisplayColoredName));
        }
    }, () =>
    {
        return NebulaAPI.GUI.RawButton(GUIAlignment.Center, new TextAttribute(Virial.Text.TextAlignment.Center, NebulaAPI.GUI.GetFont(FontAsset.GothicMasked), Virial.Text.FontStyle.Bold, new FontSize(1.8f, 1f, 2f, true), new Size(1.8f, 0.4f), new global::Virial.Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue), false, null), Language.Translate("options.role.modifierFilter").Color(Palette.ImpostorRed), delegate (GUIClickable _)
        {
            Nebula.Roles.RoleOptionHelper.OpenFilterScreen<DefinedAllocatableModifier>("ModifierFilter", Nebula.Roles.Roles.AllAllocatableModifiers().Where(r => r != MyRole&&r!=MadmatePLUS.MyRole&&r!=LastImpostor.MyRole),
    r => modifierFilter.Test(r), (r, val) => modifierFilter.SetAndShare(r, val), r => modifierFilter.ToggleAndShare(r));
        });
    }, null);
    static public Infected MyRole = new Infected();
    bool IAssignableDocument.HasTips => true;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        static TextAttributeOld ButtonAttribute = new TextAttributeOld(TextAttributeOld.BoldAttr) { Size = new(1.3f, 0.3f), Alignment = TMPro.TextAlignmentOptions.Center, FontMaterial = VanillaAsset.StandardMaskedFontMaterial }.EditFontSize(2f, 1f, 2f);
        MetaScreen OpenRoleSelectWindow(bool alpha100,Func<DefinedRole, bool> predicate, string underText, Action<DefinedRole> onSelected)
        {
            var window = MetaScreen.GenerateWindow(new UnityEngine.Vector2(7.6f, 4.2f), AmongUsLLImpl.HudManagerInstance.transform, new UnityEngine.Vector3(0, 0, -50f), alpha100, false,withCloseButton:false);
            if (!alpha100)
            {
                window.combinedObject.GetComponentsInChildren<SpriteRenderer>().Do(r =>
                {
                    var c = r.color;
                    c.a = 0.6f;
                    r.color = c;
                });
            }
            MetaWidgetOld widget = new();
            MetaWidgetOld inner = new();
            var roles = Nebula.Roles.Roles.AllRoles.Where(predicate).ToList();
            var list = new List<DefinedRole>();
            if (InfectedSelectRoleNum == 0)
            {
                list = roles;
            }
            else
            {
                while (list.Count < InfectedSelectRoleNum)
                {
                    var role = roles.Random();
                    if (list.Contains(role))
                    {
                        roles.Remove(role);
                        continue;
                    }
                    list.Add(role);
                    roles.Remove(role);
                }
            }
            inner.Append(list, r => new CombinedWidgetOld(-1f, IMetaWidgetOld.AlignmentOption.Center, new MetaWidgetOld.HorizonalMargin(0.1f),
                new MetaWidgetOld.Button(() => onSelected.Invoke(r), ButtonAttribute)
                {
                    RawText = r.DisplayColoredName,
                    PostBuilder = (button, renderer, text) =>
                    {
                        renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                        button.transform.localPosition += new Vector3(0.05f, 0f, 0f);
                        text.transform.localPosition += new Vector3(0.072f, 0f, 0f);
                        SpriteRenderer spriteRenderer = UnityHelper.CreateObject<SpriteRenderer>("Icon", button.transform, new Vector3(-0.65f, 0f, -0.1f), null);
                        Image roleIcon = r.GetRoleIcon();
                        spriteRenderer.sprite = ((roleIcon != null) ? roleIcon.GetSprite() : null);
                        spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                        spriteRenderer.material = RoleIcon.GetRoleIconMaterial(r, 0.8f, null);
                        spriteRenderer.transform.localScale = new Vector3(0.253f, 0.253f, 1f);
                        spriteRenderer.SetBothOrder(15);
                    }
                }), 4, -1, 0, 0.59f);
            MetaWidgetOld.ScrollView scroller = new(new(6.9f, 3.8f), inner, true) { Alignment = IMetaWidgetOld.AlignmentOption.Center };
            widget.Append(scroller);
            widget.Append(new MetaWidgetOld.Text(TextAttributeOld.BoldAttr) { MyText = new RawTextComponent(underText), Alignment = IMetaWidgetOld.AlignmentOption.Center });
            window.SetWidget(widget);
            System.Collections.IEnumerator CoCloseOnResult()
            {
                if (!MeetingHud.Instance.AsBoolFast()) yield break;
                while (MeetingHud.Instance.state == MeetingHud.VoteStates.Discussion) yield return null;
                if (SelectRoleScreen!=null)
                {
                    var role = list[UnityEngine.Random.Range(0, list.Count)];
                    MyPlayer.SetRole(role,role.DefaultAssignableArguments);
                    window.CloseScreen();
                }
            }
            window.StartCoroutine(CoCloseOnResult().WrapToIl2Cpp());
            return window;
        }
        MetaScreen SelectRoleScreen;
        bool Selected;
        [Local]
        void OnMeetingUpdate(GameUpdateEvent ev)
        {
            if (!MeetingHud.Instance.AsBoolFast())
            {
                return;
            }
            if (Selected)
            {
                return;
            }
            if (InfectedSetRoleImmediately)
            {
                return;
            }    
            if (MeetingHud.Instance.state==MeetingHud.VoteStates.Discussion)
            {
                if (SelectRoleScreen == null)
                {
                    Selected = true;
                    SelectRoleScreen = OpenRoleSelectWindow(true,(DefinedRole r) => r.IsSpawnable && r.Category == RoleCategory.ImpostorRole&&!r.IsSystemRole, Language.Translate("role.infected.selectImpRoleText"), (DefinedRole r) =>
                        {
                            MyPlayer.SetRole(r,r.DefaultAssignableArguments);
                            SelectRoleScreen?.CloseScreen();
                            SelectRoleScreen = null;
                        });
                }
            }
        }
        bool showtext;
        [Local]
        private void OnMeetingEnd(TaskPhaseStartEvent ev)
        {
            if (!showtext)
            {
                showtext = true;
                Game currentGame = NebulaAPI.CurrentGame;
                if (currentGame != null)
                {
                    TitleShower module = currentGame.GetModule<TitleShower>();
                    if (module != null)
                    {
                        module.SetText(Language.Translate("role.infected.OnBecameInfected"), MyRole.RoleColor.ToUnityColor(), 6f);
                    }
                }
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor, 0f, 0.8f, 0.7f, 0f);
                if (InfectedSetRoleImmediately)
                {
                    Selected = true;
                    SelectRoleScreen = OpenRoleSelectWindow(false, (DefinedRole r) => r.IsSpawnable && r.Category == RoleCategory.ImpostorRole&&!r.IsSystemRole, Language.Translate("role.infected.selectImpRoleText"), (DefinedRole r) =>
                    {
                        MyPlayer.SetRole(r,r.DefaultAssignableArguments);
                        SelectRoleScreen?.CloseScreen();
                        SelectRoleScreen = null;
                    });
                }
            }
        }
        void OnSheriffKill(SheriffCheckKillEvent ev)
        {
            if (ev.Player.IsModMadmate())
            {
                ev.CanKill = false;
            }
            else if (ev.Target.IsModMadmate())
            {
                ev.CanKill = true;
            }
        }
        void OnCollactorCheckTeam(CollatorCheckTeamEvent ev)
        {
            if (ev.Target.PlayerId == MyPlayer.PlayerId)
            {
                var value = NebulaAPI.Configurations.Configuration("options.role.collator.madmateIsClassifiedAs", new string[] { "options.role.collator.madmateIsClassifiedAs.impostor", "options.role.collator.madmateIsClassifiedAs.crewmate" }, 0, null, null).GetValue();
                ev.Team = value == 0 ? NebulaTeams.ImpostorTeam : NebulaTeams.CrewmateTeam;
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                Selected = false;
                showtext = false;
                if (MyPlayer.Modifiers.Any(m=>m is MadmatePLUS.Instance))
                {
                    showtext = true;
                    Selected = true;
                    SelectRoleScreen = OpenRoleSelectWindow(MeetingHud.Instance, (DefinedRole r) => r.IsSpawnable && r.Category == RoleCategory.ImpostorRole&&!r.IsSystemRole, Language.Translate("role.infected.selectImpRoleText"), (DefinedRole r) =>
                    {
                        MyPlayer.SetRole(r,r.DefaultAssignableArguments);
                        SelectRoleScreen?.CloseScreen();
                        SelectRoleScreen = null;
                    });
                    return;
                }
                if (MyPlayer.TryGetModifier<GuesserModifier.Instance>(out var modifier))
                {
                    modifier.LeftGuess = InfectedGuessNum;
                }
                else if (InfectedHasGuesser)
                {
                    MyPlayer.AddModifier(GuesserModifier.MyRole, new int[1] { InfectedGuessNum });
                }
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo||inEndScene) name += "∀".Color(MyRole.RoleColor.ToUnityColor());
        }
        bool RuntimeAssignable.MyCrewmateTaskIsIgnored
        {
            get
            {
                return true;
            }
        }
    }
}
