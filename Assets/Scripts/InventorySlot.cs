using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class InventorySlot : MonoBehaviour
{

    [SerializeField]
    public bool hasItem { get; private set; }

    private GameObject item;
    private Vector3 originalItemScale;
    private Quaternion itemRotation;
    private Vector3 itemScaling;
    public GameObject Item { 
        get => item; 
        private set 
        { 
            if(value == null)
                hasItem = false;
            else hasItem = true;

            item = value;
        } 
    }
    private bool itemIsInteractable;
    public bool ItemIsInteractable 
    { 
        get => itemIsInteractable;
        set 
        {
            SetItemInteractability(value);

            itemIsInteractable = value;
        }
    }

    public Inventory inventory;
    public Vector3 inventoryPosition { get; private set; }

    [SerializeField]
    private int inventoryIndex;
    public int InventoryIndex { 
        get => inventoryIndex; 
        set 
        {
            inventoryIndex = value;

            inventoryPosition = FindInventoryPos(value);
        } 
    }

    public XRSocketInteractor interactor { get; private set; }

    private void Awake()
    {
        inventory = FindAnyObjectByType<Inventory>();
        interactor = GetComponent<XRSocketInteractor>();

        interactor.selectEntered.RemoveAllListeners();
        interactor.selectExited.RemoveAllListeners();

        interactor.selectEntered.AddListener(AddItem);
        interactor.selectExited.AddListener(TakeItem);
    }

    private void Update()
    {
        if(item != null && !itemIsInteractable)
        {
            item.transform.position = transform.position;
            item.transform.rotation = itemRotation;
            item.transform.localScale = itemScaling;
        }
    }

    public void AddItem(SelectEnterEventArgs args)
    {
        Item = args.interactableObject.transform.gameObject;

        if(interactor.socketScaleMode != SocketScaleMode.StretchedToFitSize)
            ScaleItem(Item);

        inventory.AddItem(InventoryIndex);
    }

    private void ScaleItem(GameObject item)
    {
        Renderer slot = GetComponent<Renderer>();
        originalItemScale = item.transform.localScale;

        var itemRenderer = item.GetComponentInChildren<Renderer>();

        if(itemRenderer == null) return;

        float maxItemSize = Mathf.Max(
            itemRenderer.bounds.size.x,
            itemRenderer.bounds.size.y,
            itemRenderer.bounds.size.z
        );

        if (maxItemSize > slot.bounds.size.x)
        {
            interactor.socketScaleMode = SocketScaleMode.StretchedToFitSize;
            interactor.targetBoundsSize = slot.bounds.size;
        }
    }

    private void UnscaleItem(Transform item)
    {
        interactor.socketScaleMode = SocketScaleMode.None;

        if(originalItemScale != null)
            item.localScale = originalItemScale;
        originalItemScale = default;
    }

    public void TakeItem(SelectExitEventArgs args)
    {
        if (!inventory.isInventoryOpened || inventory.runningAnimations > 0) return;

        if (Item == null) return;

        UnscaleItem(Item.transform);
        Item = null;

        inventory.RemoveItem();
    }

    private Vector3 FindInventoryPos(int slotIndex)
    {
        int slotCountInCircle = inventory.firstCircleSlotCount;
        float radius = inventory.firstCircleInventoryRadius;

        while (slotIndex > slotCountInCircle - 1)
        {
            slotIndex -= slotCountInCircle;
            slotCountInCircle += inventory.nextCircleSlotsToAdd;
            radius += inventory.nextCircleRadiusToAdd;
        }

        return GetCircularPosition(radius, slotIndex, slotCountInCircle);
    }

    private Vector3 GetCircularPosition(float radius, int index, int slotCount)
    {
        float angle = (2 * Mathf.PI / slotCount) * index;

        float x = radius * Mathf.Cos(angle);
        float y = radius * Mathf.Sin(angle);

        return new Vector3(x, y, 0);
    }

    private void SetItemInteractability(bool state)
    {
        if(item ==  null) return;

        itemRotation = Item.transform.rotation;
        itemScaling = Item.transform.localScale;

        var interactable = item.GetComponent<XRGrabInteractable>();
        var rb = item.GetComponent<Rigidbody>();

        if (interactable)
        {
            interactable.enabled = state;
        }
        if (rb) rb.useGravity = state;
    }

    private void OnEnable()
    {
        if (item != null)
        {
            item.SetActive(true);
            item.transform.position = transform.position;
        }
    }

    private void OnDisable()
    {
        if (item != null)
        {
            item.SetActive(false);
            item.transform.position = transform.position;
        }
    }

    private void OnDestroy()
    {
        interactor.selectEntered.RemoveAllListeners();
        interactor.selectExited.RemoveAllListeners();
    }
}
