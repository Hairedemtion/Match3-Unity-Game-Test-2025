using UnityEngine;

[CreateAssetMenu(fileName = "ItemSkin", menuName = "ScriptableObjects/ItemSkin")]
public class ItemSkin : ScriptableObject
{
    public Sprite[] skins;

    public Sprite Get(int id) => skins[id];
}
