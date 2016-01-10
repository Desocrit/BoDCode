using UnityEngine;
using System;
using System.Collections;

namespace EffectCondition
{
    public enum Combination
    {
        AllTrue,
        AnyTrue,
        AllFalse,
        AnyFalse
    }

    public interface Condition
    {
        bool Check(Character caster, Character other,
                   DamageType damageType, float value);
    }

    // For editor use only.
    [Serializable]
    public class ConditionDescription : CombinedCondition
    {
        public Combination subType;

        public ValueCondition[] values;
        public TypeCondition[] damageTypes;
        public CharacterCondition[] charTypes;

        private ConditionDescription()
        {
            conditions = new Condition[3];
            conditions[0] = new CombinedCondition(subType, values);
            conditions[1] = new CombinedCondition(subType, damageTypes);
            conditions[2] = new CombinedCondition(subType, charTypes);
        }
    }

    [Serializable]
    public class CombinedCondition : Condition
    {
        public Combination type;

        protected Condition[] conditions; // Lists that need checking.

        protected CombinedCondition()
        {
        }

        public CombinedCondition(Combination type, Condition[] conditions)
        {
            this.type = type;
            this.conditions = conditions;
        }

        public bool Check(Character caster, Character other,
                          DamageType damageType, float value)
        {
            // Whether or not the condition is initially true.
            bool initialValue = type == Combination.AllTrue ||
                type == Combination.AllFalse;
            // What condition it has to see to reverse this value.
            bool reversedBy = type == Combination.AnyTrue ||
                type == Combination.AllFalse;
            
            // Check each parameter. If value is changed, return the inverse.
            foreach(Condition c in conditions)
                if(c.Check(caster, other, damageType, value) == reversedBy)
                    return !initialValue;
            return initialValue;
        }
    }

    [Serializable]
    public class ValueCondition : Condition
    {
        public enum ComparedValue
        {
            Value,
            CasterHealth,
            CasterPercentHealth,
            CasterSecondary,
            CasterPercentSecondary,
            TargetHealth,
            TargetPercentHealth,
            TargetSecondary,
            TargetPercentSecondary
        }

        public enum ValueComparison
        {
            EqualTo,
            NotEqualTo,
            GreaterThan,
            LessThan
        }

        public ComparedValue comparedValue;
        public ValueComparison comparison;
        public float value;

        private Func<Character, Character, float, bool> func;

        private ValueCondition()
        {
            constructFunction(comparedValue, comparison, value);
        }

        public ValueCondition(ComparedValue comparedValue,
                              ValueComparison comparison, float value)
        {
            constructFunction(comparedValue, comparison, value);
        }

        public void constructFunction(ComparedValue comparedValue,
                                      ValueComparison comparison, float value)
        {
            var comparisonFunction = getComparison(comparison, value);
            var valueFunction = getValueFunction(comparedValue);
            func = (c1, c2, f) => comparisonFunction(valueFunction(c1, c2, f));
        }

        public bool Check(Character caster, Character other,
                          DamageType damageType, float value)
        {
            return (func(caster, other, value));
        }

        private static Func<Character, Character, float, float> 
            getValueFunction(ComparedValue comparedValue)
        {
            switch(comparedValue)
            {
                case ComparedValue.CasterHealth:
                    return (c1, c2, f) => c1.Health;
                case ComparedValue.CasterPercentHealth:
                    return (c1, c2, f) => c1.Health / c1.Stats.maxHP;
                case ComparedValue.CasterSecondary:
                    return (c1, c2, f) => c1.Secondary;
                case ComparedValue.CasterPercentSecondary:
                    return (c1, c2, f) => c1.Secondary / c1.Stats.maxSecondary;
                case ComparedValue.TargetHealth:
                    return (c1, c2, f) => c2.Health;
                case ComparedValue.TargetPercentHealth:
                    return (c1, c2, f) => c2.Health / c1.Stats.maxHP;
                case ComparedValue.TargetSecondary:
                    return (c1, c2, f) => c2.Secondary;
                case ComparedValue.TargetPercentSecondary:
                    return (c1, c2, f) => c2.Secondary / c2.Stats.maxSecondary;
                default:
                case ComparedValue.Value:
                    return (c1, c2, f) => f;
            }
        }

        private static Func<float, bool> getComparison(
            ValueComparison comparison, float value)
        {
            switch(comparison)
            {
                case ValueComparison.GreaterThan:
                    return x => x > value;
                case ValueComparison.LessThan:
                    return x => x < value;
                case ValueComparison.NotEqualTo:
                    return x => x != value;
                default:
                case ValueComparison.EqualTo:
                    return x => x == value;
            }
        }
    }

    [Serializable]
    public class TypeCondition : Condition
    {
        public DamageType type;
        public bool equalTo;

        private TypeCondition()
        {
        }

        public TypeCondition(DamageType type, bool equalTo)
        {
            this.type = type;
            this.equalTo = equalTo;
        }

        public bool Check(Character caster, Character other,
                          DamageType damageType, float value)
        {
            return (type == damageType) == equalTo;
        }
    }

    [Serializable]
    public class CharacterCondition : Condition
    {
        // NYI.
        

        public bool Check(Character caster, Character other,
                          DamageType damageType, float value)
        {
            return true;
        }
    }
}