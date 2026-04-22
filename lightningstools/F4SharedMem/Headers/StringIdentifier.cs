using System;

namespace F4SharedMem.Headers
{
    // changelog:
    // 1: initial BMS 4.34 version
    // 2: added 3dbuttons.dat/3dckpit.dat file paths
    // 3: added NavPoints
    // 4: added ThrTerrdatadir

    [Serializable]
    public enum StringIdentifier : uint
    {
        // VERSION 1
        BmsExe = 0,             // BMS exe name, full path
        KeyFile,                // Key file name in use, full path

        BmsBasedir,             // BmsBasedir to BmsPictureDirectory:
        BmsBinDirectory,        // - BMS directories in use
        BmsDataDirectory,
        BmsUIArtDirectory,
        BmsUserDirectory,
        BmsAcmiDirectory,
        BmsBriefingsDirectory,
        BmsConfigDirectory,
        BmsLogsDirectory,
        BmsPatchDirectory,
        BmsPictureDirectory,

        ThrName,                 // Current theater name
        ThrCampaigndir,          // ThrCampaigndir to ThrTacrefpicsdir:
        ThrTerraindir,           // - Current theater directories in use
        ThrArtdir,
        ThrMoviedir,
        ThrUisounddir,
        ThrObjectdir,
        Thr3ddatadir,
        ThrMisctexdir,
        ThrSounddir,
        ThrTacrefdir,
        ThrSplashdir,
        ThrCockpitdir,
        ThrSimdatadir,
        ThrSubtitlesdir,
        ThrTacrefpicsdir,

        AcName,                  // Current AC name
        AcNCTR,                  // Current AC NCTR

        // VERSION 2
        ButtonsFile,             // Current 3dbuttons.dat file full path
        CockpitFile,             // Current 3dckpit.dat file full path

        // VERSION 3
        NavPoint,                // Multiple entries, one for each NavPoint. Format for each entry is (NP, O1, O2, PT can be concatenated):
                                 // (NavPoint, mandatory) NP:<index>,<type>,<x>,<y>,<z>,<grnd_elev>;
                                 //     <index>        int            NavPoint number, 1-99
                                 //     <type>         two chars      GM (GMPOINT), PO (POSPOINT), WP (WAYPOINT), MK (MARKPOINT), DL (DATALINK)
                                 //                                   CB (CAMPBULLSEYE), L1 (LINE1), L2 (LINE2), L3 (LINE3), L4 (LINE4), PT (PREPLANNEDTHREAD)
                                 //     <x>,<y>        float          position in sim coordinates
                                 //     <z>            float          altitude in 10s of feet
                                 //     <grnd_elev>    float          ground elevation in 10s of feet
                                 // (OA1/OA2, optional) O1:<bearing>,<range>,<alt>; (and/or) O2:<bearing>,<range>,<alt>;
                                 //     <bearing>      float
                                 //     <range>        unsigned int
                                 //     <alt>          unsigned int
                                 // (PPT, optional) PT:<str_id>,<range>,<declutter>;
                                 //     <str_id>       "string"
                                 //     <range>        float
                                 //     <declutter>    int            0 = false, 1 = true

        // VERSION 4
        ThrTerrdatadir,

        // VERSION 5
        VoiceHelpers,            // A comma separated list of tokens that represent flight and agency names used in AI radio comms
                                 // The format of the string is a single line laid out as follows:
                                 // 
                                 //     <player>|<seats>,<tanker>,<awacs>,<departure>,<arrival>,<alternate>
                                 // 
                                 // where:
                                 // 
                                 //     <player> represents the local player's ownship flight name, "Goblin1" for example
                                 // 
                                 //     '|' is a literal pipe symbol character as a separator between the <player> and <seats> tokens
                                 // 
                                 //     <seats> is a 4-character token representing the occupancy of the 4 slots in a flight wherein:
                                 //         'P' is for the local session player
                                 //         'H' is for any other human player
                                 //         'A' is for an AI piloted jet
                                 //         'X' is for a slot in the flight that has no jet
                                 // 
                                 //     for example as <seats> token of "HPXX" would represent a multi-player mission 2-ship with the local 
                                 //     player flying in the #2 slot.
                                 // 
                                 //     <tanker> is the callsign of any assigned tanker flight or "None" if there isn't one assigned in the
                                 //     ATO. For example "Texaco7".
                                 // 
                                 //     <awacs> is the callsign of any assign Air Battle Manager or Weapons Director agency (normally AWACS)
                                 //     assigned to the flight or "None" if there is no such support flight.  Example might be "Chalice9".
                                 // 
                                 //     <departure>, <arrival> and <alternate> are respectively the names of the landing sites for the flights
                                 //     takeoff, planned landing and alternate landing sites.  For example you might see "Osan,Osan,Pyeongtaek"
                                 //     for a flight that plans a round trip to/from Osan with the other named airbase as the alternate.

        // FIXED LAST ENTRY
        StringIdentifier_DIM     // (number of identifiers; add new IDs only *above* this one)
    };
}
