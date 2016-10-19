using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using UnityEditor;

public class AudioController : MonoBehaviour {
    struct SongData {
        public string path;
        public string songDir;
        public int index;
        public AudioClip clip;
    }

	public string path;
	public string tempDir;

    List<SongData> songs = new List<SongData>();
    SongData nowPlaying;
    SongData upNext;

	AudioSource audioSrc;
	int length = 0;
	int skipRate = 0;

    string _fpath;
    string[] files;

    // Use this for initialization
    void Start () {
		audioSrc = GetComponent<AudioSource>();

        Application.targetFrameRate = 30;
        AudioListener.pause = true;

        _fpath = EditorUtility.OpenFolderPanel("Select Music Folder Desired", "", "");

        SetupInitialSongs();
	}

	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Alpha1)) {
			audioSrc.timeSamples = skipRate;
		} else if (Input.GetKeyDown(KeyCode.Alpha2)) {
			audioSrc.timeSamples = skipRate*2;
		} else if (Input.GetKeyDown(KeyCode.Alpha3)) {
			audioSrc.timeSamples = skipRate*3;
		} else if (Input.GetKeyDown(KeyCode.Alpha4)) {
			audioSrc.timeSamples = skipRate*4;
		} else if (Input.GetKeyDown(KeyCode.Alpha5)) {
			audioSrc.timeSamples = skipRate*5;
		} else if (Input.GetKeyDown(KeyCode.Alpha6)) {
			audioSrc.timeSamples = skipRate*6;
		} else if (Input.GetKeyDown(KeyCode.Alpha7)) {
			audioSrc.timeSamples = skipRate*7;
		} else if (Input.GetKeyDown(KeyCode.Alpha8)) {
			audioSrc.timeSamples = skipRate*8;
		} else if (Input.GetKeyDown(KeyCode.Alpha9)) {
			audioSrc.timeSamples = skipRate*9;
		} else if (Input.GetKeyDown (KeyCode.Alpha0)) {
			audioSrc.timeSamples = 0;
		}

		if (Input.GetKeyDown(KeyCode.Space)) {
			AudioListener.pause = !AudioListener.pause;
			if (!audioSrc.isPlaying) {
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
//        string _fpath = Application.dataPath + path;

        // go through the directory and search for songs
        if (Directory.Exists(_fpath)) {
            files = Directory.GetFiles(_fpath, "*.mp3", SearchOption.AllDirectories);
            Debug.Log(files.Length.ToString() + " mp3's found.");

            SetNowPlaying();
            SetUpNext();
        } else {
            Debug.LogError("File path '" + _fpath + "' does not exist.");
        }
    }

    void SetNowPlaying() {
        nowPlaying = GetAvailableTrack();
        if (!string.IsNullOrEmpty(nowPlaying.path)) {
            songs.Add(nowPlaying);
        }
    }

    void SetUpNext() {
        upNext = GetAvailableTrack();
        if (!string.IsNullOrEmpty(upNext.path)) {
            StartCoroutine(LoadSong(upNext, false));
            songs.Add(upNext);
        }
    }

    SongData GetAvailableTrack() {
        SongData sd = new SongData();

//        string fpath = Application.dataPath + path;
//        string[] files = Directory.GetFiles(_fpath, "*.mp3", SearchOption.AllDirectories);

        if (files.Length == 0)
            return sd;

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

        if (tries == files.Length && files.Length > 1) {
            Debug.LogError("Clearing playlist, unable to select new track.");
            songs.Clear();
        } else {
            sd.path = files[r];
//            sd.songDir = _fpath;
            sd.songDir = Application.dataPath + tempDir + Path.GetFileNameWithoutExtension(sd.path) + ".wav";
            sd.index = r;
        }

        return sd;
    }

    IEnumerator LoadSong(SongData sd, bool playImmediate) {
        if (!File.Exists(sd.songDir) || sd.clip == null) {

            // create the temp wav file
            Debug.Log("Converting mp3 to wav: " + sd.path);
            if (!Directory.Exists(Application.dataPath + tempDir)) {
                Directory.CreateDirectory(Application.dataPath + tempDir);
            }
            using (Mp3FileReader reader = new Mp3FileReader(sd.path)) {
                WaveFileWriter.CreateWaveFile(sd.songDir, reader);
            }

            WWW song = new WWW("file://" + sd.songDir);
            yield return song;

            if (!string.IsNullOrEmpty(song.error)) {
                Debug.LogError("Could not load " + sd.songDir);
            }

            AudioClip clip = song.GetAudioClip(false, true);
            while (clip.loadState != AudioDataLoadState.Loaded && clip.loadState != AudioDataLoadState.Failed) {
                yield return song;
            }

            sd.clip = clip;
            if (!playImmediate) {
                upNext.clip = clip;
            }
        }

        if (sd.clip.loadState == AudioDataLoadState.Failed) {
            Debug.LogError("Failed to load.");
        }
        else if (playImmediate) {
            FinishedLoading(sd);
        }
        else {
            Debug.Log("Finished loading next track " + sd.songDir);
        }
        yield return null;
    }

    public void PlayNextTrack() {
        StopCoroutine(PrepareNextSong());

        if (string.IsNullOrEmpty(upNext.path)){
            SetUpNext();
        }
        else {
            PlayUpNext();
        }
    }

    void PlayUpNext() {
        audioSrc.Stop();
        audioSrc.timeSamples = 0;
        // remove previous played audio
        if (!string.IsNullOrEmpty(nowPlaying.songDir)) {
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
        while (audioSrc.isPlaying) {
            yield return new WaitForEndOfFrame();
        }

        PlayNextTrack();
        yield return null;
    }

    void FinishedLoading(SongData sd) {
		audioSrc.clip = sd.clip;
        length = audioSrc.clip.samples;
        skipRate = length / 10;
        audioSrc.Play();
        Debug.Log("Playing " + sd.songDir);

        SongTitle st = FindObjectOfType<SongTitle>();
        if (st != null) st.Set(Path.GetFileNameWithoutExtension(sd.songDir));
	}

    void UnloadWav(SongData sd) {
        if (songs.Contains(sd) && File.Exists(sd.songDir)) {
            File.Delete(sd.songDir);
            string meta = sd.songDir;
            meta = meta.Replace(".mp3", ".meta");

            if (File.Exists(meta))
                File.Delete(meta);
            //songs.Remove(key);
        }
	}

    void OnApplicationQuit() {
        if (songs == null || songs.Count == 0) return;

        foreach (SongData sd in songs) {
            if (File.Exists(sd.songDir)) {
                File.Delete(sd.songDir);
                string meta = sd.songDir;
                meta = meta.Replace(".mp3", ".meta");

                if (File.Exists(meta)) {
                    File.Delete(meta);
                }
            }
        }
        songs.Clear();
    }
}
