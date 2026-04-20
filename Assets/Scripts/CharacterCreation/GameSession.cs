using System.Collections.Generic;
using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [Header("Bag")]
    [SerializeField] private int maxBagSlots = 6;

    public List<CharacterDefinition> bag = new List<CharacterDefinition>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool AddSubject(CharacterDefinition subject)
    {
        if (subject == null)
            return false;

        if (bag.Count >= maxBagSlots)
            return false;

        bag.Add(subject);
        return true;
    }

    public bool RemoveSubject(int index)
    {
        if (index < 0 || index >= bag.Count)
            return false;

        bag.RemoveAt(index);
        return true;
    }

    public CharacterDefinition GetSubjectAt(int index)
    {
        if (index < 0 || index >= bag.Count)
            return null;

        return bag[index];
    }

    public int GetBagCount()
    {
        return bag.Count;
    }

    public int GetMaxBagSlots()
    {
        return maxBagSlots;
    }
}