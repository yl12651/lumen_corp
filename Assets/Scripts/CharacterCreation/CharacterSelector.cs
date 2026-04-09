using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TraitRatings
{
    public float curiosity;
    public float discipline;
    public float drive;
    public float empathy;
    public float instability;
    public float sincerity;
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
    [SerializeField] private TraitRowUI sincerityRow;
    
    [Header("Selection Tuning")]
    [SerializeField] private float temperature = 4f;

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
            curiosity = curiosityRow.SelectedValue,
            discipline = disciplineRow.SelectedValue,
            drive = driveRow.SelectedValue,
            empathy = empathyRow.SelectedValue,
            instability = instabilityRow.SelectedValue,
            sincerity = sincerityRow.SelectedValue
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
        List<float> scores = new List<float>();
        float maxScore = float.NegativeInfinity;

        foreach (CharacterDefinition character in database.characters)
        {
            float score = GetMatchScore(input, character.traitRatings);
            scores.Add(score);

            if (score > maxScore)
                maxScore = score;
        }

        List<float> weights = new List<float>();
        float totalWeight = 0f;

        for (int i = 0; i < database.characters.Count; i++)
        {
            float shiftedScore = scores[i] - maxScore;
            float weight = Mathf.Exp(shiftedScore / temperature);

            weights.Add(weight);
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
            return null;

        // Log probability for every character
        for (int i = 0; i < database.characters.Count; i++)
        {
            float probability = weights[i] / totalWeight;
            Debug.Log($"{database.characters[i].id} | score={scores[i]:F2} | probability={(probability * 100f):F2}%");
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float runningTotal = 0f;

        for (int i = 0; i < database.characters.Count; i++)
        {
            runningTotal += weights[i];
            if (roll < runningTotal)
            {
                float selectedProbability = weights[i] / totalWeight;
                Debug.Log($"Selected subject: {database.characters[i].id} | probability={(selectedProbability * 100f):F2}%");
                return database.characters[i];
            }
        }

        float fallbackProbability = weights[database.characters.Count - 1] / totalWeight;
        Debug.Log($"Selected subject: {database.characters[database.characters.Count - 1].id} | probability={(fallbackProbability * 100f):F2}%");

        return database.characters[database.characters.Count - 1];
    }

    private float GetMatchScore(TraitRatings input, TraitRatings archetype)
    {
        return TraitCloseness(input.curiosity, archetype.curiosity)
             + TraitCloseness(input.discipline, archetype.discipline)
             + TraitCloseness(input.drive, archetype.drive)
             + TraitCloseness(input.empathy, archetype.empathy)
             + TraitCloseness(input.instability, archetype.instability)
             + TraitCloseness(input.sincerity, archetype.sincerity);
    }

    private float TraitCloseness(float playerValue, float archetypeValue)
    {
        return 10f - Mathf.Abs(playerValue - archetypeValue);
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