using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace spider       
{
    public static class utils
    {
        public static bool[] SuitCompletions = new bool[4];
        public static void AnnounceCompletion(int suit, string cmnt)
        {
            if (SuitCompletions[suit])
            {
                Console.WriteLine(cmnt);
            }
            {
                SuitCompletions[suit] = false;
            }
        }

        public static bool bGetSetMessageGivenStatus(int suit)
        {
            bool bNotGiven = SuitCompletions[suit];
            SuitCompletions[suit] = false;
            return bNotGiven;
        }

        // do the following on a new deal
        public static void InitSuitCompletions()
        {
            for (int i = 0; i < 4; i++)
                SuitCompletions[i] = true;
        }

        private static char GetSuitChar(int iSuit)
        {
            char ch = Convert.ToChar(GlobalClass.cSuits.Substring(iSuit, 1));
            return ch;
        }
        private static string GetRankChar(int iRank)
        {
            string strName = GlobalClass.rNames.Substring(iRank * 2, 2);
            return strName.ToString();
        }

        private static string GetFormattedName(int iSuit, int iRank, bool bFaceup)
        {
            string ThisSuit = GetSuitChar(iSuit).ToString();
            string ThisRank = GlobalClass.cRanks.Substring(iRank-1,1); //GetRankChar(iRank);
            if (bFaceup)
            {
                ThisSuit = ThisSuit.ToUpper();
                ThisRank = ThisRank.ToUpper();
            }
            string strCardName = ThisRank + ThisSuit;
            return strCardName;

        }

        //public static string CVTMoveValueToText(cMoveData cMD)
        //{
        //    string strText;
        //    int CardType;
        //    int irank, isuit, src, des;
        //    CardType = cMD.ID;
        //    if (CardType > 51)
        //        CardType -= 52;
        //    isuit = GlobalClass.cRST[CardType].suit;
        //    irank = GlobalClass.cRST[CardType].rank;
        //    des = cMD.Des;
        //    src = cMD.Src;
        //    string strInfo = "";
        //    string strNumMoved = (cMD.NumMoved == 1) ? "" : cMD.NumMoved.ToString();
        //    if (cMD.bWasFacedown)
        //    {
        //        if (cMD.NumMoved == 1)
        //            strInfo = "[f]";
        //        else strInfo = "[f" + cMD.NumMoved.ToString() + "]";
        //    }
        //    else if (cMD.NumMoved > 1)
        //        strInfo = "[" + cMD.NumMoved.ToString() + "]";

        //    strText = utils.GetFormattedName(isuit, irank, true) + " " + (src + 1) + "->" + (des + 1);
        //    return utils.Rpadto(strText, 10) + strInfo;
        //}


        public static string CVTMoveValueToText(cMoveData cMD, string SuitCompleted)
        {
            string strText;
            int CardType;
            int irank, isuit, src, des;
            CardType = cMD.ID;
            if (CardType > 51)
                CardType -= 52;
            isuit = GlobalClass.cRST[CardType].suit;
            irank = GlobalClass.cRST[CardType].rank;
            des = cMD.Des;
            src = cMD.Src;
            // jys 8nov2012
            des++; if (des == 10) des = 0;
            src++; if (src == 10) src = 0;
            string strInfo = "";

#if SHOW_FACE_INFO
            string strNumMoved = (cMD.NumMoved == 1) ? "" : cMD.NumMoved.ToString();
            if (cMD.bWasFacedown)
            {
                if (cMD.NumMoved == 1)
                     strInfo = "[f]";
                else strInfo = "[f" + cMD.NumMoved.ToString() + "]";
            }
            else if(cMD.NumMoved > 1)
                strInfo = "[" + cMD.NumMoved.ToString() + "]";

            strInfo = utils.Rpadto(strInfo, 5);
#endif

            if (cMD.ShrinkCode != "")
            {
                strInfo = "[" + cMD.ShrinkCode + "]";
            }
            if (SuitCompleted == "")
                SuitCompleted = utils.Lpadto(cMD.Score.ToString(), 5) + strInfo;
            strText = utils.GetFormattedName(isuit, irank, true) + " " + (src) + "-" + (des);
            return utils.Rpadto(strText, 7) + SuitCompleted;
        }

        public static int GetIDFromMoveValue(cMoveData cMD)
        {
            return cMD.ID;   // 0..103
        }    

        public static pseudoCard CVTMoveValueToInfo(cMoveData cMD)
        {
            pseudoCard pCard = new pseudoCard(cMD.ID);
            pCard.score = cMD.Score;
            pCard.desID = cMD.Des;
            pCard.iStack = cMD.Src;
            pCard.desID_is_iStack = true;
            return pCard;
        }

        // note that PlaceHolders is modified:  a card is removed
        // this looks for available spots to stuff a card and if it cannot find one it suggests an empty stack
        public static int GetBestMove(card c, ref board tb, ref List<card>PlaceHolder)
        {
            int i, n;
            int iSameSuit = -1;
            int iAnySuit = -1;
            int ToStack = -1;
            n = PlaceHolder.Count;
            for (i = 0; i < n; i++)
            {
                if (PlaceHolder[i].ExcludePlaceholder) continue;    // cannot use this
                if (PlaceHolder[i].rank - 1 == c.rank)
                {

                    if (PlaceHolder[i].suit == c.suit)
                    {
                        iSameSuit = i;
                        ToStack = PlaceHolder[i].iStack;
                        PlaceHolder.RemoveAt(i);
                        break;
                    }
                    iAnySuit = i;
                }
            }
            if (iAnySuit < 0 && iSameSuit < 0) return -1;   // signal to use an empty column

            if (iSameSuit >= 0)
            {
                return ToStack;
            }
            ToStack = PlaceHolder[iAnySuit].iStack;
            PlaceHolder.RemoveAt(iAnySuit);
            return ToStack;
        }



        public static string SetCompletedBy(GlobalClass.TypeCompletedBy cb)
        {
            string CompletedBy = "UNK";
            switch (cb)
            {
                case GlobalClass.TypeCompletedBy.ID_UNK: CompletedBy = "UNK"; break;
                case GlobalClass.TypeCompletedBy.ID_JSS: CompletedBy = "JSS"; break;
                case GlobalClass.TypeCompletedBy.ID_SOSET: CompletedBy = "SOSet"; break;
                case GlobalClass.TypeCompletedBy.ID_BS: CompletedBy = "BS"; break;
                case GlobalClass.TypeCompletedBy.ID_RSI: CompletedBy = "RS"; break;
                case GlobalClass.TypeCompletedBy.ID_COMBN: CompletedBy = "CmbLS"; break;
                case GlobalClass.TypeCompletedBy.ID_FSS: CompletedBy = "FreeSS"; break;
                default:
                    break;
            }
            return CompletedBy;
        }

        public static int CalcSEQSeriesValue(series s, int BoardState)
        {
            double x, y = 2.33;// that gives 5 for two suited cards and 2.69 was for using only one series
            int v, n = s.bottom - s.top;
            if (n == 12)
            {
                s.nValue = 1000;
                s.sSuit = s.topCard.suit;
                return 1000;
            }
            if (n == 0)
            {
                v = 1;
            }
            else
            {
                x = n + 1;
                x = Math.Pow(x, y);
                v = Convert.ToInt32(x);
            }
            s.sSuit = s.topCard.suit;
            // 8nov2012 add value of top card and boost extra if rank was 12
            if (s.topCard.rank == 13) v += 20 * (n+1);
            v += s.topCard.rank;
            s.nValue = v;
            if (s.size == 1)
            {
                // there is only one card in the series. if ace or king give it 0 value
                int nOne = (s.topCard.rank == 13 || s.topCard.rank == 2) ? 0 : 1;
                // if the suit is buildable then use its rank as shown above
                if ((BoardState & 0xf & (1<<s.sSuit)) > 0)
                {
                    return v;
                }
                v = nOne;
                s.nValue = v;
            }
            return v;
        }

        public static int CalcSOSSeriesValue(series s)
        {
            int v, n = s.bottom - s.top;
            s.sSuit = -1;
            v = 1;
            if (n > 0)
            {
                v += n;
            }
            s.nValue = (v);
            return v;
        }


        public static bool tbsCompareSeeds(ref cSpinControl cSC)
        {
            int i, j, n = cSC.ThisSeed.Seeds.Count;
            bool bResult = true;
            if (n > GlobalClass.MIN_FILTERED_BOARDS)
                n = GlobalClass.MIN_FILTERED_BOARDS;
            if (n < 2) return true;
            for (i = 0; i < n - 1; i++)
            {
                for (j = i+1; j < n; j++)
                {
                    bResult &= tbsCompareAll(ref cSC, i, j);
                }
            }
            return bResult;
        }

        public static bool tbsCompareAll(ref cSpinControl cSC, int id1, int id2)
        {

            bool bSame1 = tbsCompare(ref cSC, id1, id2);
            bool bSame2 = cSC.stlookup.cstCompare(id1, id2);
            if (bSame1 && bSame2)
            {
                Console.WriteLine("ERROR:  Boards are identical! " + id1 + " " + id2);
                Console.WriteLine("        Unique IDs are: " + cSC.ThisBoardSeries[id1].UniqueID + " " + cSC.ThisBoardSeries[id2].UniqueID);
            }
            return (bSame1 && bSame2);
        }

        public static bool tbsCompareAny(ref cSpinControl cSC, int id1, int id2)
        {

            bool bSame1 = tbsCompare(ref cSC, id1, id2);
            bool bSame2 = cSC.stlookup.cstCompare(id1, id2);
            if (bSame1)
            {
                Console.WriteLine("ERROR:  Boards are identical! " + id1 + " " + id2);
                Console.WriteLine("        Unique IDs are: " + cSC.ThisBoardSeries[id1].UniqueID + " " + cSC.ThisBoardSeries[id2].UniqueID);
            }
            if (bSame2)
            {
                Console.WriteLine("ERROR:  Lookups are identical! " + id1 + " " + id2);
                Console.WriteLine("        Unique IDs are: " + cSC.ThisBoardSeries[id1].UniqueID + " " + cSC.ThisBoardSeries[id2].UniqueID);
            }
            return bSame1 || bSame2;
        }

        private static bool tbsCompare(ref cSpinControl cSC, int i1, int i2)
        {
            int i, j, k, n = cSC.ThisBoardSeries.Count;
            if (n < 2) return true;
            board nb1 = cSC.ThisBoardSeries[i1];
            board nb2 = cSC.ThisBoardSeries[i2];

            if (nb1.NonEmpties.Count != nb2.NonEmpties.Count) return false;
            if (nb1.Empties.Count != nb2.Empties.Count) return false;
            for (i = 0; i < nb1.NonEmpties.Count; i++)
            {
                k = nb1.NonEmpties[i];
                n = nb1.ThisColumn[k].Cards.Count;
                if (n != nb2.ThisColumn[k].Cards.Count) return false;
                for (j = 0; j < n; j++)
                {
                    if (nb1.ThisColumn[k].Cards[j].rank != nb2.ThisColumn[k].Cards[j].rank) return false;
                    if (nb1.ThisColumn[k].Cards[j].suit != nb2.ThisColumn[k].Cards[j].suit) return false;
                    if (nb1.ThisColumn[k].Cards[j].bFaceUp != nb2.ThisColumn[k].Cards[j].bFaceUp) return false;                    
                }
            }
            return true;
        }

        private static int nFactorial(int n)
        {
            if (n < 2) return 1;
            return n * nFactorial(n - 1);
        }

        
        public static GlobalClass.StrategyType  UseThisManyBoards(ref cSpinControl cSC, ref int NumToUse)
        {
            int i, n = cSC.ThisBoardSeries.Count;
            int iBiggest = 0;
            int[] tsbEmptyDistribution = new int[11] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
            int[] sedEmptyDistribution = new int[11] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            Debug.Assert(cSC.ThisBoardSeries.Count > 0);
            Debug.Assert(cSC.ThisSeed.SeedIndex.Count > 0);



            foreach(board tb in cSC.ThisBoardSeries)
            {
                tsbEmptyDistribution[tb.Empties.Count]++;
            }
            for (i = 0; i < cSC.ThisSeed.SeedIndex.Count; i++)
            {
                board tb = cSC.ThisBoardSeries[cSC.ThisSeed.SeedIndex[i]];
                sedEmptyDistribution[tb.Empties.Count]++;
            }

            for (i = 10; i >= 1; i--)
            {
                if (sedEmptyDistribution[i] > 0)
                {
                    iBiggest = i;
                    break;
                }
            }

            if (iBiggest == 0)
            {
                NumToUse = GlobalClass.MAX_FILTERED_BOARDS;    // if no empty columns run thru all boards
                return GlobalClass.StrategyType.CONTINUE_SPINNING;
            }
            if (iBiggest <= 3)
            {
                NumToUse = GlobalClass.MIN_FILTERED_BOARDS;
                return GlobalClass.StrategyType.CONTINUE_SPINNING;
            }


            if (iBiggest > 3 && cSC.ThisBoardSeries[0].bIsCompletable)
            {
                NumToUse = 0;
                return GlobalClass.StrategyType.REDUCE_SUITS; // stop the spinning - switch to end game
            }

            // UNTIL I FIX JOINSUITS
            //if (iBiggest == 3)
            //{
            //    NumToUse = 10;
            //    return GlobalClass.StrategyType.SPIN_JOINSUITS;
            //}
            //if (iBiggest == 2)
            //{
            //    NumToUse = GlobalClass.MIN_FILTERED_BOARDS;
            //    return GlobalClass.StrategyType.RUN_JOINSUITS_INPLACE;
            //}

            //foreach (board tb in cSC.ThisBoardSeries)
            //{
            //    if (tb.NumEmptyColumns == 2)
            //        Console.WriteLine(tb.ID + " " + tb.score + " ");
            //}

            NumToUse = GlobalClass.MIN_FILTERED_BOARDS;
            return GlobalClass.StrategyType.CONTINUE_SPINNING;
        }

        public static void SetSuitableValues(ref cSpinControl cSC, ref board tb)
        {
            if (tb.bIsCompletable)
            {
                if (cSC.MaxValues.MaxInserts != cSC.MaxValues.MaxInsertsWhenSuiting)
                    Console.WriteLine("Switching to max resources");
                cSC.MaxValues.MaxInserts = cSC.MaxValues.MaxInsertsWhenSuiting;
                cSC.MaxValues.TimeOutBest = cSC.MaxValues.MaxTimeoutWhenSuiting;

            }
            else
            {
                if (cSC.MaxValues.MaxInserts != cSC.MaxValues.MinInserts)
                    Console.WriteLine("Switching to min resources");
                cSC.MaxValues.MaxInserts = cSC.MaxValues.MinInserts;
                cSC.MaxValues.TimeOutBest = cSC.MaxValues.MinTimeout;

            }
        }

        // return true if an out of order series can be put into order
        // suit makes no difference
        public static bool bRankable(ref series s, ref board tb)
        {
            int i, d, r = s.topCard.rank;
            column tCol = tb.ThisColumn[s.iStack];
            int n = s.size;
            int[] CardIndex = new int[n];
            for (i = 0; i < n; i++)
            {
                CardIndex[i] = tCol.Cards[s.topCard.iCard + i].rank;

            }
            Array.Sort(CardIndex);
            for (i = 1; i < n; i++)
            {
                d = Math.Abs(CardIndex[i] - CardIndex[i - 1]);
                if (d != 1) return false;
            }
            return (r == CardIndex[n-1]);   // the one to be exposed must be the biggest
        }

        // need this as moveing card about we lose where the card actually is
        public static card FindCardFromID(ref board tb, int CardID)
        {
            foreach (column cCol in tb.ThisColumn)
            {
                if (cCol.iStack > 9) break;
                foreach (card cCrd in cCol.Cards)
                {
                    if (cCrd.ID == CardID)
                    {
                        return cCrd;
                    }
                }
            }
            Debug.Assert(false);
            return null;
        }

        public static bool GetCurrentLocation(ref board tb, ref pseudoCard pCard)
        {
            pCard.size = -1;
            int LastRank= 0, LastSuit = -1;
            foreach (column cCol in tb.ThisColumn)
            {
                if (cCol.iStack > 9) break;
                foreach (card cCrd in cCol.Cards)
                {
                    if (cCrd.ID == pCard.ID)
                    {
                        pCard.iStack = cCrd.iStack;
                        pCard.iCard = cCrd.iCard;
                        pCard.size = 0;
                        LastRank = cCrd.rank;
                        LastSuit = cCrd.suit;
                    }
                    if (pCard.size >= 0)
                    {
                        Debug.Assert(LastRank == (cCrd.rank+1) && (LastSuit == cCrd.suit));
                        if (LastRank == (cCrd.rank + 1) && (LastSuit == cCrd.suit)) return false;
                        pCard.size++;
                        LastRank = cCrd.rank;
                        LastSuit = cCrd.suit;
                    }
                }
                if (pCard.size > 0) return true;
            }
            Debug.Assert(false);
            return false;
        }

        public static int FindCardTypeFromInfo(int rank, int suit)
        {
            for (int i = 0; i < 52; i++)
            {
                if (GlobalClass.cRST[i].suit == suit &&
                    GlobalClass.cRST[i].rank == rank)
                {
                    return i;
                }
            }
            //Debug.Assert(false);
            //16nov2012 no assert since there may not be a suit left on the board!
            return -1;
        }

        private class cRE
        {
            public int Cnt;
            public int Loc;
            public cRE(int Cnt, int Loc)
            {
                this.Cnt = Cnt;
                this.Loc = Loc;
            }
        }


        // 17nov2012 would like to order duplicate column sizes by suits so that 1H in column 2 is same as 1H in column 5
        // this prevents more duplicate boards from showing up
        public static int ReOrderDups(int desptr, ref int[] des)
        {
            int[] TempDes = new int[102];
            cRE[] REsort = new cRE[10];
            int NumInREsort = 10;
            int i=0, j=0, k;
            int iTD;    // indexes 
            int jTD=0;

            while (i < 10)
            {
                k = des[j];
                if (k == 0) NumInREsort--;
                REsort[i] = new cRE(k, jTD);
                for (iTD = 0; iTD < k; iTD++)
                {
                    TempDes[jTD++] = des[iTD + j + 1];
                }
                j += (1+k);
                i++;
            }
            Array.Sort(REsort, delegate(cRE c1, cRE c2)
            {
                return c2.Cnt.CompareTo(c1.Cnt);
            });
            // put same size columns in order of suits "dchs"
            bool bMore = (NumInREsort > 1);
            while (bMore)
            {
                bMore = false;
                for (i = 0; i < NumInREsort-1; i++)
                {
                    if (REsort[i].Cnt != REsort[i+1].Cnt) continue;
                    // there are two columns with the same number of cards: i and i+1
                    // check the first card for suit and make sure it is less than the other column
                    int L1 = REsort[i].Loc;
                    int S1 = TempDes[L1] & 0x30;
                    int L2 = REsort[i + 1].Loc;
                    int S2 = TempDes[L2] & 0x30;
                    // bits b5_4 are the suit  110000 = 0x30
                    if (S1 <= S2) continue;
                    // swap locations
                    REsort[i].Loc = L2;
                    REsort[i + 1].Loc = L1;
                    bMore = true;
                }
            }

            j = 0;
            iTD = 0;
            do
            {
                i = REsort[j].Cnt;
                if (i == 0) break;
                des[iTD++] = i;
                for (k = 0; k < i; k++)
                {
                    des[iTD++] = TempDes[REsort[j].Loc + k];
                }
                j++;
                if (j == 10) break;
            }
            while(true);
            return iTD; // desptr;  // actually leaves empties at end of file, does not remove
            // 17nov2012 if a column is 0 then essentially the cards are shifted up BUT the last card of the last column
            // is left is position.  thus desptr needs to be decremented by the number of empty columns
            // or simply return iTD ??? instead of desptr
        }



        public static int hasPlaceholder(ref card topCard, ref board tb, ref List<card> PlaceHolder)
        {
            foreach (card c in PlaceHolder)
            {
                if (c.ExcludePlaceholder) continue;
                if ((c.rank - 1) == topCard.rank)
                {
                    c.ExcludePlaceholder = true;
                    return 1;
                }
            }
            return 0;
        }

        public static void FreeExcludedPlaceholders(ref List<card> PlaceHolder)
        {
            foreach (card c in PlaceHolder)
            {
                c.ExcludePlaceholder = false;
            }
        }

        public static string Rpadto(string strIn, int cnt)
        {
            int i = cnt - strIn.Length;
            if (i < 0) return strIn.Substring(0, cnt);
            return strIn + "                              ".Substring(0, i);
        }

        public static string Lpadto(string strIn, int cnt)
        {
            int i = cnt - strIn.Length;
            if (i < 0) return strIn.Substring(0, cnt);
            return "                              ".Substring(0, i) + strIn;
        }

        public static char GetShrinkCode(ref cSpinControl cSC)
        {
            if (cSC.bExceededCountLimit) return 'L';
            if (cSC.bExceededONEcolLimit) return '1';
            if (cSC.bExceededTWOcolLimit) return '2';
            if (cSC.bExceededThreecolLimit) return '3';
            if (cSC.bSpinTimedOut) return 'T';
            if (cSC.bSpinDidAllBoards) return 'D';
            if (cSC.bOutOfSpaceInSTLOOKUP) return 'S';
            if (cSC.bGotOneSuitAtLeast) return 'C';
            Debug.Assert(false);
            return ' ';
        }
    }
}