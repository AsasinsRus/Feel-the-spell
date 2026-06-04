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
    private float firstCircleInventoryRadius;
    [SerializeField]
    private float nextCircleRadiusToAdd;

    [SerializeField]
    private int firstCircleSlotCount;
    [SerializeField]
    private int nextCircleSlotsToAdd;

    private bool isInventoryOpened = false;

    [Header("Input info")]
    [SerializeField]
    private InputActionProperty grabItem;

    [Header("Animation info")]
    [SerializeField]
    private float animationTime;
    private Dictionary<Transform, Transform> inventory = new();
    private int runningAnimations = 0;

    [Header("Flags")]
    [SerializeField]
    private bool canClearSlots = true;
    [SerializeField]
    private bool canAddSlots = true;
    [SerializeField]
    private bool creative = false;

    private bool IsItem(Transform key) => !inventory[key] || !inventory[key].GetComponent<Item>();

    private void Awake()
    {
        grabItem.action.performed += UseInventory;

        for (int i = 0; i < transform.childCount; i++)
            inventory.Add(transform.GetChild(i), transform.GetChild(i).GetComponent<XRSocketInteractor>()?.attachTransform);

        SetSlotsActive(false);

        if (IsItem(inventory.Last().Key)) AddSlot();
    }

    public void AddItem(SelectEnterEventArgs args)
    {
        inventory[args.interactorObject.transform] = args.interactableObject.transform;

        if (canAddSlots && inventory.Keys.Last() == args.interactorObject.transform)
            AddSlot();
    }

    private void ScaleItem(Transform item)
    {
        Renderer slot = slotPrefab.GetComponent<Renderer>();

        //slot.bound
    }

    public void TakeItem(SelectExitEventArgs args)
    {
        if(!isInventoryOpened || runningAnimations > 0) return;

        inventory[args.interactorObject.transform] = null;

        if(canClearSlots)
            ClearSlots();
    }

    public void AddSlot()
    {
        var newSlot = Instantiate(slotPrefab, inventoryPrefab.transform);
        newSlot.transform.position = Vector3.zero;

        int slotIndex = inventory.Count;
        int slotCountInCircle = firstCircleSlotCount;
        float radius = firstCircleInventoryRadius;

        while (slotIndex > slotCountInCircle - 1)
        {
            slotIndex -= slotCountInCircle;
            slotCountInCircle += nextCircleSlotsToAdd;
            radius += nextCircleRadiusToAdd;
        }

        newSlot.transform.localPosition = GetCircularPosition(radius, slotIndex, slotCountInCircle);
        
        inventory.Add(newSlot.transform, null);

        var interactor = newSlot.GetComponent<XRSocketInteractor>();
        interactor.selectEntered.AddListener(AddItem);
        interactor.selectExited.AddListener(TakeItem);
    }

    public void ClearSlots()
    {
        var keys = inventory.Keys.ToList();

        for(int i = keys.Count - 1; i > 1; i--)
        {
            if (IsItem(keys[i - 1]))
            {
                inventory.Remove(keys[i]);
                Destroy(keys[i].gameObject);
            }
            else break;
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
        SetItemsActive(true);

        inventoryPrefab.transform.position = inventoryAnchor.position;
        SetItemsPositions(inventoryAnchor.position);

        int i = 0;
        int slotCount = firstCircleSlotCount;
        float inventoryRadius = firstCircleInventoryRadius;

        inventoryPrefab.transform.rotation = Quaternion.Euler(.0f, inventoryAnchor.eulerAngles.y, .0f);

        foreach (Transform t in inventory.Keys)
        {
            Transform captured = t;

            bool isLast = inventory.Keys.Last() == t;

            StartCoroutine(
                    InventoryAnim(captured, GetCircularPosition(inventoryRadius, i++, slotCount), () =>
                    {
                        t.gameObject.GetComponent<XRSocketInteractor>().socketActive = true;

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

        foreach (Transform t in inventory.Keys)
        {
            Transform captured = t;

            t.gameObject.GetComponent<XRSocketInteractor>().socketActive = false;

            StartCoroutine(
                InventoryAnim(captured, captured.parent.InverseTransformPoint(inventoryAnchor.position), () =>
                {
                    if (inventory[captured]) inventory[captured].gameObject.SetActive(false);
                        
                    captured.gameObject.SetActive(false);
                    captured.localPosition = Vector3.zero;
                })
            );
        }
    }

    private void SetSlotsActive(bool state)
    {
        foreach(Transform t in inventory.Keys)
        {
            t.gameObject.SetActive(state);
        }
    }

    private void SetItemsPositions(Vector3 pos)
    {
        foreach (Transform t in inventory.Keys)
        {
            t.position = pos;
        }
    }

    private void SetItemsInteractability(bool state)
    {
        foreach (Transform t in inventory.Keys)
        {
            if(inventory[t])
            {
                var interactable = inventory[t].gameObject.GetComponent<XRGrabInteractable>();
                var rb = inventory[t].gameObject.GetComponent<Rigidbody>();
                
                if (interactable) interactable.enabled = state;
                if(rb) rb.useGravity = state;
            }
        }
    }

    private void SetItemsActive(bool state)
    {
        foreach( Transform t in inventory.Keys)
        {
            if(inventory[t]) inventory[t].gameObject.SetActive(state);
        }
    }

    private Vector3 GetCircularPosition(float radius, int index, int slotCount)
    {
        float angle = (2 * Mathf.PI / slotCount) * index;

        float x = radius * Mathf.Cos(angle);
        float y = radius * Mathf.Sin(angle);

        return new Vector3(x, y, 0);
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
            if(inventory.TryGetValue(toAnim, out var item) && item) item.position = toAnim.position;

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
