using Virial.Helpers;
using Virial.Runtime;
using BindingFlags = System.Reflection.BindingFlags;
using Color = UnityEngine.Color;

namespace Plana.Core;
public static class VanillaRpc
{
    public static RemoteProcess<(GamePlayer, GamePlayer, bool,bool)> ShapeshiftRpc = new("shapeShiftPlayerRPC", (message, _) =>
    {
        if (MeetingHud.Instance.AsBoolFast())
        {
            return;
        }
        var shapeshifter = message.Item1.VanillaPlayer;
        //shapeshifter.Shapeshift(message.Item2.ToAUPlayer(), message.Item3);
        shapeshifter.shapeshifting = true;
        shapeshifter.MyPhysics.SetNormalizedVelocity(UnityEngine.Vector2.zero);
        if (GamePlayer.LocalPlayer?.AmOwner ?? false && !Minigame.Instance.AsBoolFast())
        {
            PlayerControl.HideCursorTemporarily();
        }
        RoleEffectAnimation roleEffectAnimation = UnityEngine.Object.Instantiate<RoleEffectAnimation>(DestroyableSingleton<RoleManager>.Instance.shapeshiftAnim, message.Item1.VanillaPlayer.gameObject.transform);
        roleEffectAnimation.SetMaskLayerBasedOnWhoShouldSee(GamePlayer.LocalPlayer.AmOwner);
        PlayerMaterial.SetColors(message.Item1.DefaultOutfit.outfit.ColorId, roleEffectAnimation.Renderer);
        if (message.Item1.VanillaPlayer.cosmetics.FlipX)
        {
            roleEffectAnimation.transform.position -= new UnityEngine.Vector3(0.14f, 0f, 0f);
        }
        Action midanimcb = () =>
        {
            if (message.Item1.PlayerId == message.Item2.PlayerId)
            {
                message.Item1.Unbox().RemoveOutfit("Shapeshift");
                message.Item1.VanillaPlayer.CurrentOutfitType = PlayerOutfitType.Default;
            }
            else
            {
                message.Item1.Unbox().AddOutfit(new OutfitCandidate(message.Item2.GetOutfit(75), "Shapeshift", 50, true));
                message.Item1.VanillaPlayer.CurrentOutfitType = PlayerOutfitType.Shapeshifted;
            }
            shapeshifter.cosmetics.SetScale(shapeshifter.MyPhysics.Animations.DefaultPlayerScale, shapeshifter.defaultCosmeticsScale);
            if (message.Item4)
            {
                var shapeRole = AmongUsUtil.GetRolePrefab<ShapeshifterRole>();
                Component component = UnityEngine.Object.Instantiate<ShapeshifterEvidence>(shapeRole!.EvidencePrefab);
                UnityEngine.Vector3 vector = shapeshifter.transform.position + shapeRole.EvidenceOffset * 0.7f;
                vector.z = vector.y / 1000f;
                component.transform.position = vector;
                component.tag = "Untagged";
                component.gameObject.tag = "Untagged";
            }
        };
        roleEffectAnimation.MidAnimCB = midanimcb;
        float shapeshiftScale = shapeshifter.MyPhysics.Animations.ShapeshiftScale;
        if (AprilFoolsMode.ShouldLongAround())
        {
            shapeshifter.cosmetics.ShowLongModeParts(false);
            shapeshifter.cosmetics.SetHatVisorVisible(false);
        }
        shapeshifter.StartCoroutine(shapeshifter.ScalePlayer(shapeshiftScale, 0.25f));
        Action OnClipEnd = () =>
        {
            shapeshifter.shapeshifting = false;
            if (AprilFoolsMode.ShouldLongAround())
            {
                shapeshifter.cosmetics.ShowLongModeParts(true);
                shapeshifter.cosmetics.SetHatVisorVisible(true);
            }
        };
        roleEffectAnimation.Play(shapeshifter, OnClipEnd, AmongUsLLImpl.LocalPlayer.cosmetics.FlipX, RoleEffectAnimation.SoundType.Local, 0f, true, 0f);
    });
    public static RemoteProcess<(GamePlayer, bool)> PhantomAnimVanishRpc = new("PhantomVanishRPC", (message, _) =>
    {
        if (MeetingHud.Instance.AsBoolFast())
        {
            return;
        }
        var phantomPlayer = message.Item1.VanillaPlayer;
        var phantomRole = AmongUsUtil.GetRolePrefab<PhantomRole>();
        if (message.Item2)
        {
            RoleEffectAnimation roleEffectAnimation = UnityEngine.Object.Instantiate<RoleEffectAnimation>(DestroyableSingleton<RoleManager>.Instance.vanish_ChargeAnim, phantomPlayer.gameObject.transform);
            roleEffectAnimation.SetMaskLayerBasedOnWhoShouldSee(phantomPlayer.AmOwner);
            PlayerMaterial.SetColors(message.Item1.DefaultOutfit.outfit.ColorId, roleEffectAnimation.Renderer);
            roleEffectAnimation.Play(phantomPlayer, new System.Action(()=>
            {
                if (message.Item1.AmOwner)
                {
                    if (Constants.ShouldPlaySfx())
                    {
                        AmongUsLLImpl.SoundManagerInstance.PlayDynamicSound("PhantomVanish", phantomRole!.UseSound, false, new System.Action<AudioSource, float>(phantomRole.SoundDynamics), AmongUsLLImpl.SoundManagerInstance.SfxChannel);
                    }
                }
                RoleEffectAnimation roleEffectAnimation2 = UnityEngine.Object.Instantiate<RoleEffectAnimation>(DestroyableSingleton<RoleManager>.Instance.vanish_PoofAnim, phantomPlayer.gameObject.transform);
                PlayerMaterial.SetColors(message.Item1.DefaultOutfit.outfit.ColorId, roleEffectAnimation2.Renderer);
                roleEffectAnimation2.Play(phantomPlayer,null , AmongUsLLImpl.LocalPlayer.cosmetics.FlipX, RoleEffectAnimation.SoundType.Local, 0f, false, 0f);
            }), AmongUsLLImpl.LocalPlayer.cosmetics.FlipX, RoleEffectAnimation.SoundType.Local, 0.5f, true, -0.05f);
        }
        else
        {
            if (message.Item1.AmOwner)
            {
                if (Constants.ShouldPlaySfx() && !MeetingHud.Instance.AsBoolFast())
                {
                    AmongUsLLImpl.SoundManagerInstance.PlayDynamicSound("PhantomAppear", phantomRole!.appearSound, false, new System.Action<AudioSource,float>(phantomRole.SoundDynamics), AmongUsLLImpl.SoundManagerInstance.SfxChannel);
                }
                RoleEffectAnimation roleEffectAnimation = UnityEngine.Object.Instantiate<RoleEffectAnimation>(DestroyableSingleton<RoleManager>.Instance.vanish_ChargeAnim, phantomPlayer.gameObject.transform);
                roleEffectAnimation.SetMaskLayerBasedOnWhoShouldSee(phantomPlayer.AmOwner);
                PlayerMaterial.SetColors(message.Item1.DefaultOutfit.outfit.ColorId, roleEffectAnimation.Renderer);
                roleEffectAnimation.Play(phantomPlayer, new System.Action(() =>
                {
                    RoleEffectAnimation roleEffectAnimation2 = UnityEngine.Object.Instantiate<RoleEffectAnimation>(DestroyableSingleton<RoleManager>.Instance.appear_PoofAnim, phantomPlayer.gameObject.transform);
                    PlayerMaterial.SetColors(message.Item1.DefaultOutfit.outfit.ColorId, roleEffectAnimation2.Renderer);
                    roleEffectAnimation2.SetMaskLayerBasedOnWhoShouldSee(phantomPlayer.AmOwner);
                    roleEffectAnimation2.Play(phantomPlayer, null, AmongUsLLImpl.LocalPlayer.cosmetics.FlipX, RoleEffectAnimation.SoundType.Local, 0f, true, 0f);
                }), AmongUsLLImpl.LocalPlayer.cosmetics.FlipX, RoleEffectAnimation.SoundType.Local, 1f, true, -0.05f);
            }
            else
            {
                NebulaManager.Instance.StartDelayAction(1f, () =>
                {
                    RoleEffectAnimation roleEffectAnimation = UnityEngine.Object.Instantiate<RoleEffectAnimation>(DestroyableSingleton<RoleManager>.Instance.appear_PoofAnim, phantomPlayer.gameObject.transform);
                    PlayerMaterial.SetColors(message.Item1.DefaultOutfit.outfit.ColorId, roleEffectAnimation.Renderer);
                    roleEffectAnimation.SetMaskLayerBasedOnWhoShouldSee(AmongUsLLImpl.LocalPlayer.AmOwner);
                    roleEffectAnimation.Play(phantomPlayer, null, AmongUsLLImpl.LocalPlayer.cosmetics.FlipX, RoleEffectAnimation.SoundType.Local, 0f, true, 0f);
                });
            }
        }
    });
}