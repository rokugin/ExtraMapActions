using StardewModdingAPI;
using StardewValley.SpecialOrders;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.GameData;
using StardewValley.Locations;
using StardewValley.Minigames;
using ExtraMapActions.Framework;

namespace ExtraMapActions.Actions;

public class TileActions {

    static IMonitor Monitor = null!;
    static IModHelper Helper = null!;
    static ModConfig Config = new();
    static LogLevel logLevel;

    public static void Init(IMonitor monitor, IModHelper helper, ModConfig config, LogLevel level) {
        Monitor = monitor;
        Config = config;
        Helper = helper;
        logLevel = level;

        SetUpTileActions();
    }

    static void SetUpTileActions() {
        GameLocation.RegisterTileAction("EMA_CraneGame", HandleCraneGame);
        GameLocation.RegisterTileAction("EMA_LostAndFound", HandleLostAndFound);
        GameLocation.RegisterTileAction("EMA_OfflineFarmhandInventory", HandleOfflineFarmhandInventory);
        GameLocation.RegisterTileAction("EMA_Fireplace", HandleFireplace);
    }

    static bool HandleCraneGame(GameLocation location, string[] args, Farmer farmer, Point point) {
        Monitor.Log("\nCrane game action found, attempting to open prompt.", logLevel);

        Game1.currentLocation.createQuestionDialogue(
            Config.CraneGameCost > 0
            ? $"{Config.CraneGameCost} {Helper.Translation.Get("start-play-cost.text")}"
            : $"{Helper.Translation.Get("start-play-free.text")}",
        Game1.currentLocation.createYesNoResponses(),
        TryToStartCraneGame);

        return true;
    }

    static bool HandleLostAndFound(GameLocation location, string[] args, Farmer farmer, Point point) {
        Monitor.Log("Lost and found action found, checking if lost and found can be opened.", logLevel);

        if (Game1.player.team.returnedDonations.Count > 0 && !Game1.player.team.returnedDonationsMutex.IsLocked()) {
            Monitor.Log("Lost and found can be opened, attempting to open prompt.", logLevel);
            Game1.currentLocation.createQuestionDialogue(
                Helper.Translation.Get("lost-and-found-question"),
                Game1.currentLocation.createYesNoResponses(),
                OpenLostAndFound);
        } else {
            Monitor.Log("Lost and found cannot be opened, attempting to open info dialogue.", logLevel);
            string prompt =
                SpecialOrder.IsSpecialOrdersBoardUnlocked()
                ? Game1.content.LoadString("Strings\\Locations:ManorHouse_LAF_Check_OrdersUnlocked")
                : Game1.content.LoadString("Strings\\Locations:ManorHouse_LAF_Check");
            Game1.drawObjectDialogue(prompt);
        }

        return true;
    }

    static bool HandleOfflineFarmhandInventory(GameLocation location, string[] args, Farmer farmer, Point point) {
        Monitor.Log("Offline farmhand inventory action found, checking for offline farmhand inventories.", logLevel);
        List<Response> choices = new List<Response>();

        foreach (Farmer retrievableFarmer in GetRetrievableFarmers()) {
            string key = retrievableFarmer.UniqueMultiplayerID.ToString() ?? "";
            string name = retrievableFarmer.Name;

            if (retrievableFarmer.Name == "") {
                name = Game1.content.LoadString("Strings\\UI:Chat_PlayerJoinedNewName");
            }

            choices.Add(new Response(key, name));
        }
        
        Monitor.Log($"{choices.Count} farmhand inventories found.", logLevel);
        choices.Add(new Response("Cancel", Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_TransferCancel")));
        Monitor.Log("Attempting to open prompt.", logLevel);

        Game1.currentLocation.createQuestionDialogue(
            Game1.content.LoadString("Strings\\Locations:ManorHouse_LAF_FarmhandItemsQuestion"),
            choices.ToArray(),
            OpenFarmhandInventory
        );

        return true;
    }

    private static bool HandleFireplace(GameLocation location, string[] args, Farmer farmer, Point point) {
        int xOffset = 0;
        if (args.Length > 1) {
            if (args[1] == "right") {
                xOffset = -1;
            }
        }

        point.X += xOffset;
        string lightSourceId = $"{location.NameOrUniqueName}_Fireplace_{point.X}_{point.Y}";

        if (Game1.currentLightSources.ContainsKey(lightSourceId + "_1")) {
            location.setFireplace(on: false, point.X, point.Y);
            location.modData[$"EMA_Fireplace_{point.X}_{point.Y}"] = "off";
            Helper.Multiplayer.SendMessage(new FireplaceState(location.Name, point, "off"), "FireplaceState");
        } else {
            location.setFireplace(on: true, point.X, point.Y);
            location.modData[$"EMA_Fireplace_{point.X}_{point.Y}"] = "on";
            Helper.Multiplayer.SendMessage(new FireplaceState(location.Name, point, "on"), "FireplaceState");
        }
        location.modData["rokugin.EMA"] = "Fireplace";

        return true;
    }

    static void TryToStartCraneGame(Farmer who, string whichAnswer) {
        if (!(whichAnswer.ToLower() == "yes")) return;

        if (Game1.player.Money >= Config.CraneGameCost) {
            Game1.player.Money -= Config.CraneGameCost;
            Game1.changeMusicTrack("none", track_interruptable: false, MusicContext.MiniGame);

            Game1.globalFadeToBlack(delegate {
                Game1.currentMinigame = new CraneGame();
            }, 0.008f);
        } else {
            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11325"));
        }
    }

    static void OpenLostAndFound(Farmer who, string answer) {
        if (answer.ToLower() == "yes") Game1.player.team.CheckReturnedDonations();
    }

    static void OpenFarmhandInventory(Farmer who, string answer) {
        if (answer.ToLower() == "cancel") return;

        if (long.TryParse(answer.Split('_')[0], out var id)) {
            Farmer? farmhand = Game1.GetPlayer(id);
            if (farmhand != null && !farmhand.isActive() && Utility.getHomeOfFarmer(farmhand) is Cabin home) {
                home.inventoryMutex.RequestLock(home.openFarmhandInventory);
            }
        }
    }

    static List<Farmer> GetRetrievableFarmers() {
        List<Farmer> offline_farmers = new List<Farmer>(Game1.getAllFarmers());

        foreach (Farmer online_farmer in Game1.getOnlineFarmers()) {
            offline_farmers.Remove(online_farmer);
        }

        for (int i = 0; i < offline_farmers.Count; i++) {
            Farmer farmer = offline_farmers[i];
            if (Utility.getHomeOfFarmer(farmer) is Cabin home && (farmer.isUnclaimedFarmhand || home.inventoryMutex.IsLocked())) {
                offline_farmers.RemoveAt(i);
                i--;
            }
        }

        return offline_farmers;
    }

}