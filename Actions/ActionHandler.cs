using StardewValley.GameData;
using StardewValley.Locations;
using StardewValley;
using System.Reflection;
using StardewValley.Minigames;
using ExtraMapActions.Models;
using ExtraMapActions.Framework;
using StardewModdingAPI.Utilities;
using StardewValley.Extensions;

namespace ExtraMapActions.Actions;

public static class ActionHandler {
    
    public static Dictionary<string, Farmer> SendMoneyMapping = new();
    public static PerScreen<Dictionary<string, HashSet<string>>> SeenMessages = new(() => new());

    public static void LedgerOptions(Farmer who, string whichAnswer) {
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

    public static void ChooseRecipient() {
        SendMoneyMapping.Clear();
        List<Response> otherFarmers = new List<Response>();
        foreach (Farmer farmer in Game1.getAllFarmers()) {
            if (farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID && !farmer.isUnclaimedFarmhand) {
                string key = "Transfer" + (otherFarmers.Count + 1);
                string farmerName = farmer.Name;
                if (farmer.Name == "") {
                    farmerName = Game1.content.LoadString("Strings\\UI:Chat_PlayerJoinedNewName");
                }
                otherFarmers.Add(new Response(key, farmerName));
                SendMoneyMapping.Add(key, farmer);
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

    public static void PreBeginSendMoney(Farmer who, string whichAnswer) {
        if (whichAnswer.ToLower() == "cancel") return;

        Type type = Game1.getLocationFromName("ManorHouse").GetType();
        MethodInfo beginSendMoney = type.GetMethod("beginSendMoney", BindingFlags.NonPublic | BindingFlags.Instance)!;
        beginSendMoney.Invoke(Game1.getLocationFromName("ManorHouse"), [SendMoneyMapping[whichAnswer]]);
        SendMoneyMapping.Clear();
    }

    public static void DivorceChoice(Farmer who, string whichAnswer) {
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

    public static void TryToStartCraneGame(Farmer who, string whichAnswer) {
        if (!(whichAnswer.ToLower() == "yes")) return;

        if (who.Money >= ModEntry.Config.CraneGameCost) {
            who.Money -= ModEntry.Config.CraneGameCost;
            Game1.changeMusicTrack("none", track_interruptable: false, MusicContext.MiniGame);

            Game1.globalFadeToBlack(delegate {
                Game1.currentMinigame = new CraneGame();
            }, 0.008f);
        } else {
            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11325"));
        }
    }

    public static void OpenLostAndFound(Farmer who, string answer) {
        if (answer.ToLower() == "yes") who.team.CheckReturnedDonations();
    }

    public static void OpenFarmhandInventory(Farmer who, string answer) {
        if (answer.ToLower() == "cancel") return;

        if (long.TryParse(answer.Split('_')[0], out var id)) {
            Farmer? farmhand = Game1.GetPlayer(id);
            if (farmhand != null && !farmhand.isActive() && Utility.getHomeOfFarmer(farmhand) is Cabin home) {
                home.inventoryMutex.RequestLock(home.openFarmhandInventory);
            }
        }
    }

    public static List<Farmer> GetRetrievableFarmers() {
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

    public static string GetMessage(string entry, bool random) {
        if (!SeenMessages.Value.ContainsKey(entry)) {
            SeenMessages.Value.Add(entry, new HashSet<string>());
        }

        HashSet<string> seenMessages = SeenMessages.Value[entry];

        List<string> messages = [.. AssetManager.MessagesData[entry].Messages];

        messages.RemoveAll(message => string.IsNullOrWhiteSpace(message) || seenMessages.Contains(message));

        if (messages.Count == 0 && seenMessages.Count > 0) {
            seenMessages.Clear();
            return GetMessage(entry, random);
        }

        string selected = random ? Game1.random.ChooseFrom(messages) : messages[0];
        seenMessages.Add(selected);
        return selected;
    }

}