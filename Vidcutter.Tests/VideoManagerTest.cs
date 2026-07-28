using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Vidcutter.Models;
using Celeste.Mod.Vidcutter.Utils;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Vidcutter.Tests;

[TestClass]
[TestSubject(typeof(VideoManager))]
public class VideoManagerTest {
    
    
    [TestMethod]
    public void TestProcessLogs() {
        List<LoggedString> logs = [
            new(DateTime.Parse("18:38:02.670"), "ROOM PASSED", "Bleh", "intro", true),
            new(DateTime.Parse("18:38:02.671"), "CLOSE TO SPAWNPOINT", "Bleh", "intro", true),
            new(DateTime.Parse("18:38:09.312"), "ROOM PASSED", "Bleh", "a", true),
            new(DateTime.Parse("18:38:09.312"), "CLOSE TO SPAWNPOINT", "Bleh", "a", true),
            new(DateTime.Parse("18:38:18.519"), "DEATH", "Bleh", "a", true),
            new(DateTime.Parse("18:38:20.364"), "DEATH", "Bleh", "a", true),
            new(DateTime.Parse("18:38:22.596"), "DEATH", "Bleh", "a", true),
            new(DateTime.Parse("18:38:25.148"), "ROOM PASSED", "Bleh", "b-1", true),
            new(DateTime.Parse("18:38:25.148"), "CLOSE TO SPAWNPOINT", "Bleh", "b-1", true),
            new(DateTime.Parse("18:38:26.913"), "DEATH", "Bleh", "b-1", true),
            new(DateTime.Parse("18:38:30.862"), "ROOM PASSED", "Bleh", "b-2", true),
            new(DateTime.Parse("18:38:30.862"), "CLOSE TO SPAWNPOINT", "Bleh", "b-2", true),
            new(DateTime.Parse("18:38:33.029"), "ROOM PASSED", "Bleh", "b-3", true),
            new(DateTime.Parse("18:38:33.029"), "CLOSE TO SPAWNPOINT", "Bleh", "b-3", true),
            new(DateTime.Parse("18:38:35.895"), "ROOM PASSED", "Bleh", "b-4", true),
            new(DateTime.Parse("18:38:35.895"), "CLOSE TO SPAWNPOINT", "Bleh", "b-4", true),
            new(DateTime.Parse("18:38:39.732"), "BACK TO START OF INTER ROOM", "Bleh", "b-1", true),
            new(DateTime.Parse("18:38:39.732"), "DEATH", "Bleh", "b-1", true),
            new(DateTime.Parse("18:38:42.928"), "ROOM PASSED", "Bleh", "b-2", true),
            new(DateTime.Parse("18:38:42.928"), "CLOSE TO SPAWNPOINT", "Bleh", "b-2", true),
            new(DateTime.Parse("18:38:45.045"), "ROOM PASSED", "Bleh", "b-3", true),
            new(DateTime.Parse("18:38:45.045"), "CLOSE TO SPAWNPOINT", "Bleh", "b-3", true),
            new(DateTime.Parse("18:38:47.640"), "BACK TO START OF INTER ROOM", "Bleh", "b-1", true),
            new(DateTime.Parse("18:38:47.640"), "DEATH", "Bleh", "b-1", true),
            new(DateTime.Parse("18:38:51.695"), "ROOM PASSED", "Bleh", "b-2", true),
            new(DateTime.Parse("18:38:51.695"), "CLOSE TO SPAWNPOINT", "Bleh", "b-2", true),
            new(DateTime.Parse("18:38:52.911"), "BACK TO START OF INTER ROOM", "Bleh", "b-1", true),
            new(DateTime.Parse("18:38:52.911"), "DEATH", "Bleh", "b-1", true),
            new(DateTime.Parse("18:38:55.945"), "ROOM PASSED", "Bleh", "b-2", true),
            new(DateTime.Parse("18:38:55.945"), "CLOSE TO SPAWNPOINT", "Bleh", "b-2", true),
            new(DateTime.Parse("18:38:58.145"), "ROOM PASSED", "Bleh", "b-3", true),
            new(DateTime.Parse("18:38:58.145"), "CLOSE TO SPAWNPOINT", "Bleh", "b-3", true),
            new(DateTime.Parse("18:39:01.078"), "ROOM PASSED", "Bleh", "b-4", true),
            new(DateTime.Parse("18:39:01.078"), "CLOSE TO SPAWNPOINT", "Bleh", "b-4", true),
            new(DateTime.Parse("18:39:03.795"), "ROOM PASSED", "Bleh", "c", true),
            new(DateTime.Parse("18:39:03.795"), "CLOSE TO SPAWNPOINT", "Bleh", "c", true),
            new(DateTime.Parse("18:39:19.322"), "ROOM PASSED", "Forsaken City", "1", true),
            new(DateTime.Parse("18:39:19.795"), "STATE", "Forsaken City", "1", false),
            new(DateTime.Parse("18:39:20.681"), "DEATH", "Forsaken City", "1", true),
            new(DateTime.Parse("18:39:21.200"), "ROOM PASSED", "Forsaken City", "1b", true),
            new(DateTime.Parse("18:39:21.352"), "CLOSE TO SPAWNPOINT", "Forsaken City", "1b", true),
            new(DateTime.Parse("18:39:21.795"), "STATE", "Forsaken City", "2", false),
            new(DateTime.Parse("18:39:23.396"), "ROOM PASSED", "Forsaken City", "2", false),
            new(DateTime.Parse("18:39:23.396"), "CLOSE TO SPAWNPOINT", "Forsaken City", "2", false),
            new(DateTime.Parse("18:39:25.681"), "DEATH", "Forsaken City", "2", true),
            new(DateTime.Parse("18:39:28.613"), "DEATH", "Forsaken City", "2", true),
            new(DateTime.Parse("18:39:33.478"), "ROOM PASSED", "Forsaken City", "3", true),
            new(DateTime.Parse("18:39:33.478"), "CLOSE TO SPAWNPOINT", "Forsaken City", "3", true),
            new(DateTime.Parse("18:39:36.602"), "DEATH", "Forsaken City", "3", true),
            new(DateTime.Parse("18:39:41.496"), "BERRY", "Forsaken City", "3", true),
            new(DateTime.Parse("18:39:46.263"), "DEATH", "Forsaken City", "3", true),
            new(DateTime.Parse("18:39:51.032"), "DEATH", "Forsaken City", "3", true),
            new(DateTime.Parse("18:39:55.195"), "ROOM PASSED", "Forsaken City", "4", true),
            new(DateTime.Parse("18:39:55.195"), "CLOSE TO SPAWNPOINT", "Forsaken City", "4", true),
            new(DateTime.Parse("18:39:58.712"), "ROOM PASSED", "Forsaken City", "3b", true),
            new(DateTime.Parse("18:39:58.712"), "CLOSE TO SPAWNPOINT", "Forsaken City", "3b", true),
            new(DateTime.Parse("18:40:08.752"), "ROOM PASSED", "Prologue", "0", true),
            new(DateTime.Parse("18:40:08.752"), "CLOSE TO SPAWNPOINT", "Prologue", "0", true),
            new(DateTime.Parse("18:40:15.312"), "ROOM PASSED", "Prologue", "1", true),
            new(DateTime.Parse("18:40:15.312"), "CLOSE TO SPAWNPOINT", "Prologue", "1", true)
        ];

        List<GameplayClip> clips = VideoManager.ProcessLogs(logs);

        List<GameplayClip> expectedClips =
        [
            new(
                new LoggedString(DateTime.Parse("18:38:02.670"), "ROOM PASSED", "Bleh", "intro", true),
                new LoggedString(DateTime.Parse("18:38:09.312"), "CLOSE TO SPAWNPOINT", "Bleh", "a", true)
            ),
            new(
                new LoggedString(DateTime.Parse("18:38:22.596"), "DEATH", "Bleh", "a", true),
                new LoggedString(DateTime.Parse("18:38:25.148"), "CLOSE TO SPAWNPOINT", "Bleh", "b-1", true)
            ),
            new(
                new LoggedString(DateTime.Parse("18:38:52.911"), "DEATH", "Bleh", "b-1", true),
                new LoggedString(DateTime.Parse("18:39:03.795"), "CLOSE TO SPAWNPOINT", "Bleh", "c", true)
            ),
            new(
                new LoggedString(DateTime.Parse("18:39:20.681"), "DEATH", "Forsaken City", "1", true),
                new LoggedString(DateTime.Parse("18:39:21.352"), "CLOSE TO SPAWNPOINT", "Forsaken City", "1b", true)
            ),
            new(
                new LoggedString(DateTime.Parse("18:39:28.613"), "DEATH", "Forsaken City", "2", true),
                new LoggedString(DateTime.Parse("18:39:33.478"), "CLOSE TO SPAWNPOINT", "Forsaken City", "3", true)
            ),
            new(
                new LoggedString(DateTime.Parse("18:39:36.602"), "DEATH", "Forsaken City", "3", true),
                new LoggedString(DateTime.Parse("18:39:46.263"), "DEATH", "Forsaken City", "3", true)
            ),
            new(
                new LoggedString(DateTime.Parse("18:39:51.032"), "DEATH", "Forsaken City", "3", true),
                new LoggedString(DateTime.Parse("18:39:58.712"), "CLOSE TO SPAWNPOINT", "Forsaken City", "3b", true)
            ),
            new(
                new LoggedString(DateTime.Parse("18:40:08.752"), "ROOM PASSED", "Prologue", "0", true),
                new LoggedString(DateTime.Parse("18:40:15.312"), "CLOSE TO SPAWNPOINT", "Prologue", "1", true)
            )
        ];
        
        Assert.HasCount(expectedClips.Count, clips);
        foreach (var (clip, expectedClip) in expectedClips.Zip(clips)) {
            Assert.AreEqual(expectedClip, clip);
        }
    }
}