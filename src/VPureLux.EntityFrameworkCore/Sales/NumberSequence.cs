namespace VPureLux.Sales;

public class NumberSequence
{
    public string Name { get; private set; } = string.Empty;
    public int CurrentValue { get; private set; }

    protected NumberSequence()
    {
    }

    public NumberSequence(string name, int currentValue)
    {
        Name = name;
        CurrentValue = currentValue;
    }
}
