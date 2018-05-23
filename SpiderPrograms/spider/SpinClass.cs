using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spider
{

    public enum eSavedType
    {
        eDEAL,
        eSEED,
        eFIRST,
        eSUIT,
        eMERGE
    }

   public class cSavedBin
    {
        public int Ordinal; // 0..99 files can be created from each deal
        public int iDeal;   // 0..5 where 0 is the original deck
        public eSavedType eSB;
        public string strLead;  // filename prefix excluding path and extension
    }

    public class cSpinControl
    {
        public DateTime TimeProgramStarted = DateTime.Now;
        public DateTime TimeDealStarted;
        public cBuildDeck cBD;
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
        public string XML_Diag_filename;    // XML that can create a game
        public string BIN_Diag_filename;    // output saved game file created from xml
        public List<cEventClass> EventList = new List<cEventClass>();
        public static int[] BinDealCount = new int[6] { 0, 0, 0, 0, 0, 0 };
        public cMergeXmlFile cMXF;
        public byte[] PngArray;
        public byte[] Hdr = new byte[0x2028];
        public string FormName(int iDeal, eSavedType e)
        {
            int nfile = BinDealCount[iDeal];
            string strLead = "";
            switch (e)
            {
                case eSavedType.eFIRST:
                    strLead = "a" + iDeal.ToString() + "FirstEmptyColumn";
                    break;
                case eSavedType.eDEAL:
                    strLead = "Deal" + iDeal.ToString() + "_" + nfile.ToString();
                    BinDealCount[iDeal]++;
                    break;
                case eSavedType.eSEED:
                    strLead = "Deal" + iDeal.ToString() + "_" + nfile.ToString() + "_" + "Seed" ;
                    BinDealCount[iDeal]++;
                    break;
                case eSavedType.eSUIT:
                    strLead = "Deal" + iDeal.ToString() + "_" + nfile.ToString() + "_" + "Suit" ;
                    BinDealCount[iDeal]++;
                    break;
                case eSavedType.eMERGE:
                    return "";
                    break;
            }
            return strLead;
        }
    }

}
