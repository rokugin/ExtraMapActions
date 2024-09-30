using ExtraMapActions.Models;
using StardewModdingAPI.Events;
using StardewValley;

namespace ExtraMapActions.Framework;

internal static class AssetManager {

    internal static void OnAssetRequested(AssetRequestedEventArgs e) {
        if (e.NameWithoutLocale.IsEquivalentTo("rokugin.EMA/FireplaceConditions")) {
            e.LoadFromModFile<Dictionary<string, FireplaceConditionsModel>>("assets/default-data.json", AssetLoadPriority.Medium);
        }
    }

    internal static void OnAssetReady(AssetReadyEventArgs e) {
        if (e.NameWithoutLocale.IsEquivalentTo("rokugin.EMA/FireplaceConditions")) {
            ModEntry.FireplaceConditionsData = Game1.content.Load<Dictionary<string, FireplaceConditionsModel>>("rokugin.EMA/FireplaceConditions");
        }
    }

    internal static void OnSaveLoaded(SaveLoadedEventArgs e) {
        ModEntry.FireplaceConditionsData = Game1.content.Load<Dictionary<string, FireplaceConditionsModel>>("rokugin.EMA/FireplaceConditions");
    }

}