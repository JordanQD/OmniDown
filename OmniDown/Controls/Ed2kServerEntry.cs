namespace OmniDown.Controls;

public sealed class Ed2kServerEntry
{
    public Ed2kServerEntry()
    {
    }

    public Ed2kServerEntry(string address, bool isSelected = true)
    {
        Address = address;
        IsSelected = isSelected;
    }

    public string Address { get; set; } = string.Empty;

    public bool IsSelected { get; set; } = true;
}
