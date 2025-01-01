using ExtraMapActions.Framework;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using xTile.Dimensions;
using xTile.Layers;

namespace ExtraMapActions.Patches;

public static class InteriorDoorsPatch {

    public static void ResetLocalState_Postfix(InteriorDoor __instance) {
        try {
            int x = __instance.Position.X;
            int y = __instance.Position.Y;
            Location doorLocation = new Location(x, y);
            Layer buildingsLayer = __instance.Location.Map.RequireLayer("Buildings");

            if (__instance.Tile == null) {
                __instance.Tile = buildingsLayer.Tiles[doorLocation];
            }

            if (__instance.Tile == null) return;

            if (__instance.Tile.Properties.TryGetValue("EMA_CustomDoor", out string value)) {
                if (AssetManager.CustomDoorsData.TryGetValue(value, out var door)) {
                    if (door.Texture == null) {
                        ModEntry.SMonitor.Log($"\nNo Texture set, please check your patch to the custom doors data asset.\n",
                            StardewModdingAPI.LogLevel.Error);
                        return;
                    }

                    __instance.Sprite = new TemporaryAnimatedSprite(door.Texture,
                                            door.SourceRect,
                                            animationInterval: door.FrameDuration,
                                            animationLength: door.AnimationFrames,
                                            1,
                                            new Vector2(x + door.PositionOffset.X, y - 2 + door.PositionOffset.Y) * 64f,
                                            flicker: false,
                                            flipped: door.Flip,
                                            (((y + 1) * 64 - 12) + door.DepthOffset) / 10000f,
                                            0f,
                                            Color.White,
                                            4f,
                                            0f,
                                            0f,
                                            0f) {
                        holdLastFrame = true,
                        paused = true
                    };

                    if (__instance.Value) {
                        __instance.Sprite.paused = true;
                        __instance.Sprite.resetEnd();
                    }
                } else {
                    ModEntry.SMonitor.Log($"\nEMA_CustomDoor value not set, please check your tile properties.\n",
                        StardewModdingAPI.LogLevel.Error);
                    return;
                }
            } else {
                ModEntry.SMonitor.Log($"\nNo entry for \"{value}\" found in custom doors data asset. Please check your patch to the data asset.",
                    StardewModdingAPI.LogLevel.Error);
                return;
            }
        } catch (Exception ex) {
            ModEntry.SMonitor.Log($"\nException in ResetLocalState_Postfix. Message:\n{ex.Message}\n", StardewModdingAPI.LogLevel.Error);
        }
    }

}