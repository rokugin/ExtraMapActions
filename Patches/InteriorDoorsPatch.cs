using ExtraMapActions.Framework;
using ExtraMapActions.Models;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;

namespace ExtraMapActions.Patches;

public static class InteriorDoorsPatch {

    public static void ResetLocalState_Postfix(InteriorDoor __instance) {
        try {
            if (__instance.Tile.Properties.TryGetValue("EMA_CustomDoor", out string value) && AssetManager.CustomDoorsData.TryGetValue(value, out var door)) {
                int x = __instance.Position.X;
                int y = __instance.Position.Y;
                try {
                    __instance.Sprite = new TemporaryAnimatedSprite(door.Texture,
                                            door.SourceRect,
                                            animationInterval: door.FrameDuration,
                                            animationLength: door.AnimationFrames,
                                            1,
                                            new Vector2(x, y - 2) * 64f,
                                            flicker: false,
                                            flipped: door.Flip,
                                            ((y + 1) * 64 - 12) / 10000f,
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
                }
                catch (Exception e) {
                    ModEntry.SMonitor.Log($"\nException in ResetLocalState_Prefix for {value}. Message:\n{e.Message}\n", StardewModdingAPI.LogLevel.Error);
                }
            }
        } catch (Exception ex) {
            ModEntry.SMonitor.Log($"\nException in ResetLocalState_Prefix. Message:\n{ex.Message}\n", StardewModdingAPI.LogLevel.Error);
        }
    }

}