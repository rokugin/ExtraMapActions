namespace ExtraMapActions.Models;

public sealed class MessagesModel {

    public string? Prefix { get; set; }
    public List<string> Messages { get; set; } = new();
    public string? Suffix { get; set; }

}