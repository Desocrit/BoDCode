using System;

[Serializable]
public class StatModifier : IEquatable<StatModifier>
{
    // Stat to be modified.
    public Statistic modifiedStat;
    // Amount stat is to be modified.
    public float modifier;
    // If the stat should be multiplied instead of added.
    public bool isMultiplier;
    // If the stat modifier can be applied multiple times.
    public bool stackable;

    private StatModifier()
    {
        // For serializer.
    }

    public StatModifier(Statistic stat, float mod, bool isMultiplier)
    {
        modifiedStat = stat;
        modifier = mod;
        this.isMultiplier = isMultiplier;
    }

    public bool Equals(StatModifier other)
    {
        return modifiedStat == other.modifiedStat && 
            modifier == other.modifier && 
            isMultiplier == other.isMultiplier;
    }
}

