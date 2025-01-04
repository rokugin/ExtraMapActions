# Extra Map Actions (EMA)

## Contents
* [Introduction](#introduction)
* [Usage](#usage)
  * [Tile Actions](#tileactions)
  * [Map Properties](#mapprops)
  * [Tile Properties](#tileprops)

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

| Map Property | Description |
| :--- | :--- |
| EMA_FireplaceLocation \[`<X> <Y> <fireplaceConditionsKey>`] + | Conditionally starts or stops a fireplace.<br>Combine with `EMA_Fireplace` tile action to make it further operable. |
<br>

| Tile Property | Description |
| :--- | :--- |
| EMA_CustomDoor `<customDoorKey>` | Links this door to an entry in the `rokugin.EMA/CustomDoors` asset. |
<br>
The config contains two settings, `Debug Logging` and `Crane Game Cost`.<br>
Crane Game Cost must be a positive value or zero.<br>
If zero, the prompt will change to reflect that it's free, otherwise if the player has enough money they'll be charged the cost when they select `Yes` on the dialogue box.

## Usage<span id="usage"></span>
### Tile Actions<span id="tileactions"></span>
Tile actions are added directly in [Tiled](https://stardewvalleywiki.com/Modding:Maps#Tile_properties) or as part of a block in [CP](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide/action-editmap.md).<br>
<br>

#### EMA_OfflineFarmhandInventory
Normally an option on the Lost and Found box in the Mayor's Manor, if in a multiplayer game with offline farmhands, allows you to choose an offline player's inventory to open in order to retrieve item's from them.<br>
<br>

#### EMA_Fireplace
Interacting with this tile starts or stops a fireplace, based on the location of this tile. Intended to be placed inside a normal 2 tile wide fireplace. `EMA_Fireplace` should be on the left tile and `EMA_Fireplace right` should be on the right tile.<br>
<br>


***

### Map Properties<span id="mapprops"></span>
#### EMA_FireplaceLocation
A map property that combines with a custom data asset: `rokugin.EMA/FireplaceConditions`, to set up conditionally active fireplaces.<br>

The format of `EMA_FireplaceLocation` is:<br>
`EMA_FireplaceLocation [<intX> <intY> <fireplaceConditionsKey>] +`<br>
Like the main wiki, `[ ]` indicates a group of required fields that create one entry and `+` indicates that you can create multiple entries, separated by a space.<br>

`<intX> <intY>` is the tile coordinates of the left tile of the fireplace.<br>
`<fireplaceConditionsKey>` is the entry key of the condition you want to check in the Fireplace Conditions data asset to determine if the fireplace should turn on.<br>

Make sure to remove all the `<>` angle brackets when filling in your map property.<br>
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


***

### Tile Properties<span id="tileprops"></span>
### Doors
In order to place custom doors on a custom map, you need to:
  * Place at least the bottom transparent tile of the door on the `Buildings` tile layer
  * Add the doors position to the `Doors` Map Property
  * Add an `Action Door` tile property to the bottom tile of the door
  * Add an `EMA_CustomDoor <customDoorsKey>` tile property to that same tile
  * Add an entry to `rokugin.EMA/CustomDoors` for the tile property to link to

Currently doors wider than 1 tile are not supported, it's something I will be looking into for a future update though.<br>

Examples can be found [here](Examples/Custom-Doors-Examples).
<br>

#### Transparent Tiles
The only required transparent tile is the bottom one that goes on the `Buildings` layer, however the game will automatically fill in the two above that tile from the tilesheet.<br>
You can leave those two tiles blank on the tilesheet. This is important to keep in mind if you have a custom door in a regular tilesheet, so you keep those tiles open.<br>

If creating a door that's larger than 3 tiles tall, make sure to only place up to the bottom 3 transparent tiles.<br>
This mod doesn't currently adjust the removal of the temporary tiles, so if you place more than the bottom 3 then the rest will still be there when the door opens.<br>
<br>

#### Doors Map Property
The custom door must be added to the `Doors` Map Property.<br>
Only the coordinates for each door are actually used, so the entries you add can look something like: `10 12 0 0`<br>
<br>

#### Action Door Tile Property
Custom doors still use the regular `Action Door` tile property.<br>
<br>

#### EMA_CustomDoor Tile Property
A tile property for linking the door to the appropriate Sprite settings in `rokugin.EMA/CustomDoors`.<br>
The value of the property must match a key in the custom doors data asset.<br>
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
      "SourceRect": {"X": 16, "Y": 0, "Width": 16, "Height": 48}
    }
  }
}
```
