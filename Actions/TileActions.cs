using StardewModdingAPI;
using StardewValley.SpecialOrders;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.GameData;
using StardewValley.Locations;
using StardewValley.Minigames;
using ExtraMapActions.Framework;
using System.Reflection;

namespace ExtraMapActions.Actions;

public class TileActions {

    IMonitor Monitor = null!;
    IModHelper Helper = null!;
    ModConfig Config = new();
    LogLevel logLevel;

    Dictionary<string, Farmer> sendMoneyMapping = new();

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
    }

    bool HandleCraneGame(GameLocation location, string[] args, Farmer farmer, Point point) {
        Monitor.Log("\nCrane game action found, attempting to open prompt.", logLevel);

        location.createQuestionDialogue(
            Config.CraneGameCost > 0
            ? $"{Config.CraneGameCost} {Helper.Translation.Get("start-play-cost.text")}"
            : $"{Helper.Translation.Get("start-play-free.text")}",
        location.createYesNoResponses(),
        TryToStartCraneGame);

        return true;
    }

    bool HandleLostAndFound(GameLocation location, string[] args, Farmer farmer, Point point) {
        Monitor.Log("Lost and found action found, checking if lost and found can be opened.", logLevel);

        if (farmer.team.returnedDonations.Count > 0 && !farmer.team.returnedDonationsMutex.IsLocked()) {
            Monitor.Log("Lost and found can be opened, attempting to open prompt.", logLevel);
            location.createQuestionDialogue(
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

    bool HandleOfflineFarmhandInventory(GameLocation location, string[] args, Farmer farmer, Point point) {
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

        location.createQuestionDialogue(
            Game1.content.LoadString("Strings\\Locations:ManorHouse_LAF_FarmhandItemsQuestion"),
            choices.ToArray(),
            OpenFarmhandInventory
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
            location.createQuestionDialogue(s, location.createYesNoResponses(), DivorceChoice);
        } else if (farmer.isMarriedOrRoommates()) {
            string s = null!;
            if (farmer.hasCurrentOrPendingRoommate()) {
                s = Game1.content.LoadString("Strings\\Locations:ManorHouse_DivorceBook_Question_Krobus", farmer.getSpouse().displayName);
            }
            if (s == null) {
                s = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Question");
            }
            location.createQuestionDialogue(s, location.createYesNoResponses(), DivorceChoice);
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
                    choices.ToArray(), LedgerOptions);
            } else {
                ChooseRecipient();
            }
        } else if (!Game1.getAllFarmhands().Any()) {
            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_Singleplayer"));
        } else if (Game1.IsMasterGame) {
            if (Game1.player.changeWalletTypeTonight.Value) {
                string s = Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SharedWallets_CancelQuestion");
                location.createQuestionDialogue(s, location.createYesNoResponses(), LedgerOptions);
            } else {
                string s = Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SharedWallets_SeparateQuestion");
                location.createQuestionDialogue(s, location.createYesNoResponses(), LedgerOptions);
            }
        } else {
            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SharedWallets_Client"));
        }

        return true;
    }

    bool HandleCampfire(GameLocation location, string[] args, Farmer farmer, Point point) {


        return true;
    }

    void LedgerOptions(Farmer who, string whichAnswer) {
        if (whichAnswer.ToLower() == "leave") return;
        if (whichAnswer.ToLower() == "no") return;

        if (whichAnswer.ToLower() == "sendmoney") {
            DelayedAction delayed = new DelayedAction(50);
            delayed.behavior = () => {
                ChooseRecipient();
            };
            Game1.delayedActions.Add(delayed);
        } else if (whichAnswer.ToLower() == "cancelmerge") {
            who.changeWalletTypeTonight.Value = false;
            Game1.Multiplayer.globalChatInfoMessage("MergeWalletsCancel", who.Name);
        } else if (whichAnswer.ToLower() == "mergewallets") {
            who.changeWalletTypeTonight.Value = true;
            Game1.Multiplayer.globalChatInfoMessage("MergeWallets", who.Name);
        } else if (whichAnswer.ToLower() == "yes") {
            if (who.changeWalletTypeTonight.Value) {
                who.changeWalletTypeTonight.Value = false;
                Game1.Multiplayer.globalChatInfoMessage("SeparateWalletsCancel", who.Name);
            } else {
                who.changeWalletTypeTonight.Value = true;
                Game1.Multiplayer.globalChatInfoMessage("SeparateWallets", who.Name);
            }
        }
    }

    void ChooseRecipient() {
        sendMoneyMapping.Clear();
        List<Response> otherFarmers = new List<Response>();
        foreach (Farmer farmer in Game1.getAllFarmers()) {
            if (farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID && !farmer.isUnclaimedFarmhand) {
                string key = "Transfer" + (otherFarmers.Count + 1);
                string farmerName = farmer.Name;
                if (farmer.Name == "") {
                    farmerName = Game1.content.LoadString("Strings\\UI:Chat_PlayerJoinedNewName");
                }
                otherFarmers.Add(new Response(key, farmerName));
                sendMoneyMapping.Add(key, farmer);
            }
        }
        if (otherFarmers.Count == 0) {
            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_NoFarmhands"));
            return;
        }
        otherFarmers.Sort((Response x, Response y) => string.Compare(x.responseKey, y.responseKey));
        otherFarmers.Add(new Response("Cancel", Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_TransferCancel")));
        Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SeparateWallets_TransferQuestion"),
            otherFarmers.ToArray(), PreBeginSendMoney);
    }

    void PreBeginSendMoney(Farmer who, string whichAnswer) {
        if (whichAnswer.ToLower() == "cancel") return;

        Type type = Game1.getLocationFromName("ManorHouse").GetType();
        MethodInfo beginSendMoney = type.GetMethod("beginSendMoney", BindingFlags.NonPublic | BindingFlags.Instance)!;
        beginSendMoney.Invoke(Game1.getLocationFromName("ManorHouse"), [sendMoneyMapping[whichAnswer]]);
        sendMoneyMapping.Clear();
    }

    void DivorceChoice(Farmer who, string whichAnswer) {
        if (whichAnswer.ToLower() != "yes") return;

        string s = null!;
        if (who.divorceTonight.Value) {
            who.divorceTonight.Value = false;
            if (!who.hasRoommate()) {
                who.addUnearnedMoney(50000);
            }
            if (who.hasCurrentOrPendingRoommate()) {
                s = Game1.content.LoadString("Strings\\Locations:ManorHouse_DivorceBook_Cancelled_Krobus", who.getSpouse().displayName);
            }
            if (s == null) {
                s = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Cancelled");
            }
            Game1.drawObjectDialogue(s);
            if (!who.hasRoommate()) {
                Game1.Multiplayer.globalChatInfoMessage("DivorceCancel", who.Name);
            }
        } else {
            if (who.Money >= 50000 || who.hasCurrentOrPendingRoommate()) {
                if (!who.hasRoommate()) {
                    who.Money -= 50000;
                }
                who.divorceTonight.Value = true;
                if (who.hasCurrentOrPendingRoommate()) {
                    s = Game1.content.LoadString("Strings\\Locations:ManorHouse_DivorceBook_Filed_Krobus", who.getSpouse().displayName);
                }
                if (s == null) {
                    s = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Filed");
                }
                Game1.drawObjectDialogue(s);
                if (!who.hasRoommate()) {
                    Game1.Multiplayer.globalChatInfoMessage("Divorce", who.Name);
                }
            } else {
                Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney1"));
            }
        }
    }

    void TryToStartCraneGame(Farmer who, string whichAnswer) {
        if (!(whichAnswer.ToLower() == "yes")) return;

        if (who.Money >= Config.CraneGameCost) {
            who.Money -= Config.CraneGameCost;
            Game1.changeMusicTrack("none", track_interruptable: false, MusicContext.MiniGame);

            Game1.globalFadeToBlack(delegate {
                Game1.currentMinigame = new CraneGame();
            }, 0.008f);
        } else {
            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11325"));
        }
    }

    void OpenLostAndFound(Farmer who, string answer) {
        if (answer.ToLower() == "yes") who.team.CheckReturnedDonations();
    }

    void OpenFarmhandInventory(Farmer who, string answer) {
        if (answer.ToLower() == "cancel") return;

        if (long.TryParse(answer.Split('_')[0], out var id)) {
            Farmer? farmhand = Game1.GetPlayer(id);
            if (farmhand != null && !farmhand.isActive() && Utility.getHomeOfFarmer(farmhand) is Cabin home) {
                home.inventoryMutex.RequestLock(home.openFarmhandInventory);
            }
        }
    }

    List<Farmer> GetRetrievableFarmers() {
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