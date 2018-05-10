using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace spider
{

    public class cMoveData
    {
        public int Src;
        public int Des;
        public int ID;
        public bool bWasFacedown;   // if true then moveing the card exposed a facedown one
        public bool bBiggest;       // if true then this score was the best score for this deal
        public int NumMoved;
        public int Score;
        public int WhereInfo;
        public string ShrinkCode;     // this is added after the move has been saved!
        public cMoveData(int ID, int Src, int Des, int Score, bool bWasFacedown, int NumMoved)
        {
            this.ID = ID;   // this is card id
            this.Src = Src;
            this.Des = Des;
            this.Score = Score;
            this.bWasFacedown = bWasFacedown;
            this.NumMoved = NumMoved;
            WhereInfo = -1;
            this.ShrinkCode = "";
        }
    }

    public class cMoveInfo
    {
        public int deal;
        public int score;
        public int suiter;
        public int suit;
        public int id;  // this is NOT the card id, it is an info id use to identify the move infomation
        public int MinutesLastDeal;
        public DateTime TimeOfMove;
    }

    public class cMoveClass
    {
        public DateTime InitialCardTime;
        public DateTime LastBuildTime;
        public DateTime LastDealTime;

        public List<cMoveData> TheseMoves = new List<cMoveData>();
        public List<cMoveInfo> ThisMoveInfo = new List<cMoveInfo>(GlobalClass.MAXINFO);
        private TimeSpan ts;
        private bool bOnFirstBuild;
        private bool bOnFirstDeal;

        public cMoveClass()
        {
            bOnFirstBuild = true;
            bOnFirstDeal = true;
        }


        public void AddMove(int CardID,int srcStack, int desStack, int score, bool bSameSuit, int NumMoved)
        {
            int i, j, n, m = 24, v1, v2, t = 0, NumMoves;
            cMoveData cMD = new cMoveData(CardID, srcStack, desStack, score, bSameSuit, NumMoved);
            TheseMoves.Add(cMD);
            NumMoves = TheseMoves.Count;
            if (NumMoves > m)
            {
                n = NumMoves - m;   // last move to test is NumMoves-1 and first to test is NumMoves-16
                for (i = 0; i < (m - 1); i++)
                {
                    v1 = TheseMoves[n + i].ID;
                    for (j = i + 1; j < m; j++)
                    {
                        v2 = TheseMoves[n + j].ID;
                        if (v1 == v2) t++;  // increment total duplicate move count

                    }
                }
                // use 35 with m=16 or 84 with m=24
                if (t < 84) return; // see test program:  catches a pattern of 3 or less moves repeating
                Debug.Assert(true);
                Console.WriteLine("Runaway moves?  t:" + t);
                TraceBoard();
                for (i = 0; i < m; i++)
                {
                    Console.Write("{0:X} ", TheseMoves[n + i].ID);
                }
                Console.WriteLine("");
                GlobalClass.Runaway_Moves.Data.Add("MSG", "Runaway moves!  moves are repeating");
                throw GlobalClass.Runaway_Moves;
            }
        }

        public void AddShrinkCode(ref cSpinControl cSC, int cnt)
        {
            int n = TheseMoves.Count-1;
            if (cnt > 99) cnt = 99;
            // jys oct 2012 remove  this???
           // nov 2012 Debug.Assert(n > 0);
            TheseMoves[n].ShrinkCode = utils.GetShrinkCode(ref cSC).ToString() + "-" + utils.Lpadto(cnt.ToString(), 2);
        }

        // on FIRST_CARD vInfo is the number of deals, if any, before the first card
        // on BUILT_SUIT vInfo is the suit and vInfo1 the id of the suit builder
        // on DEALT_CARD vInfo is the score and vInfo1 is the dealcounter
        public void AddMoveInfo(int iInfo, int vInfo, int vInfo1)
        {
            cMoveInfo thisMI = new cMoveInfo();
            switch (iInfo)
            {
                case GlobalClass.FIRST_CARD:
                    InitialCardTime = DateTime.Now;
                    return; // cannot add anything since there have been no cards moved.  just record time

                case GlobalClass.DEALT_A_CARD:  // is always appended to the last move

                    thisMI.score = vInfo;
                    thisMI.deal = vInfo1;
                    thisMI.TimeOfMove = DateTime.Now;
                    if (bOnFirstDeal)
                        LastDealTime = InitialCardTime;
                    ts = thisMI.TimeOfMove.Subtract(LastDealTime);
                    thisMI.MinutesLastDeal = Convert.ToInt32(ts.TotalMinutes);
                    LastDealTime = thisMI.TimeOfMove;
                    bOnFirstDeal = false;
                    break;
                case GlobalClass.BUILT_SUIT:  // is always appended to the last move
                    thisMI.suit = vInfo;
                    thisMI.suiter = vInfo1;
                    thisMI.TimeOfMove = DateTime.Now;
                    if (bOnFirstBuild)
                        LastBuildTime = InitialCardTime;
                    ts = thisMI.TimeOfMove.Subtract(LastBuildTime);
                    thisMI.MinutesLastDeal = Convert.ToInt32(ts.TotalMinutes);
                    LastBuildTime = thisMI.TimeOfMove;
                    bOnFirstBuild = false;
                    break;
                default: Debug.Assert(false);
                    break;
            }
            thisMI.id = iInfo;
            // jys nov2012 the problem is the deal is not added if there are no moves!!!!
            // cannot append if there are no moves
            if (TheseMoves.Count > 0)
            {
                TheseMoves[TheseMoves.Count - 1].WhereInfo = ThisMoveInfo.Count;
            }
            else
            {
                if (iInfo == GlobalClass.DEALT_A_CARD)
                {
                    cMoveData cm = new cMoveData(GlobalClass.DEALT_A_CARD, -1, -1, -1, true, 0);
                    TheseMoves.Add(cm);
                    TheseMoves[TheseMoves.Count - 1].WhereInfo = ThisMoveInfo.Count;
                }
            }
            if (ThisMoveInfo.Count > 0)
            {
                int iCnt = ThisMoveInfo.Count - 1;
                if (ThisMoveInfo[iCnt].id == GlobalClass.DEALT_A_CARD && thisMI.id == GlobalClass.DEALT_A_CARD)
                {
                    iCnt = 0;
                }
            }
            ThisMoveInfo.Add(thisMI);
        }



        public int HasInfo(int i, ref string[] strOut)
        {
            Debug.Assert(i >= 0);
            cMoveInfo thisMI = ThisMoveInfo[i];
            int RtnCod = 0;
            //DateTime LastTimeOfWhatever = FindLastTime(i, thisMI.id);

            switch (thisMI.id)
            {
                case GlobalClass.FIRST_CARD:
                    strOut = new string[1];
                    strOut[0] = "First (" + thisMI.deal + ")";
                    RtnCod = 1;
                    break;
                case GlobalClass.DEALT_A_CARD:  // is always appended to the last move
                    strOut = new string[3];
                    strOut[0] = "Deal# " + thisMI.deal;
                    strOut[1] = "Min: " + thisMI.MinutesLastDeal;
                    strOut[2] = "Best:" + thisMI.score;
                    RtnCod = 2;
                    break;
                case GlobalClass.BUILT_SUIT:  // is always appended to the last move
                    //strOut = new string[3];
                    //strOut[0] = "Suit " + GlobalClass.SuitNames[thisMI.suit];
                    //strOut[1] = "Min:" + thisMI.MinutesLastDeal.ToString("0");
                    //strOut[2] = "By:" + utils.SetCompletedBy((GlobalClass.TypeCompletedBy)thisMI.suiter);
                    strOut = new string[1];
                    strOut[0] = "S" + GlobalClass.SuitNames[thisMI.suit].Substring(0,1) +
                        thisMI.MinutesLastDeal.ToString("0") +
                        utils.SetCompletedBy((GlobalClass.TypeCompletedBy)thisMI.suiter);
                    RtnCod = 3;
                    break;
                default: Debug.Assert(false);
                    break;
            }
            return RtnCod;
        }

        public void CopyMoves(ref cMoveClass cMC)
        {
            cMoveData cmd;
            cMoveInfo cmi;
            TheseMoves.Clear();
            ThisMoveInfo.Clear();
            for (int i = 0; i < cMC.TheseMoves.Count; i++)
            {
                cMoveData dcMC = cMC.TheseMoves[i];
                cmd = new cMoveData(dcMC.ID, dcMC.Src, dcMC.Des, dcMC.Score, dcMC.bWasFacedown, dcMC.NumMoved);
                cmd.ShrinkCode = dcMC.ShrinkCode;
                cmd.WhereInfo = dcMC.WhereInfo;
                TheseMoves.Add(cmd);
            }
            for (int i = 0; i < cMC.ThisMoveInfo.Count; i++)
            {
                cmi = new cMoveInfo();
                cmi.deal = cMC.ThisMoveInfo[i].deal;
                cmi.id = cMC.ThisMoveInfo[i].id;
                cmi.MinutesLastDeal = cMC.ThisMoveInfo[i].MinutesLastDeal;
                cmi.suit = cMC.ThisMoveInfo[i].suit;
                cmi.suiter = cMC.ThisMoveInfo[i].suiter;
                cmi.score = cMC.ThisMoveInfo[i].score;
                cmi.TimeOfMove = cMC.ThisMoveInfo[i].TimeOfMove;
                ThisMoveInfo.Add(cmi);
            }
            InitialCardTime = cMC.InitialCardTime;
            LastBuildTime = cMC.LastBuildTime;
            LastDealTime = cMC.LastDealTime;
            bOnFirstBuild = cMC.bOnFirstBuild;
            bOnFirstDeal = cMC.bOnFirstDeal;
        }



        public void TraceBoard()
        {
            TraceBoard(null);
        }

        public void TraceBoard(StreamWriter sw)
        {
            int i, j, k, e, r, NumMoves = TheseMoves.Count;
            int MaxRows = TheseMoves.Count + 10;    // leave room for info such as "Deal" after the move statements
            int padSize = 19;   // can use 13 if we abbreviate diamonds to diamon
            char[] charsToTrim = { ' ' };
            int nInfo;
            string strMove;
            string[,] page = new string[6, MaxRows];
            int[] lCnt = new int[6];
            string[] strMoveInfo = null;

            for (i = 0; i < 6; i++)
            {
                for (j = 0; j < MaxRows; j++) page[i, j] = utils.Rpadto(" ", padSize);
                lCnt[i] = 0;
            }

            e = 0;
            r = 0;
            for (i = 0; i < TheseMoves.Count; i++)
            {
                cMoveData cMD = TheseMoves[i];
                if (cMD.WhereInfo >= 0)
                {
                    nInfo = HasInfo(cMD.WhereInfo, ref strMoveInfo);
                }
                else nInfo = 0;
                string strTemp = "";
                if (nInfo == 3)
                    strTemp = strMoveInfo[0];
                strMove = utils.CVTMoveValueToText(cMD, strTemp);                
                page[e, r] = utils.Rpadto(strMove, padSize);
                lCnt[e]++;
                r++;
                if (nInfo == 2)
                {
                    Debug.Assert(strMoveInfo != null);
                    for (j = 0; j < strMoveInfo.Length; j++)
                    {
                        page[e, r] = utils.Rpadto(strMoveInfo[j], padSize);
                        lCnt[e]++;
                        r++;
                    }
                    if (nInfo == 2)
                    {
                        e++;
                        r = 0;
                    }
                }
            }
            k = 0;
            for (i = 0; i < 6; i++)
            {
                if (k < lCnt[i])
                    k = lCnt[i];
            }

            for (i = 0; i < k; i++)
            {
                strMove = utils.Lpadto(i.ToString(), 3) + " ";
                for (j = 0; j < 6; j++)
                    strMove += page[j, i];
                strMove = strMove.TrimEnd(charsToTrim);
                if (sw == null)
                    Console.WriteLine(strMove);
                else
                {
                    sw.WriteLine(strMove);
                }
            }
        }

        /*
         * fixes problems such as
         * JD 3->6  [s]  (uncovered card had same suit as the 3)
         * JD 6->3  [s]  (uncovered card had same suit as the 6)
         * the above cannot be replaced if it was used to expose a card
         * otherwise BOTH can be deleted
         * 
         * AC 9->3       if suits uncovered are different ..
         * AC 6->9       if cards above were already 
         * the above can be replace with AC 6->3 only if 
         * no cards were exposed
         * 
         * the number of cards moved are the same (1)
         * 
         * */
        public bool IsLastMoveDuplicated(ref cMoveData cMD)
        {
            int n = TheseMoves.Count-1;
            if(n < 0)return false;
            cMoveData LastMove = TheseMoves[n];
            bool bIsDup = false;
            

            return bIsDup;
        }

    }
}
