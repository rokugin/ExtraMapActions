﻿namespace ExtraMapActions;

public class ModConfig {

    /// <summary> Toggle showing debug info in the SMAPI console. </summary> <remarks> Default: false </remarks>
    public bool DebugLogging { get; set; } = false;

    /// <summary> Coin cost to play the crane game triggered by custom map action. </summary> <remarks> Default: 500 </remarks>
    public int CraneGameCost { get; set; } = 500;

}