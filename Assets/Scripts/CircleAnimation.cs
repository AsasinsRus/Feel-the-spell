//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using Unity.Mathematics;
//using UnityEngine;
//using UnityEngine.Splines;
//using UnityEngine.UI;
//using UnityEngine.XR.Interaction.Toolkit;
//using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Deprecated

//public class CircleAnimation : MonoBehaviour
//{
//    [SerializeField]
//    private Transform alchemyCircle;

//    public float baseRotationSpeed;
//    public Color baseCircleColor = Color.black;
//    public float activeRotationSpeed;
//    public Color activeCircleColor = Color.white;
//    public float acceleration;

//    private float currentSpeed;
//    private float targetSpeed;

//    [SerializeField]
//    private float circleCreatingTime = .3f;
//    public bool isCircleVisible = false;
//    [SerializeField]
//    private float itemsCirlceRadius = .5f;

//    private Image circleImage;
//    private Color currentColor;
//    private Color targetColor;

//    private Dictionary<Item, Vector3> itemPositons = new();

//    private void OnEnable()
//    {
//        currentSpeed = .0f;
//        targetSpeed = baseRotationSpeed;

//        currentColor = Color.black;
//        targetColor = baseCircleColor;

//        circleImage = alchemyCircle.gameObject.GetComponent<Image>();
//    }

//    void Update()
//    {
//        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * acceleration);
//        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * acceleration);

//        alchemyCircle.Rotate(new Vector3(0, 0, currentSpeed * Time.deltaTime));
//        circleImage.color = currentColor;
//    }

//    public void SetSpellReady(bool ready)
//    {
//        targetSpeed = ready ? activeRotationSpeed : baseRotationSpeed;
//        targetColor = ready ? activeCircleColor : baseCircleColor;
//    }

//    public void SetActive(bool active)
//    {
//        if(active)
//        {
//            StartCoroutine(CreateCircle());
//        }
//        else
//        {
//            foreach (Item item in itemPositons.Keys.ToList())
//            {
//                item.KeepInPlace = false;
//                item.GetComponent<Rigidbody>().useGravity = true;
//            }

//            itemPositons.Clear();
//        }
//    }

//    public void Clear()
//    {
//        itemPositons.Clear();
//    }

//    private IEnumerator CreateCircle()
//    {
//        circleImage.fillAmount = 0;
//        circleImage.gameObject.SetActive(true);

//        float elapsed = 0f;

//        while (elapsed < circleCreatingTime)
//        {
//            elapsed += Time.deltaTime;
//            circleImage.fillAmount = Mathf.Clamp01(elapsed / circleCreatingTime);
//            yield return null;
//        }

//        isCircleVisible = true;
//    }

//    private void OnTriggerEnter(Collider other)
//    {
//        if(other.TryGetComponent(typeof(Item), out var component))
//        {
//            if(itemPositons.ContainsKey(component as Item)) return;

//            XRGrabInteractable interactable;


//            if (!component.TryGetComponent(typeof(SplineAnimate), out var animateComp))
//            {
//                component.gameObject.AddComponent<SplineContainer>();
//                component.gameObject.AddComponent<SplineAnimate>();
                
//            }

//            if (component.TryGetComponent(typeof(XRGrabInteractable), out var interactableComp))
//            {
//                interactable = interactableComp as XRGrabInteractable;

//                if (interactable.isSelected)
//                    interactable.selectExited.AddListener(FindAndAnimateSpline);
//                else
//                {
//                    SelectExitEventArgs args = new SelectExitEventArgs();
//                    args.interactableObject = interactable;

//                    FindAndAnimateSpline(args);
//                }
//            }
//            else return;
//        }
//    }

//    private void OnTriggerExit(Collider other)
//    {
//        if (other.TryGetComponent(typeof(Item), out var component))
//        {
//            if (component.TryGetComponent(typeof(XRGrabInteractable), out var interactableComp))
//            {
//                XRGrabInteractable interactable = interactableComp as XRGrabInteractable;
//                interactable.selectExited.RemoveListener(FindAndAnimateSpline);
//            }
//        }
//    }

//    private void FindAndAnimateSpline(SelectExitEventArgs args)
//    {
//        Item newItem = args.interactableObject.transform.GetComponent<Item>();
//        Rigidbody newItemRB = args.interactableObject.transform.GetComponent<Rigidbody>();

//        // find position to move item
//        itemPositons.Add(newItem, new Vector3());
//        newItem.OnDestroy_ += OnItemDestroy;
//        FindItemPoses();
//        UpdateItemPositions();

//        // disable physics for item
//        Vector3 velocity = newItemRB.linearVelocity;

//        newItemRB.useGravity = false;

//        // creating spline to move along
//        UpdateItemPosition(newItem, velocity);

//        // if item will be took back
//        args.interactableObject.selectEntered.AddListener(OnCircleExit);
//    }

//    private IEnumerator AnimateAlongSpline(Item item, Spline spline, float duration)
//    {
//        item.KeepInPlace = false;

//        float elapsed = 0f;
//        while (elapsed < duration)
//        {
//            elapsed += Time.deltaTime;
//            float t = Mathf.Clamp01(elapsed / duration);
//            spline.Evaluate(t, out float3 pos, out float3 tangent, out float3 up);
//            item.transform.position = pos;
//            yield return null;
//        }

//        item.KeepInPlace = true;
//    }

//    private void OnCircleExit(SelectEnterEventArgs args)
//    {
//        Rigidbody itemRB = args.interactableObject.transform.GetComponent<Rigidbody>();
//        XRGrabInteractable interactable = args.interactableObject as XRGrabInteractable;

//        Item item = args.interactableObject.transform.GetComponent<Item>();
//        item.KeepInPlace = false;

//        args.interactableObject.selectEntered.RemoveListener(OnCircleExit);
//        args.interactableObject.selectExited.AddListener(RestoreGravity);

//        itemPositons.Remove(args.interactableObject.transform.GetComponent<Item>());
//        item.OnDestroy_ -= OnItemDestroy;

//        FindItemPoses();
//        UpdateItemPositions();
//        if (itemPositons.Count > 1)
//            UpdateItemPosition(itemPositons.Keys.Last(), itemPositons[itemPositons.Keys.Last()] - itemPositons[itemPositons.Keys.First()]);
//    }

//    private void OnItemDestroy(Item item)
//    {
//        itemPositons.Remove(item);
//    }

//    private void RestoreGravity(SelectExitEventArgs args)
//    {
//        args.interactableObject.selectExited.RemoveListener(RestoreGravity);

//        Rigidbody itemRB = args.interactableObject.transform.GetComponent<Rigidbody>();

//        itemRB.useGravity = true;
//    }

//    private void FindItemPoses()
//    {
//        int i = 0;

//        foreach(var item in itemPositons.Keys.ToList())
//        {
//            itemPositons[item] = GetCircularPosition(itemsCirlceRadius, i++, itemPositons.Count);
//        }
//    }

//    private void UpdateItemPositions()
//    {
//        var itemKeys = itemPositons.Keys.ToList();

//        for(int i = 0; i < itemKeys.Count - 1; i++)
//        {
//            // creating spline to move along
//            UpdateItemPosition(itemKeys[i], itemPositons[itemKeys[i + 1]] - itemPositons[itemKeys[i]]);
//        }
//    }

//    private void UpdateItemPosition(Item item, Vector3 velocity)
//    {
//        Spline spline = new Spline();

//        BezierKnot startingKnot = new BezierKnot(item.transform.position);
//        float tangentStrength = velocity.magnitude * .3f;
//        startingKnot.TangentIn = (float3)(velocity.normalized * tangentStrength);
//        startingKnot.TangentOut = -(float3)(velocity.normalized * tangentStrength);

//        BezierKnot endKnot = new BezierKnot(transform.TransformPoint(itemPositons[item]));
//        endKnot.TangentIn = new float3(0, -0.3f, 0);
//        endKnot.TangentOut = new float3(0, -0.3f, 0);

//        spline.Add(startingKnot, TangentMode.AutoSmooth);
//        spline.Add(endKnot, TangentMode.AutoSmooth);

//        StartCoroutine(AnimateAlongSpline(item, spline, Mathf.Clamp(velocity.magnitude * .2f, .5f, 2f)));
//    }

//    private Vector3 GetCircularPosition(float radius, int index, int slotCount)
//    {
//        float angle = (2 * Mathf.PI / slotCount) * index;

//        float x = radius * Mathf.Cos(angle);
//        float z = radius * Mathf.Sin(angle);

//        return new Vector3(x, 0, z);
//    }

//}
