using System;
using System.Text;
using Grenades.Collectible;
using Grenades.Entities;
using Grenades.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Grenades.Items;

public class ItemThrownExplosive : Item, IGrenade {

    public DefGrenadeStatValues Stats { get; private set; }

    public AssetLocation ProjectileCode { get; private set; }

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);
        var stats = Attributes["explosive"].AsObject<DefGrenadeStatValues>(default, Code.Domain);
        var config = api.ModLoader.GetModSystem<ConfigModSystem>().Config;
        config.Apply(ref stats, Code, api.Logger);

        ProjectileCode = AssetLocation.Create(Attributes["projectile"].AsString(), Code.Domain);
        ProjectileCode = ProjectileCode.WithVariants(VariantStrict);
        
        Stats = stats;
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling) {
        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        byEntity.Attributes.SetInt("aiming", 1);
        byEntity.Attributes.SetInt("aimingCancel", 0);
        byEntity.StartAnimation("aim");

        handling = EnumHandHandling.Handled;
    }

    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
        if (byEntity.Attributes.GetInt("aimingCancel", 0) == 1)
            return false;
        return true;
    }

    public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason) {
        byEntity.Attributes.SetInt("aiming", 0);
        byEntity.StopAnimation("aim");
        if (cancelReason != EnumItemUseCancelReason.ReleasedMouse)
            byEntity.Attributes.SetInt("aimingCancel", 1);
        return true;
    }

    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) {
        if (byEntity.Attributes.GetInt("aimingCancel", 0) == 1)
            return;
        byEntity.Attributes.SetInt("aiming", 0);
        byEntity.StopAnimation("aim");
        if (secondsUsed < 0.3499999940395355)
            return;
        
        ItemStack itemStack = slot.TakeOut(1);
        slot.MarkDirty();
        
        var dualCallByPlayer = (IPlayer?) null;
        if (byEntity is EntityPlayer)
            dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer) byEntity).PlayerUID);
        byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/throw"), (Entity) byEntity, dualCallByPlayer, false, 8f);
        
        EntityProperties entityType = byEntity.World.GetEntityType(ProjectileCode);
        Entity entity = byEntity.World.ClassRegistry.CreateEntity(entityType);

        if (entity is not IGrenadeProjectile grenade) {
            api.Logger.Error($"Invalid projectile code in {Code}; {entity} is not {typeof(IGrenadeProjectile)}");
            return;
        }
        
        grenade.GrenadeStack = itemStack;
        grenade.GrenadeStats = Stats;
        grenade.FiredBy = byEntity;
        // grenade.Damage = 1; //TODO Impact damage
        
        float num2 = 1f - byEntity.Attributes.GetFloat("aimingAccuracy", 0.0f);
        double num3 = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1.0) * (double) num2 * 0.75;
        double num4 = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1.0) * (double) num2 * 0.75;
        
        var force = Stats.LaunchForce;
        
        
        Vec3d motion = Vec3d.Zero.AheadCopy( force, byEntity.ServerPos.Pitch + num3, byEntity.ServerPos.Yaw + num4);
        
        const double spawnOffset = -0.1;
        
        entity.ServerPos.SetPosWithDimension(byEntity.ServerPos.AheadCopy(spawnOffset).XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0));
        entity.ServerPos.ApplyImpulse(motion);
        entity.Pos.SetFrom(entity.ServerPos);
        entity.World = byEntity.World;
        byEntity.World.SpawnEntity(entity);
        byEntity.StartAnimation("throw");
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        
        dsc.AppendLine(Lang.Get("grenades:desc-fuse", Stats.Fuse));
        dsc.AppendLine(Lang.Get("grenades:desc-radius", Stats.Radius));
        dsc.AppendLine(Lang.Get("grenades:desc-damage", Stats.Damage));
        dsc.AppendLine(Lang.Get("grenades:desc-damageTier", Stats.DamageTier));
        dsc.AppendLine(Lang.Get("grenades:desc-force", Stats.LaunchForce));
    }

    // public override bool RequiresTransitionableTicking(IWorldAccessor world, ItemStack itemstack) {
    //     return itemstack.Attributes.GetBool("Ignited");
    // }
    //
    // public override ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties props) {
    //     return base.OnTransitionNow(slot, props);
    // }


    // public Vec3d AimRandomly(Entity byEntity) {
    //     float accuracy = Math.Max(1f / 1000f, 1f - byEntity.Attributes.GetFloat("aimingAccuracy", 0.0f));
    //     // double num4 = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1.0) * (double) num3 * 0.75;
    //     // double num5 = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1.0) * (double) num3 * 0.75;
    //     Vec3d vec3d = byEntity.ServerPos.XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0);
    //     Vec3d pos = (vec3d.AheadCopy(1.0, (double) byEntity.SidedPos.Pitch + num4, (double) byEntity.SidedPos.Yaw + num5) - vec3d) * byEntity.Stats.GetBlended("bowDrawingStrength");
    //
    // }
}