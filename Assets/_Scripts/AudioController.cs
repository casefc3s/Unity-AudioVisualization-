using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio;
using NAudio.Wave;

public class AudioController : MonoBehaviour {
    struct SongData {
        public string path;
        public string tempDir;
        public int index;
        public AudioClip clip;
    }

	public string path;
	public string tempDir;

    List<SongData> songs = new List<SongData>();
    SongData nowPlaying;
    SongData upNext;

	AudioSource src;
	int length = 0;
	int skipRate = 0;

    int loaded = 0;

    List<AudioClip> clips = new List<AudioClip>();

	// Use this for initialization
	void Start () {
		src = GetComponent<AudioSource>();

        Application.targetFrameRate = 30;
        AudioListener.pause = true;

        SetupInitialSongs();
	}

	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Alpha1)) {
			src.timeSamples = skipRate;
		} else if (Input.GetKeyDown(KeyCode.Alpha2)) {
			src.timeSamples = skipRate*2;
		} else if (Input.GetKeyDown(KeyCode.Alpha3)) {
			src.timeSamples = skipRate*3;
		} else if (Input.GetKeyDown(KeyCode.Alpha4)) {
			src.timeSamples = skipRate*4;
		} else if (Input.GetKeyDown(KeyCode.Alpha5)) {
			src.timeSamples = skipRate*5;
		} else if (Input.GetKeyDown(KeyCode.Alpha6)) {
			src.timeSamples = skipRate*6;
		} else if (Input.GetKeyDown(KeyCode.Alpha7)) {
			src.timeSamples = skipRate*7;
		} else if (Input.GetKeyDown(KeyCode.Alpha8)) {
			src.timeSamples = skipRate*8;
		} else if (Input.GetKeyDown(KeyCode.Alpha9)) {
			src.timeSamples = skipRate*9;
		} else if (Input.GetKeyDown (KeyCode.Alpha0)) {
			src.timeSamples = 0;
		}

		if (Input.GetKeyDown(KeyCode.Space)) {
			AudioListener.pause = !AudioListener.pause;
			if (!src.isPlaying) {
                PlayNextTrack();
			}
		}

		if (Input.GetKeyDown(KeyCode.S)) {
			PlayNextTrack();
		}

		if (Input.GetKeyDown(KeyCode.Escape) && !Application.isEditor)
			Application.Quit();
	}

    // set now playing and up next tracks
    void SetupInitialSongs() {
        string fpath = Application.dataPath + path;
        // go through the directory and search for songs
        if (Directory.Exists(fpath)) {
            string[] files = Directory.GetFiles(fpath, "*.mp3", SearchOption.AllDirectories);
            Debug.Log(files.Length.ToString() + " mp3's found.");

            SetNowPlaying();
            SetUpNext();
        } else {
            Debug.LogError("File path '" + fpath + "' does not exist.");
        }
    }

    SongData GetAvailableTrack() {
        SongData sd = new SongData();

        string fpath = Application.dataPath + path;
        string[] files = Directory.GetFiles(fpath, "*.mp3", SearchOption.AllDirectories);

        if (files.Length == 0) return sd;
        // pick a random song
        int r = Random.Range(0,files.Length);

        // iterate through songs until we play a new one
        bool exists = true;
        int tries = 0;
        while (exists && tries != files.Length) {
            exists = false;
            if (files[r].Contains("._")) {
                exists = true;
            }
            foreach(SongData song in songs) {
                if (song.index == r) {
                    exists = true;
                    break;
                }
            }

            if (exists) {
                r = Random.Range(0,files.Length);
                tries++;
            }
        }

        if (tries == files.Length) {
            Debug.LogError("Clearing playlist, unable to select new track.");
            songs.Clear();
        } else {
            sd.path = files[r];
            sd.tempDir = Application.dataPath + tempDir + Path.GetFileNameWithoutExtension(files[r]) + ".wav";
            sd.index = r;
        }

        return sd;
    }

    void SetNowPlaying() {
        nowPlaying = GetAvailableTrack();
        if (!string.IsNullOrEmpty(nowPlaying.path)) {
            songs.Add(nowPlaying);
        }
    }

    void PlayNextTrack() {
        StopCoroutine(PrepareNextSong());

        if (string.IsNullOrEmpty(upNext.path))
            SetUpNext();

        if (!string.IsNullOrEmpty(upNext.path)) {
            PlayUpNext();
        }
    }

    void SetUpNext() {
        upNext = GetAvailableTrack();
        if (!string.IsNullOrEmpty(upNext.path)) {
            StartCoroutine(LoadSong(upNext, false));
            songs.Add(upNext);
        }
    }

    void PlayUpNext() {
        src.Stop();
        src.timeSamples = 0;
        // remove previous played audio
        if (!string.IsNullOrEmpty(nowPlaying.tempDir)) {
            UnloadWav(nowPlaying);
        }
        nowPlaying = upNext;
        upNext = new SongData();
        PlayOrLoadNowPlaying();
    }

    void PlayOrLoadNowPlaying() {
        StartCoroutine(LoadSong(nowPlaying, true));
        // get next track ready
        StopCoroutine(PrepareNextSong());
        StartCoroutine(PrepareNextSong());
    }

    IEnumerator PrepareNextSong() {
        yield return null;
        SetUpNext();

        while (nowPlaying.clip == null || nowPlaying.clip.loadState != AudioDataLoadState.Loaded)
            yield return new WaitForEndOfFrame();

        yield return new WaitForSeconds(nowPlaying.clip.length);
        while (src.isPlaying) {
            yield return new WaitForEndOfFrame();
        }

        PlayNextTrack();
        yield return null;
    }

    IEnumerator LoadSong(SongData sd, bool playImmediate) {
        if (!File.Exists(sd.tempDir) || sd.clip == null) {
            // create the temp wav file
            Debug.Log("Converting mp3 to wav: " + sd.path);
            if (!Directory.Exists(Application.dataPath + tempDir)) {
                Directory.CreateDirectory(Application.dataPath + tempDir);
            }
            using (Mp3FileReader reader = new Mp3FileReader(sd.path)) {
                WaveFileWriter.CreateWaveFile(sd.tempDir, reader);
            }

            WWW song = new WWW("file://" + sd.tempDir);
            yield return song;

            if (!string.IsNullOrEmpty(song.error)) {
                Debug.LogError("Could not load " + sd.tempDir);
            }

            AudioClip clip = song.GetAudioClip(false,true);
            while  (clip.loadState != AudioDataLoadState.Loaded &&
                clip.loadState != AudioDataLoadState.Failed)
                yield return song;

            sd.clip = clip;
            if (!playImmediate)
                upNext.clip = clip;
        }

        if (sd.clip.loadState == AudioDataLoadState.Failed) {
            Debug.LogError("Failed to load.");
        } else if (playImmediate) {
            FinishedLoading(sd);
        } else {
            Debug.Log("Finished loading next track " + sd.tempDir);
        }
        yield return null;
    }

	/*public void PlayRandomAudio() {
		Debug.Log("Searching for new song...");
		string fpath = Application.dataPath + path;
        src.Stop();
        // remove previous played audio
        if (!string.IsNullOrEmpty(nowPlaying)) {
            UnloadWav(nowPlaying);
            nowPlaying = "";
        }
        src.timeSamples = 0;

		if (Directory.Exists(fpath)) {
			string[] files = Directory.GetFiles(fpath, "*.mp3", SearchOption.AllDirectories);
            Debug.Log(files.Length.ToString() + " mp3's found.");
            if (files.Length == 0) return;

			int r = Random.Range(0,files.Length);
            bool exists = true;
            int tries = 0;
            while (exists && tries != files.Length) {
                exists = false;
                foreach(KeyValuePair<string,int> kvp in songs) {
                    if (kvp.Value == r) {
                        r = Random.Range(0,files.Length);
                        tries++;
                        exists = true;
                        break;
                    }
                }
            }

            if (tries == files.Length) {
                Debug.LogWarning("Clearing playlist, cycling through again.");
                songs.Clear();
            }

			StartCoroutine(AudioGrabRoutine(files[r], r, false));
		} else {
				Debug.LogError("File path '" + fpath + "' does not exist.");
		}
	}

    IEnumerator AudioGrabRoutine(string path, int r, bool nowPlaying) {
		string tPath = Application.dataPath + tempDir;
		string filepath = tPath + Path.GetFileNameWithoutExtension(path) + ".wav";
		if (!Directory.Exists(tPath)) {
				Directory.CreateDirectory(tPath);
		}
		using (Mp3FileReader reader = new Mp3FileReader(path)) {
				WaveFileWriter.CreateWaveFile(filepath, reader);
		}

		WWW song = new WWW("file://" + filepath);
		yield return song;

		if (!string.IsNullOrEmpty(song.error)) {
			Debug.LogError("Could not load " + filepath);
		}

		AudioClip clip = song.GetAudioClip(false,true);
		Debug.Log("Loading " + filepath + "...");
		while  (clip.loadState != AudioDataLoadState.Loaded &&
				clip.loadState != AudioDataLoadState.Failed)
			yield return song;

		if (clip.loadState == AudioDataLoadState.Failed) {
			Debug.LogError("Failed to load.");
		} else {
            FinishedLoading(clip, filepath, r);
		}
		yield return null;
	}*/

    void FinishedLoading(SongData sd) {
		src.clip = sd.clip;
        length = src.clip.samples;
        skipRate = length / 10;
        src.Play();
        Debug.Log("Playing " + sd.tempDir);

        SongTitle st = FindObjectOfType<SongTitle>();
        if (st != null) st.Set(Path.GetFileNameWithoutExtension(sd.tempDir));
	}

    void UnloadWav(SongData sd) {
        if (songs.Contains(sd) && File.Exists(sd.tempDir)) {
            File.Delete(sd.tempDir);
            //songs.Remove(key);
        }
	}

    void OnApplicationQuit() {
        if (songs == null || songs.Count == 0) return;

        foreach (SongData sd in songs) {
            if (File.Exists(sd.tempDir)) {
                File.Delete(sd.tempDir);
            }
        }

        songs.Clear();
    }
}
