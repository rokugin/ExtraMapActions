using ExtraMapActions.Framework;
using Microsoft.Xna.Framework;
using StardewValley;
using xTile.Dimensions;

namespace ExtraMapActions.Patches;

public class GameLocationPatch {

    public static bool OpenDoor_Prefix(GameLocation __instance, Location tileLocation, bool playSound) {
        try {
            var propertyValue = __instance.doesTileHaveProperty(tileLocation.X, tileLocation.Y, "EMA_CustomDoor", "Buildings");
                if (propertyValue != null) {
                Point point = new Point(tileLocation.X, tileLocation.Y);

                if (!__instance.interiorDoors.ContainsKey(point)) {
                    return true;
                }

                __instance.interiorDoors[point] = true;

                if (playSound) {
                    Vector2 pos = new Vector2(tileLocation.X, tileLocation.Y);
                    string audioCue = "doorOpen";

                    if (AssetManager.CustomDoorsData.TryGetValue(propertyValue, out var door)) {
                        if (door.AudioCue != null) audioCue = door.AudioCue;
                    }

                    __instance.playSound(audioCue, pos);
                }
                return false;
            }
        } catch {
        }
        return true;
    }

}