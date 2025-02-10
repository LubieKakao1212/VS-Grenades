using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Grenades.Items;

public class ItemThrownExplosive : Item {
    
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
        
        var dualCallByPlayer = (IPlayer) null;
        if (byEntity is EntityPlayer)
            dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer) byEntity).PlayerUID);
        byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/throw"), (Entity) byEntity, dualCallByPlayer, false, 8f);

        var entityAssetLocation = itemStack.Collectible.Attributes["projectile"].AsString();
        
        EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation(entityAssetLocation));
        EntityProjectile entity = byEntity.World.ClassRegistry.CreateEntity(entityType) as EntityProjectile;
        // ((EntityThrownStone) entity).FiredBy = (Entity) byEntity;
        // ((EntityThrownStone) entity).Damage = num1;
        // ((EntityThrownStone) entity).ProjectileStack = itemStack;
        entity.ProjectileStack = itemStack;
        entity.FiredBy = byEntity;
        entity.Damage = 1; //TODO
        entity.DropOnImpactChance = 0f;
        
        float num2 = 1f - byEntity.Attributes.GetFloat("aimingAccuracy", 0.0f);
        double num3 = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1.0) * (double) num2 * 0.75;
        double num4 = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1.0) * (double) num2 * 0.75;
        Vec3d vec3d = byEntity.ServerPos.XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0);
        Vec3d pos = (vec3d.AheadCopy(1.0, (double) byEntity.ServerPos.Pitch + num3, (double) byEntity.ServerPos.Yaw + num4) - vec3d) * 0.5;
        
        entity.ServerPos.SetPosWithDimension(byEntity.ServerPos.BehindCopy(0.21).XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0));
        entity.ServerPos.Motion.Set(pos);
        entity.Pos.SetFrom(entity.ServerPos);
        entity.World = byEntity.World;
        byEntity.World.SpawnEntity(entity);
        byEntity.StartAnimation("throw");
    }
    
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        var stack = inSlot.Itemstack;

        var collectibleAttributes = this.Attributes;
        
        var fuse = collectibleAttributes["fuse"].AsDouble(1);
        var damageRadius = collectibleAttributes["aoeRadius"].AsDouble(1);
        var peakDamage = collectibleAttributes["damage"].AsDouble(1);
        var damageTier = collectibleAttributes["damageTier"].AsInt(1);

        dsc.AppendLine(Lang.Get("grenades:desc-fuse", fuse));
        dsc.AppendLine(Lang.Get("grenades:desc-radius", damageRadius));
        dsc.AppendLine(Lang.Get("grenades:desc-damage", peakDamage));
        dsc.AppendLine(Lang.Get("grenades:desc-damageTier", damageTier));
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