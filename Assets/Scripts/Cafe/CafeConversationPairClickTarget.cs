using UnityEngine;

public class CafeConversationPairClickTarget : MonoBehaviour
{
    [SerializeField] private CafeConversationSetController conversationSetController;
    [SerializeField] private string pairKey;
    [SerializeField] private bool requirePreparedConversation = true;

    public string PairKey => pairKey;

    private void Awake()
    {
        if (conversationSetController == null)
            conversationSetController = FindFirstObjectByType<CafeConversationSetController>();
    }

    private void OnMouseDown()
    {
        PlayConversation();
    }

    public void PlayConversation()
    {
        if (conversationSetController == null)
        {
            Debug.LogWarning("[CafeConversationPairClickTarget] Conversation set controller is not assigned.", this);
            return;
        }

        if (requirePreparedConversation && !conversationSetController.HasConversationForPair(pairKey))
        {
            Debug.LogWarning("[CafeConversationPairClickTarget] No prepared conversation for pair key: " + pairKey, this);
            return;
        }

        conversationSetController.PlayPair(pairKey);
    }
}
