using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;



namespace spider
{



    public static class GlobalClass
    {


        public static cCardMeta[] LocMeta;

        public static string SaveName = "Spider Solitaire.SpiderSolitaireSave-ms";
        public static string[] SuitNames = new string[4] { "Diamonds", "Clubs", "Hearts", "Spades" };
        public static string[] CardNames = new string[13] { "Ace", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Jack", "Queen", "King" };
        //public static string[] CardNames  // if use this, then add m_ to the above CardNames
        //{
        //    get { return m_CardNames; }
        //    //set { m_CardNames[] = value; }
        //}

        public static string cSuits = "dchs";
        public static string cRanks = "a234567890jqk";
        public static string rNames = "   a 2 3 4 5 6 7 8 910 j q k   ";
        public static string cstrInxVars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public static int MaxScore = 24000;    // since on last deal we are using 0 for empty columns
        public enum WorkingType
        {
            tTopMost,
            tBottomMost,
            tEmpties,
            tNonEmpties
        }

        // smallest first unless noted
        public enum eSortedSEQtype
        {
            SortBySize, 
            SortByCost,
            SortByRank  // we want largest first for stacking purposes
        }

        public enum TypeCompletedBy
        {
            ID_UNK,
            ID_JSS,
            ID_RSI,
            ID_SOSET,
            ID_BS,
            ID_USTK,
            ID_FSS,
            ID_COMBN
        }

        public enum StrategyType
        {
            CONTINUE_SPINNING,
            EXPOSE_ONE_TOP,
            RUN_JOINSUITS_INPLACE,
            SPIN_JOINSUITS,
            REDUCE_SUITS
        }


#if DEBUG
        /*
 * trace bits 
 * 1 : show all boards remaining after a shrink
 * 2: info at abort and spindone within SpinOneCard, at end of ShrinkThisBoardSeries, and at didallboards in RunFilter
 * 4: in SpinOneCard, show each board at the 5000 mark
 * 8 :in SpinOneCard, show each board just before it is queued
 * 10: in TrySpin and was used to check the verify code but is not used anymore
 */
        public static int TraceBits = 6;//(2 + 4); // 18 (10x + 2)
        public static int MaxInsertsWhenSuiting = 200000;
        public static int MinInserts = 100000;
        public static int MaxTimeoutWhenSuiting = 240;   // 60 was 240
        public static int MinTimeout = 120;              // 30 was 120
#else
        public static int TraceBits = 6;    // was 2
        public static int MaxInsertsWhenSuiting = 600000;
        public static int MinInserts = 300000;
        public static int MaxTimeoutWhenSuiting = 1000;
        public static int MinTimeout = 500;
#endif


        public const int FIRST_CARD = 1;
        public const int DEALT_A_CARD = 2;    // 0xe000  rank 14 does not exist
        public const int BUILT_SUIT = 3;      // 0xf000  neither does rank 15
        public const int MAXINFO = 14;        // 1 initial card, 5 deals, 8 built suits
        public const int MAX_FILTERED_BOARDS = 512; // was 512
        public const int MIN_FILTERED_BOARDS = 20;  // was 20
        public const int STLOOKUP_MPLY = 128;
        public const int EXTRA_POINTS_IF_UNEXPOSED_CAN_COMPLETE_SUIT = 50;
        public const int EXTRA_POINTS_IF_EXPOSED_CAN_COMPLETE_SUIT = 100;
        public static string strSpiderBin;
        public static cRankSuitType[] cRST;
        public enum eExceptionType
        {
            eBoardCompleted,
            eRunawayMoves,
            eNoCardsLeft,
            eCannotFindFile,
            eNewXMLFileExists
        }

        public static string ProperName(card c)
        {
            string suit = SuitNames[c.suit];
            string name = CardNames[c.rank - 1] + "Of" + suit;
            if (c.ID > 51) name += "1";
            return name;
        }

        public static Exception Board_Completed = new Exception("Spider");
        public static Exception Runaway_Moves = new Exception("Spider");
        public static Exception No_Cards_Left = new Exception("Spider");
        public static Exception Cannot_find_file = new Exception("Spider");
        public static Exception NewXML_file_exists = new Exception("Spider");
        //public static void InitExceptions()
        //{
        //    Board_Completed.Data.Add("ID", eExceptionType.eBoardCompleted);
        //    Runaway_Moves.Data.Add("ID",eExceptionType.eRunawayMoves);
        //    No_Cards_Left.Data.Add("ID", eExceptionType.eNoCardsLeft);
        //    Cannot_find_file.Data.Add("ID", eExceptionType.eCannotFindFile);
        //    NewXML_file_exists.Data.Add("ID", eExceptionType.eNewXMLFileExists);
        //}
    }

    // 2018 had to use this "hack" to find where "Saved Games" is located when the user has used the "move" property to relocate his \user\username
    //  win7 spider installs into win10 go in to the original users home folder which still exists, but is not used when configured for its contents
    // to be moved.  Since it is not always the C drive then we need to look for where it was originally

    public static class KnownFolderFinder
    {
        private static readonly Guid CommonDocumentsGuid = new Guid("{FDD39AD0-238F-46AF-ADB4-6C85480369C7}");

        [Flags]
        public enum KnownFolderFlag : uint
        {
            KF_FLAG_DEFAULT = 0x00000000,
            KF_FLAG_SIMPLE_IDLIST = 0x00000100,
            KF_FLAG_NOT_PARENT_RELATIVE = 0x00000200,
            KF_FLAG_DEFAULT_PATH = 0x00000400,
            KF_FLAG_INIT = 0x00000800,
            KF_FLAG_NO_ALIAS = 0x00001000,
            KF_FLAG_DONT_UNEXPAND = 0x00002000,
            KF_FLAG_DONT_VERIFY = 0x00004000,
            KF_FLAG_CREATE = 0x00008000,
            KF_FLAG_NO_PACKAGE_REDIRECTION = 0x00010000,
            KF_FLAG_NO_APPCONTAINER_REDIRECTION = 0x00010000,
            KF_FLAG_FORCE_PACKAGE_REDIRECTION = 0x00020000,
            KF_FLAG_FORCE_APPCONTAINER_REDIRECTION = 0x00020000,
            KF_FLAG_RETURN_FILTER_REDIRECTION_TARGET = 0x00040000,
            KF_FLAG_FORCE_APP_DATA_REDIRECTION = 0x00080000,
            KF_FLAG_ALIAS_ONLY = 0x80000000
        }

        [DllImport("shell32.dll")]
        static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);

        public static string GetFolderFromKnownFolderGUID(Guid guid)
        {
            return pinvokePath(guid, KnownFolderFlag.KF_FLAG_DEFAULT_PATH);
        }

        public static void EnumerateKnownFolders()
        {
            KnownFolderFlag[] flags = new KnownFolderFlag[] {
            KnownFolderFlag.KF_FLAG_DEFAULT,
            KnownFolderFlag.KF_FLAG_ALIAS_ONLY | KnownFolderFlag.KF_FLAG_DONT_VERIFY,
            KnownFolderFlag.KF_FLAG_DEFAULT_PATH | KnownFolderFlag.KF_FLAG_NOT_PARENT_RELATIVE,
            };


            foreach (var flag in flags)
            {
                Console.WriteLine(string.Format("{0}; P/Invoke==>{1}\n", flag, pinvokePath(CommonDocumentsGuid, flag)));
            }
            Console.ReadLine();
        }

        private static string pinvokePath(Guid guid, KnownFolderFlag flags)
        {
            IntPtr pPath;
            SHGetKnownFolderPath(guid, (uint)flags, IntPtr.Zero, out pPath); // public documents

            string path = System.Runtime.InteropServices.Marshal.PtrToStringUni(pPath);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pPath);
            return path;
        }
    }

}
