using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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

    [SerializeField]
    public float firstCircleInventoryRadius;
    [SerializeField]
    public float nextCircleRadiusToAdd;

    [SerializeField]
    public int firstCircleSlotCount;
    [SerializeField]
    public int nextCircleSlotsToAdd;

    public bool isInventoryOpened { get; private set; }

    [Header("Input info")]
    [SerializeField]
    private InputActionProperty grabItem;

    [Header("Animation info")]
    [SerializeField]
    private float animationTime;
    private List<InventorySlot> slots = new();
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

        SetSlotsActive(false);

        if (slots.Last().hasItem) AddSlot();
    }

    public void AddItem(int slotIndex)
    {
        if (canAddSlots && slotIndex == slots.Count - 1 && slots.Last().hasItem)
            AddSlot();
    }

    public void RemoveItem()
    {
        if(canClearSlots)
            ClearSlots();
    }

    public void AddSlot()
    {
        var newSlot = Instantiate(slotPrefab, inventoryPrefab.transform);
        int newSlotIndex = slots.Count;

        newSlot.transform.position = Vector3.zero;
        slots.Add(newSlot.GetComponent<InventorySlot>());

        slots[newSlotIndex].InventoryIndex = newSlotIndex;

        newSlot.transform.localPosition = slots[newSlotIndex].inventoryPosition;
    }

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
