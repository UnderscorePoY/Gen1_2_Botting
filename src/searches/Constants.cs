using System.Collections.Generic;

public class Maps {
    public const int UnionCave1F = 797;
    public const int UnionCaveB1F = 798;
    public const int Route33 = 2054;

    public const int Route37 = 2564;
    public const int Route38 = 268;

    public const int EcruteakCity = 1033;

    public static string GetName(int num){
        switch(num){
            case UnionCave1F : return "Union Cave 1F";
            case UnionCaveB1F : return "Union Cave B1F";
            case Route33 : return "Route 33";
            case Route37 : return "Route 37";
            case Route38 : return "Route 38";
            case EcruteakCity : return "Ecruteak City";
            default : return "???";
        }
    }

}

public class Days {
    public const int Sunday = 0;
    public const int Monday = 1;
    public const int Tuesday = 2;
    public const int Wednesday = 3;
    public const int Thursday = 4;
    public const int Friday = 5;
    public const int Saturday = 6;
}

public class Audios {
    public const int Mono = 0xc1;
    public const int Stereo = 0xe1;
}

public class PlayerState {
    public const int Foot = 0;
    public const int Bike = 1;
}

public class C {
    public const int Any = -1;
}

public class Save {
    // addr - 0xB198 = sav addr

    public const int IgtSecond = 0x2056; // wGameTimeSeconds
    public const int IgtFrame = 0x2057; // wGameTimeFrames

    public const int StepCount = 0x2825; // wStepCount
    public const int PoisonStepCount = 0x2826; // wPoisonStepCount


    public const int Audio = 0x2000;
    public const int FrameType = 0x2002;
    public const int MenuAccount = 0x2005;

    public const int StartDay = 0x2044; // wStartDay
    public const int StartHour = 0x2045; // wStartHour
    public const int StartMinute = 0x2046; // wStartMinute
    public const int StartSecond = 0x2047;  // wStartSecond

    public const int PlayerState = 0x24EA; // 0 = walking, 1 = biking

    public const int FacingDir = 0x206D; // 0x00 = down, 0x04 = up, 0x08 = left, 0x0C = right

    public const int RepelEffect = 0x2853;  // repel steps still active

    public const int PartyCount = 0x288A;     // party count
            
    public const int ChecksumStart = 0x2009; // included
    public const int ChecksumEnd = 0x2d68; // included

    public const int ChecksumBot = 0x2d69;
    public const int ChecksumTop = 0x2d6a;

    public const int XCoord = 0x286B;
    public const int YCoord = 0x286A;
}

public class RAM {

    public const int MapGroup = (01 << 16) | 0xda00;
    public const int MapNumber = (01 << 16) | 0xda01;

    public const int RaikouSpecies = (01 << 16) | 0xdd1a;
    public const int RaikouLevel = (01 << 16) | 0xdd1b;
    public const int RaikouMapGroup = (01 << 16) | 0xdd1c;
    public const int RaikouMapNumber = (01 << 16) | 0xdd1d;
    public const int RaikouHP = (01 << 16) | 0xdd1e; 
    public const int RaikouDVs = (01 << 16) | 0xdd1f;

    public const int EnteiSpecies = (01 << 16) | 0xdd21;
    public const int EnteiLevel = (01 << 16) | 0xdd22;
    public const int EnteiMapGroup = (01 << 16) | 0xdd23;
    public const int EnteiMapNumber = (01 << 16) | 0xdd24;
    public const int EnteiHP = (01 << 16) | 0xdd25;
    public const int EnteiDVs = (01 << 16) | 0xdd26;

    public const int SuicuneSpecies = (01 << 16) | 0xdd28;
    public const int SuicuneLevel = (01 << 16) | 0xdd29;
    public const int SuicuneMapGroup = (01 << 16) | 0xdd2a;
    public const int SuicuneMapNumber = (01 << 16) | 0xdd2b;
    public const int SuicuneHP = (01 << 16) | 0xdd2c;
    public const int SuicuneDVs = (01 << 16) | 0xdd2d;
}

public class Species {
    public const int Poliwag = 0x3C;

    public const int Terminator = 0xFF;
}

public class FacingDir {
    public const int Down = 0x00;
    public const int Up = 0x04;
    public const int Left = 0x08;
    public const int Right = 0x0C;
}