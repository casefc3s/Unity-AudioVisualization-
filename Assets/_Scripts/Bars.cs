using UnityEngine;
using System.Collections;

public enum BarScheme {
	Loop = 0,
	CogSmall,
	CogLarge,
	Star,
	SpiralStar,
	Spiral
}

public class BarSetup {
		public BarScheme scheme = BarScheme.CogLarge;
		public float radius = 38f;
		public float radiusDescentRate = 0.1f;
		public int barSamples = 275;
		public float barScale = 150f;
		public bool autoRadius = true;
		public bool autoDescentRate = true;
}

public class Bars : MonoBehaviour {

	public BarScheme scheme = BarScheme.Loop;

	public GameObject barPrefab;
	public float radius = 10f;
	public float radiusDescentRate = 0.05f;
	public int barSamples = 128;

	public float barScale = 150f;

	public bool autoRadius = false;
	public bool autoDescentRate = true;
	float r = 0f;

	[HideInInspector]
	public Transform tr;

	void Awake() {
		tr = transform;
	}

	// Use this for initialization
		public void Init (GameObject prefab) {
		r = radius;
		if (autoRadius) r = 0.15f*(float)barSamples;
		if (autoDescentRate) {
			if (scheme == BarScheme.CogSmall) radiusDescentRate = 0.01f;
			else if (scheme == BarScheme.Star) radiusDescentRate = 0.1f;
			else if (scheme == BarScheme.CogLarge || scheme == BarScheme.SpiralStar) radiusDescentRate = 0.05f;
		}

		float angle = 1f;
		if (scheme == BarScheme.Loop || scheme == BarScheme.Spiral) {
			angle = 360f/(float)barSamples;	// loop/spiral
		} else {
			angle = (0.15f)*barSamples;		// cog/star
		}
		float a = 0f, x = 0f, y = 0f;

		for (int i = 0; i < barSamples; i++) {
			GameObject bar = Instantiate (/*barPrefab*/prefab) as GameObject;
			Transform b = bar.transform;
			b.name = "Bar " + i.ToString();
			b.SetParent(tr);

			x = tr.position.x + r * Mathf.Cos(Mathf.Deg2Rad*a);
			y = tr.position.z + r * Mathf.Sin(Mathf.Deg2Rad*a);

			// spiral segments
			if (scheme == BarScheme.Spiral) {
				r = 0.15f*(float)(barSamples-i)+(float)i*0.02f;
				angle = 360f/(float)(barSamples-i);
			}
			/// end spiral

			// cog/star segments
			if (scheme != BarScheme.Loop && scheme != BarScheme.Spiral)
				r -= radiusDescentRate;

			if (scheme == BarScheme.SpiralStar)
				a += radiusDescentRate*radiusDescentRate;
			/// end cog/star segments

			b.position = new Vector3(x, 0, y);
			b.LookAt(tr.position, Vector3.up);

			a += angle;

			bar.GetComponent<Bar>().container = this;
		}
	}
}

/* Interesting settings:
 * "COG effect"
 * radius 15-20, radiusDescentRate = 0.01f, samples 500, scale 150, no auto-radius, float angle = 0.15f*barSamples;
 * "STAR effect"
 * same as above, descent rate 0.1f, radius 50
 * "COG (large) effect"
 * r=50, descent=0.05f
 * "SPIRAL STAR effect"
 * same as above, r=40, add line a -= descent rate
 */