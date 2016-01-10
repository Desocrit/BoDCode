using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ModifiedStats : CharacterStats
{
    private CharacterStats baseStats;

    private IDictionary<Statistic, FieldInfo> fields;
    private List<StatModifier> modifiers;

    public Resource health;
    public Resource secondary;

    public ModifiedStats(CharacterStats original)
    {
        baseStats = original;
        modifiers = new List<StatModifier>();

        // Get FieldInfo objects for each field for easy reflection.
        fields = new Dictionary<Statistic, FieldInfo>();
        foreach(Statistic stat in Enum.GetValues(typeof(Statistic)))
            fields[stat] = typeof(ModifiedStats).GetField(ToLowerPascal(stat));

        // Default all stats to their base value.
        Rebase();

        // Also set abilities, alliance and name to default values.
        characterName = original.characterName;
        alliance = original.alliance;
        abilities = new List<Ability>(original.abilities);
        secondaryType = original.secondaryType;

        // Set up resource bars.
        health = new Resource(Resource.Name.Health, maxHP, maxHP);
        secondary = new Resource(secondaryType, maxSecondary, maxSecondary);
    }

    public void Ability(Ability newAbility)
    {
        abilities.Add(newAbility);
    }

    public void AddSlider(Resource.Name resourceType, 
                          UnityEngine.UI.Slider slider)
    {
        if(resourceType == Resource.Name.Health)
            health.addSlider(slider);
        else
            secondary.addSlider(slider);
    }

    public void AddModifier(StatModifier mod)
    {
        if(mod.stackable || !modifiers.Contains(mod))
        {
            modifiers.Add(mod);
            UpdateStats();
        }
    }

    public void RemoveModifier(StatModifier mod)
    {
        if(modifiers.Contains(mod))
        {
            modifiers.Remove(mod);
            UpdateStats();
        }
    }

    public float GetBaseValue(Statistic stat)
    {
        return (float)fields[stat].GetValue(this);
    }

    private void Rebase()
    {
        FieldInfo[] stats = new FieldInfo[fields.Count];
        fields.Values.CopyTo(stats, 0);
        foreach(FieldInfo field in stats)
            field.SetValue(this, field.GetValue(baseStats));
    }

    public void UpdateStats()
    {
        // Set up variables.
        Rebase();
        var bonusses = new Dictionary<Statistic, float>();
        var multipliers = new Dictionary<Statistic, float>();
        var divides = new Dictionary<Statistic, float>();

        // Next, update the dictionaries with all modifiers.
        foreach(StatModifier mod in modifiers)
        {
            if(mod.isMultiplier)
            {
                if(mod.modifier >= 0)
                    incrDict(multipliers, mod.modifiedStat, mod.modifier);
                else
                    incrDict(divides, mod.modifiedStat, (1 / mod.modifier) - 1);
            } else
                incrDict(bonusses, mod.modifiedStat, mod.modifier);
        }

        // Finally, update current fields based on stat modifiers.
        foreach(Statistic stat in Enum.GetValues(typeof(Statistic)))
        {
            if(bonusses.ContainsKey(stat))
                fields[stat].SetValue(this, (float)fields[stat].GetValue(this)
                    + bonusses[stat]);
            if(multipliers.ContainsKey(stat))
                fields[stat].SetValue(this, (float)fields[stat].GetValue(this)
                    * multipliers[stat]);
            if(divides.ContainsKey(stat))
                fields[stat].SetValue(this, (float)fields[stat].GetValue(this)
                    / (multipliers[stat] + 1));
        }

        // Update resource bars.
        health.Max = maxHP;
        secondary.Max = maxSecondary;
    }

    private static void incrDict<K>(
        Dictionary<K, float> dict, K key, float incr)
    {
        if(dict.ContainsKey(key))
            dict[key] += incr;
        else
            dict[key] = incr;
    }

    private string ToLowerPascal(Statistic stat)
    {
        return stat.ToString()[0].ToString().ToLower() + 
            stat.ToString().Substring(1);
    }
}

