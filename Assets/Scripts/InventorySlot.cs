//using UnityEngine;
//using UnityEngine.XR.Interaction.Toolkit;

//public class InventorySlot : MonoBehaviour
//{

//    public bool hasItem;

//    public GameObject item;
//    public bool itemIsInteractable;

//    public Inventory inventory;

//    public void AddItem(SelectEnterEventArgs args)
//    {
//        item = args.interactableObject.transform.gameObject;

//        hasItem = true;

//        inventory.AddItem()
//    }

//    private void ScaleItem(Transform item)
//    {
//        Renderer slot = slotPrefab.GetComponent<Renderer>();

//        //slot.bound
//    }

//    public void TakeItem(SelectExitEventArgs args)
//    {
//        if (!isInventoryOpened || runningAnimations > 0) return;

//        inventory[args.interactorObject.transform] = null;

//        if (canClearSlots)
//            ClearSlots();
//    }

//    private void ScaleItem(Transform item)
//    {
//        Renderer slot = slotPrefab.GetComponent<Renderer>();

//        //slot.bound
//    }
//}
