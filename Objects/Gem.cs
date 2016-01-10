using UnityEngine;
using System.Collections;

public class Gem : MonoBehaviour
{
    private Transform player = null;
    private new Transform transform;

    private float velocity = 1f;
    // Smallest it has yet been, so gems don't grow.
    private float minScale = 10f;

    void Start()
    {
        transform = gameObject.transform;
    }

    void Update()
    {
        if(player == null)
            return;

        if(velocity < 10f)
            velocity += 0.1f;

        Vector3 playerPos = player.position + Vector3.up * 1.2f;
        transform.position = Vector3.MoveTowards(transform.position, playerPos,
                                          Time.deltaTime * velocity);
        float dist = Vector3.Distance(transform.position, playerPos);
        if(dist > 0.3f)
        {
            minScale = Mathf.Min((dist * 5f), minScale);
            transform.localScale = Vector3.one * minScale;
        } else
        {
            player.gameObject.GetComponent<Player>().Gems += 1;
            Destroy(gameObject);
        }
    }
	
    void OnTriggerEnter(Collider other)
    {
        if(player != null)
            return;
        Player playerComp = other.GetComponent<Player>();
        if(playerComp == null)
            return;
        player = playerComp.transform;
    }
}
