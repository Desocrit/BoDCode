using UnityEngine;
using System.Collections.Generic;

public class Exit : MonoBehaviour
{
    private Player player;
    private Transform ptransform;
    private List<MeshRenderer> gems;
    private ParticleSystem flies;
    private ParticleSystem portal;

    private bool active = false;
    private int animDuration = 0;
    private Vector3 center = new Vector3(5f, -2.2f, 6f);

    // Use this for initialization
    void Start()
    {
        center = transform.rotation * center;
        portal = GameObject.Find("Frame/Portal").GetComponent<ParticleSystem>();
        flies = GameObject.Find("Frame/Sparkle").GetComponent<ParticleSystem>();
        gems = new List<MeshRenderer>();
        foreach(Transform gem in GameObject.Find("Frame/Gems").transform)
            gems.Add(gem.gameObject.GetComponent<MeshRenderer>());
    }
	
    void Update()
    {
        if(active)
        {
            if(player == null)
                return;
            // Manual box collider.
            Vector3 pos = ptransform.position - (transform.position + center);

            if(Mathf.Abs(pos.x) < 1f && Mathf.Abs(pos.y) < 2.5f &&
                Mathf.Abs(pos.z) < 1f)
            {
                Debug.Log("You win. Good job.");
                Application.Quit();
            }
        } else if(player != null && player.Gems >= 4)
        {
            animDuration += 1;
            if(animDuration >= 100)
            {
                flies.Stop();
                portal.Play();
                active = true;
            }
            foreach(MeshRenderer gem in gems)
            {
                Color gemColor = gem.material.color;
                gemColor.b = 0.25f + animDuration / 75f;
                gem.material.color = gemColor;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(player != null)
            return;
        Player playerComp = other.GetComponent<Player>();
        if(playerComp == null)
            return;
        player = playerComp;
        ptransform = player.transform;
    }

    void OnTriggerExit(Collider other)
    {
        if(other.transform == ptransform)
            player = null;
    }
}
