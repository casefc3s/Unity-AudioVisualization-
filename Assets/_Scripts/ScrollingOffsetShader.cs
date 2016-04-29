using UnityEngine;
using System.Collections;

public class ScrollingOffsetShader : MonoBehaviour {
	public float scale = 0.01f;
	public float x = 0f;	// scroll speeds
	public float y = 0f;
	public float xR = 0.05f;
	public float yR = 0.08f;

	float xTime = 0f;
	float yTime = 0f;
	float xC = 0f, yC = 0f, xNew = 0f, yNew = 0f;

	Vector2 offset = Vector2.zero;
	Material mat;

	float lastSwitch = 0f;
	float switchTime = 1f;

	// Use this for initialization
	void Start () {
		mat = GetComponent<Renderer>().material;
		xC = xNew = x;
		yC = yNew = y;
	}
	
	// Update is called once per frame
	void Update () {
		xTime += Time.deltaTime+(Random.value*xR*scale);
		yTime += Time.deltaTime+(Random.value*yR*scale);

		xC = Mathf.Lerp (xC, xNew, xTime-lastSwitch);
		yC = Mathf.Lerp (yC, yNew, yTime-lastSwitch);

		offset.x += xC/**Mathf.Sin (xTime)*/*scale;
		offset.y += yC*Mathf.Cos (yTime)*scale;

		if ((xTime-lastSwitch) >= switchTime) {
			xNew = Mathf.Max (Random.value*x, 0.1f);
		}
		if ((yTime-lastSwitch) >= switchTime) {
			yNew = Mathf.Max (Random.value*y, 0.1f);
		}

		if (offset.x > 1f) offset.x -= 1f;
		if (offset.x < 0f) offset.x += 1f;
		if (offset.y > 1f) offset.y -= 1f;
		if (offset.y < 0f) offset.y += 1f;

		mat.mainTextureOffset = offset;
	}
}
