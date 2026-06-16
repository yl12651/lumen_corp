using UnityEngine;
using UnityEngine.EventSystems;

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
        if (IsPointerOverUi())
            return;

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

    private bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
            return false;

        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return EventSystem.current.IsPointerOverGameObject();
    }
}
