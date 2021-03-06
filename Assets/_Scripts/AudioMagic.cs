﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// vocal frequency 300Hz - 3.4kHz
// http://en.wikipedia.org/wiki/Voice_frequency
// http://www.audio-issues.com/music-mixing/5-need-to-know-frequency-areas-of-the-vocal/

// common equalizer frequencies
// public int[] freqRange = new int[10]{31, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000};

public class AudioMagic : MonoBehaviour {
    private float[] spectrumL;
    private float[] spectrumR;
    private float[] outputL;
    private float[] outputR;

    #region Sphere Variables
    [SerializeField]
    private GameObject vocalSphere;
    [SerializeField]
    private float sphereBaseScale = 5f;
    [SerializeField]
    private float sphereScaleFactor = 2f;
    [SerializeField]
    private Vector2 vocalFreqRange = new Vector2(500f, 3400f);
    [SerializeField]
    private float vocalLerpRate = 20f;
	private float vocalSamples = 0f;
	private float sphereScale = 0f;
    #endregion

    #region Bar Variables
    [SerializeField]
    private Transform barContainer;
	private List<GameObject> barGOs = new List<GameObject>();
    private List<float> bars = new List<float>();
    [SerializeField]
    private float baseHeight = 0.1f;
    [SerializeField]
    private float barScaleFactor = 150f;
    [SerializeField]
    private int numSamples = 8192;
    [SerializeField]
    private int lowerSampleRange = 0;
    [SerializeField]
    private int upperSampleRange = 8192;
    #endregion

    [Tooltip("We use this to adjust scale from lower to higher frequencies. 0-1 is the base to highest frequency.")]
    public AnimationCurve freqCurve;

	[HideInInspector]
	public bool ready = false;

    #region Lerping and Sample Rates/Resolutions
    int range = 0;
	float freqResolution = 0f;
	float samples = 0f;
	float maxSample = 0f;
	float sampleTime = 0f;
	int barsPerRange = 0;
    [SerializeField]
    private float sampleTimeRate = 1f;
    [SerializeField]
    private float riseLerpRate = 100f;
    [SerializeField]
    private float fallLerpRate = 10f;
    [SerializeField]
    private float[] freqRange = new float[10]{31f, 62f, 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f, 16000f};
    #endregion

    #region Materials
    private List<Material> mats = new List<Material>();
	private float scValue = 0f;
	private Color c;
    #endregion

    #region Additional Options
    [SerializeField]
    private bool fullSpectrumMode = true;
    [SerializeField]
    private bool greenLightShow = false;
    #endregion

    const float halfSampleRate = 24000f;
	private List<float> freqs = new List<float>();

    #region For Equalized Display
    float start = 0f;	// opening frequency
	float end = 0f;		// closing frequency
	float goalFreq = 0f;// target frequency of bar
	int ndx = 0;		// index of freqs[] for bar to use
	int prevNdx = 0;	// storing index to start search from
	int b = 0;			// bar index counter
	float desiredFreqStep = 0f;
    #endregion


    private List<Bar> barScripts = new List<Bar>();

    #region Emission References
    private float highestSample = 0f;
    private float highestSampleFreq = 0f;
    #endregion

    private bool showGUI = false;

    private Color[] sphereColors = new Color[2];

	// Use this for initialization
	public void Init (Color a, Color b) {
			sphereColors[0] = a;
			sphereColors[1] = b;

		if (barGOs.Count > 0) barGOs.Clear();
		if (bars.Count > 0) bars.Clear();
		if (mats.Count > 0) mats.Clear();
		if (barScripts.Count > 0) barScripts.Clear ();

		if (barContainer != null) {
			for (int i = 0; i < barContainer.childCount; i++) {
				for (int x = 0; x < barContainer.GetChild (i).childCount; x++) {
					GameObject bgo = barContainer.GetChild(i).GetChild (x).gameObject;
					barGOs.Add (bgo);
					barScripts.Add (bgo.GetComponent<Bar>());
				}
			}
		}

		if (barGOs.Count < 1) {
			Debug.Log ("No bars set.");
			return;
		}

		freqResolution = ((float)AudioSettings.outputSampleRate*0.5f)/(float)numSamples;
		Debug.Log ("Frequency Resolution: " + freqResolution.ToString());

		// instantiation
		Debug.Log("Bars: " + barGOs.Count.ToString());
		for (int i = 0; i < barGOs.Count; i++) {
			bars.Add (0.1f);
			mats.Add (barGOs[i].GetComponent<MeshRenderer>().material);
		}

		range = (upperSampleRange-lowerSampleRange) / barGOs.Count;
		Debug.Log ("Samples per bar: " + range.ToString() + " (" + freqResolution*(float)range + "Hz)");

		if (bars.Count < range) {
			Debug.Log ("Invalid number of bars available for samples.");
			return;
		}

		// storing each freq
		for (int i = 0; i < numSamples; i++) {
			freqs.Add ((float)i*freqResolution);
		}

		barsPerRange = (barGOs.Count/freqRange.Length);
		Debug.Log ("Bars per range: " + barsPerRange);

		spectrumL = spectrumR = new float[numSamples];
		outputL = outputR = new float[numSamples];

		ready = true;
	}

	// Update is called once per frame
	void Update () {
		if (!ready) return;

		// pull fresh spectrum data each frame
		AudioListener.GetSpectrumData(spectrumL, 0, FFTWindow.BlackmanHarris);
        AudioListener.GetSpectrumData(spectrumR, 1, FFTWindow.BlackmanHarris);
		AudioListener.GetOutputData(outputL, 0);
        AudioListener.GetOutputData(outputR, 1);
		//spectrum = new float[numSamples];
		//AudioListener.GetOutputData(spectrum, 0);

		// clear previous values
		vocalSamples = 0f;
		samples = 0f;

		if (fullSpectrumMode) {
			FullSpectrumUpdate();
		} else {
			FreqSpecificUpdate();
		}

		SphereUpdate();

		if (Input.GetKeyDown(KeyCode.U)) {
			showGUI = !showGUI;
		}
	}

	void FreqSpecificUpdate() {
		// with 10 peak frequency ranges (based on equalizer settings) and x number of bars, use x/10 to determine how many bars
		// represent a given single range and lerp between preceding or succeeding values
		ndx = prevNdx = b = 0;

		for (int i = 0; i < freqRange.Length; i++) {
			// determine bar freq desired for this range
			if (i == 0) start = 0f;
			else start = (freqRange[i]+freqRange[i-1])*0.5f;
			if (i == freqRange.Length-1) end = halfSampleRate;
			else end = (freqRange[i]+freqRange[i+1])*0.5f;

			// frequency step to have coverage across current frequency range
			desiredFreqStep = (end-start)/barsPerRange;

			for (int j = 1; j <= barsPerRange; j++) {
				goalFreq = (float)j*desiredFreqStep;

				// get spectrum index by frequency table
				for (int x = prevNdx; x < freqs.Count; x++) {
					if (goalFreq <= freqs[x]) {
						ndx = x;
					}

					if (x < freqs.Count-1) {
						if (Mathf.Abs (freqs[x]-goalFreq) < Mathf.Abs (freqs[x+1]-goalFreq)) {
							break;
						}
					}
				}

				// above needs a check for if A) prevNdx and ndx are more than one index apart, so that interim samples
				//	of the spectrum can be added to this one for accumulation of broad spectrums, and B) to lerp with neighbors
				// 	or zero if prevNdx and ndx are the same to prevent flat bars in low frequencies
				bars[b] = spectrumL[ndx];
				prevNdx = ndx;

				if (bars[b] > maxSample || (Time.time-sampleTime) > sampleTimeRate) {
					maxSample = bars[b];
					sampleTime = Time.time;
				}

				// scale assignment
				barGOs[b].transform.localScale = new Vector3(1f, baseHeight + (bars[b] * barScaleFactor), 1f);

				// material assignment
				scValue = maxSample > 0f ? (bars[b]/maxSample) : 0f;
				c = mats[b].GetColor("_EmissionColorUI") * Mathf.Clamp01(scValue);
				barGOs[b].GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", c);

				b++;
			}
		}
	}

	void FullSpectrumUpdate() {
		highestSample = 0f;
		highestSampleFreq = 0f;

		float deviance = 0f;
		int nRange = 0;
		int v = 0;
		// assign each bar a value based on current spectrum range
		for (int i = 0; i < bars.Count; i++) {
			// if we can lerp the 'range' based on its progress through the bar range to encompass
			//	more samples the higher the freq, better results
			// i.e. bar[0] = range of 1 sample, bar[n] = range of n samples
			//int nRange = Mathf.CeilToInt(Mathf.Lerp (5, 100, i/bars.Count));

			// accumulate value of samples
			samples = 0f;

			// accumulating nearby frequencies for smoothing
			nRange = Mathf.Min (Mathf.Max ((i/8), 3), 10);
			for (int j = i-nRange; j < i+nRange; j++) {
				if (j+1 >= spectrumL.Length) v = j-spectrumL.Length;
				else if (j < 0) v = spectrumL.Length+j;
				else v = j;
				deviance = Mathf.Abs (j-i)/((float)nRange);
                samples += (Mathf.Lerp (Mathf.Abs(spectrumL[v]), 0f, deviance)+Mathf.Lerp(Mathf.Abs(spectrumR[v]), 0f, deviance))*0.5f;
			}

			// value assignment
            //samples = Mathf.Clamp01(samples+(((outputL[i]*0.05f)+(outputR[i]*0.05f))*0.9f)/*/(float)range*/); // modified freq + sample ratio
            //samples = Mathf.Clamp01(samples*((Mathf.Abs(outputL[i])+Mathf.Abs(outputR[i]))*1f));  // frequency output and sample output
            //samples = Mathf.Clamp01((Mathf.Abs(outputL[i])+Mathf.Abs(outputR[i])));               // sample output only
            //samples = Mathf.Clamp01(samples);                                                     // combined L/R frequency output
            samples = Mathf.Clamp01(samples)*freqCurve.Evaluate((float)i/(float)bars.Count);    // combined L/R channels freq + level balance curve

			bars[i] = Mathf.Lerp (bars[i], samples, (bars[i] < samples ? Time.deltaTime*riseLerpRate : Time.deltaTime*fallLerpRate));

			// updating UI debug
			if (highestSample < bars[i]) {
				highestSample = bars[i];
				highestSampleFreq = (float)i*(freqResolution*(float)range);
			}

		    if (bars[i] > maxSample || (Time.time-sampleTime) > sampleTimeRate) {
				maxSample = bars[i];
				sampleTime = Time.time;
			}

			// scale assignment
            barGOs[i].transform.localScale = new Vector3(1f, baseHeight + (bars[i] * barScripts[i].container.barScale), 1f);

			// material assignment
			if (mats[i].HasProperty("_EmissionColor")) {
				scValue = maxSample > 0f ? (bars[i]/maxSample) : 0f;

                if (greenLightShow)
                {
                    c = Color.Lerp(Color.green, Color.cyan, bars[i]) * Mathf.Clamp01(scValue);
                }
                else
                {
                    c = mats[i].GetColor("_Color") * Mathf.Clamp01(scValue);
                }

				barGOs[i].GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", c);
			}
		}
	}

	void SphereUpdate() {
		for (int i = 0; i < bars.Count; i++) {
			for (int j = lowerSampleRange+(i*range); j < lowerSampleRange+(range*(i+1)); j++) {
				// vocal sample increment
				if ((float)j*freqResolution >= vocalFreqRange.x && (float)j*freqResolution <= vocalFreqRange.y) {
					vocalSamples += spectrumL[j];
                    vocalSamples += spectrumR[j];
				}
			}
		}

		// vocalSample visual
		sphereScale = Mathf.Lerp (sphereScale, vocalSamples*1.2f, (sphereScale < vocalSamples*1.2f ? 1f : 0.5f) * Time.deltaTime*vocalLerpRate);
		vocalSphere.transform.localScale = Vector3.Max((Vector3.one*sphereBaseScale), (Vector3.one*sphereBaseScale)+(Vector3.one*sphereScale)*sphereScaleFactor);
		Material sphereMat = vocalSphere.GetComponent<MeshRenderer>().material;
		if (sphereMat.HasProperty("_EmissionColor")) {

            // this takes the inner/outer colors of the surrounding bars and lerps between the two
			c = Color.Lerp(sphereColors[0], sphereColors[1], Mathf.PingPong(Time.time*0.25f, 1f)) * Mathf.Clamp01(vocalSamples*0.5f);

			//c = /*sphereMat.GetColor("_Color")*/Color.cyan * Mathf.Clamp01 (vocalSamples*/*1f*/0.5f);
			sphereMat.SetColor("_EmissionColor", c);
            sphereMat.SetColor("_Color", Color.Lerp(sphereColors[1], sphereColors[0], Mathf.PingPong(Time.time*0.25f, 1f)) * Mathf.Max(0.4f,Mathf.Clamp01(vocalSamples*0.5f)));
		}
	}

	void OnGUI() {
		if (showGUI) {
			string msg = ("Highest Sample: " + highestSample + "\nSample Freq: " + highestSampleFreq + "kHz");
			GUI.TextArea(new Rect(10, 10, 230, 40), msg);
		}
	}
}
