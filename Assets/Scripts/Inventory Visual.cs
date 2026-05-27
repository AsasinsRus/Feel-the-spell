using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class InventoryVisual : MonoBehaviour
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
    private int firstCirleSlotCount;
    [SerializeField]
    private int nextCirleSlotsToAdd;

    private bool isInventoryOpened;

    [Header("Input info")]
    [SerializeField]
    private InputActionProperty grabItem;

    [Header("Animation info")]
    [SerializeField]
    private float animationTime;
    private Dictionary<Transform, Transform> inventory = new();
    private bool isAnimationEnded = true;

    [Header("Flags")]
    [SerializeField]
    public bool canClearSlots = true;
    [SerializeField]
    public bool canAddSlots = true;
    [SerializeField]
    public bool creative = false;

    private void Awake()
    {
        grabItem.action.performed += UseInventory;

        for (int i = 0; i < transform.childCount; i++)
            inventory.Add(transform.GetChild(i), transform.GetChild(i).GetComponent<XRSocketInteractor>()?.attachTransform);

        SetSlotsActive(false);
    }

    public void AddItem(SelectEnterEventArgs args)
    {
        inventory[args.interactorObject.transform] = args.interactableObject.transform;

        Debug.Log(inventory[args.interactorObject.transform]);

        if (canAddSlots && inventory.Keys.Last() == args.interactorObject.transform)
            AddSlot();
    }

    public void TakeItem(SelectExitEventArgs args)
    {
        if(!isInventoryOpened || !isAnimationEnded) return;

        inventory[args.interactorObject.transform] = null;

        if(canClearSlots)
            ClearSlots();
    }

    public void AddSlot()
    {
        var newSlot = Instantiate(slotPrefab, inventoryPrefab.transform);
        newSlot.transform.position = Vector3.zero;

        int slotIndex = inventory.Count;
        int slotCountInCircle = firstCirleSlotCount;
        float radius = firstCircleInventoryRadius;

        while (slotIndex > slotCountInCircle - 1)
        {
            slotIndex -= slotCountInCircle;
            slotCountInCircle += nextCirleSlotsToAdd;
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
            if (!inventory[keys[i - 1]] || !inventory[keys[i - 1]].GetComponent<Item>())
            {
                inventory.Remove(keys[i]);
                Destroy(keys[i].gameObject);
            }
            else break;
        }
    }

    private void UseInventory(InputAction.CallbackContext obj)
    {
        if (!isAnimationEnded) return;

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
        int slotCount = firstCirleSlotCount;
        float inventoryRadius = firstCircleInventoryRadius;

        inventoryPrefab.transform.rotation = Quaternion.Euler(.0f, inventoryAnchor.eulerAngles.y, .0f);

        foreach(Transform t in inventory.Keys.ToList())
        {
            StartCoroutine(
                    InventoryAnim(t, GetCircularPosition(inventoryRadius, i++, slotCount), () =>
                    {
                        SetItemsInteractability(true);
                        isInventoryOpened = true;
                    })
                );

            if (i >= slotCount)
            {
                i = 0;
                slotCount += nextCirleSlotsToAdd;
                inventoryRadius += nextCircleRadiusToAdd;
            }
        }
    }

    private void HideInventory()
    {
        foreach (Transform t in inventory.Keys.ToList())
        {
            StartCoroutine(
                    InventoryAnim(t, t.parent.InverseTransformPoint(inventoryAnchor.position), () =>
                    {
                        if (inventory[t]) inventory[t].gameObject.SetActive(false);
                        t.gameObject.SetActive(false);
                        isInventoryOpened = false;
                        t.localPosition = Vector3.zero;
                    })
                );
        }

        SetItemsInteractability(false);
    }

    private void SetSlotsActive(bool state)
    {
        foreach(Transform t in inventory.Keys.ToList())
        {
            t.gameObject.SetActive(state);
        }
    }

    private void SetItemsPositions(Vector3 pos)
    {
        foreach (Transform t in inventory.Keys.ToList())
        {
            t.position = pos;
        }
    }

    private void SetItemsInteractability(bool state)
    {
        foreach (Transform t in inventory.Keys.ToList())
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
        foreach( Transform t in inventory.Keys.ToList())
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

    IEnumerator InventoryAnim(Transform toAnim, Vector3 to, Action afterAction = null, Action inAction = null)
    {
        Vector3 startPos = toAnim.localPosition;
        float timeStamp = Time.time;

        isAnimationEnded = false;

        while (Time.time - timeStamp < animationTime)
        {
            float percent = (Time.time - timeStamp) / animationTime;

            toAnim.localPosition = Vector3.Lerp(startPos, to, percent);
            if(inventory[toAnim]) inventory[toAnim].position = toAnim.position;

            yield return null;
        }

        toAnim.localPosition = to;
        afterAction?.Invoke();

        isAnimationEnded = true;
    }
    
}
