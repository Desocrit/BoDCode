using UnityEngine;
using System.Collections;

public class PlayerClass
{
    public enum ClassName
    {
        WIZARD,
        WARLOCK,
        CLERIC,
        MONK,
        VANGUARD, 
        BERZERKER,
        SHADOW,
        RANGER,
        DUELIST
    }
    
    public static Resource.Name Resource(ClassName playerClass)
    {
        switch(playerClass)
        {
            case ClassName.WIZARD:
            case ClassName.WARLOCK:
            case ClassName.CLERIC:
                return global::Resource.Name.Mana;
            case ClassName.MONK:
            case ClassName.VANGUARD:
            case ClassName.BERZERKER:
                return global::Resource.Name.Focus;
            case ClassName.SHADOW:
            case ClassName.RANGER:
            case ClassName.DUELIST:
            default:
                return global::Resource.Name.Energy;
        }
    }
}