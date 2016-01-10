using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Weapon : MonoBehaviour
{
    private List<Character> charactersHit;
    private int seenCount;

    private Weapon()
    {
        charactersHit = new List<Character>();
        seenCount = 0;
    }

    public void ClearList()
    {
        charactersHit.Clear();
        seenCount = 0;
    }

    public void OnTriggerEnter(Collider collider)
    {
        Character cha = collider.gameObject.GetComponent<Character>();
        if(cha != null && !charactersHit.Contains(cha))
            charactersHit.Add(cha);
    }

    public bool HasNewCollisions()
    {
        return (seenCount != charactersHit.Count);
    }

    public List<Character> GetCollisions()
    {
        if(seenCount == charactersHit.Count)
            return new List<Character>();
        int unseen = charactersHit.Count - seenCount;
        List<Character> collisions = charactersHit.GetRange(seenCount, unseen);

        seenCount += collisions.Count;
        return collisions;
    }
}