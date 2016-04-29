using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SongTitle : MonoBehaviour {
    public void Set(string txt) {
        Text text = gameObject.GetComponentInChildren<Text>();
        if ( text != null) text.text = txt;
    }
}
