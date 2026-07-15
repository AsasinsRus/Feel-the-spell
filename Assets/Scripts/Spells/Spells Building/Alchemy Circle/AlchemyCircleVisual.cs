using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates visual effects of the alchemist circle
/// </summary>
public class AlchemyCircleVisual : MonoBehaviour
{
    [SerializeField]
    private Transform alchemyCircle;

    [Header("Base")]
    public float baseRotationSpeed;
    public Color baseCircleColor = Color.black;
    [Header("Active")]
    public float activeRotationSpeed;
    public Color activeCircleColor = Color.white;
    [Header("Change")] 
    public float acceleration;

    private float currentSpeed;
    private float targetSpeed;

    [SerializeField]
    private float circleCreatingTime = .3f;
    public bool isCircleVisible = false;

    private Image circleImage;
    private Color currentColor;
    private Color targetColor;

    private void OnEnable()
    {
        currentSpeed = .0f;
        targetSpeed = baseRotationSpeed;

        currentColor = Color.black;
        targetColor = baseCircleColor;

        circleImage = alchemyCircle.gameObject.GetComponent<Image>();
        circleImage.fillAmount = 0;
    }

    void Update()
    {
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * acceleration);
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * acceleration);

        alchemyCircle.Rotate(new Vector3(0, 0, -currentSpeed * Time.deltaTime));
        circleImage.color = currentColor;
    }

    public void SetSpellReady(bool ready)
    {
        targetSpeed = ready ? activeRotationSpeed : baseRotationSpeed;
        targetColor = ready ? activeCircleColor : baseCircleColor;
    }

    public void Show() => StartCoroutine(CreateCircle());
    private IEnumerator CreateCircle()
    {
        circleImage.fillAmount = 0;
        circleImage.gameObject.SetActive(true);

        float elapsed = 0f;

        while (elapsed < circleCreatingTime)
        {
            elapsed += Time.deltaTime;
            circleImage.fillAmount = Mathf.Clamp01(elapsed / circleCreatingTime);
            yield return null;
        }

        isCircleVisible = true;
    }
}
