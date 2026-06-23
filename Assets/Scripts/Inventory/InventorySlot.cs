using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Describes one inventory slot.
/// </summary>
public class InventorySlot : MonoBehaviour
{
    /// <summary>
    /// Shows if a slot has an item.
    /// </summary>
    [SerializeField]
    public bool hasItem { get; private set; }

    private GameObject item;
    private Vector3 originalItemScale;
    private Quaternion itemRotation;
    [HideInInspector]
    public Vector3 itemScaling;

    public bool infinite;

    // fixing onDestroy Exception
    private bool isShuttingDown = false;

    private void OnApplicationQuit()
    {
        isShuttingDown = true;
    }

    /// <summary>
    /// Item that this slot has.
    /// </summary>
    public GameObject Item
    {
        get => item;
        private set
        {
            if (value == null)
                hasItem = false;
            else hasItem = true;

            item = value;
        }
    }
    private bool itemIsInteractable;
    /// <summary>
    /// Shows and changes if the item can be picked up.
    /// </summary>
    public bool ItemIsInteractable
    {
        get => itemIsInteractable;
        set
        {
            SetItemInteractability(value);

            itemIsInteractable = value;
        }
    }

    /// <summary>
    /// Inventory that manges this slot.
    /// </summary>
    public Inventory inventory;

    /// <summary>
    /// Position in inventory where the slot will be placed after opening an <see cref="inventory"/>.
    /// </summary>
    public Vector3 inventoryPosition { get; private set; }


    [SerializeField]
    private int inventoryIndex;

    /// <summary>
    /// Index in inventory list.
    /// </summary>
    public int InventoryIndex
    {
        get => inventoryIndex;
        set
        {
            inventoryIndex = value;

            inventoryPosition = FindInventoryPos(value);
        }
    }

    public XRSocketInteractor interactor { get; private set; }

    [SerializeField]
    private float distanceToGenarateItem = 0.4f;

    private void Awake()
    {
        inventory = FindAnyObjectByType<Inventory>();
        interactor = GetComponent<XRSocketInteractor>();

        interactor.selectEntered.RemoveAllListeners();
        interactor.selectExited.RemoveAllListeners();

        interactor.selectEntered.AddListener(AddItem);
        interactor.selectExited.AddListener(TakeItem);

        if (interactor.attachTransform != null)
            AddItem(interactor.attachTransform.gameObject);
    }

    private void Update()
    {
        if (item != null && !itemIsInteractable)
        {
            item.transform.position = transform.position;
            item.transform.rotation = itemRotation;
            item.transform.localScale = itemScaling;
        }
    }

    /// <summary>
    /// Adds an item to the slot and slot to an <seealso cref="inventory"/>.
    /// </summary>
    /// <param name="args"></param>
    public void AddItem(SelectEnterEventArgs args)
    {
        AddItem(args.interactableObject.transform.gameObject);
    }

    /// <summary>
    /// Adds an item to the slot and slot to an <seealso cref="inventory"/>.
    /// </summary>
    /// <param name="args"></param>
    public void AddItem(GameObject item)
    {
        Item = item;

        itemScaling = item.transform.localScale;
        itemRotation = item.transform.rotation;

        if (interactor.socketScaleMode != SocketScaleMode.StretchedToFitSize)
            ScaleItem(Item);

        inventory.AddItem(InventoryIndex);
    }

    private void ScaleItem(GameObject item)
    {
        Renderer slot = GetComponent<Renderer>();
        originalItemScale = item.transform.localScale;

        var itemRenderer = item.GetComponentInChildren<Renderer>();

        if (itemRenderer == null) return;

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

        if (originalItemScale != null)
            item.localScale = originalItemScale;
        originalItemScale = default;
    }

    /// <summary>
    /// Deletes item from <seealso cref="inventory"/> and a slot.
    /// </summary>
    /// <param name="args"></param>
    public void TakeItem(SelectExitEventArgs args)
    {
        // to fix exceptions after closing the game
        if (isShuttingDown || !gameObject.activeInHierarchy) return;

        if (!inventory.isInventoryOpened || inventory.runningAnimations > 0) return;

        if (Item == null) return;

        UnscaleItem(Item.transform);

        if (infinite)
        {
            //StartCoroutine(RegenarateItem(Item.transform));

            RegenarateItem(Item);
        }
        else
        {
            Item = null;
            inventory.RemoveItem();
        }
    }

    private IEnumerator RegenarateItem(Transform takenItem)
    {
        while (takenItem != null && Vector3.Distance(takenItem.transform.position, transform.position) < distanceToGenarateItem)
        {
            yield return null;
        }

        Item.GetComponent<Rigidbody>().useGravity = true;
        Item = Instantiate(item, transform.position, transform.rotation);
    }

    private void RegenarateItem(GameObject takenItem)
    {
        var newItem = Instantiate(takenItem, transform.position, transform.rotation).GetComponent<Collider>();

        Physics.IgnoreCollision(newItem, takenItem.GetComponent<Collider>());

        StartCoroutine(DisableCollisionIgnore(takenItem.GetComponent<Collider>(), newItem));
    }

    private IEnumerator DisableCollisionIgnore(Collider takenItem, Collider newItem)
    {
        while (takenItem != null && Vector3.Distance(takenItem.transform.position, newItem.transform.position) < distanceToGenarateItem)
        {
            yield return null;
        }

        Physics.IgnoreCollision(newItem, takenItem.GetComponent<Collider>(), false);
    }

    private void SetCreativeModeActive(bool state)
    {
        if(state)
        {
            if(TryGetComponent(typeof(XRGrabInteractable), out var component))
            {
                Destroy(component);
            }

            var creativeSlot = gameObject.AddComponent<CreativeSlotInteractable>();

            creativeSlot.prefab = Item;
        }
        else
        {
            if (TryGetComponent(typeof(CreativeSlotInteractable), out var component))
            {
                Destroy(component);
            }

            gameObject.AddComponent<XRGrabInteractable>();
        }
    }

    /// <summary>
    /// Finds right local position in inventory.
    /// </summary>
    /// <param name="slotIndex">Depending of this value will the position be found</param>
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

    /// <summary>
    /// Finds right local position in inventory.
    /// </summary>
    /// <param name="radius"></param>
    /// <param name="index"></param>
    /// <param name="slotCount"></param>
    /// <returns></returns>
    private Vector3 GetCircularPosition(float radius, int index, int slotCount)
    {
        float angle = (2 * Mathf.PI / slotCount) * index;

        float x = radius * Mathf.Cos(angle);
        float y = radius * Mathf.Sin(angle);

        return new Vector3(x, y, 0);
    }

    private void SetItemInteractability(bool state)
    {
        if (item == null) return;

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
