using System;
using UnityEngine;

public class ItemsManager : MonoBehaviour
{
    [Serializable]
    public class ItemData
    {
        public string Name;
        public Sprite Icon;
        public GameObject Prefab;
    }

    [SerializeField] private string _answerItemName = "Answer";
    [SerializeField] private ItemData[] _itemsData;

    public static ItemsManager GetInstance() => FindObjectOfType<ItemsManager>(); // TODO: Remove

    public PaperBallController InstantiateAnswer(Vector3 position, Quaternion rotation, Transform parent)
    {
        return InstantiateItem(_answerItemName, position, rotation, parent)?.GetComponent<PaperBallController>();
    }

    public GameObject InstantiateItem(string itemName, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (TryGetItemDataByName(itemName, out ItemData itemData, verbose: true))
        {
            return Instantiate(itemData.Prefab, position, rotation, parent);
        }
        return null;
    }

    private bool TryGetItemDataByName(string itemName, out ItemData itemData, bool verbose)
    {
        foreach (var item in _itemsData)
        {
            if (item.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
            {
                itemData = item;
                return true;
            }
        }
        if (verbose)
        {
            Debug.LogError($"Item data not found by name: {itemName}");
        }
        itemData = default;
        return false;
    }
}
