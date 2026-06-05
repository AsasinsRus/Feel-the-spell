using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Manages inventory slots, shows inventory visuals.
/// </summary>
public class Inventory : MonoBehaviour
{
    [Header("Slot info")]
    [SerializeField]
    private GameObject slotPrefab;

    [Header("Inventory info")]
    [SerializeField]
    private GameObject inventoryPrefab;
    [SerializeField]
    private Transform inventoryAnchor;

    /// <summary>
    /// Radius for the first layer of an inventory after openning.
    /// </summary>
    [SerializeField]
    public float firstCircleInventoryRadius;
    /// <summary>
    /// How far should each next layer be.
    /// </summary>
    [SerializeField]
    public float nextCircleRadiusToAdd;

    /// <summary>
    /// How many slots should first layer have.
    /// </summary>
    [SerializeField]
    public int firstCircleSlotCount;
    /// <summary>
    /// How many slots to add to each new layer.
    /// </summary>
    [SerializeField]
    public int nextCircleSlotsToAdd;

    /// <summary>
    /// Indicates if inventory is open.
    /// </summary>
    public bool isInventoryOpened { get; private set; }

    /// <summary>
    /// Input for openning/closing an inventory.
    /// </summary>
    [Header("Input info")]
    [SerializeField]
    private InputActionProperty grabItem;

    [Header("Animation info")]
    [SerializeField]
    private float animationTime;
    private List<InventorySlot> slots = new();
    /// <summary>
    /// Indicates how many slots being animated right now.
    /// </summary>
    public int runningAnimations { get; private set; }

    [Header("Flags")]
    [SerializeField]
    private bool canClearSlots = true;
    [SerializeField]
    private bool canAddSlots = true;
    [SerializeField]
    private bool creative = false;

    private void Awake()
    {
        grabItem.action.performed += UseInventory;

        int i = 0;

        foreach(InventorySlot slot in GetComponentsInChildren<InventorySlot>())
        {
            slots.Add(slot);
            slot.InventoryIndex = i;

            i++;
        }

        if (creative) SetCreativeActive();

        SetSlotsActive(false);

        if (slots.Last().hasItem && !creative) AddSlot();
    }

    /// <summary>
    /// Adds items to an inventory.
    /// </summary>
    /// <param name="slotIndex">At which slot was item added.</param>
    public void AddItem(int slotIndex)
    {
        if (canAddSlots && slotIndex == slots.Count - 1 && slots.Last().hasItem)
            AddSlot();
    }

    /// <summary>
    /// Removes item from an inventory.
    /// </summary>
    public void RemoveItem()
    {
        if(canClearSlots)
            ClearSlots();
    }

    /// <summary>
    /// Adds new slot to an inventory.
    /// </summary>
    public void AddSlot()
    {
        var newSlot = Instantiate(slotPrefab, inventoryPrefab.transform);
        int newSlotIndex = slots.Count;

        newSlot.transform.position = Vector3.zero;
        slots.Add(newSlot.GetComponent<InventorySlot>());

        slots[newSlotIndex].InventoryIndex = newSlotIndex;

        newSlot.transform.localPosition = slots[newSlotIndex].inventoryPosition;
    }

    /// <summary>
    /// Clears all slots form the end until only one empty slots stays after the last item in inventory.
    /// </summary>
    public void ClearSlots()
    {
        int toRemoveElems = 0;

        for(int i = slots.Count - 1; i > 1; i--)
        {
            if (!slots[i - 1].hasItem)
            {
                toRemoveElems++;
                Destroy(slots[i].gameObject);
            }
            else break;
        }

        slots.RemoveRange(slots.Count - toRemoveElems, toRemoveElems);
    }

    private void SetCreativeActive()
    {
        canAddSlots = false;
        canClearSlots = false;

        var items = ItemsRegistry.Instance.Items;

        while (slots.Count < items.Count)
        {
            AddSlot();
        }

        for(int i = 0; i < items.Count; i++)
        {
            var item = Instantiate(items[i].prefab);

            item.SetActive(false);

            slots[i].AddItem(item);
            slots[i].infinite = true;
            slots[i].itemScaling = item.transform.localScale;

            slots[i].interactor.showInteractableHoverMeshes = false;
        }
    }

    private void UseInventory(InputAction.CallbackContext obj)
    {
        if (runningAnimations > 0) return;

        if (!isInventoryOpened)
        {
            ShowInventory();
        }
        else
        {
            HideInventory();
        }
    }

    private void ShowInventory()
    {
        SetSlotsActive(true);

        inventoryPrefab.transform.position = inventoryAnchor.position;

        int i = 0;
        int slotCount = firstCircleSlotCount;
        float inventoryRadius = firstCircleInventoryRadius;

        inventoryPrefab.transform.rotation = Quaternion.Euler(.0f, inventoryAnchor.eulerAngles.y, .0f);

        foreach (InventorySlot slot in slots)
        {
            Transform captured = slot.transform;

            bool isLast = slots.Last() == slot;

            StartCoroutine(
                    InventoryAnim(captured, slot.inventoryPosition, () =>
                    {
                        slot.interactor.socketActive = true;

                        if (isLast)
                        {
                            SetItemsInteractability(true);
                            isInventoryOpened = true;
                        }
                    })
                );

            if (i >= slotCount)
            {
                i = 0;
                slotCount += nextCircleSlotsToAdd;
                inventoryRadius += nextCircleRadiusToAdd;
            }
        }
    }

    private void HideInventory()
    {
        isInventoryOpened = false;
        SetItemsInteractability(false);

        foreach (InventorySlot slot in slots)
        {
            Transform captured = slot.transform;

            slot.interactor.socketActive = false;

            StartCoroutine(
                InventoryAnim(captured, captured.parent.InverseTransformPoint(inventoryAnchor.position), () =>
                {
                    captured.gameObject.SetActive(false);
                    captured.localPosition = Vector3.zero;
                })
            );
        }
    }

    private void SetSlotsActive(bool state)
    {
        foreach(InventorySlot slot in slots)
        {
            slot.gameObject.SetActive(state);
        }
    }
    private void SetItemsInteractability(bool state)
    {
        foreach (InventorySlot slot in slots)
        {
            slot.ItemIsInteractable = state;
        }
    }

    IEnumerator InventoryAnim(Transform toAnim, Vector3 to, Action callback = null)
    {
        Vector3 startPos = toAnim.localPosition;
        float timeStamp = Time.time;

        runningAnimations++;

        while (Time.time - timeStamp < animationTime)
        {
            float percent = (Time.time - timeStamp) / animationTime;

            toAnim.localPosition = Vector3.Lerp(startPos, to, percent);

            yield return null;
        }

        toAnim.localPosition = to;
        callback?.Invoke();

        runningAnimations--;

        if(runningAnimations <= 0) runningAnimations = 0;
    }

    private void OnDestroy()
    {
        grabItem.action.performed -= UseInventory;
    }
}
