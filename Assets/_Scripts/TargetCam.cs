using UnityEngine;
using System.Collections;

public class TargetCam : MonoBehaviour {

	public Transform target;
	public float orbitRadius = 20f;
	public float anglePerSec = 1f;
	public float angleChangeSpeed = 1f;
	[HideInInspector]
	public float a = 0f;
	public float height = 13f;
	public float heightChangeSpeed = 5f;
	public float camSpeed = 5f;

	public float fovChangeSpeed = 5f;

	[HideInInspector]
	public Transform tr;

	float x = 0f, y = 0f;

	bool rdy = false;

	// Use this for initialization
	void Start () {
		tr = transform;

		if (target != null) {
			rdy = true;
		}
	}

	void Update() {
		if (Input.GetKey (KeyCode.UpArrow)) {
			height += heightChangeSpeed*Time.deltaTime;
		} else if (Input.GetKey (KeyCode.DownArrow)) {
			height -= heightChangeSpeed*Time.deltaTime;
		}

		if (Input.GetKey (KeyCode.LeftArrow)) {
			anglePerSec -= angleChangeSpeed*Time.deltaTime;
		} else if (Input.GetKey (KeyCode.RightArrow)) {
			anglePerSec += angleChangeSpeed*Time.deltaTime;
		}

		if (Input.GetKey (KeyCode.LeftBracket)) {
			Camera.main.fieldOfView -= fovChangeSpeed*Time.deltaTime;
		} else if (Input.GetKey (KeyCode.RightBracket)) {
			Camera.main.fieldOfView += fovChangeSpeed*Time.deltaTime;
		}

		height = Mathf.Clamp (height, 1f, 100f);
		anglePerSec = Mathf.Clamp(anglePerSec, -10f, 10f);
	}
	
	// Update is called once per frame
	void LateUpdate () {
		if (!rdy) return;


		x = target.position.x + orbitRadius * Mathf.Cos (Mathf.Deg2Rad*a);
		y = target.position.z + orbitRadius * Mathf.Sin (Mathf.Deg2Rad*a);
		tr.position = Vector3.Slerp(tr.position, new Vector3 (x, height, y), camSpeed*Time.deltaTime);

		tr.LookAt(target.position);

		a += Time.deltaTime*anglePerSec;
		if (a > 360f) a -= 360f;
	}
}
