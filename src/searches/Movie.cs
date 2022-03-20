public class Movie {
    public static void makeGeodudeMovie(){
        string savefileStr = "gold_geodude_0xfd_DRR_buffered";
        byte startDay = Days.Sunday;
        byte hour = 9;
        byte minute = 59;
        byte audio = Audios.Stereo;
        byte frameType = 0;
        byte menuAccount = 0;
        byte igt = 0;
        int playerState = C.Any;
        int partyCount = C.Any;
        int stepCount = C.Any;

        int rtc = 0*3600 + 20*60 + 0;

        (int, string)[] delayPaths = {
            (22, "RDDDDDLLLDDLLLLDLDDDDDLDDDDLLLUULUUUUURR"+"DLUULR"),
            (23, "RDDDDDLLLDDLLLLDLDDDDDLDDDDLLLUULUUUUURR"+"RDLLLR")
        };

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
            //poisonStepCount = poisonStepCount,
            playerState = playerState,
            //facingDir = facingDir,
            //repelEffect = repelEffect,
            partyCount = partyCount,
        };
        Encounter.MakeSave(savefileData);

        foreach((int, string) delayPath in delayPaths){
            int delay = delayPath.Item1;
            string path = delayPath.Item2;

            Gold gb = new Gold("roms/"+ savefileStr + "_temp.sav");
            gb.Record(savefileStr + "_" + delay);
            
            gb.SetTimeSec(rtc);
            gb.Hold(Joypad.Left, 0x100);
            
            GscStrat.GfSkip.Execute(gb);
            GscStrat.TitleSkip.Execute(gb);
            GscStrat.Continue.Execute(gb);
            gb.Hold(Joypad.Left, "GetJoypad");
            for(int i = 0; i <= delay; i++) {
                gb.AdvanceFrame(Joypad.Left);
            }
                                                                
            gb.Hold(Joypad.A, "OWPlayerInput");
            int ret = gb.Execute(Encounter.LogToPath(path));

            if(ret == gb.SYM["RandomEncounter.ok"]){
                gb.RunUntil("GetJoypad");
                gb.Press(Joypad.A);
                gb.RunUntil("GetJoypad");
            }

            gb.Dispose();
        }
    }

    public static void makeSpearowMovie(){
        string savefileStr = "gold_spearow_Vulpixguy_RDD_buffered";
        byte startDay = Days.Sunday;
        byte hour = 9;
        byte minute = 59;
        byte audio = Audios.Stereo;
        byte frameType = 0;
        byte menuAccount = 0;
        byte igt = 0;
        int playerState = C.Any;
        int partyCount = C.Any;
        int stepCount = C.Any;

        int rtc = 0*3600 + 22*60 + 0;

        (int, string)[] delayPaths = {
            (0, "DDD" + "D" + "RDDDDDDUDL"),
        };

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
            //poisonStepCount = poisonStepCount,
            playerState = playerState,
            //facingDir = facingDir,
            //repelEffect = repelEffect,
            partyCount = partyCount,
        };
        Encounter.MakeSave(savefileData);

        foreach((int, string) delayPath in delayPaths){
            int delay = delayPath.Item1;
            string path = delayPath.Item2;

            Gold gb = new Gold("roms/"+ savefileStr + "_temp.sav");
            gb.Record(savefileStr + "_" + delay);
            
            gb.SetTimeSec(rtc);
            gb.Hold(Joypad.Left, 0x100);
            
            GscStrat.GfSkip.Execute(gb);
            GscStrat.TitleSkip.Execute(gb);
            GscStrat.Continue.Execute(gb);
            gb.Hold(Joypad.Left, "GetJoypad");
            for(int i = 0; i <= delay; i++) {
                gb.AdvanceFrame(Joypad.Left);
            }
                                                                
            gb.Hold(Joypad.A, "OWPlayerInput");
            int ret = gb.Execute(Encounter.LogToPath(path));

            if(ret == gb.SYM["RandomEncounter.ok"]){
                gb.RunUntil("GetJoypad");
                gb.Press(Joypad.A);
                gb.RunUntil("GetJoypad");
            }

            gb.Dispose();
        }
    }

    public static void makeSuicuneMovie(){
        string savefileStr = "gold_suicune_ecruteak_R37_Raikouspot_bike_r";
        byte startDay = Days.Friday;
        byte hour = 9;
        byte minute = 0;
        byte audio = Audios.Stereo;
        byte frameType = 0;
        byte menuAccount = 0;
        byte igt = 0;
        int playerState = C.Any;
        int partyCount = C.Any;
        int stepCount = C.Any;

        int rtc = 1*3600 + 40*60 + 0;

        (int, int, int, string)[] delayPaths = {
            (1, 0, 121, "RRRRDDDDDDDDDRLL"),
        };

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
            //poisonStepCount = poisonStepCount,
            playerState = playerState,
            //facingDir = facingDir,
            //repelEffect = repelEffect,
            partyCount = partyCount,
        };
        Encounter.MakeSave(savefileData);

        foreach((int, int, int, string) delayPath in delayPaths){
            int mmBack = delayPath.Item1;
            int fsBack = delayPath.Item2;
            int delay = delayPath.Item3;
            string path = delayPath.Item4;

            Gold gb = new Gold("roms/"+ savefileStr + "_temp.sav");
            gb.Record(savefileStr + "_" + delay);
            
            gb.SetTimeSec(rtc);
            gb.Hold(Joypad.Left, 0x100);
            
            GscStrat.GfSkip.Execute(gb);
            GscStrat.TitleSkip.Execute(gb);
            for(int _mmBack = 0; _mmBack < mmBack; _mmBack++) {
                GscStrat.MmBack.Execute(gb);
                GscStrat.TitleSkip.Execute(gb);
            }

            GscStrat.Continue.Execute(gb);

            for(int _fsback = 0; _fsback < fsBack; _fsback++){
                GscStrat.FsBack.Execute(gb);
                GscStrat.Continue.Execute(gb);
            }

            gb.Hold(Joypad.Left, "GetJoypad");
            gb.AdvanceFrame(Joypad.Left);
            for(int _delay = 0; _delay < delay; _delay++) {
                gb.AdvanceFrame(Joypad.Left);
            }
            gb.Hold(Joypad.A, "OWPlayerInput");

            int ret = gb.Execute(Encounter.LogToPath(path));

            if(ret == gb.SYM["RandomEncounter.ok"]){
                gb.RunUntil("GetJoypad");
                gb.Press(Joypad.A);
                gb.RunUntil("GetJoypad");
            }

            gb.Dispose();
        }
    }
}