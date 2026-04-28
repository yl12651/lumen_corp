using System.Collections.Generic;
using UnityEngine;

public class CafeDebugGameSessionBootstrap : MonoBehaviour
{
    [Header("Debug Only")]
    [SerializeField] private bool enableDebugBootstrap = true;

    [Header("Test Characters")]
    private readonly List<CharacterDefinition> debugCharacters = new List<CharacterDefinition>
    {
        new CharacterDefinition
        {
            id = "Emo",
            type = "Subject-E",
            description = "The Emotional type.\nHigh emotional sensitivity, strong empathy feedback loop; reacts swiftly to group mood swings; used to monitor collective emotional waves.",
            traitRatings = new TraitRatings
            {
                curiosity = 5.0f,
                discipline = 3.0f,
                drive = 3.0f,
                empathy = 9.5f,
                instability = 9.0f,
                sincerity = 6.0f
            }
        },

        new CharacterDefinition
        {
            id = "Rep",
            type = "Subject-R",
            description = "The Repulsive type.\nLow impulse-control threshold; quick to act under stress; designed for crisis-response simulations; reveals how sudden decisions affect controlled environments.",
            traitRatings = new TraitRatings
            {
                curiosity = 3.0f,
                discipline = 1.0f,
                drive = 8.5f,
                empathy = 1.5f,
                instability = 9.5f,
                sincerity = 1.5f
            }
        },

        new CharacterDefinition
        {
            id = "Soc",
            type = "Subject-S",
            description = "The Social type.\nHighly extroverted and persuasive; excels at networking and influence; key to studying opinion spread, crowd dynamics, and viral communication.",
            traitRatings = new TraitRatings
            {
                curiosity = 6.0f,
                discipline = 4.0f,
                drive = 9.5f,
                empathy = 8.0f,
                instability = 3.0f,
                sincerity = 4.5f
            }
        },

        new CharacterDefinition
        {
            id = "Log",
            type = "Subject-L",
            description = "The Logical type.\nPredominantly rational and composed; excels at structured decision-making and calculations; serves as a stabilizing baseline in behavioral experiments.",
            traitRatings = new TraitRatings
            {
                curiosity = 8.5f,
                discipline = 9.5f,
                drive = 3.5f,
                empathy = 3.0f,
                instability = 1.0f,
                sincerity = 6.5f
            }
        },

        new CharacterDefinition
        {
            id = "Inw",
            type = "Subject-I",
            description = "The Inward type.\nLow external energy and visibility; blends into background populations; useful for studying observer bias and unnoticed anomaly detection in simulations.",
            traitRatings = new TraitRatings
            {
                curiosity = 3.5f,
                discipline = 6.5f,
                drive = 1.5f,
                empathy = 3.0f,
                instability = 5.0f,
                sincerity = 7.0f
            }
        },

        new CharacterDefinition
        {
            id = "Cor",
            type = "Subject-C",
            description = "The Core type.\nHigh and stable life-energy baseline; balanced across emotional, reactive, and social traits; represents the laboratory's ideal equilibrium model for sustaining the controlled world.",
            traitRatings = new TraitRatings
            {
                curiosity = 5.0f,
                discipline = 7.0f,
                drive = 5.5f,
                empathy = 6.0f,
                instability = 1.5f,
                sincerity = 8.0f
            }
        },

        new CharacterDefinition
        {
            id = "Soc",
            type = "Subject-S",
            description = "The Social type.\nHighly extroverted and persuasive; excels at networking and influence; key to studying opinion spread, crowd dynamics, and viral communication.",
            traitRatings = new TraitRatings
            {
                curiosity = 6.0f,
                discipline = 5.0f,
                drive = 6.0f,
                empathy = 7.0f,
                instability = 6.0f,
                sincerity = 5.0f
            }
        },

        new CharacterDefinition
        {
            id = "Rep",
            type = "Subject-R",
            description = "The Repulsive type.\nLow impulse-control threshold; quick to act under stress; designed for crisis-response simulations; reveals how sudden decisions affect controlled environments.",
            traitRatings = new TraitRatings
            {
                curiosity = 4.0f,
                discipline = 2.0f,
                drive = 6.0f,
                empathy = 4.0f,
                instability = 9.0f,
                sincerity = 2.0f
            }
        }
    };

    private void Awake()
    {
        if (!enableDebugBootstrap)
            return;

        if (GameSession.Instance != null)
            return;

        GameObject sessionObject = new GameObject("GameSession");
        GameSession session = sessionObject.AddComponent<GameSession>();

        DontDestroyOnLoad(sessionObject);
        
        foreach (CharacterDefinition subject in debugCharacters)
        {
            session.AddSubject(subject);
        }

        Debug.Log("[CafeDebugGameSessionBootstrap] Created debug GameSession with "
                  + debugCharacters.Count + " subjects.");
    }
}