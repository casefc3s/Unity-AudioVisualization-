# Unity-AudioVisualization-
Messy clump of hobby code meant to play songs with a simple visualization. Lots random stuff laying about in there from my learning/testing process.
Hopefully I'll be getting things tidy shortly, but if you're feeling brave, take a look and feel free to contribute!


## What to do:

Create a link to your music folder under StreamingAssets/Music, or dump a few songs in there.

Hit Play in the Editor, then press the Spacebar. Arrow keys control the camera motion, "S" skips to a random track. Numbered keys (non-keypad) should skip around the song.

## NOTES:

Right now the color effects are hard-coded in for white bars that glow green while I was messing around. I'll revert that code later so that the material prefabs assigned in the inspector are actually used properly again.

## Future:

What I'd like to do, first and foremost, is get the scene and code cleaned up so it's more easily readable. However, I would like to get the following in place, and feel free to fork/contribute!
* Music browser and playlist - allow user to open a dialogue to select songs to play or add to a playlist.
* Audio streaming - not just .mp3's, but most common audio formats including .ogg and .mp4. Currently the audio must be placed in a resource folder and exported to .wav temporarily.
* Real-time device listening - instead of having to play mp3s, I want the visualization to work off any audio playing on the system.
* Submit your ideas to go here!
