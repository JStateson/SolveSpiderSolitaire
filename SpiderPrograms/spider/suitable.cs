using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace spider
{
    public class csuitable
    {
        static readonly int ALL_AVAILABLE = Convert.ToInt32("11111111111110", 2);
        public string[] CardNames = new string[13] { "Ace", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Jack", "Queen", "King" };
        public string[] SuitNames = new string[4] { "Diamonds", "Clubs", "Hearts", "Spades" };
        public int NumberCompletable;
        public cDisassemble Disassemble;
        public class cExposeStack
        {
            public int Stack;
            public int NumToRemove;
            public int Rank;
            public int AtDeal;  // no longer need to expose after this deal as we get the card in the deal
            public cExposeStack(int iStack, int iNumToRemove, int iRank)
            {
                Stack = iStack;
                NumToRemove = iNumToRemove;
                Rank = iRank;
            }
        }


        public csuitable(ref cSpinControl cSC, ref cDisassemble Disassemble)
        {
            this.cSC = cSC;
            this.Disassemble = Disassemble;
            NumberCompletable = 0;
        }

 

        public int CostToComplete;
        public int DealsRequired;
        public List<cExposeStack> ExposesRequired = new List<cExposeStack>();
        public List<int> eMissingRanks = new List<int>();   // used with exposes required
        public List<int> dMissingRanks = new List<int>();   // used with deals required
        public List<int> CardsReqAfterDeal = new List<int>();  // how many cards are obtainable after each deal
        // an obtainable card may include a face down one
        private cExposeStack ces;
        private cSpinControl cSC;
        public bool PerformSuitabiltyStudy(ref board tb, int isuit)
        {

            column tCol;
            card tCrd;
            List<int> DupList = new List<int>();
            int cRankBits = ALL_AVAILABLE;
            int i, j, k, rBit;
            int DealCnt = 0;
            tb.BuildingThisSuit = 0;

            if (!tb.SuitStatus[isuit].bSuitsCompletable)
            {
                // calculate all missing ranks, keep a copy (dMissing)
                for (j = 1; j < 14; j++)
                {
                    rBit = 1 << j;
                    if ((rBit & tb.SuitStatus[isuit].SuitedRankBits) == 0)
                    {
                        eMissingRanks.Add(j);
                        dMissingRanks.Add(j);
                    }
                }
                // see if we can expose some cards so as to reduce the number of required (eMissing)
                for (i = 0; i < 10; i++)
                {
                    tCol = tb.ThisColumn[i];
                    if (tCol.Cards.Count < 2) continue; // there are no cards to expose
                    for (j = tCol.top - 1; j >= 0; j--)
                    {
                        tCrd = tCol.Cards[j];
                        if (isuit != tCrd.suit) continue;
                        if(DupList.Contains(tCrd.rank)) // must have put it in earlier
                        {
                            for (k = j + 1; k < tCol.Cards.Count; k++)
                            {
                                if (tCol.Cards[k].rank == 13) continue;
                            }
                            int nUncover = tCol.NumUnstacksReq(tCrd);
                            foreach (cExposeStack cES in ExposesRequired)
                            {
                                if (cES.Rank != tCrd.rank) continue;
                                if (cES.NumToRemove > nUncover)
                                {
                                    cES.NumToRemove = nUncover;
                                    cES.Stack = i;
                                }
                            }
                        }
                        if (eMissingRanks.Contains(tCrd.rank))
                        {
                            eMissingRanks.Remove(tCrd.rank);
                            DupList.Add(tCrd.rank);
                            // if a king is below this face down card then assume it cannot be exposed
                            for (k = j + 1; k < tCol.Cards.Count; k++)
                            {
                                if (tCol.Cards[k].rank == 13) continue;
                            }
                            cExposeStack ces = new cExposeStack(i, tCol.NumUnstacksReq(tCrd), tCrd.rank);
                            ExposesRequired.Add(ces);
                        }
                    }
                }
                

                if (eMissingRanks.Count > 0) // seems cannot get a suit unless we deal
                {
                    tCol = tb.ThisColumn[10];

                    // for each deal, go thru the available deck and see if we can form a complete suit
                    // if a card from the deck duplicates one that is facedown then remove the facedown one
                    // from the exposes required list
                    for (i = 0; i < tCol.Cards.Count; i++)
                    {
                        tCrd = tCol.Cards[i];
                        if (0 == (i % 10))
                        {
                            DealCnt++;
                            if (eMissingRanks.Count > 0)
                            {
                                DealsRequired++;
                            }
                            if(eMissingRanks.Count>0)
                                CardsReqAfterDeal.Add(eMissingRanks.Count);
                        }

                        if (tCrd.suit != isuit) continue;
                        if (eMissingRanks.Contains(tCrd.rank))
                        {
                            eMissingRanks.Remove(tCrd.rank);
                            // no need to expose any cards if they will be in the deal
                            for (j = 0; j < ExposesRequired.Count; j++)
                            {
                                ces = ExposesRequired[j];
                                if (dMissingRanks.Contains(ces.Rank))
                                {
                                    //ExposesRequired.RemoveAt(j);
                                    ExposesRequired[j].AtDeal = DealCnt;
                                }
                            }
                        }
                        //if (eMissingRanks.Count == 0) break;
                    }
                    if(eMissingRanks.Count>0)
                        CardsReqAfterDeal.Add(eMissingRanks.Count);
                }
            }

            if(tb.SuitStatus[isuit].bSuitsCompletable)
                    NumberCompletable++;
            return tb.SuitStatus[isuit].bSuitsCompletable;
        }

        // find the sequence that contains the ExpectedRank.  if more than one do the same analysis as the king
        private series sGetNext(ref board tb, ref List<series> SortedSEQ, int ExpectedRank, ref int cost)
        {
            series s1 = null, s2 = null;
            bool bFound1 = false, bFound2 = false;
            card c1 = null, c2 = null;
            column cCol;
            int cost1, cost2;
            foreach (series s in SortedSEQ)
            {
                if (s.topCard.rank >= ExpectedRank && ExpectedRank >= s.bottomCard.rank)
                {
                    if (!bFound1)
                    {
                        s1 = s;
                        bFound1 = true;
                        cCol = tb.ThisColumn[s.iStack];
                        for (int i = s.top; i <= s.bottom; i++)
                        {

                            c1 = cCol.Cards[i];
                            if (c1.rank == ExpectedRank) break;
                        }
                        continue;
                    }
                    else
                    {
                        s2 = s;
                        bFound2 = true;
                        cCol = tb.ThisColumn[s.iStack];
                        for (int i = s.top; i <= s.bottom; i++)
                        {
                            c2 = cCol.Cards[i];
                            if (c2.rank == ExpectedRank) break;
                        }
                        break;
                    }
                }
            }
            if (bFound1 && bFound2)
            {
                cost1 = s1.nEmptiesToUnstack - c1.next;
                cost2 = s2.nEmptiesToUnstack - c2.next;
                if (cost2 <= cost1)
                {
                    s1 = s2;
                    c1 = c2;
                }
            }
            cost = s1.nEmptiesToUnstack;
            s1.AssemblyLink = c1;
            return s1;
        }

        // if the kings are in the same stack they will still be different SEQ and the lower king
        // will always have 1 better cost to unstack.  If the size of K2 is the same (or greater than K1)
        // then K2 will be selected.  If K1 is 1 or more greater in size then K2 must be discarded
        private series sGetKing(ref List<series> SortedSEQ, ref int cost )
        {
            series sKing = null;
            int rank = SortedSEQ[0].topCard.rank;
            Debug.Assert(rank == 13);
            bool bHaveTwoKings = (SortedSEQ[1].topCard.rank==13);
            if (bHaveTwoKings)
            {
                int costK1 = SortedSEQ[0].nEmptiesToUnstack - SortedSEQ[0].size;
                int costK2 = SortedSEQ[1].nEmptiesToUnstack - SortedSEQ[1].size;
                if (costK2 <= costK1)
                {
                    cost = SortedSEQ[1].nEmptiesToUnstack;
                    sKing = SortedSEQ[1];
                }
                else
                {
                    cost = SortedSEQ[0].nEmptiesToUnstack;
                    sKing = SortedSEQ[0];
                }
                SortedSEQ.RemoveRange(0, 2);
                return sKing;
            }
            sKing = SortedSEQ[0];
            SortedSEQ.RemoveAt(0);
            return sKing;
        }


        private int FormCostToComplete(ref board tb, int suit)
        {
            List<series> SortedSEQ = tb.BuildSortedSEQ(1<<suit,GlobalClass.eSortedSEQtype.SortByRank);
            SortedSEQ.Sort(delegate(series s1, series s2)
            {
                return Comparer<int>.Default.Compare(s2.topCard.rank, s1.topCard.rank);
            });
            tb.SuitedSEQ.Clear();
            if (SortedSEQ == null || SortedSEQ.Count < 2)
            {
                return 0;
            }
            int cost = 0, rank;
            series ThisSEQ, PrevSEQ;

            ThisSEQ = sGetKing(ref SortedSEQ, ref cost);
            tb.SuitedSEQ.Add(ThisSEQ);
            rank = ThisSEQ.bottomCard.rank - 1;
            while (rank > 0)
            {
                PrevSEQ = ThisSEQ;
                if (SortedSEQ == null || SortedSEQ.Count < 2)
                {
                    return 1;
                }
                ThisSEQ = sGetNext(ref tb, ref SortedSEQ, rank, ref cost);
                PrevSEQ.AssemblyLink = ThisSEQ.AssemblyLink;
                tb.SuitedSEQ.Add(ThisSEQ);
                rank = ThisSEQ.bottomCard.rank - 1;
            }
            ThisSEQ.AssemblyLink = null;
            return cost;
        }



        // this looks for available spots to stuff a card and if it cannot find one it suggests an empty stack
        // this is different than the one in strategy because it can use the default BottomMost. This is true
        // because we are leaving the list unchanged.
        private int GetBestMove(card c, ref board tb)
        {
            int i, n;
            int iSameSuit = -1;
            int iAnySuit = -1;
            int ToStack = -1;
            n = tb.BottomMost.Count;
            for (i = 0; i < n; i++)
            {
                if (tb.BottomMost[i].ExcludePlaceholder) continue;    // cannot use this
                if (tb.BottomMost[i].rank - 1 == c.rank)
                {

                    if (tb.BottomMost[i].suit == c.suit)
                    {
                        iSameSuit = i;
                        ToStack = tb.BottomMost[i].iStack;
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
            ToStack = tb.BottomMost[iAnySuit].iStack;
            return ToStack;
        }
        // free up a column by joining any SEQ that are joinable
        // or moves one suited sequence under another of proper rank
        // this only looks at the bottom card and tries to find an SEQ that can fit under it
        // of same suit
        public bool FreeSuitedSuits(ref board tb, ref List<card> LastSEQ)
        {
            bool bFound = false;
            column cCol;
            card Src = null;
            card Des = null;
            tb.AssignCompletedID(GlobalClass.TypeCompletedBy.ID_FSS);
            do
            {
                bFound = false;
                LastSEQ.Clear();
                foreach (int e in tb.NonEmpties)
                {
                    cCol = tb.ThisColumn[e];
                    LastSEQ.Add(cCol.ThisSeries.Last().topCard);
                }
                //LastSEQ.Sort(delegate(series s1, series s2)
                //{
                //    return Comparer<int>.Default.Compare(s1.topCard.rank, s2.topCard.rank);
                //});

                // see if any suits can be combined
                foreach (card tm in tb.BottomMost)
                {
                    foreach (card bm in LastSEQ)
                    {
                        if (tm.iStack == bm.iStack || tm.rank==1) continue;
                        if ((bm.rank+1) == tm.rank && bm.suit == tm.suit)
                        {
                            bFound = true;
                            Des = tm;
                            Src = bm;
                            break;
                        }
                    }
                    if (bFound) break;
                }
                if (bFound)
                {
                    tb.moveto(Src.iStack, Src.iCard, Des.iStack);
                    tb.ReScoreBoard();
                    if (tb.NotifySuitJustCompleted)
                    {
                        tb.ExplainNoRescoreBoard("Free Suited Suits succeeded");
                        tb.NotifySuitJustCompleted = false;
                    }
                }
            } while (bFound);
            tb.AssignCompletedID();
            return false;
        }


        // exchange cards that are under the wrong suit
        public bool CombineLikeSuits(ref board tb)
        {
            column cCol = null;
            int n=0;
            bool ChangedAnyBoards = false;
            int OriginalSrcStack=0;
            card bm, Src=null, Des=null;
            bool bFound = false, bAny = false;
            List<card> LastSEQ = new List<card>();  // this holds the top card of the last SEQ series
            card BelowBM, cAbove;
            // Below Bm rank + 1 must equal to cAbove rank to swap (unless one card is top)

            tb.AssignCompletedID(GlobalClass.TypeCompletedBy.ID_COMBN);
            do
            {
                bFound = false;
                LastSEQ.Clear();
                bAny = FreeSuitedSuits(ref tb, ref LastSEQ);
                if (tb.NumEmptyColumns == 0)
                {
                    return bAny;
                }


                //tb.ExplainBoard("before");

                // locate the bottom card of the upper series, if any
                foreach (int e in tb.NonEmpties)
                {
                    cCol = tb.ThisColumn[e];
                    n = cCol.ThisSeries.Count;
                    if (n < 2) continue;              
                    bm = cCol.ThisSeries[n - 2].bottomCard;
                    foreach (card tm in LastSEQ)
                    {
                        if (tm.iStack == bm.iStack) continue;
                        if ((bm.rank - 1) == tm.rank && bm.suit == tm.suit)
                        {
                            OriginalSrcStack = tm.iStack;
                            int iCardAbove = tm.iCard - 1; // this card must either be null (top of column) 
                                                            // or it must be 1 greater in rank
                            if (iCardAbove > -1)    // there is at least one card above us.  It can be facedown too
                            {
                                cAbove = tb.ThisColumn[tm.iStack].Cards[iCardAbove];
                                if (cAbove.rank != (tm.rank + 1)) continue;
                                BelowBM = cCol.Cards[bm.iCard + 1];
                                if (BelowBM.rank != (cAbove.rank - 1)) continue;
                            }
                            bFound = true;
                            Src = tm;
                            Des = bm;
                            break;
                        }
                    }
                    if (bFound == true) break;
                }
                if (bFound)
                {
                    card MustMove = cCol.ThisSeries[n - 1].topCard;
                    int nDest = GetBestMove(Src, ref tb);

                    if (nDest < 0)
                    {
                        nDest = tb.Empties[0];
                    }
                    tb.moveto(MustMove.iStack, MustMove.iCard, nDest);
                    tb.moveto(Src.iStack, Src.iCard, Des.iStack);
                    // do not bother moving from one top to another top when doing a swap
                    if (!(tb.ThisColumn[nDest].Cards.Count == 1 &&
                       tb.ThisColumn[OriginalSrcStack].Cards.Count == 0))
                    {
                        tb.moveto(nDest, MustMove.iCard, OriginalSrcStack);
                    }

                    tb.ReScoreBoard();
                    if (tb.NotifySuitJustCompleted)
                    {
                        tb.ExplainNoRescoreBoard("Combine Like Suits succeeded");
                        tb.NotifySuitJustCompleted = false;
                    }
                    ChangedAnyBoards = true;
                }
            } while (bFound == true);

            tb.AssignCompletedID();
            //tb.ExplainBoard("after2");
            return ChangedAnyBoards;
        }


        // this spins off up to 4 boards, one for each suit if that suit can be reduced
        // we spin into NextBoardSeries
        public void PerformSuitabilityScreening(ref board tb)
        {
            int i;
            CombineLikeSuits(ref tb);   // this does a rescore which sets bIsCompletable and sets all rank bit
            for (i = 0; i < 4; i++)
            {
                if (tb.SuitStatus[i].bSuitsCompletable)
                {
                    board nb = new board(ref tb);
                    nb.ReScoreBoard();
                    nb.BuildingThisSuit = 1 + i;
                    FormCostToComplete(ref  nb, i);
                    cSC.NextBoardSeries.Add(nb);
                }
            }
        }
    }
}
