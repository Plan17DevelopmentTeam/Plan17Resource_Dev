using Nebula.Patches;
using Nebula.Roles.Scripts;
using Plana.Core;
using Vector2 = UnityEngine.Vector2;

namespace Plana.Roles.Crewmate;

public class Prismologist : DefinedSingleAbilityRoleTemplate<Prismologist.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, IAssignableDocument
{
    private const float extendLen=100f;
    private const float alpha=0.2f;
    private const int usesPerRound = 2;
    private Prismologist() : base("prismologist", Prism.special ? new Virial.Color(255,155,0) : new Virial.Color(40,255,218), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [PrismCooldown,VisibleNextRound])
    {
        ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImages/SpecialPrismologist.png.png")!.AsImage(115f)!;
    }
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    Image? DefinedAssignable.IconImage => NebulaAPI.AddonAsset.GetResource("RoleSmallIcon/Prismologist.PNG")!.AsImage();
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CannotLoad;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player);
    public static readonly FloatConfiguration PrismCooldown = NebulaAPI.Configurations.Configuration("options.role.prismologist.prismCooldown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    public static readonly BoolConfiguration VisibleNextRound = NebulaAPI.Configurations.Configuration("options.role.prismologist.visibleNextRound", true);
    static public Prismologist MyRole = new Prismologist();
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(Ability.PlaceIcon, "role.prismologist.ability.prism");
    }

    public class Ability : AbstractPlayerAbility, IPlayerAbility
    {
        public static Image PlaceIcon = NebulaAPI.AddonAsset.GetResource(Prism.special ? "SpecialPrismButton.PNG" : "PrismButton.png")!.AsImage(115f)!;
        int leftUses=usesPerRound;
        int firstPrism;
        List<int> prisms = new();
        Vector2 prismPointA, prismB;
        bool hasMirror;
        ModAbilityButton? placeButton;
        Dictionary<byte, PlayerDisplay> mirrors = new();
        List<GameObject> shown = new();

        public Ability(GamePlayer player) : base(player)
        {
            if (AmOwner)
            {
                placeButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.Ability, PrismCooldown, "prism", PlaceIcon, _ => MyPlayer.CanMove, _ => !MyPlayer.IsDead && leftUses > 0);
                placeButton.SetLabelType(ModAbilityButton.LabelType.Crewmate).ShowUsesIcon(3, leftUses.ToString());
                placeButton.OnClick = button =>
                {
                    SetPrism();
                    button.UpdateUsesIcon(leftUses.ToString());
                    button.StartCoolDown();
                };
                GameOperatorManager.Instance?.RegisterOnReleased(() =>
                {
                    foreach (var mirror in mirrors.Values) UnityEngine.Object.Destroy(mirror.gameObject);
                }, this);
            }
        }
        [Local]
        void OnUpdate(GameUpdateEvent ev)
        {
            if (!hasMirror || MyPlayer.IsDead || MeetingHud.Instance != null)
            {
                mirrors.Values.Do(m => m.gameObject.SetActive(false));
                return;
            }
            shown.Clear();
            foreach (var p in GamePlayer.AllPlayers)
            {
                if (p.IsDead || p.Logic.InVent) continue;
                if (p.IsInvisible && p.Role.Role == Plana.Roles.Impostor.NinjaFix.MyRole) continue;
                var mirrored = Reflect(p.TruePosition.ToUnityVector());
                if (mirrored != null) shown.Add(ShowMirror(p, mirrored.Value));
            }
            foreach (var mirror in mirrors.Values) if (!shown.Contains(mirror.gameObject)) mirror.gameObject.SetActive(false);
            foreach (var mirror in shown)
                foreach (var renderer in mirror.GetComponentsInChildren<SpriteRenderer>(true)) renderer.color = renderer.color with { a = alpha };
        }
        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            leftUses = usesPerRound;
            placeButton?.UpdateUsesIcon(leftUses.ToString());
            DestroyPending();
        }
        [Local]
        void OnOutfitChanged(PlayerOutfitChangeEvent ev)
        {
            if (mirrors.TryGetValue(ev.Player.PlayerId, out var mirror)) mirror.UpdateFromPlayerOutfit(ev.Player.VanillaPlayer, false, false);
        }
        void SetPrism()
        {
            Vector2 pos = MyPlayer.TruePosition.ToUnityVector();
            var enemy = PrismSystem.FindCandidate(MyPlayer, false);
            if (enemy != null)
            {
                FormMixed(enemy.Value, pos);
                return;
            }
            if (firstPrism != 0)
            {
                Prism? first = NebulaSyncObject.GetObject<Prism>(firstPrism);
                if (first != null)
                {
                    FormOwn(first, pos);
                    leftUses--;
                    return;
                }
                firstPrism = 0;
            }
            Prism placed = Prism.Place(pos, 0, true);
            firstPrism = placed.ObjectId;
            PrismSystem.SharePrism(placed.ObjectId, MyPlayer.PlayerId, false, pos.x, pos.y);
            leftUses--;
        }
        Vector2? Reflect(Vector2 worldPos)
        {
            Vector2 lensVec = prismB - prismPointA;
            Vector2 lensDir = lensVec.normalized;
            float along = Vector2.Dot(worldPos - prismPointA, lensDir);
            if (along < -extendLen || along > lensVec.magnitude + extendLen) return null;
            Vector2 foot = prismPointA + lensDir * along;
            Vector2 mirrored = 2f * foot - worldPos;
            if (Vector2.Distance(mirrored, worldPos) < 0.5f) return null;
            return mirrored;
        }
        void FormOwn(Prism first, Vector2 pos)
        {
            DestroyMirror();
            Vector2 aPos = first.Position.ToUnityVector();
            Prism second = Prism.Place(pos, first.ObjectId, false);
            firstPrism = 0;
            prisms.Add(first.ObjectId);
            prisms.Add(second.ObjectId);
            prismPointA = aPos;
            prismB = pos;
            hasMirror = true;
            if (VisibleNextRound)
            {
                first.ScheduleGlobal(0, aPos);
                second.ScheduleGlobal(first.ObjectId, pos);
            }
            PrismSystem.ConnectPrisms(first.ObjectId, second.ObjectId, MyPlayer.PlayerId, aPos.x, aPos.y, pos.x, pos.y);
        }
        void FormMixed((int id, byte owner, bool killer, Vector2 pos) enemy, Vector2 pos)
        {
            Vector2 enemyPos = enemy.pos;
            PrismSystem.DestroyMirrorOf(enemy.owner);
            DestroyMirror();
            PrismSystem.DropLocalPrism(enemy.id);
            PrismSystem.RemovePrism(enemy.id);
            Prism a = Prism.PlaceGlobal(enemyPos, 0);
            Prism b = Prism.PlaceGlobal(pos, a.ObjectId);
            prisms.Add(a.ObjectId);
            prisms.Add(b.ObjectId);
            prismPointA = enemyPos;
            prismB = pos;
            hasMirror = true;
            PrismSystem.ConnectMixedPrisms(a.ObjectId, b.ObjectId, enemy.owner, MyPlayer.PlayerId, enemy.killer, enemyPos.x, enemyPos.y, pos.x, pos.y, enemy.id, 0);
        }
        void DestroyPending()
        {
            if (firstPrism == 0) return;
            NebulaSyncObject.RpcDestroy(firstPrism);
            PrismSystem.RemovePrism(firstPrism);
            firstPrism = 0;
        }
        public void DestroyMirror()
        {
            prisms.Do(id =>
            {
                NebulaSyncObject.RpcDestroy(id);
                PrismSystem.RemovePrism(id);
            });
            prisms.Clear();
            hasMirror = false;
        }
        public void ApplyOwnPair(int aId, int bId, float ax, float ay, float bx, float by)
        {
            firstPrism = 0;
            prisms.Clear();
            prisms.Add(aId);
            prisms.Add(bId);
            prismPointA = new Vector2(ax, ay);
            prismB = new Vector2(bx, by);
            hasMirror = true;
        }
        public void ApplyMixedPair(int aId, int bId, float ax, float ay, float bx, float by, int consumedId)
        {
            if (firstPrism == consumedId) firstPrism = 0;
            prisms.Clear();
            prisms.Add(aId);
            prisms.Add(bId);
            prismPointA = new Vector2(ax, ay);
            prismB = new Vector2(bx, by);
            hasMirror = true;
            leftUses = 0;
            placeButton?.UpdateUsesIcon("0");
        }
        GameObject ShowMirror(GamePlayer player, Vector2 target)
        {
            if (!mirrors.TryGetValue(player.PlayerId, out var mirror))
            {
                mirror = VanillaAsset.GetPlayerDisplay();
                mirror.name = "PrismPhantom";
                UnityHelper.DoForAllChildren(mirror.gameObject, child => { child.tag = "Untagged"; child.layer = LayerExpansion.GetPlayersLayer(); }, true);
                foreach (var renderer in mirror.GetComponentsInChildren<SpriteRenderer>(true)) renderer.maskInteraction = SpriteMaskInteraction.None;
                mirror.UpdateFromPlayerOutfit(player.VanillaPlayer, false, false);
                mirrors[player.PlayerId] = mirror;
            }
            mirror.gameObject.SetActive(true);
            Vector2 delta = target - (Vector2)mirror.transform.position;
            mirror.transform.position = target.AsVector3(target.y / 1000f);
            if (delta.sqrMagnitude > 0.0001f)
            {
                var flip = mirror.Cosmetics.FlipX;
                if (delta.x < -0.01f) mirror.Cosmetics.SetFlipX(true);
                else if (delta.x > 0.01f) mirror.Cosmetics.SetFlipX(false);
                if (!mirror.Animations.IsPlayingRunAnimation() || flip != mirror.Cosmetics.FlipX) mirror.Animations.PlayRunAnimation();
                if (!mirror.Cosmetics.IsSkinPlayingRunAnim()) mirror.Cosmetics.AnimateSkinRun();
            }
            else if (mirror.Animations.IsPlayingRunAnimation() || !mirror.Animations.IsPlayingSomeAnimation())
            {
                mirror.Animations.PlayIdleAnimation();
                mirror.Cosmetics.AnimateSkinIdle();
            }
            return mirror.gameObject;
        }
    }
}
