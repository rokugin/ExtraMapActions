using Microsoft.Xna.Framework;

namespace ExtraMapActions.Framework;

public class FireplaceState {

    public string Location { get; set; }

    public Point Point { get; set; }

    public bool On { get; set; }

    public FireplaceState(string location, Point point, bool on) {
        Location = location;
        Point = point;
        On = on;
    }

}