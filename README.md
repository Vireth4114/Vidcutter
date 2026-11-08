# Vidcutter

## An automatic editor for your Celeste map clears

> Vidcutter is currently working and tested on Windows 64bits **and** Linux 64bits. ARM isn't supported and we also can't ensure compatibility with MacOS.  
> ⚠️ Vidcutter is NOT a recording software, you need to record your sessions independently with a recording software like OBS for example.

## Features

- Cut your session recordings in clear videos (partial or complete)
- Save your clips with F10 (from last savestate until last event, useful for speedrun showcases!), the keybind can be changed

## Note for Linux users

Linux doesn't provide natively the birth time (or creation time) of a file, forcing Vidcutter to rely on guess work to determine if a video file is
part of the session recording. In a last resort scenario, Vidcutter will try to find the creation time in the videofile name.  
Please ensure your recording software is outputing a file matching this scenario (this is the OBS default behavior).

## How the mod works

It watches event on your map such a entering a room, leaving a room, collecting a strawberry... With a few exceptions such as permanent events (tokens, doors...) *it is planned tho*.  
Due to how it works, installing Vidcutter **after** a clear won't be useful in any manner as the mod did not log any event for the map clear. You need to have the mod enabled from **start to end**.

## Planned features

- Log "permanent event" (happens one time and are always unlocked even after death), such as doors, tokens, etc.
- Change audio and video codecs for special use/niche case
- Allow map behavior with a config file (like CCT with the room orders for example) for maps that requires specific backtracking for example
- Add death counter & room name in video to make the video clear a bit more "interesting"
