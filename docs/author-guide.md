# Extra Map Actions (EMA)

## Contents
* [Introduction](#introduction)
* [Usage](#usage)
  * [Fireplaces](#fireplaces)
  * [Doors](#doors)
* [See Also](#seealso)

* [Examples](#examples)
* [Extras](#extras)
  * [Crane Game Prizes](#craneprizes)
  * [Replacing Existing Doors](#doorsextra)

## Introduction<span id="introduction"></span>
Extra Map Actions adds new tile actions, properties, and map properties.<br>
| Tile Action | Description |
| :--- | :--- |
| EMA_CraneGame | Opens the crane game dialogue |
| EMA_LostAndFound | Opens [lost and found](https://stardewvalleywiki.com/Mayor%27s_Manor#Lost_and_Found) |
| EMA_OfflineFarmhandInventory | Opens dialogue window to choose an offline farmhand inventory to open |
| EMA_Fireplace \[right] | Can be used to create an operable fireplace |
| EMA_DivorceBook | Opens [divorce book](https://stardewvalleywiki.com/Mayor%27s_Manor#Divorce) |
| EMA_LedgerBook | Opens [ledger book](https://stardewvalleywiki.com/Multiplayer#Money) |
<br>

| Tile Property | Description |
| :--- | :--- |
| EMA_CustomDoor `<customDoorKey>` | Links this door to an entry in the `rokugin.EMA/CustomDoors` asset. |
<br>

| Map Property | Description |
| :--- | :--- |
| EMA_FireplaceLocation \[`<X> <Y> <fireplaceConditionsKey>`] + | Conditionally starts or stops a fireplace.<br>Combine with `EMA_Fireplace` tile action to make it further operable. |

The config contains two settings, `Debug Logging` and `Crane Game Cost`.<br>
Crane Game Cost must be a positive value or zero.<br>
If zero, the prompt will change to reflect that it's free, otherwise if the player has enough money they'll be charged the cost when they select `Yes` on the dialogue box.

## Examples<span id="examples"></span>
### Fireplaces<span id="fireplaces"></span>
#### EMA_Fireplace Tile Action
A tile action that starts or stops a fireplace. `EMA_Fireplace` should be placed on the left tile of the fireplace and `EMA_Fireplace right` should be placed on the right tile.<br>

Fireplace tile actions can be added directly to Tiled:<br>
![Screenshot of properties window with fireplace left action filled out.](screenshots/fireplace-left-tiled.png)<br>
![Screenshot of properties window with fireplace right action filled out.](screenshots/fireplace-right-tiled.png)


Or they can be added through CP:<br>
```jsonc
{
  "Action": "EditMap",
  "Target": "Maps/<TargetMapGoesHere>",
  "MapTiles": [
    {
      "Position": {// tile position of the left tile of the fireplace
        "X": 0,
        "Y": 4
      },
      "Layer": "Buildings",
      "SetProperties": {
        "Action": "EMA_Fireplace"
      }
    },
    {
      "Position": {// tile position of the right tile of the fireplace
        "X": 1,
        "Y": 4
      },
      "Layer": "Buildings",
      "SetProperties": {
        "Action": "EMA_Fireplace right"
      }
    }
  ]
}
```
<br>

#### EMA_FireplaceLocation Map Property
A map property that combines with a new CP dictionary asset `rokugin.EMA/FireplaceConditions` to set up conditionally active fireplaces.<br>

The format of Fireplace Location is:<br>
`EMA_FireplaceLocation [<intX> <intY> <fireplaceConditionsKey>] +`<br>
The `+` indicates that you can create multiple entries, separated by a space. Check the example below.<br>

`<intX> <intY>` is the tile coordinates of the left tile of the fireplace.<br>
`<fireplaceConditionsKey>` is the entry key of the condition you want to check to determine if the fireplace should turn on.<br>
AlwaysOn is the default key in the asset.<br>

Make sure to remove all the `<>`.<br>

The map property can be set in Tiled:<br>
![Screenshot of map properties window with fireplace location property filled out and highlighted.](screenshots/fireplacelocation-map-property.png)<br>

![Screenshot of map properties window with fireplace location property filled out with multiple entries and edit text window partially shown](screenshots/fireplacelocation-tiled-multiple.png)


Or with CP:<br>
```jsonc
{
  "Action": "EditMap",
  "Target": "Maps/<TargetMapGoesHere>",
  "MapProperties": {
    "EMA_FireplaceLocation": "1 4 AlwaysOn 10 4 AlwaysOn"
  }
}
```

<br>

#### Fireplace Conditions Data Asset
A dictionary of string → models containing a Condition field that accepts a [Game State Query](https://stardewvalleywiki.com/Modding:Game_state_queries).<br>

Existing entries can be edited or new entries can be added using CP's [EditData](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide/action-editdata.md):<br>
```jsonc
{
  "Action": "EditData",
  "Target": "rokugin.EMA/FireplaceConditions",
  "Entries": {
    "rokugin_custom1": {// adds new entry
      "Condition": "ANY \"SEASON fall winter\" \"WEATHER Here Rain Snow Storm GreenRain\""
    },
    "AlwaysOn": {// overwrites existing AlwaysOn entry
      "Condition": "false"
    },
  }
}
```

<br>

### Doors<span id="doors"></span>
If adding a custom door to an existing map you should read through this section and see [Replacing Existing Doors](#doorsextra) in the Extras section for additional information.<br>

In order to place custom doors on a custom map, you need to:
  * Place at least the bottom transparent tile of the door on the `Buildings` tile layer
  * Add the doors position to the `Doors` Map Property
  * Add an `Action Door` tile property to the bottom tile of the door
  * Add an `EMA_CustomDoor <customDoorsKey>` tile property to that same tile
  * Add an entry to `rokugin.EMA/CustomDoors` for the tile property to link to

Currently doors wider than 1 tile are not supported, it's something I will be looking into for a future update though.

<br>

#### Transparent Tiles
The only required transparent tile is the bottom one that goes on the `Buildings` layer, however the game will automatically fill in the two above that tile from the tilesheet.<br>
You can leave those two tiles blank on the tilesheet. This is important to keep in mind if you have a custom door in a regular tilesheet, so you keep those tiles open.<br>

If creating a door that's larger than 3 tiles tall, make sure to only place up to the bottom 3 transparent tiles.<br>
This mod doesn't currently adjust the removal of the temporary tiles, so if you place more than the bottom 3 then the rest will still be there when the door opens.

<br>

#### Doors Map Property
The custom door must be added to the `Doors` Map Property.<br>
Only the coordinates for each door are actually used, so the entries you add can look something like: `10 12 0 0`<br>

Tiled example<br>

If you want to add your `Doors` Map Property through CP instead you can do something like this:<span id="doormapproperty"></span>
```jsonc
{
  "Action": "EditMap",
  "Target": "Maps/ExampleMap",
  "MapProperties": {
    "Doors": "10 12 0 0"
  }
}
```
In my opinion, this would make it more readable but is a little more annoying since you have to go between two programs to get the coordinates and set them.

<br>

#### Action Door Tile Property
Custom doors still use the regular `Action Door` tile property.<br>

Tiled example<br>

Setting this in CP would look something like this:
```jsonc
{
  "Action": "EditMap",
  "Target": "Maps/ExampleMap",
  "MapTiles": [
    {
      "Layer": "Buildings",
      "Position": {"X": 10, "Y": 12},
      "SetProperties": {
        "Action": "Door"
      }
    }
  ]
}
```

<br>

#### EMA_CustomDoor Tile Property
A tile property for linking the door to the appropriate Sprite settings in `rokugin.EMA/CustomDoors`.<br>

Tiled example goes here.<br>

CP example goes here.<br>
```jsonc
{
  "Action": "EditMap",
  "Target": "Maps/ExampleMap",
  "MapTiles": [
    {
      "Layer": "Buildings",
      "Position": {"X": 10, "Y": 12},
      "SetProperties": {
        "EMA_CustomDoor": "rokuginExample"
      }
    }
  ]
}
```
The value of the property must match a key in the custom doors data asset.

<br>

#### Custom Doors Data Asset<span id="customdoorsasset"></span>
A dictionary of string → models, used to construct Temporary Animated Sprites of the door opening animation.

| Field | Description |
| :--- | :--- |
| Texture | The asset to pull the textures for the animation from.<br>*Example: `"Texture": "LooseSprites/Cursors"`* |
| SourceRect | The rectangle of the first frame of the animation. Constructed from the top left pixel coordinate and the width and height of the door sprite.<br>*Example: `"SourceRect": {"X": 512, "Y": 144, "Width": 16, "Height": 48}`* |
| Flip | *(Optional)* Bool value for if the sprite should be flipped horizontally, allows for doors that swing open in the opposite direction without having to create a whole new animation for it.<br>Make sure to use a flipped transparent placeholder for any flipped doors or you'll get an undesirable ghost door effect on closed doors.<br>Defaults to `false`, if not set.<br>*Example: `"Flip": true`* |
| AnimationFrames | *(Optional)* Number of frames of the animation, including the starting frame.<br>Be wary of adding too many frames, currently the collider that stops players from moving through doorways isn't adjustable by this mod, so if your animation is too long, players can run through your door while it's still opening.<br>Defaults to `4`, if not set.<br>*Example: `"AnimationFrames": 4`* |
| FrameDuration | *(Optional)* The time in milliseconds that each frame of the animation lasts.<br>Be wary of settings this too high, like with `AnimationFrames` this can extend your animation too long and allow players to clip through the door while the animation is still happening.<br>Defaults to `100`, if not set.<br>*Example: `"FrameDuration": 100`* |
| PositionOffset | *(Optional)* Simple Vector2 tile offset for the animation position. This is primarily useful for repositioning taller than normal doors.<br>Defaults to `"0, 0"`, if not set.<br>*Example: `"PositionOffset": "0, 0"`* |
| DepthOffset | *(Optional)* Amount to offset the render depth. This is used primarily on sideways doors to make the top door render behind the player, rather than always in front.<br>Defaults to `0`, if not set.<br>*Example: `"DepthOffset": 0`* |

Existing entries can be edited or new entries can be added using CP's [EditData](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide/action-editdata.md):<br>
```jsonc
{
  "Action": "EditData",
  "Target": "rokugin.EMA/CustomDoors",
  "Entries": {
    "rokuginExample": {
      "Texture": "Maps/rokuginDoors",
      "SourceRect": {"X": 16, "Y": 0, "Width": 16, "Height": 48},
      "Flip": false,
      "AnimationFrames": 4,
      "FrameDuration": 100,
      "PositionOffset": "0, 0",
      "DepthOffset": 0
    }
  }
}
```

## Extras<span id="extras"></span>
### Crane Game Prizes<span id="craneprizes"></span>
Crane game prizes are mostly hardcoded, however you can use `Data/Movies` to add to or completely overwrite the prize lists.<br>
More detailed information can be found on the [migration page of the wiki](https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6#Custom_movies), but I have some examples and notes I will make here.<br><br>

#### Example 1
The first example is for completely overwriting the prize lists so that only your chosen prizes are available:
```json
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
```
Notes:<br>
An empty prize group will cause errors, so if you use `ClearDefaultCranePrizeGroup` you have to add at least one entry to `CranePrizes` for every group you clear. In my example, since I'm clearing all three groups, I add the diamond to each group to avoid errors.

The `When` and `Update` fields are included to make the changes only happen when you enter the map with your crane game, this way you don't make changes to the default crane game at the theater. I use `BusStop` in my example because that's where my crane game is, but you would use the location name of wherever your crane game is.

These prize lists are specific to what movie should be playing, even if you haven't unlocked the theater yet, so in order to have prize lists that are the same you have to make changes to the `CranePrizes` for every movie, alternatively this means you can easily have seasonal and alternating yearly different prize lists.<br><br>

#### Example 2
The next example is for if you only want to add prizes without modifying the existing lists:
```json
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
```
Notes:<br>
This time, in order to avoid overwriting the movie specific prizes, we use `TargetField` to target `CranePrizes` and negative indexed entries in order to only add new entries without risking editing existing entries.

The `When` and `Update` fields are included to make the changes only happen when you enter the map with your crane game, this way you don't make changes to the default crane game at the theater. I use `BusStop` in my example because that's where my crane game is, but you would use the location name of wherever your crane game is.<br><br>

#### Example 3
Because we're targeting `CranePrizes` like this, we can't also edit `ClearDefaultCranePrizeGroups` in one patch, so if we want to any of the default prize groups, we have to make a separate patch:
```json
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
```
Notes:<br>
This is obviously optional if you don't want to clear default prize groups and the same rules as earlier apply if you do, any groups you clear have to have an entry in the previous patch to add a new entry, empty groups will cause errors.

The `When` and `Update` fields are included to make the changes only happen when you enter the map with your crane game, this way you don't make changes to the default crane game at the theater. I use `BusStop` in my example because that's where my crane game is, but you would use the location name of wherever your crane game is.<br><br>

<br>

### Replacing Existing Doors<span id="doorsextra"></span>
In order to add custom doors to an existing map, you need to:
 * Add your custom doors transparent tiles or replace the tiles of existing doors with your custom doors tiles
 * Add your doors to the maps `Doors` Map Property
 * Make sure you have an entry in the custom doors data asset

The easiest way to add or replace the transparent tiles is patching them in with EditMap and a small tmx, this also allows you to have the tile properties you need.<br>
The only needed tile is the bottom of the door that goes on the Buildings layer. You will also need the `Action Door` and `EMA_CustomDoor <customDoorKey>` tile properties.<br>

If you're adding doors to a map that doesn't already have a `Doors` Map Property then you can simply patch it in with `EditMap` [as described earlier](#doormapproperty).<br>
If you're adding doors to a map that already has a `Doors` Map Property, in order to avoid having to rewrite it and to maintain compat, you'll need to use a [`Text Operation Append`](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide/text-operations.md) to add to the values:<br>
```jsonc
{
  "Action": "EditMap",
  "Target": "Maps/ExampleMap",
  "TextOperations": [
    {
      "Operation": "Append",
      "Target": ["MapProperties", "Doors"],
      "Delimiter": " ",
      "Value": "10 12 0 0"
    }
  ]
}
```

Adding an entry to the custom doors data asset [was covered earlier as well](#customdoorsasset).
