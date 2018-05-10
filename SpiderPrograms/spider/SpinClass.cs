using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spider
{
    public class cSpinControl
    {
        public DateTime TimeProgramStarted = DateTime.Now;
        public DateTime TimeDealStarted;
        public int GameSeed;
        public bool bTrace = false;
        public bool bTrigger = false;
        public bool bSpinTimedOut;
        public int LocalDealCounter;    // used for dealname 
        public bool bOutOfSpaceInSTLOOKUP;
        public bool bGotOneSuitAtLeast;
        public bool bSpinDidAllBoards;
        public bool bStopThisRun;
        public int BestScore;   // highest score we got since the last deal
        public int NumberDuplicates;
        public int CountLimit;  // number of boards created
        public bool bExceededCountLimit;    // exceeded the hard limit of array size
        public bool bExceededTWOcolLimit;   // too many two or more empty column empties
        public bool bExceededONEcolLimit;   // too many single column empties
        public bool bExceededThreecolLimit; // 3 or more and no completes triggers a "done" after 100 events
        public cMaxValues MaxValues = new cMaxValues(
                GlobalClass.MaxInsertsWhenSuiting,
                GlobalClass.MinInserts,
                GlobalClass.MaxTimeoutWhenSuiting,
                GlobalClass.MinTimeout
            );
        public Cstlookup stlookup;
        public cSeed PrevSeed, ThisSeed;
        public List<board> ThisBoardSeries = new List<board>(32768);
        public List<board> NextBoardSeries = new List<board>(32768);
        public List<cBestScore> SortedScores;
        public List<int> BestScoreIndex = new List<int>(32768);
        public bool bSignalSpinDone;
        public int RunCounter;
        public cStrategy Strategy;
        public csuitable Suitable;
        public cJoinSuited JoinSuited;
        public cBuildDeck Deck;
        public board BestBoard;
        public bool bJustReadXML = false;
        public string XML_Diag_filename;
        public List<cEventClass> EventList = new List<cEventClass>();
        public cMergeXmlFile cMXF;
        public byte[] PngArray;
        public byte[] Hdr = new byte[0x2028];
    }

}
