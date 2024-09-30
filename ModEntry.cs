using ExtraMapActions.Actions;
using ExtraMapActions.Framework;
using ExtraMapActions.Models;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using xTile.Dimensions;

namespace ExtraMapActions;

internal class ModEntry : Mod {

    ModConfig Config = new();
    LogLevel logLevel => Config.DebugLogging ? LogLevel.Debug : LogLevel.Trace;
    public static string ModId = null!;
    internal static Dictionary<string, FireplaceConditionsModel> FireplaceConditionsData = new();

    public override void Entry(IModHelper helper) {
        Config = helper.ReadConfig<ModConfig>();
        ModId = ModManifest.UniqueID;

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.OnAssetRequested(e);
        helper.Events.Content.AssetReady += static (_, e) => AssetManager.OnAssetReady(e);
        helper.Events.GameLoop.SaveLoaded += static (_, e) => AssetManager.OnSaveLoaded(e);

        TileActions.Init(Monitor, helper, Config, logLevel);
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e) {
        if (!Game1.IsMasterGame) return;

        Utility.ForEachLocation((GameLocation location) => {
            if (location.modData.TryGetValue("rokugin.EMA", out string value)) {
                foreach (string key in location.modData.Keys) {
                    if (!key.StartsWith("EMA_Fireplace")) continue;

                    location.modData.Remove(key);
                }
            }

            return true;
        });
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e) {
        if (e.FromModID != ModId || e.Type != "FireplaceState") return;

        var fireplaceState = e.ReadAs<FireplaceState>();
        GameLocation location = Game1.getLocationFromName(fireplaceState.Location);
        if (fireplaceState.State == "on") {
            location.setFireplace(on: true, fireplaceState.Point.X, fireplaceState.Point.Y);
        } else {
            location.setFireplace(on: false, fireplaceState.Point.X, fireplaceState.Point.Y);
        }
    }

    private void OnWarped(object? sender, WarpedEventArgs e) {
        GameLocation location = e.NewLocation;
        string[] values = location.GetMapPropertySplitBySpaces("EMA_FireplaceLocation");
        if (values.Length < 3) return;

        Point point = new();

        if (!location.modData.TryGetValue("rokugin.EMA", out string value)) {
            return;
        }

        for (int i = 0; i < values.Length; i += 3) {
            if (!int.TryParse(values[i], out point.X)) {
                Monitor.Log("\nTile X is not valid. Must be an integer.", LogLevel.Error);
                return;
            }
            if (!int.TryParse(values[i + 1], out point.Y)) {
                Monitor.Log("\nTile Y is not valid. Must be an integer.", LogLevel.Error);
                return;
            }
            if (!GameStateQuery.CheckConditions(FireplaceConditionsData[values[i + 2]].Condition, location, Game1.player)) {
                Monitor.Log("\nConditions not met.", logLevel);
                location.setFireplace(on: false, point.X, point.Y, false);
                return;
            }

            location.setFireplace(on: true, point.X, point.Y, false);
        }

        foreach (string key in location.modData.Keys) {
            if (!key.StartsWith("EMA_Fireplace")) continue;

            string[] keySplit = key.Split("_");
            int.TryParse(keySplit[2], out int X);
            int.TryParse(keySplit[3], out int Y);

            location.setFireplace(on: location.modData[key] == "on" ? true : false, X, Y, false);
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
        var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null) return;

        configMenu.Register(
            mod: ModManifest,
            reset: () => Config = new ModConfig(),
            save: () => Helper.WriteConfig(Config)
        );

        configMenu.AddSectionTitle(
            mod: ModManifest,
            text: () => Helper.Translation.Get("crane-game-section.text")
        );
        configMenu.AddNumberOption(
            mod: ModManifest,
            getValue: () => Config.CraneGameCost,
            setValue: value => Config.CraneGameCost = value,
            name: () => Helper.Translation.Get("cost.name"),
            tooltip: () => Helper.Translation.Get("cost.tooltip"),
            min: 0
        );

        configMenu.AddSectionTitle(
            mod: ModManifest,
            text: () => Helper.Translation.Get("debug-section.text")
        );
        configMenu.AddBoolOption(
            mod: ModManifest,
            getValue: () => Config.DebugLogging,
            setValue: value => Config.DebugLogging = value,
            name: () => Helper.Translation.Get("enabled.name"),
            tooltip: () => Helper.Translation.Get("debug-enabled.tooltip")
        );
    }

}