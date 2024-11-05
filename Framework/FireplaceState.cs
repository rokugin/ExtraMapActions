using Microsoft.Xna.Framework;

namespace ExtraMapActions.Framework;

public class FireplaceState {

    public string Location { get; set; }

    public Point Point { get; set; }

    public string State { get; set; }

    public FireplaceState(string location, Point point, string state) {
        Location = location;
        Point = point;
        State = state;
    }

}