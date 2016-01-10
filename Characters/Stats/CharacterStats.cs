using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CharacterStats
{
    public string characterName;

    public enum Alliance
    {
        Friendly,
        Enemy,
        Neutral,
        Immune
    }
    public Alliance alliance;

    public List<Ability> abilities;

    // Modifiable stats.

    public float maxHP;
    public float maxSecondary;
    public Resource.Name secondaryType;

    public float attackDamage;
    public float attackSpeed;

    public float critChance;
    public float critDamage;

    public float movementSpeed;

    public float meleeDefense;
    public float magicDefense;

    public float luck;
    public float experienceValue;
    
    // XP Value. Enemy AI.

}