using TMPro;
using UnityEngine;

public class CatchTextForTMP : MonoBehaviour
{
    [SerializeField]
    private TMP_Text text;

    private void Start()
    {
        text = GetComponent<TMP_Text>();
    }

    public void SetText(string txt)
    {
        text.text = txt;
    }
}
