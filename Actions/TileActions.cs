using StardewModdingAPI;
using StardewValley.SpecialOrders;
using StardewValley;
using Microsoft.Xna.Framework;
using ExtraMapActions.Framework;

namespace ExtraMapActions.Actions;

public class TileActions {

    IMonitor Monitor = null!;
    IModHelper Helper = null!;
    static ModConfig Config = new();
    LogLevel logLevel;

    

    public void Init(IMonitor monitor, IModHelper helper, ModConfig config, LogLevel level) {
        Monitor = monitor;
        Config = config;
        Helper = helper;
        logLevel = level;

        SetUpTileActions();
    }

    void SetUpTileActions() {
        GameLocation.RegisterTileAction("EMA_CraneGame", HandleCraneGame);
        GameLocation.RegisterTileAction("EMA_LostAndFound", HandleLostAndFound);
        GameLocation.RegisterTileAction("EMA_OfflineFarmhandInventory", HandleOfflineFarmhandInventory);
        GameLocation.RegisterTileAction("EMA_Fireplace", HandleFireplace);
        GameLocation.RegisterTileAction("EMA_DivorceBook", HandleDivorce);
        GameLocation.RegisterTileAction("EMA_LedgerBook", HandleLedger);
        //GameLocation.RegisterTileAction("EMA_Campfire", HandleCampfire);
        GameLocation.RegisterTileAction("EMA_Message", HandleMessage);
    }

    private bool HandleMessage(GameLocation location, string[] args, Farmer farmer, Point point) {
        string entry = ArgUtility.Get(args, 1);
        bool random = ArgUtility.GetBool(args, 2);
        
        if (!AssetManager.MessagesData.ContainsKey(entry)) {
            Monitor.Log($"\nNo entry matching '{entry}' found in 'rokugin.EMA/Messages' data asset.", LogLevel.Error);
            return false;
        }

        string message = ActionHandler.GetMessage(entry, random);

        if (!string.IsNullOrWhiteSpace(message)) {
            Game1.drawDialogueNoTyping(message);
            return true;
        }

        return false;
    }

    bool HandleCraneGame(GameLocation location, string[] args, Farmer farmer, Point point) {
        Monitor.Log("\nCrane game action found, attempting to open prompt.", logLevel);

        location.createQuestionDialogue(
            Config.CraneGameCost > 0
            ? $"{Config.CraneGameCost} {Helper.Translation.Get("start-play-cost.text")}"
            : $"{Helper.Translation.Get("start-play-free.text")}",
        location.createYesNoResponses(),
        ActionHandler.TryToStartCraneGame);

        return true;
    }

    bool HandleLostAndFound(GameLocation location, string[] args, Farmer farmer, Point point) {
        Monitor.Log("Lost and found action found, checking if lost and found can be opened.", logLevel);

        if (farmer.team.returnedDonations.Count > 0 && !farmer.team.returnedDonationsMutex.IsLocked()) {
            Monitor.Log("Lost and found can be opened, attempting to open prompt.", logLevel);
            location.createQuestionDialogue(
                Helper.Translation.Get("lost-and-found-question"),
                Game1.currentLocation.createYesNoResponses(),
                ActionHandler.OpenLostAndFound);
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

    bool HandleOfflineFarmhandInventory(GameLocation location, string[] args, Farmer farmer, Point point) {
        Monitor.Log("Offline farmhand inventory action found, checking for offline farmhand inventories.", logLevel);
        List<Response> choices = new List<Response>();

        foreach (Farmer retrievableFarmer in ActionHandler.GetRetrievableFarmers()) {
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

        location.createQuestionDialogue(
            Game1.content.LoadString("Strings\\Locations:ManorHouse_LAF_FarmhandItemsQuestion"),
            choices.ToArray(),
            ActionHandler.OpenFarmhandInventory
        );

        return true;
    }

    bool HandleFireplace(GameLocation location, string[] args, Farmer farmer, Point point) {
        int xOffset = 0;
        if (args.Length > 1) {
            if (args[1] == "right") {
                xOffset = -1;
            }
        }

        point.X += xOffset;
        string lightSourceId = $"{location.NameOrUniqueName}_Fireplace_{point.X}_{point.Y}";
        bool fireplaceOn = Game1.currentLightSources.ContainsKey(lightSourceId + "_1");
        string oppositeState = fireplaceOn ? "off" : "on";
        location.setFireplace(on: !fireplaceOn, point.X, point.Y);
        location.modData[$"EMA_Fireplace_{point.X}_{point.Y}"] = oppositeState;
        Helper.Multiplayer.SendMessage(new FireplaceState(location.Name, point, !fireplaceOn), "FireplaceState", [ModEntry.ModId]);
        location.modData["rokugin.EMA"] = "Fireplace";

        return true;
    }

    bool HandleDivorce(GameLocation location, string[] args, Farmer farmer, Point point) {
        if (farmer.divorceTonight.Value) {
            string s = null!;
            if (farmer.hasCurrentOrPendingRoommate()) {
                s = Game1.content.LoadString("Strings\\Locations:ManorHouse_DivorceBook_CancelQuestion_Krobus", farmer.getSpouse().displayName);
            }
            if (s == null) {
                s = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_CancelQuestion");
            }
            location.createQuestionDialogue(s, location.createYesNoResponses(), ActionHandler.DivorceChoice);
        } else if (farmer.isMarriedOrRoommates()) {
            string s = null!;
            if (farmer.hasCurrentOrPendingRoommate()) {
                s = Game1.content.LoadString("Strings\\Locations:ManorHouse_DivorceBook_Question_Krobus", farmer.getSpouse().displayName);
            }
            if (s == null) {
                s = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Question");
            }
            location.createQuestionDialogue(s, location.createYesNoResponses(), ActionHandler.DivorceChoice);
        } else {
            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_DivorceBook_NoSpouse"));
        }

        return true;
    }

    bool HandleLedger(GameLocation location, string[] args, Farmer farmer, Point point) {
        if (farmer.useSeparateWallets) {
            List<Response> choices = new();
            if (Game1.IsMasterGame) {
                choices.Add(new Response("SendMoney", Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SendMoney")));
                if (Game1.player.changeWalletTypeTonight.Value) {
                    choices.Add(new Response("CancelMerge", Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_CancelMerge")));
                } else {
                    choices.Add(new Response("MergeWallets", Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_MergeWallets")));
                }
                choices.Add(new Response("Leave", Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_Leave")));
                location.createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SeparateWallets_HostQuestion"),
                    choices.ToArray(), ActionHandler.LedgerOptions);
            } else {
                ActionHandler.ChooseRecipient();
            }
        } else if (!Game1.getAllFarmhands().Any()) {
            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_Singleplayer"));
        } else if (Game1.IsMasterGame) {
            if (Game1.player.changeWalletTypeTonight.Value) {
                string s = Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SharedWallets_CancelQuestion");
                location.createQuestionDialogue(s, location.createYesNoResponses(), ActionHandler.LedgerOptions);
            } else {
                string s = Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SharedWallets_SeparateQuestion");
                location.createQuestionDialogue(s, location.createYesNoResponses(), ActionHandler.LedgerOptions);
            }
        } else {
            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SharedWallets_Client"));
        }

        return true;
    }

    bool HandleCampfire(GameLocation location, string[] args, Farmer farmer, Point point) {


        return true;
    }

}