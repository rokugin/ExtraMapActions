using ExtraMapActions.Actions;
using ExtraMapActions.Framework;
using ExtraMapActions.Patches;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ExtraMapActions;

internal class ModEntry : Mod {

    public static ModConfig Config = new();
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
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        //helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.OnAssetRequested(e);
        helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.OnAssetInvalidated(e);

        TileActions.Init(Monitor, helper, Config, logLevel);

        var harmony = new Harmony(ModManifest.UniqueID);

        harmony.Patch(
            original: AccessTools.Method(typeof(InteriorDoor), nameof(InteriorDoor.ResetLocalState)),
            postfix: new HarmonyMethod(typeof(InteriorDoorsPatch), nameof(InteriorDoorsPatch.ResetLocalState_Postfix)));
        harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.openDoor)),
            prefix: new HarmonyMethod(typeof(GameLocationPatch), nameof(GameLocationPatch.OpenDoor_Prefix)));
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e) {
        GameLocation location = Game1.currentLocation;

        SetFireplacesByPreviousState(location);
        SetFireplacesByMapProperty(location);
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

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e) {
        if (e.FromModID == ModId && e.Type == "FireplaceState") {
            var fireplaceState = e.ReadAs<FireplaceState>();
            Game1.getLocationFromName(fireplaceState.Location).setFireplace(on: fireplaceState.On, fireplaceState.Point.X, fireplaceState.Point.Y, false);
            Monitor.Log("Message received, fireplace set", LogLevel.Debug);
        }
    }

    private void OnWarped(object? sender, WarpedEventArgs e) {
        GameLocation location = e.NewLocation;
        //string[] smokeMapProperty = location.GetMapPropertySplitBySpaces("EMA_SmokeLocation");
        SetFireplacesByPreviousState(location);
        SetFireplacesByMapProperty(location);

        //if (smokeMapProperty.Length > 0) {

        //}
    }

    void SetFireplacesByPreviousState(GameLocation location) {
        if (!location.modData.TryGetValue("rokugin.EMA", out string value) || value == null) return;
        List<string> fireplaces = location.modData.Keys.Where(s => s.StartsWith("EMA_Fireplace")).ToList();

        foreach (var fireplace in fireplaces) {
            string[] fireplaceSplit = fireplace.Split("_");
            int.TryParse(fireplaceSplit[2], out int X);
            int.TryParse(fireplaceSplit[3], out int Y);
            location.setFireplace(location.modData[fireplace] == "on" ? true : false, X, Y, false);
        }
    }

    void SetFireplacesByMapProperty(GameLocation location) {
        string[] fireplaceLocation = location.GetMapPropertySplitBySpaces("EMA_FireplaceLocation");
        if (fireplaceLocation.Length < 3) return;
        Point point = new();

        for (int i = 0; i < fireplaceLocation.Length; i += 3) {
            if (!int.TryParse(fireplaceLocation[i], out point.X)) {
                Monitor.Log($"\nInvalid X field for entry #{i} in EMA_FireplaceLocation map property for {location.NameOrUniqueName}.\n", logLevel);
                return;
            }
            if (!int.TryParse(fireplaceLocation[i + 1], out point.Y)) {
                Monitor.Log($"\nInvalid Y field for entry #{i} in EMA_FireplaceLocation map property for {location.NameOrUniqueName}.\n", logLevel);
                return;
            }

            bool flag = GameStateQuery.CheckConditions(AssetManager.FireplaceConditionsData[fireplaceLocation[i + 2]].Condition, location, Game1.player);
            if (AssetManager.FireplaceConditionsData[fireplaceLocation[i + 2]].UsePlayerState) flag = PreviousState(location, flag, point);
            SetFireplace(location, point, flag);
        }
    }

    bool PreviousState(GameLocation location, bool flag, Point point) {
        if (!location.modData.TryGetValue("rokugin.EMA", out string value)) return flag;

        if (location.modData.TryGetValue($"EMA_Fireplace_{point.X}_{point.Y}", out value)) {
            return value == "on" ? true : false;
        }
        return flag;
    }

    void SetFireplace(GameLocation location, Point point, bool flag) {
        string state = flag ? "on" : "off";
        location.setFireplace(on: flag, point.X, point.Y, false);
        location.modData[$"EMA_Fireplace_{point.X}_{point.Y}"] = state;
        Monitor.Log("Fireplace set, message sent", LogLevel.Debug);
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