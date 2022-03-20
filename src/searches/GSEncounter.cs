using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Text;

public class EncounterState {

    public string Log;
    public GscTile Tile;
    public int EdgeSet;
    public int WastedFrames;
    public bool CanA;
}

public class SavefileData {
    public String savefileStr;
    public String outputSavefileStr;
    public byte day;
    public byte hour;
    public byte minute;
    public byte audio;
    public byte frameType;
    public byte menuAccount;
    public byte igt;

    public int stepCount = C.Any;
    public int poisonStepCount = C.Any;
    public int playerState = C.Any;
    public int facingDir = C.Any;
    public int repelEffect = C.Any;
    public int partyCount = C.Any;
}

public class EncounterTarget{
    public String targetSpecies;

    public int minAttack = 0;
    public int minDefense = 0;
    public int minSpecial = 0;
    public int minSpeed = 0;

    public int maxAttack = 15;
    public int maxDefense = 15;
    public int maxSpecial = 15;
    public int maxSpeed = 15;

    public int minLevel = 1;
    public int maxLevel = 100;

    public int targetMap = C.Any;
    public int targetX = C.Any;
    public int targetY = C.Any;
}

public enum ManipOutcome {
    Encounter, Bonk, Textbox, Nothing, Anything
}

public class ClusterDicEntry {
    public String path;
    public int mmBack;
    public int fsBack;
    public int delay;

    public bool IsAntecedentOf(ClusterDicEntry other){
        return mmBack == other.mmBack && fsBack == other.fsBack && delay == other.delay - 1;
    }
}

public class ClusterDic {
    // (mmBack, fsBack) -> path -> entry
    private Dictionary<(int, int), Dictionary<String, HashSet<ClusterDicEntry>>> clusterDic = new Dictionary<(int, int), Dictionary<String, HashSet<ClusterDicEntry>>>();

    public void Add(ClusterDicEntry entry){
        (int, int) mm_fs = (entry.mmBack, entry.fsBack);
        String path = entry.path;
        
        if(!clusterDic.ContainsKey(mm_fs)){
            clusterDic[mm_fs] = new Dictionary<String, HashSet<ClusterDicEntry>>();
        }

        Dictionary<String, HashSet<ClusterDicEntry>> mm_fs_dic = clusterDic[mm_fs];
        if(!mm_fs_dic.ContainsKey(path)){
            mm_fs_dic[path] = new HashSet<ClusterDicEntry>();
        }

        // Gather subpaths entries with the current path entry
        for(int len = path.Length - 1; len >= 1; len--){
            String subPath = path.Substring(0, len);
            if(!mm_fs_dic.ContainsKey(subPath)){
                continue;
            }
            foreach(ClusterDicEntry old_entry in mm_fs_dic[subPath]){
                if(old_entry.IsAntecedentOf(entry)){
                    mm_fs_dic[path].Add(old_entry);
                }
            }
        }

        // Add current entry at the end
        mm_fs_dic[path].Add(entry);
    }

    public string GetClusterString(){
        StringBuilder sb = new StringBuilder();
        sb.Append("## Cluster list ##");
        sb.AppendLine();
        foreach(KeyValuePair<(int, int), Dictionary<String, HashSet<ClusterDicEntry>>> mm_fs_pair in clusterDic){
            (int, int) mm_fs = mm_fs_pair.Key;
            bool encountered_mm_fs = false;

            foreach(KeyValuePair<String, HashSet<ClusterDicEntry>> path_pair in mm_fs_pair.Value){
                String path = path_pair.Key;
                bool encountered_path = false;

                HashSet<ClusterDicEntry> entries = path_pair.Value;
                if(entries.Count < 2){
                    continue;
                }

                foreach(ClusterDicEntry entry in entries){
                    int delay = entry.delay;
                    if(!encountered_mm_fs){
                        sb.Append("(mmBack, fsBack) = " + mm_fs);
                        sb.AppendLine();
                        encountered_mm_fs = true;
                    }
                    if(!encountered_path) {
                        sb.Append(path + " : ");
                        encountered_path = true;
                    }
                    sb.Append(delay + ", ");
                }
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}

// Code heavily plagiarized from: https://github.com/entrpntr/gb-rta-bruteforce/blob/master/src/dabomstew/rta/entei/GSToto.java

public static class Encounter {

    static StreamWriter Writer;

    public static void CheckDelays(SavefileData s, int rtc, int mmBack, int fsBack, int minDelay, int maxDelay, string logPath, int wastedFrames){
        StreamWriter writer = new StreamWriter(s.savefileStr + "_CheckDelays_" + DateTime.Now.Ticks + ".txt");
        for(int delay = minDelay; delay <= maxDelay; delay++){
            IGT60(s, rtc, mmBack, fsBack, delay, logPath, wastedFrames, writer,
                        expectedOutcome: ManipOutcome.Anything, minSuccesses: 0);
        }
    }

    public static void IGT60(SavefileData s, int rtc, int mmBack, int fsBack, int delay, string logPath, int wastedFrames, StreamWriter writer,
                        ManipOutcome expectedOutcome = ManipOutcome.Anything, EncounterTarget encounterTarget = null, 
                        bool recordIgt = false, bool displayIgt = false, int minSuccesses = 0,
                        ClusterDic clusterDic = null){
        Console.WriteLine("IGT60 for " + (mmBack, fsBack, delay) + " " + logPath + " ...");

        s.outputSavefileStr = s.savefileStr + "_igt";

        int successes = 0;
        String[] resultsArr = new String[60];

        Gold igtGb;
        for(byte _igt = 0; _igt < 60 ; _igt++){
            s.igt = _igt;

            MakeSave(s);
            igtGb = new Gold("roms/"+ s.outputSavefileStr + "_temp.sav");

            if(_igt == 0) igtGb.Record("nomnom");

            ManipOutcome outcome = ExecuteIGTCore(igtGb, s, rtc, mmBack, fsBack,delay, logPath, recordIgt: recordIgt, displayIgt: displayIgt);

            // Text output
            switch(outcome){
            case ManipOutcome.Encounter:
                resultsArr[_igt] = "[f" + _igt + "] " + igtGb.EnemyMon.Species.Name + " L" + igtGb.EnemyMon.Level + " DVs: " + igtGb.EnemyMon.DVs.ToString() + " Map,Tile: " + Maps.GetName(igtGb.Map.Id) + "," + igtGb.Tile.X + "/" + igtGb.Tile.Y;
                break;
            case ManipOutcome.Bonk:
                resultsArr[_igt] = "[f" + _igt + "] Bonk";
                break;
            case ManipOutcome.Textbox:
                resultsArr[_igt] = "[f" + _igt + "] Spinner hit / trainer";
                break;
            case ManipOutcome.Nothing:
                resultsArr[_igt] = "[f" + _igt + "] No encounter";
                break;
            default:
                break;
            }

            // Successes counter
            bool isSuccess = false;
            if(expectedOutcome == ManipOutcome.Anything){
                isSuccess = true;
            } else {
                if(expectedOutcome == outcome){
                    if(outcome == ManipOutcome.Encounter && CheckEncounter(igtGb, encounterTarget)){
                        isSuccess = true;
                    } else {
                        isSuccess = true;
                    }
                }
            }
            if(isSuccess) successes++;
            
            igtGb.Dispose();

            int remainingIgts = 59 - _igt;
            int remainingSuccessesToGet = minSuccesses - successes;

            if(remainingIgts < remainingSuccessesToGet){
                Console.WriteLine("Breaking at " + successes + " successes after " + (_igt + 1) + " igt examinations.");
                Console.WriteLine("---------------");
                break;
            }
        }

        if(successes >= minSuccesses){
            if(clusterDic != null){
                ClusterDicEntry entry = new ClusterDicEntry{
                    path = logPath,
                    mmBack = mmBack,
                    fsBack = fsBack,
                    delay = delay,
                };

                clusterDic.Add(entry);
            }

            String line = successes + "/60" + " Map:" + (encounterTarget == null ? Maps.GetName(0) : Maps.GetName(encounterTarget.targetMap)) + " Cost:" + wastedFrames + " Path:" + logPath + "\r\n";
            line += "H:M=" + s.hour + ":" + s.minute + " RTC:" + ( (rtc/3600)+":"+(rtc%3600)/60+":"+(rtc%60) ) + " Audio:" + (s.audio == Audios.Mono ? "Mono" : "Stereo") + " mmBack:" + mmBack + " fsback:" + fsBack + " delay:" + delay + "\r\n";
            for(int i = 0; i < 60; i++){
                line += resultsArr[i] + "\r\n";
            }
            
            writer.WriteLine(line);
            writer.Flush();
            Console.WriteLine(successes + "/60" + " Cost: " + wastedFrames);
            Console.WriteLine("---------------");
        }
    }

    public static ManipOutcome ExecuteIGTCore(Gold igtGb, SavefileData s, int rtc, int mmBack, int fsBack, int delay, string logPath, 
                                bool recordIgt = false, bool displayIgt = false){
        
        if(s.igt == 0 && recordIgt)
            igtGb.Record(s.savefileStr);
        if(displayIgt)
            igtGb.Show();
        igtGb.SetTimeSec(rtc);
        igtGb.Hold(Joypad.Left, 0x100);
        
        GscStrat.GfSkip.Execute(igtGb);
        GscStrat.TitleSkip.Execute(igtGb);
        for(int _mmBack = 0; _mmBack < mmBack; _mmBack++) {
            GscStrat.MmBack.Execute(igtGb);
            GscStrat.TitleSkip.Execute(igtGb);
        }

        GscStrat.Continue.Execute(igtGb);

        for(int _fsback = 0; _fsback < fsBack; _fsback++){
            GscStrat.FsBack.Execute(igtGb);
            GscStrat.Continue.Execute(igtGb);
        }

        igtGb.Hold(Joypad.Left, "GetJoypad");
        igtGb.AdvanceFrame(Joypad.Left);
        for(int _delay = 0; _delay < delay; _delay++) {
            igtGb.AdvanceFrame(Joypad.Left);
        }
        igtGb.Hold(Joypad.A, "OWPlayerInput");

        int ret = igtGb.Execute(LogToPath(logPath));

        if(ret == igtGb.SYM["OWPlayerInput"])
            return ManipOutcome.Nothing;
        else if(ret == igtGb.SYM["PrintLetterDelay.checkjoypad"])
            return ManipOutcome.Textbox;
        else if(ret == igtGb.SYM["DoPlayerMovement.BumpSound"])
            return ManipOutcome.Bonk;
        else {
            igtGb.RunUntil(igtGb.SYM["CalcMonStats"]);
            return ManipOutcome.Encounter;
        }
    }
                    
    public static (int, int) MakeSave(SavefileData s) {
        byte[] baseSave = File.ReadAllBytes("basesaves/" + s.savefileStr + ".sav");

        // addr - 0xB198 = sav addr

        baseSave[Save.IgtSecond] = (byte) 58;
        baseSave[Save.IgtFrame] = (byte) s.igt;

        if(s.stepCount == C.Any)
            s.stepCount = (int) (baseSave[Save.StepCount]);
        if(s.poisonStepCount == C.Any)
            s.poisonStepCount = (int) (baseSave[Save.PoisonStepCount]);

        baseSave[Save.StepCount] = (byte) s.stepCount;
        baseSave[Save.PoisonStepCount] = (byte) s.poisonStepCount;

        baseSave[Save.Audio] = s.audio;
        baseSave[Save.FrameType] = s.frameType;
        baseSave[Save.MenuAccount] = s.menuAccount;

        baseSave[Save.StartDay] = s.day;
        baseSave[Save.StartHour] = s.hour;
        baseSave[Save.StartMinute] = s.minute;
        baseSave[Save.StartSecond] = 0;

        if(s.playerState != C.Any)
            baseSave[Save.PlayerState] = (byte) s.playerState; // 0 = walking, 1 = biking

        if(s.facingDir != C.Any)
            baseSave[Save.FacingDir] = (byte) s.facingDir;

        if(s.repelEffect != C.Any)
            baseSave[Save.RepelEffect] = (byte) s.repelEffect;  // repel steps still active

        if(s.partyCount != C.Any){
            baseSave[Save.PartyCount] = (byte) s.partyCount;     // party count
            
            for(int off = 1 ; off <= s.partyCount ; off++){
                int addr = Save.PartyCount + off;
                if(baseSave[addr] == Species.Terminator && off <= s.partyCount)
                    baseSave[addr] = (byte) Species.Poliwag; // poliwag filler
            }
            baseSave[Save.PartyCount + s.partyCount + 1] = (byte) Species.Terminator; // terminator
        }

        //baseSave[0x28A7] = 0xFD; // Forcing Totodile stats
        //baseSave[0x28A8] = 0xFF;

        int checksum = 0;
        for(int i = Save.ChecksumStart; i <= Save.ChecksumEnd; i++) { // wPlayerData -> wGameDataEnd
            checksum += (baseSave[i] & 0xff);
        }
        baseSave[Save.ChecksumBot] = (byte) ((checksum) & 0xff);
        baseSave[Save.ChecksumTop] = (byte) ((checksum >> 8) & 0xff);

        File.WriteAllBytes("roms/" + s.outputSavefileStr + "_temp.sav", baseSave);

        int x = baseSave[Save.XCoord], y = baseSave[Save.YCoord];
        return (x, y);
    }

    public static Boolean CheckEncounter(Gsc gb, EncounterTarget t){
        int mapID = gb.Map.Id;
        string name = gb.EnemyMon.Species.Name;
        int attack = gb.EnemyMon.DVs.Attack;
        int defense = gb.EnemyMon.DVs.Defense;
        int speed = gb.EnemyMon.DVs.Speed;
        int special = gb.EnemyMon.DVs.Special;
        int level = gb.EnemyMon.Level;

        return t.targetMap != C.Any && mapID == t.targetMap
            // TODO && t.targetX != C.Any && ... = t.targetX
            // TODO && t.targetY != C.Any && ... = t.targetY
            && name == t.targetSpecies.ToUpper()
            && t.minAttack <= attack && attack <= t.maxAttack
            && t.minDefense <= defense && defense <= t.maxDefense
            && t.minSpeed <= speed && speed <= t.maxSpeed
            && t.minSpecial <= special && special <= t.maxSpecial
            && t.minLevel <= level && level <= t.maxLevel;
    }

    // Creates a word from two bytes
    public static int Word(int top, int bot){
        return (top & 0xF) << 4 | (bot & 0xF);
    }

    public static bool InRangeLeniant(int top, int bot, int minTop, int maxTop, int minBot, int maxBot){
        if(minTop <= top && top <= maxTop && minBot <= bot && bot <= maxBot){
            return true;
        }
        
        int word = Word(top, bot);
        int word_m1 = (word - 1) & 0xFF;
        top = (word_m1 >> 4) & 0xF;
        bot = word_m1 & 0xF;
        if(minTop <= top && top <= maxTop && minBot <= bot && bot <= maxBot)
            return true;

        int word_p1 = (word + 1) & 0xFF;
        top = (word_p1 >> 4) & 0xF;
        bot = word_p1 & 0xF;
        if(minTop <= top && top <= maxTop && minBot <= bot && bot <= maxBot)
            return true;

        return false;
    }

    // Checks encounter allowing for words atkdef and spdspc to have a +1/-1 variance.
    public static Boolean CheckEncounterLeniant(Gsc gb, int targetMap, String targetSpecies, int minLevel, 
                                        int minAttack, int minDefense, int minSpecial, int minSpeed,
                                        int maxAttack, int maxDefense, int maxSpecial, int maxSpeed){    
        return targetMap != -1 && gb.Map.Id == targetMap
            && gb.EnemyMon.Species.Name == targetSpecies.ToUpper()
            && gb.EnemyMon.Level >= minLevel
            && InRangeLeniant(gb.EnemyMon.DVs.Attack, gb.EnemyMon.DVs.Defense, minAttack, maxAttack, minDefense, maxDefense)
            && InRangeLeniant(gb.EnemyMon.DVs.Speed, gb.EnemyMon.DVs.Special, minSpeed, maxSpeed, minSpecial, maxSpecial);
    }

    public static String LogToPath(String log){
        return log.Replace("L", "L ").Replace("R", "R ").Replace("U", "U ").Replace("D", "D ").Replace("S_B", "S_B ");
    }

    // StartB always has a fixed cost of 91
    public static void AddEdge(GscMap _map, int _x, int _y, int _edgeSet, Action _action, int _cost, int _nextEdgeset = -1){
        int _next_x = -1, _next_y = -1;
        if(_nextEdgeset == -1){
            _nextEdgeset = _edgeSet;
        }
        switch(_action){
            case Action.Left:   _next_x = _x - 1; _next_y = _y    ; break;
            case Action.Right:  _next_x = _x + 1; _next_y = _y    ; break;
            case Action.Up:     _next_x = _x    ; _next_y = _y - 1; break;
            case Action.Down:   _next_x = _x    ; _next_y = _y + 1; break;
            case Action.StartB: _next_x = _x    ; _next_y = _y    ; _cost = 91; break;
            case Action.Select: _next_x = _x    ; _next_y = _y    ; break;
            default: Console.WriteLine("Unexpected custom AddEdge action: " + _action); break;
        }
        _map[_x, _y].AddEdge(_edgeSet, new Edge<GscMap, GscTile> { Action = _action, Cost = _cost, NextEdgeset = _nextEdgeset, NextTile = _map[_next_x, _next_y] });
    }

    public static void AddEdges(GscMap _map, int _x, int _y, int _edgeSet, int _cost, params Action[] _actions){
        foreach(Action _action in _actions){
            AddEdge(_map, _x, _y, _edgeSet, _action, _cost : _cost, _nextEdgeset: _edgeSet);
        }
    }

    public static void Geodude_InitUnionCave1F(GscMap uc1F, GscMap ucB1F){
        int x, y, es, c;
        // Union Cave 1F edges
        es = 0; c = 0;
        x = 13; y =  8; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x = 13; y =  9; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Right);
        x = 14; y =  9; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Right);
        x = 15; y =  9; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Right);
        x = 16; y =  9; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x = 16; y = 10; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x = 16; y = 11; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x = 16; y = 12; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x = 16; y = 13; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x = 16; y = 14; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left);
        x = 15; y = 14; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left);
        x = 14; y = 14; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left, Action.Down);
        x = 13; y = 14; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);

        x = 14; y = 15; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left, Action.Down);
        x = 13; y = 15; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left, Action.Down);
        x = 12; y = 15; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        
        x = 14; y = 16; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left);
        x = 13; y = 16; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left);
        x = 12; y = 16; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down, Action.Left);
        x = 11; y = 16; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down, Action.Left);
        x = 10; y = 16; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down, Action.Left);
        x =  9; y = 16; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down, Action.Left);
        x =  8; y = 16; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        
        x = 11; y = 17; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left);
        x = 10; y = 17; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left);
        x =  9; y = 17; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left);
        x =  8; y = 17; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x =  8; y = 18; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x =  8; y = 19; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x = 12; y = 17; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left);
        x =  8; y = 20; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x =  8; y = 21; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x =  8; y = 22; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left, Action.Down);
        x =  8; y = 23; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left);
        
        x =  7; y = 22; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x =  7; y = 23; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x =  7; y = 24; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left, Action.Down);
        x =  7; y = 25; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left, Action.Down);
        x =  7; y = 26; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left);
        
        x =  6; y = 24; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x =  6; y = 25; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);

        x =  6; y = 26; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left);
        x =  5; y = 26; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left);
        x =  4; y = 26; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Up);
        x =  4; y = 25; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Up);
        x =  4; y = 24; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Left);

        x =  3; y = 24; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Up);
        x =  3; y = 23; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Up);
        x =  3; y = 22; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Up);
        x =  3; y = 21; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Up);

        x =  3; y = 20; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Up, Action.Right);
        x =  3; y = 19; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Right);
        x =  4; y = 20; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Up, Action.Right);

        // Warp
        uc1F[4, 19].AddEdge(0, new Edge<GscMap, GscTile> { Action = Action.Right, Cost = 0, NextEdgeset = 1, NextTile = ucB1F[7, 19] });
        uc1F[5, 20].AddEdge(0, new Edge<GscMap, GscTile> { Action = Action.Up, Cost = 0, NextEdgeset = 1, NextTile = ucB1F[7, 19] });
    }

    public static void Geodude_InitUnionCaveB1F(GscMap ucB1F){
        int x, y, es, c;
        // Union Cave B1F edges
        es = 1; c = 0;
        x = 7; y = 19; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Right, Action.Down, Action.Up, Action.Left);

        es = 1; c = 17;
        x = 5; y = 18; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Right, Action.Down);
        x = 6; y = 18; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Right, Action.Down, Action.Left);
        x = 7; y = 18; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Right, Action.Left);
        x = 8; y = 18; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Down, Action.Left);
        x = 5; y = 19; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Right, Action.Down, Action.Up);
        x = 6; y = 19; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Down, Action.Left, Action.Up);
        x = 8; y = 19; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Down, Action.Up);

        x = 5; y = 20; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Right, Action.Up);
        x = 6; y = 20; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Right, Action.Left, Action.Up);
        x = 7; y = 20; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Right, Action.Down, Action.Left);
        x = 8; y = 20; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Left, Action.Up);

        x = 7; y = 21; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Down, Action.Up);

        x = 7; y = 22; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Right, Action.Down, Action.Left, Action.Up);
        x = 6; y = 22; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Right);
        x = 8; y = 22; AddEdges(ucB1F, x, y, es, c, Action.Left);

        x = 7; y = 23; AddEdges(ucB1F, x, y, es, c, Action.StartB, Action.Up);
    }

    public static void Spearow_InitUnionCave1F(GscMap uc1F, GscMap r33){
        int x, y, es, c;
        
        es = 0; c = 0;
        x = 15; y = 27; AddEdges(uc1F, x, y, es, c, Action.Right);
        x = 16; y = 27; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Right);
        x = 17; y = 27; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x = 17; y = 28; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x = 17; y = 29; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        x = 17; y = 30; AddEdges(uc1F, x, y, es, c, Action.StartB, Action.Down);
        
        // Warp
        uc1F[17, 31].AddEdge(0, new Edge<GscMap, GscTile> { Action = Action.Down, Cost = 0, NextEdgeset = 1, NextTile = r33[11, 9] });
    }

    public static void Spearow_InitR33(GscMap r33){
        Action u = Action.Up, d = Action.Down, r = Action.Right, l = Action.Left, sb = Action.StartB;
        int x, y, es;
        es = 1;

        x = 11; y =  9; AddEdges(r33, x, y, es, 0, d); // Union Cave entrance tile
        
        x =  9; y = 10; AddEdges(r33, x, y, es,  0, sb, d);
        x = 10; y = 10; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, sb, l);
        x = 11; y = 10; AddEdges(r33, x, y, es,  0, sb, l); AddEdges(r33, x, y, es, 17, sb, r);// out of Union Cave tile
        x = 12; y = 10; AddEdges(r33, x, y, es,  0, sb, d);
        //x=13

        x =  9; y = 11; AddEdges(r33, x, y, es,  0, sb, d);
        x = 10; y = 11; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, sb, l);
        x = 12; y = 11; AddEdges(r33, x, y, es,  0, sb, d);
        //x=13

        r33[ 9, 12].AddEdge(es, new Edge<GscMap, GscTile> { Action = Action.Down, Cost =  0, NextEdgeset = es, NextTile = r33[ 9, 14] });
        r33[10, 12].AddEdge(es, new Edge<GscMap, GscTile> { Action = Action.Down, Cost =  0, NextEdgeset = es, NextTile = r33[10, 14] });
        r33[11, 12].AddEdge(es, new Edge<GscMap, GscTile> { Action = Action.Down, Cost =  0, NextEdgeset = es, NextTile = r33[11, 14] });
        r33[12, 12].AddEdge(es, new Edge<GscMap, GscTile> { Action = Action.Down, Cost =  0, NextEdgeset = es, NextTile = r33[12, 14] });
        x =  9; y = 12; AddEdges(r33, x, y, es, 17, sb, r);
        x = 10; y = 12; AddEdges(r33, x, y, es, 17, sb, l, r);
        x = 11; y = 12; AddEdges(r33, x, y, es, 17, sb, l, r);
        x = 12; y = 12; AddEdges(r33, x, y, es,  0, sb, l);
        //x=13

        // x = 13; y = 13; AddEdges(r33, x, y, es, 17, Action.StartB, Action.Down, Action.Up);

        x =  6; y = 14; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, r);
        x =  7; y = 14; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, l, r);
        x =  8; y = 14; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, l, r);
        x =  9; y = 14; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, l, r);
        x = 10; y = 14; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, l, r);
        x = 11; y = 14; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, l, r);
        x = 12; y = 14; AddEdges(r33, x, y, es,  0, sb, d, l);
        //x = 13; y = 14; AddEdges(r33, x, y, es, 17, Action.StartB, Action.Down, Action.Left, Action.Right, Action.Up);
        //x = 14; y = 14; AddEdges(r33, x, y, es, 17, Action.StartB, Action.Down, Action.Left, Action.Right);
        //x = 15; y = 14; AddEdges(r33, x, y, es, 17, Action.StartB, Action.Down, Action.Left);

        x =  6; y = 15; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, r);
        x =  7; y = 15; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, l, r);
        x =  8; y = 15; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, l, r); 
        x =  9; y = 15; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, l, r);
        x = 10; y = 15; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, l, r);
        x = 11; y = 15; AddEdges(r33, x, y, es,  0, sb, d); AddEdges(r33, x, y, es, 17, l, r);
        x = 12; y = 15; AddEdges(r33, x, y, es,  0, sb, d, l);
        //x = 13; y = 15; AddEdges(r33, x, y, es, 17, Action.StartB, Action.Down, Action.Right, Action.Left);
        //x = 14; y = 15; AddEdges(r33, x, y, es, 17, Action.StartB, Action.Left, Action.Right);
        //x = 15; y = 15; AddEdges(r33, x, y, es, 17, Action.StartB, Action.Down, Action.Left);

        x =  6; y = 16; AddEdges(r33, x, y, es,  0, sb, r); AddEdges(r33, x, y, es, 17, d); AddEdges(r33, x, y, es, 34, u);
        x =  7; y = 16; AddEdges(r33, x, y, es,  0, sb, r); AddEdges(r33, x, y, es, 17, d); AddEdges(r33, x, y, es, 34, u, l);
        x =  8; y = 16; AddEdges(r33, x, y, es,  0, sb, r); AddEdges(r33, x, y, es, 17, d); AddEdges(r33, x, y, es, 34, u, l);
        x =  9; y = 16; AddEdges(r33, x, y, es,  0, sb, r); AddEdges(r33, x, y, es, 17, d); AddEdges(r33, x, y, es, 34, u, l);
        x = 10; y = 16; AddEdges(r33, x, y, es,  0, sb, r); AddEdges(r33, x, y, es, 17, d); AddEdges(r33, x, y, es, 34, u, l);
        x = 11; y = 16; AddEdges(r33, x, y, es,  0, sb); AddEdges(r33, x, y, es, 17, d, l); AddEdges(r33, x, y, es, 34, u, r);
        x = 12; y = 16; AddEdges(r33, x, y, es, 17, sb, l, d); AddEdges(r33, x, y, es, 34, u);
        //x = 13; y = 16; AddEdges(r33, x, y, es, 17, Action.StartB, Action.Left, Action.Down, Action.Up);
        //x = 15; y = 16; AddEdges(r33, x, y, es, 17, Action.StartB, Action.Down, Action.Up);

        x =  6; y = 17; AddEdges(r33, x, y, es, 17, sb, u, r);
        x =  7; y = 17; AddEdges(r33, x, y, es, 17, sb, u, l, r);
        x =  8; y = 17; AddEdges(r33, x, y, es, 17, sb, u, l, r); 
        x =  9; y = 17; AddEdges(r33, x, y, es, 17, sb, u, l, r);
        x = 10; y = 17; AddEdges(r33, x, y, es, 17, sb, u, l, r);
        x = 11; y = 17; AddEdges(r33, x, y, es, 17, sb, u, l); AddEdges(r33, x, y, es, 34, r);
        x = 12; y = 17; AddEdges(r33, x, y, es, 17, sb, u, l);
        //x = 13; y = 17; AddEdges(r33, x, y, es, 17, sb, u, l, r);
        //x = 14; y = 17; AddEdges(r33, x, y, es, 17, Action.StartB, Action.Left, Action.Right);
        //x = 15; y = 17; AddEdges(r33, x, y, es, 17, Action.StartB, Action.Left, Action.Up);
    }

    public static void Raticate_InitR38(GscMap uc1F){
        int x, y, es, c;
        int sc = 72; // select cost
        
        es = 0; c = 0;

        for(x = 26; x <= 32; x++){
            for(y = 5; y <= 7 ; y++){
                AddEdges(uc1F, x, y, es, c, Action.StartB);
                AddEdges(uc1F, x, y, es, sc, Action.Select);
                if(x > 26){
                    AddEdges(uc1F, x, y, es, 17, Action.Left);
                }
                if(x < 32){
                    AddEdges(uc1F, x, y, es, 17, Action.Right);
                }
                if(y > 5){
                    AddEdges(uc1F, x, y, es, 17, Action.Up);
                }
                if(y < 7){
                    AddEdges(uc1F, x, y, es, 17, Action.Down);
                }
            }
        }
    }

    public static void Suicune_InitEcruteak(GscMap ecr, GscMap r37){
        int x, y, es, c;
        es = 0; c = 0;
        y = 29; 
        for(x = 14; x <= 16; x++){
            AddEdges(ecr, x, y, es, c, Action.Right);
        }
        x = 17; AddEdges(ecr, x, y, es, 17, Action.Right);

        for(x = 17; x <= 18; x++){
            for(y = 29; y <= 34; y++){
                AddEdges(ecr, x, y, es, c, Action.Down);
            }
        }

        y = 35; // Warp
        x = 17; ecr[x, y].AddEdge(0, new Edge<GscMap, GscTile> { Action = Action.Down, Cost = c, NextEdgeset = 1, NextTile = r37[7, 0] });
        x = 18; ecr[x, y].AddEdge(0, new Edge<GscMap, GscTile> { Action = Action.Down, Cost = c, NextEdgeset = 1, NextTile = r37[8, 0] });
    }
    
    public static void Suicune_InitR37(GscMap r37){
        int x, y, es, c;
        es = 1; c = 17;
        y = 29; 
        for(x = 6; x <= 9; x++){
            for(y = 0; y <= 5; y++){
                if(x == 6 && (y == 0 || y == 1)) continue;
                AddEdges(r37, x, y, es, c, Action.StartB);
                AddEdges(r37, x, y, es, 72, Action.Select);
                
                if(y <= 4) AddEdges(r37, x, y, es, c, Action.Down);
                if(y >= 3 || y >= 1 && x >=7) AddEdges(r37, x, y, es, c, Action.Up);
                if(x <= 8) AddEdges(r37, x, y, es, c, Action.Right);
                if(x >= 8 || x >= 7 && y >=2) AddEdges(r37, x, y, es, c, Action.Left);
            }
        }
    }

    public static void StartSearch(int numThreads) {
        bool[] threadsRunning = new bool[numThreads];
        Thread[] threads = new Thread[numThreads];
        Gold dummyGb = new Gold();
        GscMap map = dummyGb.Maps[Maps.EcruteakCity]; // Always use name "map" for the starting map
        GscMap r37 = dummyGb.Maps[Maps.Route37];
        //GscMap b1f = dummyGb.Maps[Maps.UnionCaveB1F];
        //GscMap r33 = dummyGb.Maps[Maps.Route33];
        
        /*
        Pathfinding.GenerateEdges<GscMap, GscTile>(dummyGb, 0, map[4, 19], Action.Up | Action.Right | Action.Left | Action.Down | Action.StartB);
        
        Pathfinding.GenerateEdges<GscMap, GscTile>(dummyGb, 1, b1f[5, 19], Action.Up | Action.Right | Action.Left | Action.Down | Action.StartB,
                                                    b1f[7, 19]);
        */

        /*
        Geodude_InitUnionCave1F(map, b1f);
        Geodude_InitUnionCaveB1F(b1f);
        */

        /*
        Spearow_InitUnionCave1F(map, r33);
        Spearow_InitR33(r33);
        */

        /*
        Raticate_InitR38(map);
        */

        Suicune_InitEcruteak(map, r37);
        Suicune_InitR37(r37);



        /*
        Pathfinding.DebugDrawEdges<GscMap, GscTile>(dummyGb, r37, 1);
        Environment.Exit(0);
        */

        // GscTile[] startTiles = { map[15, 9] }; // Geodude
        

        // Search parameters
        const int MaxCost = 350;
        int minSuccesses = 51;

        // Save data
        // String savefileStr = "gold_geodude_0xfd_DRR_buffered";
        String[] savefileArr = {
        //    "gold_geodude_0xfd_DRR_buffered"

        //    "gold_spearow_Vulpixguy_L_buffered"
        //    "gold_spearow_Vulpixguy_RDD_buffered",
        //    "gold_spearow_Vulpixguy_RD_buffered",
        //    "gold_spearow_Vulpixguy_R_buffered",

        //    "gold_Raticate_Route38_bike_psc3"

            "gold_suicune_ecruteak_R37_Raikouspot_bike_r"
        };

        byte startDay = Days.Friday;
        byte[] startHours = { 9 }; // 8, 2, 18 
        byte[] startMinutes = { 0 }; // 51
        byte[] audios = { Audios.Stereo, Audios.Mono }; // 0xc1 : mono
        int rtc = 1*3600 + 40*60 + 0; // hour - minute - second

        byte frameType = 0;
        byte menuAccount = 0;
        int playerState = C.Any; // PlayerState.
        int facingDir = C.Any;
        int partyCount = C.Any; // 1 to 6
        int stepCount = C.Any;
        int poisonStepCount = C.Any;
        int repelEffect = C.Any;
        

        String startPath = ""; //"DRRRDDDDDS_BLLLDDLLLLLDDDDDDDLDDDLLLUULUUUURS_BRUULRL";
        Boolean discardSimilarStates = true;

        // Encounter
        
        /*
        String targetSpecies = "GEODUDE";
        int minAttack = 2, minDefense = 0, minSpecial = 0, minSpeed = 0;
        int maxAttack = 7, maxDefense = 15, maxSpecial = 15, maxSpeed = 15;
        int minLevel = 8;
        int targetMap = Maps.UnionCaveB1F;
        */

        /*
        String targetSpecies = "SPEAROW";
        int minAttack = 15, minDefense = 12, minSpecial = 1, minSpeed = 12;
        int maxAttack = 15, maxDefense = 15, maxSpecial = 15, maxSpeed = 15;
        int minLevel = 6;
        int targetMap = Maps.Route33;
        */

        EncounterTarget raticate = new EncounterTarget {
            targetSpecies = "RATICATE",

            minAttack = 7,
            minDefense = 0,
            minSpecial = 0,
            minSpeed = 4,

            maxAttack = 15,
            maxDefense = 15,
            maxSpecial = 15,
            maxSpeed = 15,

            minLevel = 16,

            targetMap = Maps.Route38,
        };

        EncounterTarget suicune = new EncounterTarget {
            targetSpecies = "SUICUNE",

            minAttack = 13,
            minDefense = 0,
            minSpecial = 14,
            minSpeed = 4,

            maxAttack = 15,
            maxDefense = 15,
            maxSpecial = 15,
            maxSpeed = 15,

            minLevel = 40,

            targetMap = Maps.Route37,
        };

        EncounterTarget target = suicune;

        // Display
        Boolean display = false, displayIgt = false;
        Boolean record = false, recordIgt = false;

        
         // CheckDelays
        SavefileData igtSaveData = new SavefileData {
            savefileStr = savefileArr[0],
            outputSavefileStr = savefileArr[0],
            day = startDay,
            hour = startHours[0],
            minute = startMinutes[0],
            audio = audios[0],
            frameType = frameType,
            menuAccount = menuAccount,
            igt = 0,

            stepCount = stepCount,
            poisonStepCount = poisonStepCount,
            playerState = playerState,
            facingDir = facingDir,
            repelEffect = repelEffect,
            partyCount = partyCount,
        };

        CheckDelays(igtSaveData, rtc, 1, 0, 121, 121, "RRRRDDDDDDDDDRLL", 0);
        Environment.Exit(0);
        
        
        for(int idx = 0; idx < savefileArr.Length; idx++) {
            String savefileStr = savefileArr[idx];
            //GscTile tile = startTiles[idx];
            Console.WriteLine("Savefile: " + savefileStr);
            Writer = new StreamWriter(savefileStr + "_" + target.targetSpecies + "_" + DateTime.Now.Ticks + ".txt");

            foreach(byte hour in startHours) {
                foreach(byte minute in startMinutes) {
                    //for(byte momStep = 0; momStep <= 1; momStep++) {
                        foreach(byte audio in audios) {
                            //for(byte frameType = 0; frameType <= 7; frameType++) {
                                //for(byte menuAccount = 0; menuAccount <= 1; menuAccount++) {
                                    ClusterDic clusterDic = new ClusterDic();
                                    for(byte igt = 0; igt < 60; igt += 60) {

                                        Console.WriteLine("igt: "+igt);
                                        
                                        int x, y;
                                        SavefileData savefileData = new SavefileData {
                                            savefileStr = savefileStr,
                                            outputSavefileStr = savefileStr,
                                            day = startDay,
                                            hour = hour,
                                            minute = minute,
                                            audio = audio,
                                            frameType = frameType,
                                            menuAccount = menuAccount,
                                            igt = igt,

                                            stepCount = stepCount,
                                            poisonStepCount = poisonStepCount,
                                            playerState = playerState,
                                            facingDir = facingDir,
                                            repelEffect = repelEffect,
                                            partyCount = partyCount,
                                        };

                                        (x,y) = MakeSave(savefileData);
                                        GscTile tile = map[x,y]; // Init starting tile to the one of the savefile

                                        Gold gb = new Gold("roms/"+ savefileStr + "_temp.sav");
                                        if(record){
                                            gb.Record(savefileStr);
                                        }
                                        if(display)
                                            gb.Show();

                                        gb.SetTimeSec(rtc);
                                        File.WriteAllBytes("roms/" + savefileStr + "_forRTC.gqs", gb.SaveState());

                                        gb.Hold(Joypad.Left, 0x100);
                                        
                                        GscStrat.GfSkip.Execute(gb);
                                        GscStrat.TitleSkip.Execute(gb);
                                        byte[] mmbackState = gb.SaveState();
                                        for(int mmBack = 0; mmBack <= 3; mmBack++) {
                                            Console.WriteLine("mmBack: "+mmBack);
                                            GscStrat.Continue.Execute(gb);
                                            byte[] fsbackState = gb.SaveState();
                                            for(int fsBack = 0; fsBack <= 3; fsBack++) {
                                                Console.WriteLine("fsBack: "+fsBack);
                                                gb.Hold(Joypad.Left, "GetJoypad");
                                                gb.AdvanceFrame(Joypad.Left);
                                                byte[] delayState = gb.SaveState();
                                                
                                                for(int delay = 0; delay <= MaxCost; delay++) {

                                                    Console.WriteLine("(" + DateTime.Now.ToString("HH:mm:ss") + ")" + " delay: "+delay);
                                                    int introCost = mmBack * 83 + fsBack * 101 + delay;
                                                    if(introCost > MaxCost) break;
                                                    gb.Hold(Joypad.A, "OWPlayerInput");

                                                    if(startPath.Length > 0 && gb.OverworldLoopAddress != gb.Execute(LogToPath(startPath))){
                                                        gb.LoadState(delayState);
                                                        gb.AdvanceFrame(Joypad.Left);
                                                        delayState = gb.SaveState();
                                                        continue;
                                                    }


                                                    //Console.WriteLine("test0 "+gb.EmulatedSamples + " " + gb.A + " " + gb.B + " " + gb.C + " " + gb.D + " "+ gb.E + " " + gb.F);
                                                    
                                                    // Search for an encounter
                                                    DFParameters<Gold, GscMap, GscTile> parameters = new DFParameters<Gold, GscMap, GscTile>() {
                                                        SuccessSS = 1,
                                                        PruneAlreadySeenStates = discardSimilarStates,
                                                        MaxCost = MaxCost - introCost,
                                                        EndEdgeSet = 1,
                                                        EncounterCallback = gb => CheckEncounter(gb, target),
                                                        //MapTransitionCallback = gb => CheckDog(gb, target.targetMap),
                                                        SingleCallback = (state,gb) => IGT60(
                                                            savefileData, rtc, mmBack, fsBack, delay, startPath+state.Log, state.WastedFrames, Writer,
                                                            expectedOutcome: ManipOutcome.Encounter, 
                                                            encounterTarget: target, 
                                                            recordIgt: recordIgt,
                                                            displayIgt: displayIgt,
                                                            minSuccesses: minSuccesses,
                                                            clusterDic: clusterDic
                                                        ),
                                                    };
                                                    IGTResults results = new IGTResults(1);
                                                    results[0] = new IGTState(gb, true, 0);
                                                    DepthFirstSearch.StartSearch(new Gold[] { gb }, parameters, tile, 0, results);
                                                    gb.LoadState(delayState);
                                                    gb.AdvanceFrame(Joypad.Left);
                                                    delayState = gb.SaveState();
                                                }
                                                gb.LoadState(fsbackState);
                                                GscStrat.FsBack.Execute(gb);
                                                GscStrat.Continue.Execute(gb);
                                                fsbackState = gb.SaveState();
                                            }
                                            gb.LoadState(mmbackState);
                                            GscStrat.MmBack.Execute(gb);
                                            GscStrat.TitleSkip.Execute(gb);
                                            mmbackState = gb.SaveState();
                                        }
                                    }
                                    Writer.WriteLine(clusterDic.GetClusterString());
                                    Writer.Flush();

                                //}
                            //}
                        }
                    //}
                }
            }
        }
        Console.WriteLine("End of search space.");
        /*
        while(true) {
            Thread.Sleep(10000);
        }
        */
    }
}