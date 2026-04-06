using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TraitRatings
{
    public int curiosity;
    public int discipline;
    public int drive;
    public int empathy;
    public int instability;
    public int submission;
}

[Serializable]
public class CharacterDefinition
{
    public string id;
    public string type;
    public string description;
    public TraitRatings traitRatings;
}

[Serializable]
public class CharacterDatabase
{
    public List<CharacterDefinition> characters;
}

[Serializable]
public class CharacterSpriteEntry
{
    public string id;
    public Sprite sprite;
}

public class CharacterSelector : MonoBehaviour
{
    [Header("JSON")]
    [SerializeField] private TextAsset charactersJson;

    [Header("Trait Rows")]
    [SerializeField] private TraitRowUI curiosityRow;
    [SerializeField] private TraitRowUI disciplineRow;
    [SerializeField] private TraitRowUI driveRow;
    [SerializeField] private TraitRowUI empathyRow;
    [SerializeField] private TraitRowUI instabilityRow;
    [SerializeField] private TraitRowUI submissionRow;

    [Header("UI Output")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Sprite Mapping")]
    [SerializeField] private List<CharacterSpriteEntry> characterSprites = new List<CharacterSpriteEntry>();

    private CharacterDatabase database;
    private Dictionary<string, Sprite> spriteLookup = new Dictionary<string, Sprite>();

    private void Awake()
    {
        LoadDatabase();
        BuildSpriteLookup();
    }

    private void LoadDatabase()
    {
        if (charactersJson == null)
        {
            Debug.LogError("Characters JSON is not assigned.");
            return;
        }

        database = JsonUtility.FromJson<CharacterDatabase>(charactersJson.text);

        if (database == null || database.characters == null || database.characters.Count == 0)
        {
            Debug.LogError("Failed to parse character database. Check your JSON structure.");
        }
    }

    private void BuildSpriteLookup()
    {
        spriteLookup.Clear();

        foreach (CharacterSpriteEntry entry in characterSprites)
        {
            if (!string.IsNullOrEmpty(entry.id) && entry.sprite != null)
            {
                spriteLookup[entry.id] = entry.sprite;
            }
        }
    }

    public void PickCharacter()
    {
        if (database == null || database.characters == null || database.characters.Count == 0)
        {
            Debug.LogError("Character database is empty or failed to load.");
            return;
        }

        TraitRatings playerInput = new TraitRatings
        {
            curiosity = curiosityRow.SelectedLevel,
            discipline = disciplineRow.SelectedLevel,
            drive = driveRow.SelectedLevel,
            empathy = empathyRow.SelectedLevel,
            instability = instabilityRow.SelectedLevel,
            submission = submissionRow.SelectedLevel
        };

        CharacterDefinition selected = GetWeightedRandomCharacter(playerInput);

        if (selected == null)
        {
            Debug.LogError("No character was selected.");
            return;
        }

        ApplyCharacterToUI(selected);
    }

    private CharacterDefinition GetWeightedRandomCharacter(TraitRatings input)
    {
        List<int> weights = new List<int>();
        int totalWeight = 0;

        foreach (CharacterDefinition character in database.characters)
        {
            int score = GetMatchScore(input, character.traitRatings);
            int weight = score * score;

            weights.Add(weight);
            totalWeight += weight;

            Debug.Log($"{character.id} | score={score} | weight={weight}");
        }

        if (totalWeight <= 0)
            return null;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int runningTotal = 0;

        for (int i = 0; i < database.characters.Count; i++)
        {
            runningTotal += weights[i];
            if (roll < runningTotal)
            {
                return database.characters[i];
            }
        }

        return database.characters[database.characters.Count - 1];
    }

    private int GetMatchScore(TraitRatings input, TraitRatings archetype)
    {
        return TraitCloseness(input.curiosity, archetype.curiosity)
             + TraitCloseness(input.discipline, archetype.discipline)
             + TraitCloseness(input.drive, archetype.drive)
             + TraitCloseness(input.empathy, archetype.empathy)
             + TraitCloseness(input.instability, archetype.instability)
             + TraitCloseness(input.submission, archetype.submission);
    }

    private int TraitCloseness(int playerValue, int archetypeValue)
    {
        return 5 - Mathf.Abs(playerValue - archetypeValue);
    }

    private void ApplyCharacterToUI(CharacterDefinition character)
    {
        if (typeText != null)
            typeText.text = character.type;

        if (descriptionText != null)
            descriptionText.text = character.description;

        if (characterImage != null)
        {
            if (spriteLookup.TryGetValue(character.id, out Sprite sprite))
            {
                characterImage.sprite = sprite;
                characterImage.preserveAspect = true;
            }
            else
            {
                Debug.LogWarning($"No sprite mapped for character id: {character.id}");
            }
        }
    }
}