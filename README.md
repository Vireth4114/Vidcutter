# Vidcutter

## An automatic editor for your Celeste map clears

> Vidcutter is currently working and tested on Windows 64bits **and** Linux 64bits. ARM isn't supported and we also can't ensure compatibility with MacOS.  
> ⚠️ Vidcutter is NOT a recording software, you need to record your sessions independently with a recording software like OBS for example.

## Features

- Cut your session recordings in clear videos (partial or complete)
- Save your clips with F10 (from last savestate until last event, useful for speedrun showcases!)

## Note for Linux users

Linux doesn't provide natively the birth time (or creation time) of a file, forcing Vidcutter to rely on guess work to determine if a video file is
part of the session recording. In a last resort scenario, Vidcutter will try to find the creation time in the videofile name.  
Please ensure your recording software is outputing a file matching this scenario (this is the OBS default behavior).

## How the mod works

It watches event on your map such a entering a room, leaving a room, collecting a strawberry... With a few exceptions such as permanent events (tokens, doors...) *it is planned tho*.  
Due to how it works, installing Vidcutter **after** a clear won't be useful in any manner as the mod did not log any event for the map clear. You need to have the mod enabled from **start to end**.
