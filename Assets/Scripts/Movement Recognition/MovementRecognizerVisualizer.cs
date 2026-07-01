using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(MovementRecognizer))]
public class MovementRecognizerVisualizer : MonoBehaviour
{
    [SerializeField]
    private LineRenderer lineRenderer;

    [SerializeField, Range(.0f, 5)]
    private float lineFadeOutTime = 0.5f;

    private MovementRecognizer recognizer;

    private Coroutine currentFadeOutAnim;

    private void OnEnable()
    {
        recognizer = GetComponent<MovementRecognizer>();

        recognizer.OnAddPoint += OnAddPoint;
        recognizer.OnRecognitionEnd += OnRecognitionEnd;
        recognizer.OnRecognitionStart += OnRecognitionStart;
    }

    private void OnDisable()
    {
        recognizer.OnAddPoint -= OnAddPoint;
        recognizer.OnRecognitionEnd -= OnRecognitionEnd;
        recognizer.OnRecognitionStart -= OnRecognitionStart;
    }

    private void OnRecognitionStart()
    {
        if (currentFadeOutAnim != null)
        {
            StopCoroutine(currentFadeOutAnim);
            lineRenderer.positionCount = 0;
        }
    }

    private void OnAddPoint(Vector3 point)
    {
        lineRenderer.SetPosition(lineRenderer.positionCount++, point);
    }

    private void OnRecognitionEnd()
    {
        //lineRenderer.positionCount = 0;

        currentFadeOutAnim = StartCoroutine(LineFadeOut());
    }

    private IEnumerator LineFadeOut()
    {
        float timeStamp = Time.time;
        Vector3[] points = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(points);

        for(int i = lineRenderer.positionCount - 1; i >= 0; i--)
        {
            lineRenderer.SetPosition(lineRenderer.positionCount - i - 1, points[i]);
        }

        float seconds = lineFadeOutTime / points.Length;

        while (lineRenderer.positionCount > 0)
        {
            lineRenderer.positionCount--;

            yield return new WaitForSeconds(seconds);
        }
    }
}
