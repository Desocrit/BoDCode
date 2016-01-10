using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GemDisplay : MonoBehaviour
{

    private int gems = 0;
    private float alpha = 0f;
    private Text text;
    private Color textColour;
    private Image image;
    private Color imageColour;

    void Start()
    {
        text = GetComponentInChildren<Text>();
        textColour = text.color;
        image = GetComponentInChildren<Image>();
        imageColour = image.color;
    }

    public void SetGems(int count)
    {
        gems = count;
        text.text = "x " + gems;
        alpha = 500f;
    }

    public int GetGems()
    {
        return gems;
    }
	
    // Update is called once per frame
    void Update()
    {
        if(alpha > 0f)
        {
            alpha -= 1f;
            float boundedAlpha = alpha > 100f ? 1f : alpha / 100f;
            textColour.a = boundedAlpha;
            text.color = textColour;
            imageColour.a = boundedAlpha;
            image.color = imageColour;
        }
    }
}
