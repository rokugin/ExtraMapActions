using ExtraMapActions.Actions;
using ExtraMapActions.Framework;
using ExtraMapActions.Models;
using ExtraMapActions.Patches;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ExtraMapActions;

internal class ModEntry : Mod {

    ModConfig Config = new();
    public static IMonitor SMonitor = null!;
    TileActions TileActions = new();
    LogLevel logLevel => Config.DebugLogging ? LogLevel.Debug : LogLevel.Trace;
    public static string ModId = null!;
    
    //int smokeTimer = 250;
    //string TextureName = "LooseSprites\\Cursors";
    //Rectangle SourceRect = new Rectangle(372, 1956, 10, 10);
    //Vector2 Position;
    //bool Flipped = false;
    //float AlphaFade = 0.002f;
    //Color Color = Color.Lime;

    public override void Entry(IModHelper helper) {
        Config = helper.ReadConfig<ModConfig>();
        SMonitor = Monitor;
        ModId = ModManifest.UniqueID;

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        //helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.OnAssetRequested(e);
        helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.OnAssetInvalidated(e);

        TileActions.Init(Monitor, helper, Config, logLevel);

        var harmony = new Harmony(ModManifest.UniqueID);

        harmony.Patch(
            original: AccessTools.Method(typeof(InteriorDoor), nameof(InteriorDoor.ResetLocalState)),
            postfix: new HarmonyMethod(typeof(InteriorDoorsPatch), nameof(InteriorDoorsPatch.ResetLocalState_Postfix)));
    }

    //private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e) {
    //    if (Context.IsWorldReady) {
    //        Position = new Vector2(3f, 3f) * 64f + new Vector2(Game1.random.Next(-32, 64), Game1.random.Next(16));
    //        smokeTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
    //        if (smokeTimer <= 0) {
    //            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(
    //            textureName: TextureName,
    //            sourceRect: SourceRect,
    //            position: Position,
    //            flipped: Flipped,
    //            alphaFade: AlphaFade,
    //            color: Color) {
    //                alpha = 0.75f,
    //                motion = new Vector2(0f, -0.5f),
    //                acceleration = new Vector2(-0.002f, 0f),
    //                interval = 99999f,
    //                layerDepth = 0.144f - (float)Game1.random.Next(100) / 10000f,
    //                scale = 3f,
    //                scaleChange = 0.01f,
    //                rotationChange = (float)Game1.random.Next(-5, 6) * (float)Math.PI / 256f
    //            });
    //            smokeTimer = 100;
    //        }
    //    }
    //}

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
        GameLocation location = Game1.currentLocation;
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
            if (!GameStateQuery.CheckConditions(AssetManager.FireplaceConditionsData[values[i + 2]].Condition, location, Game1.player)) {
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
        if (e.FromModID == ModId) {
            if (e.Type == "FireplaceState") {
                var fireplaceState = e.ReadAs<FireplaceState>();
                GameLocation location = Game1.getLocationFromName(fireplaceState.Location);
                if (fireplaceState.State == "on") {
                    location.setFireplace(on: true, fireplaceState.Point.X, fireplaceState.Point.Y);
                } else {
                    location.setFireplace(on: false, fireplaceState.Point.X, fireplaceState.Point.Y);
                }
            }
        }
    }

    private void OnWarped(object? sender, WarpedEventArgs e) {
        Point point = new();
        GameLocation location = e.NewLocation;
        string[] fireplaceMapProperty = location.GetMapPropertySplitBySpaces("EMA_FireplaceLocation");
        string[] smokeMapProperty = location.GetMapPropertySplitBySpaces("EMA_SmokeLocation");
        
        if (fireplaceMapProperty.Length >=3 && location.modData.TryGetValue("rokugin.EMA", out string value)) {
            for (int i = 0; i < fireplaceMapProperty.Length; i += 3) {
                if (!int.TryParse(fireplaceMapProperty[i], out point.X)) {
                    Monitor.Log("\nTile X is not valid. Must be an integer.", LogLevel.Error);
                    return;
                }
                if (!int.TryParse(fireplaceMapProperty[i + 1], out point.Y)) {
                    Monitor.Log("\nTile Y is not valid. Must be an integer.", LogLevel.Error);
                    return;
                }
                if (!GameStateQuery.CheckConditions(AssetManager.FireplaceConditionsData[fireplaceMapProperty[i + 2]].Condition, location, Game1.player)) {
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

        if (smokeMapProperty.Length > 0) {

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