namespace Soundpad;

public sealed class AudioDeviceItem
{
    public string Id { get; }
    public string Name { get; }

    public AudioDeviceItem(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public override string ToString() => Name;
}

