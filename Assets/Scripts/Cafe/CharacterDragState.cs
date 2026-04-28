public static class CharacterDragState
{
    public static bool IsDragging { get; private set; }
    public static int InventoryIndex { get; private set; } = -1;
    public static CharacterDefinition Subject { get; private set; }

    public static void BeginDrag(int inventoryIndex, CharacterDefinition subject)
    {
        IsDragging = true;
        InventoryIndex = inventoryIndex;
        Subject = subject;
    }

    public static void EndDrag()
    {
        IsDragging = false;
        InventoryIndex = -1;
        Subject = null;
    }
}