# Extra Map Actions

## Contents

* [Introduction](#introduction)
* [Examples](#examples)
  * [Content Patcher](#cp)
  * [Tiled](#tiled)
* [Extras](#extras)
  * [Crane Game Prizes](#craneprizes)

## Introduction<span id="introduction"></span>
Extra Map Actions listens for player interaction with tiles with specific TileData properties, in order to allow for more places that can open the crane game, lost and found, and offline farmhand inventories.

The config contains two settings, `Debug Logging` and `Crane Game Cost`. Debug Logging will post logs to the console window as you interact with tiles that have the appropriate TileData, it's not that useful.<br>
Crane Game Cost must be a positive value or zero.<br>
If zero the prompt will change to reflect that it's free, otherwise if the player has enough money they'll be charged the cost when they select Yes to start playing.

## Examples<span id="examples"></span>
First, if you're not familiar with Content Patcher's `EditMap` then I suggest [checking out the docs](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide/action-editmap.md) for more detailed info about how it works.

### Content Patcher<span id="cp"></span>
For my example, I added a few test objects to the bus stop.<br>
![Screenshot of test objects at the bus stop.](screenshots/test-objects.png)


With those in place, I used CP to add my custom properties to the bus stop map.
```json
{
  "Changes": [
    {
      "Action": "EditMap",
      "Target": "Maps/BusStop",
      "MapTiles": [
        {
          "Position": {
            "X": 19,
            "Y": 22
          },
          "Layer": "Buildings",
          "SetProperties": {
            "Action": "EMA_LostAndFound"
          }
        },
        {
          "Position": {
            "X": 17,
            "Y": 22
          },
          "Layer": "Buildings",
          "SetProperties": {
            "Action": "ema_offlinefarmhandinventory"
          }
        }
      ]
    }
  ]
}
```
The property value is case-insensitive.

### Tiled<span id="tiled"></span>
In order to add the property to your map in Tiled, select the Buildings object layer<br>
![Screenshot of Buildings object layer in Tiled selected.](screenshots/select-objects-layer.png)


Select the Insert Rectangle tool<br>
![Screenshot of Insert Rectangle tool selected.](screenshots/insert-rectangle-tool.png)


Use the Insert Rectangle tool to drag a rectangle onto the tile you want the action to be on, which will create this dotted rectangle and the properties panel will show the data for that object<br>
![Screenshot of object and properties window.](screenshots/tile-data-properties-window.png)
<br>
Note: something must be on the tile layer, even if it's an invisible tile


If you need to select the object for any reason, select the Select Objects tool<br>
![Screenshot of Select Objects tool.](screenshots/select-objects-tool.png)


Name the object TileData<br>
![Screenshot of object named TileData.](screenshots/name-object.png)


At the bottom of the properties window, click the + button<br>
![Screenshot of add property button.](screenshots/add-property-button.png)


A new window will pop up, here we'll put `Action` and click OK<br>
![Screenshot of add property window.](screenshots/add-property-window.png)


Now in our properties window, we'll put the action we're trying to invoke<br>
![Screenshot of properties window with action filled out.](screenshots/properties-window-action.png)


Note: the action value is case-insensitive.
## Extras<span id="extras"></span>
### Crane Game Prizes<span id="craneprizes"></span>
Crane game prizes are mostly hardcoded, however you can use `Data/Movies` to add to or completely overwrite the prize lists.<br>
More detailed information can be found on the [migration page of the wiki](https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6#Custom_movies), but I have some examples and notes I will make here.

The first example is for completely overwriting the prize lists so that only your chosen prizes are available:
```json
{
  "Changes": [
    {
      "Action": "EditData",
      "Target": "Data/Movies",
      "TargetField": [
        "spring_movie_0"
      ],
      "Entries": {
        "CranePrizes": [
          {
            "Rarity": 1,
            "Id": "CommonGroupDiamond",
            "ItemId": "(O)72"
          },
          {
            "Rarity": 2,
            "Id": "RareGroupDiamond",
            "ItemId": "(O)72"
          },
          {
            "Rarity": 3,
            "Id": "DeluxeGroupDiamond",
            "ItemId": "(O)72"
          }
        ],
        "ClearDefaultCranePrizeGroups": [
          1,
          2,
          3
        ]
      },
      "When": {
        "LocationName": "BusStop"
      },
      "Update": "OnLocationChange"
    }
  ]
}
```
Notes:<br>
An empty prize group will cause errors, so if you use `ClearDefaultCranePrizeGroup` you have to add at least one entry to `CranePrizes` for every group you clear. In my example, since I'm clearing all three groups, I add the diamond to each group to avoid errors.

The `When` and `Update` fields are included to make the changes only happen when you enter the map with your crane game, this way you don't make changes to the default crane game at the theater. I use `BusStop` in my example because that's where my crane game is, but you would use the location name of wherever your crane game is.

These prize lists are specific to what movie should be playing, even if you haven't unlocked the theater yet, so in order to have prize lists that are the same you have to make changes to the `CranePrizes` for every movie, alternatively this means you can easily have seasonal and alternating yearly different prize lists.

---

The next example is for if you only want to add prizes without modifying the existing lists:
```json
{
  "Changes": [
    {
      "Action": "EditData",
      "Target": "Data/Movies",
      "TargetField": [
        "spring_movie_0",
        "CranePrizes"
      ],
      "Entries": {
        "-1": {
          "Rarity": 1,
          "Id": "CommonDiamond",
          "ItemId": "(O)72"
        },
        "-2": {
          "Rarity": 2,
          "Id": "RareDiamond",
          "ItemId": "(O)72"
        },
        "-3": {
          "Rarity": 3,
          "Id": "DeluxeDiamond",
          "ItemId": "(O)72"
        }
      },
      "When": {
        "LocationName": "BusStop"
      },
      "Update": "OnLocationChange"
    }
  ]
}
```
Notes:<br>
This time, in order to avoid overwriting the movie specific prizes, we use `TargetField` to target `CranePrizes` and negative indexed entries in order to only add new entries without risking editing existing entries.

The `When` and `Update` fields are included to make the changes only happen when you enter the map with your crane game, this way you don't make changes to the default crane game at the theater. I use `BusStop` in my example because that's where my crane game is, but you would use the location name of wherever your crane game is.

---

Because we're targeting `CranePrizes` like this, we can't also edit `ClearDefaultCranePrizeGroups` in one patch, so if we want to any of the default prize groups, we have to make a separate patch:
```json
{
  "Changes": [
    {
      "Action": "EditData",
      "Target": "Data/Movies",
      "TargetField": [
        "spring_movie_0",
        "ClearDefaultCranePrizeGroups"
      ],
      "Entries": {
        "1": "1",
        "2": "2",
        "3": "3"
      },
      "When": {
        "LocationName": "BusStop"
      },
      "Update": "OnLocationChange"
    }
  ]
}
```
Notes:<br>
This is obviously optional if you don't want to clear default prize groups and the same rules as earlier apply if you do, any groups you clear have to have an entry in the previous patch to add a new entry, empty groups will cause errors.

The `When` and `Update` fields are included to make the changes only happen when you enter the map with your crane game, this way you don't make changes to the default crane game at the theater. I use `BusStop` in my example because that's where my crane game is, but you would use the location name of wherever your crane game is.
