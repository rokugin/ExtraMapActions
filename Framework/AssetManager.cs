using ExtraMapActions.Models;
using StardewModdingAPI.Events;
using StardewValley;

namespace ExtraMapActions.Framework;

internal static class AssetManager {

    static Dictionary<string, CustomDoorsModel> PrivateCustomDoorsData = null!;
    public static Dictionary<string, CustomDoorsModel> CustomDoorsData {
        get
        {
            if (PrivateCustomDoorsData == null) {
                PrivateCustomDoorsData = Game1.content.Load<Dictionary<string, CustomDoorsModel>>("rokugin.EMA/CustomDoors");
            }
            return PrivateCustomDoorsData;
        }
    }

    static Dictionary<string, FireplaceConditionsModel> PrivateFireplaceConditionsData = null!;
    public static Dictionary<string, FireplaceConditionsModel> FireplaceConditionsData {
        get
        {
            if (PrivateFireplaceConditionsData == null) {
                PrivateFireplaceConditionsData = Game1.content.Load<Dictionary<string, FireplaceConditionsModel>>("rokugin.EMA/FireplaceConditions");
            }
            return PrivateFireplaceConditionsData;
        }
    }

    static Dictionary<string, MessagesModel> PrivateMessagesData = null!;
    public static Dictionary<string, MessagesModel> MessagesData {
        get
        {
            if (PrivateMessagesData == null) {
                PrivateMessagesData = Game1.content.Load<Dictionary<string, MessagesModel>>("rokugin.EMA/Messages");
            }
            return PrivateMessagesData;
        }
    }

    internal static void OnAssetRequested(AssetRequestedEventArgs e) {
        if (e.NameWithoutLocale.IsEquivalentTo("rokugin.EMA/FireplaceConditions")) {
            e.LoadFromModFile<Dictionary<string, FireplaceConditionsModel>>("assets/DefaultFireplaceConditions.json", AssetLoadPriority.Exclusive);
        }
        if (e.NameWithoutLocale.IsEquivalentTo("rokugin.EMA/CustomDoors")) {
            e.LoadFrom(() => new Dictionary<string, CustomDoorsModel>(), AssetLoadPriority.Exclusive);
        }
        if (e.NameWithoutLocale.IsEquivalentTo("rokugin.EMA/Messages")) {
            e.LoadFrom(() => new Dictionary<string, MessagesModel>(), AssetLoadPriority.Exclusive);
        }
    }

    internal static void OnAssetInvalidated(AssetsInvalidatedEventArgs e) {
        foreach (var name in e.NamesWithoutLocale) {
            if (name.IsEquivalentTo("rokugin.EMA/CustomDoors")) {
                PrivateCustomDoorsData = null!;
            }
            if (name.IsEquivalentTo("rokugin.EMA/FireplaceConditions")) {
                PrivateFireplaceConditionsData = null!;
            }
            if (name.IsEquivalentTo("rokugin.EMA/Messages")) {
                PrivateMessagesData = null!;
            }
        }
    }
}