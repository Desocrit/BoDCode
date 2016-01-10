using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class Resource
{
    public enum Name
    {
        Health,
        Mana,
        Energy,
        Focus,
        XP
    }

    public Name name;

    private float _value;
    public float Value
    {
        get { return _value; }
        set
        {
            _value = Mathf.Clamp(value, 0, _max);
            if(slider != null)
                slider.value = Proportion;
        }
    }

    public float Proportion { get { return (float)_value / _max; } }
    
    private float _max;
    public float Max
    {
        get { return _max; }
        set
        {   // Gaining max health boosts health a bit, too.
            if(value > _max)
                Value = Value + _max - value;
            _max = value;
        }
    }

    private Slider slider;

    public Resource(Name type, float initialValue, float maxValue)
        : this(type, initialValue, maxValue, null)
    {
    }

    public Resource(Name type, float initialValue,
                    float maxValue, Slider resourceSlider)
    {
        this.name = type;
        _max = maxValue;
        _value = initialValue;
        if(resourceSlider != null)
            addSlider(resourceSlider);
    }

    public void addSlider(Slider resourceSlider)
    {

        slider = resourceSlider;
        if(slider != null)
            slider.value = _value / _max;
    }

    // To be refactored out.
    private string getResourceBarName(Name resourceName)
    {
        switch(resourceName)
        {
            case Name.Health:
                return "Health Bar";
            case Name.Mana:
            case Name.Focus:
            case Name.Energy:
                return "Secondary Bar";
            case Name.XP:
            default:
                return "XP Bar";
        }
    }
}