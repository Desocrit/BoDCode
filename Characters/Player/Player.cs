using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Player : Character
{
    public MovementControls controls;

    private Resource _xp;
    public float XP
    {
        get { return _xp.Value; } 
        set
        { 
            if(value > _xp.Value && Stats != null)
            {
                _xp.Value += (value - _xp.Value) * Stats.experienceValue;
                if(_xp.Value >= _xp.Max)
                    LevelUp();
            } else
                _xp.Value = value;
                
        }
    }

    private int _level;
    public int Level { get { return _level; } }

    public int Gems
    {
        get { return gemDisplay.GetGems(); }
        set { gemDisplay.SetGems(value); } 
    }

    public override float Velocity { get { return controls.playerSpeed; } }
    public override Quaternion Direction { get {
            return controls.mainCam.rotation; } }

    private GemDisplay gemDisplay;

    public Player(CharacterStats stats) : base(stats)
    {
        controls = new MovementControls();
    }

    public override void Initialise()
    {
        // Initialise the base.
        base.Initialise();

        // Find the UI Sliders.
        Slider xpBar = null, hpBar = null, secondaryBar = null;
        foreach(GameObject bar in GameObject.
                FindGameObjectsWithTag("ResourceBar"))
        {
            if(bar.name == "HP Bar")
                hpBar = (bar.GetComponent<Slider>());
            if(bar.name == "Secondary Bar")
                secondaryBar = (bar.GetComponent<Slider>());
            if(bar.name == "XP Bar")
                xpBar = (bar.GetComponent<Slider>());
        }

        _level = 1;

        
        // Add sliders, and generate xp bar.
        _xp = new Resource(Resource.Name.XP, 0, 100, xpBar);
        XP = 0;

        if(hpBar == null)
            return;

        gemDisplay = GameObject.FindGameObjectWithTag("GemDisplay").
            GetComponent<GemDisplay>();

        _stats.AddSlider(Resource.Name.Health, hpBar);
        _stats.AddSlider(_stats.secondaryType, secondaryBar);
        // TODO: Attributes

    }

    public void LevelUp()
    {
        XP = XP - _xp.Max;
        _level += 1;
        _xp.Max = _xp.Max * 1.2f;

        // Animation should play here.

        // Player gets to select an ability.
    }

    public override void RegisterKill(Character target)
    {
        XP += target.Stats.experienceValue;
    }

    public override void Die(Character killer)
    {
        ReceiveDamageOrHealing(this, -100f, DamageType.Nature, false);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if(Input.GetButtonDown("Attack1"))
            Stats.abilities[0].Cast(this);
        if(Input.GetButtonDown("Attack2"))
            Stats.abilities[1].Cast(this);
    }

}