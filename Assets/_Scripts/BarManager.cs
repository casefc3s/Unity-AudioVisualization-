using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BarManager : MonoBehaviour {
		public GameObject[] colorPrefabs;
	AudioMagic magicScript;
	public Bars[] bars;
	bool set = false;
		Color startColor;
		Color endColor;

	// Use this for initialization
	void Start () {
		Init();
	}

	void Init() {
		List<GameObject> avail = new List<GameObject>();
		for (int i = 0; i < colorPrefabs.Length; i++) {
			avail.Add(colorPrefabs[i]);
		}
		for (int i = 0; i < bars.Length; i++) {
			int r = Random.Range(0,avail.Count);
			bars[i].Init(avail[r]);

			if (i == 0) {
				startColor = avail[r].GetComponentInChildren<MeshRenderer>().sharedMaterial.GetColor("_Color");
			} else if (i == bars.Length-1) {
				endColor = avail[r].GetComponentInChildren<MeshRenderer>().sharedMaterial.GetColor("_Color");
			}

			avail.RemoveAt(r);
	}
		
		if (magicScript == null) magicScript = GameObject.FindObjectOfType<AudioMagic>();
				if (magicScript != null) magicScript.Init(startColor, endColor);
	}

	void Update() {
		if (Input.GetKeyDown(KeyCode.R)) {
            DoReset();
		}
	}

    public void DoReset() {
        StartCoroutine(Reset());
    }

	IEnumerator Reset() {
		magicScript.ready = false;

		yield return new WaitForSeconds(1f);

		for (int i = 0; i < bars.Length; i++) {
			Transform tr = bars[i].transform;

			List<GameObject> children = new List<GameObject>();
			foreach (Transform child in tr) 
				children.Add(child.gameObject);

			children.ForEach(child => Destroy(child));
		}

		yield return new WaitForSeconds(1f);

		Init();

		yield return null;
	}
}
