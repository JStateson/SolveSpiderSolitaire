using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace spider
{


    public class series
    {
        public card topCard;
        public card bottomCard;
        public int SubSeries;   // starting subseries of the series of series
        public int NumSubSeries;    // number of series in the series of series
        public int iStack;      // where it came from
        public int tag;
        public int size;
        public int nEmptiesToUnstack;   // if 1 then we need an empty column to move this series 2:two empties
        // note that a rank higher can be used instead of an empty stack
        public series NextSeries;
        public bool bRankable;
        /*
         * rankable as in the series ...
(5..4),1 is not rankable and cost/finalcost is 2/2
(3..2),(5,,4) has the pattern AB with cost/final of 1/1
(3..2),(5,,4),1 has pattern BCA and 2/1
1,(3..2),(5..4) has pattern ABC and 1/1
         * */
        public int sSuit;
        public int top;
        public int bottom;
        public int nValue;
        public card AssemblyLink;   // link to the card that is to follow our bottom card when assembling

        // the following pattern stuff when used with SOS are for cards
        // when used with SEQ are for rankable series
        public string pattern;
        public int[] PatternCount;

        public series()
        {
            bRankable = false;
        }

        /*
        10H..8H,7D is pattern BA because all of B is SEQ and A fits under the end of B
        another one
        10H
        9S-8S
        9C-6C
        6S
        5C
        KC..JC
        10H..9H
        7D
        */
        // this is either A, BA, CBA, etc since we are dealing with a single series
        // that is sequential
        public int FormPatternSOS(ref List<card> Cards)
        {

            int LastSuit, ThisSuit;
            int LastRank, ThisRank;
            string LastPattern = "";
            List<int> LastPatternCount = new List<int>();
            int A = Convert.ToInt32('A');
            int i, k, j = 0;
            char aChar;
            int NumSuitChanges = 0;
            pattern = "";
            if (Cards.Count == 0) return 0;
            int n = bottomCard.iCard - topCard.iCard + 1;


            if (n == 1)
            {
                pattern = "A";
                PatternCount = new int[1];
                PatternCount[0] = 1;
                return 0;
            }
            LastPattern = "A";

            LastSuit = bottomCard.suit;
            LastRank = bottomCard.rank;
            k = 1;   // count number of like suits
            for (i = 1; i < n; i++)
            {
                ThisSuit = Cards[bottomCard.iCard - i].suit;
                ThisRank = Cards[bottomCard.iCard - i].rank;
                Debug.Assert((ThisRank - LastRank) == 1);
                if (ThisSuit != LastSuit)
                {
                    NumSuitChanges++;
                    LastPatternCount.Add(k);
                    pattern = LastPattern + pattern;
                    aChar = Convert.ToChar(A + ++j);
                    LastPattern = aChar.ToString();
                    k = 1;
                }
                else k++;
                LastSuit = ThisSuit;
                LastRank = ThisRank;
            }
            LastPatternCount.Add(k);// +k.ToString("00");
            pattern = LastPattern + pattern;
            PatternCount = new int[pattern.Length];
            for (i = 0; i < pattern.Length; i++)
            {
                PatternCount[i] = LastPatternCount[pattern.Length - i - 1];
            }
            return NumSuitChanges;
        }
    }


    public class column
    {
        public List<card> Cards;
        public List<series> ThisSeries;
        public List<series> ThisSOS;    // series of series
        public Stack<int> HoldMoves;
        public int SOSvalue, SEQvalue;
        public int value;           // value of column as calculated by scoring algorithm
        public int top;            // topmost card that is face up
        // count ace or king as only 1 point if alone in a column
        public cCardStack CardStack;
        static int[] SingleValues = new int[13] { 1, 2, 3, 4, 5, 6, 7, 6, 5, 4, 3, 2, 1 };
        int NumEmptyColumns;
        bool bLastDeal = false;
        bool bSuitable = false;
        int iSuitStatus = 0;    // bit set if suitable
        int BoardState = 0;
        public int iStack; // which column am I? 0..11 but cannot use 10 or 11 for lookups as they are not on board

        private bool bDoingStackables;  // if true, then we must protect the SEQ series that is part
        // of an SOS being built.  It needs to be moved as part of a
        // suit building stack and not just unstacked like the other SOS

        // if a king under a series that we want to access, its value of "1" means it is not worth much
        // however, to remove the king we will subtract 8-1 = 6 points from the series above it we wish
        // to keep
        private int CostToRemove(int rank)
        {
            int n = 8 - SingleValues[rank - 1];
            return n;
        }


        public void init()
        {
            ThisSeries = new List<series>();
            ThisSOS = new List<series>();
            Cards = new List<card>();
            HoldMoves = new Stack<int>();
            bSuitable = false;
        }


        // find top faceup card in stack
        public int SetTop()
        {
            int i, iTop = 0;
            top = 0;
            int UnExposed = 0;
            for (i = 0; i < Cards.Count; i++)
            {
                if (Cards[i].bFaceUp)
                {
                    top = (iTop);
                    return UnExposed;
                }
                iTop++;
                UnExposed++;
            }
            return 0;
        }


        private bool VerifyThisSeries()
        {
            series s;
            int i, j;
            int lastSuit, lastRank;
            for (i = 0; i < ThisSeries.Count; i++)
            {
                s = ThisSeries[i];
                lastRank = s.topCard.rank;
                lastSuit = s.topCard.suit;
                if (lastSuit != s.bottomCard.suit) return false;
                for (j = s.top; j <= s.bottom; j++)
                {
                    if (Cards[j].suit != lastSuit) return false;
                    if (Cards[j].rank != lastRank) return false;
                    lastRank--;
                }
            }
            return true;
        }


        private void GetRemainingSOS(int StartIndex, ref List<series> xThisSOS)
        {
            series ThisList;
            int PrevIndex = StartIndex;
            bool bStillDoingStackables;
            PrevIndex--;
            if (StartIndex == (Cards.Count))
            {
                xThisSOS[xThisSOS.Count - 1].bottom = PrevIndex;
                xThisSOS[xThisSOS.Count - 1].bottomCard = Cards[PrevIndex];
                return;
            }
            bStillDoingStackables = Cards[StartIndex].PartOfStackables;
            if ((Cards[StartIndex].rank + 1 == Cards[PrevIndex].rank)
                && (bStillDoingStackables == bDoingStackables))
            {
                StartIndex++;
                GetRemainingSOS(StartIndex, ref xThisSOS);
                return;
            }
            else
            {
                bDoingStackables = bStillDoingStackables;
                xThisSOS[xThisSOS.Count - 1].bottomCard = Cards[PrevIndex];
                xThisSOS[xThisSOS.Count - 1].bottom = PrevIndex;
                ThisList = new series();
                ThisList.top = StartIndex;
                ThisList.topCard = Cards[StartIndex];
                xThisSOS.Add(ThisList);
                StartIndex++;
                GetRemainingSOS(StartIndex, ref xThisSOS);
            }

        }

        public int FormSOSseries(int iFromColumn, int itop, ref List<series> xThisSOS)
        {
            series ThisList;
            int i;
            int SOSvalue = 0;
            int ThisTop = itop;
            xThisSOS.Clear();
            if (Cards.Count == 0 || itop >= Cards.Count) return 0;
            ThisList = new series();
            ThisList.top = ThisTop;
            ThisList.topCard = Cards[ThisTop];
            bDoingStackables = ThisList.topCard.PartOfStackables;
            xThisSOS.Add(ThisList);
            ThisTop++;
            GetRemainingSOS(ThisTop, ref xThisSOS);
            for (i = 0; i < xThisSOS.Count; i++)
            {
                xThisSOS[i].iStack = iFromColumn;
                //  xThisSOS[i].nEmptiesToUnstack = xThisSOS.Count - i - 1;
                // the above number would be true if the series was all one suit
                // the number of suit changes needs to be added to it.
                // for example: 10H,9H,8H,7D at the bottom of a column shows "0" to unstack
                // however, the 7D does need to be unstacked first.
                xThisSOS[i].nEmptiesToUnstack = xThisSOS.Count - i - 1 + xThisSOS[i].FormPatternSOS(ref Cards);
            }
            for (i = 0; i < xThisSOS.Count; i++)
            {
                utils.CalcSOSSeriesValue(xThisSOS[i]);
                xThisSOS[i].size = xThisSOS[i].bottom - xThisSOS[i].top + 1;
            }
            if (xThisSOS.Count == 0) return 0;
            SOSvalue = xThisSOS.Last().nValue;
            return SOSvalue;
        }

        public int NumUnstacksReq(card tc)
        {
            int iChange = ThisSeries.Count; // at least this many from top down
            // guesstimate additional change to be number of cards before the top
            // jys !!!
            return iChange + top - tc.iCard;
        }

        private void GetRemainingSEQ(int StartIndex)
        {
            series ThisList;
            int PrevIndex = StartIndex;
            card PrevCard;
            PrevIndex--;

            PrevCard = Cards[PrevIndex];
            if (StartIndex == (Cards.Count))
            {
                ThisSeries[ThisSeries.Count - 1].bottom = PrevIndex;
                ThisSeries[ThisSeries.Count - 1].bottomCard = PrevCard;
                return;
            }

            // is the card sequential with the previous one
            if (Cards[StartIndex].rank + 1 == PrevCard.rank &&
                Cards[StartIndex].suit == PrevCard.suit)
            {
                StartIndex++;
                GetRemainingSEQ(StartIndex);
                return;
            }
            else
            {
                ThisSeries[ThisSeries.Count - 1].bottomCard = PrevCard;
                ThisSeries[ThisSeries.Count - 1].bottom = PrevIndex;
                ThisList = new series();
                ThisList.top = StartIndex;
                ThisList.topCard = Cards[StartIndex];
                ThisSeries.Add(ThisList);
                StartIndex++;
                GetRemainingSEQ(StartIndex);
            }

        }

        // suits must be the same
        // pick the best series for the value of the entire series but subtract the
        // number of cards below the bottom
        public void FormSEQseries(int iFromColumn)
        {
            series ThisList, SaveSeries=null;
            int nValue;
            int NumToUnstack;
            int i, j = 0, iSEQ = 0;
            int ThisTop = top;
            ThisSeries.Clear();
            if (Cards.Count == 0) return;
            ThisList = new series();
            ThisList.top = ThisTop;
            ThisList.topCard = Cards[top];
            ThisSeries.Add(ThisList);
            ThisTop++;
            GetRemainingSEQ(ThisTop);

            // when unstacking, the top card is never unstacked since we are trying to get to the top
            // if only 1 series, then takes nothing to unstack (it just gets moved to the destination
            // if 2 series the bottom one takes nothing to unstack, the one above it requires that
            // the one below it be unstacked first so it is = 1 and on up 2,3,etc
            // example:  4 series, K has 3, Q has 2, (J 10) has 1 and (6 5 4 3 2 1) has 0

            for (i = 0; i < ThisSeries.Count; i++)
            {
                ThisSeries[i].iStack = iFromColumn;
                for (j = ThisSeries[i].top; j <= ThisSeries[i].bottom; j++)
                {
                    Cards[j].WhichSEQSeries = i;
                }
                ThisSeries[i].nEmptiesToUnstack = ThisSeries.Count - i - 1;
                if (i == 0) NumToUnstack = ThisSeries.Count - i - 1;
            }

            SEQvalue = 0;
            if (ThisSeries.Count == 0) return;
            ThisList = null;
            series LastSeries = null;

            foreach (series s in ThisSeries)
            {
                if (LastSeries != null)
                    LastSeries.NextSeries = s;
#if USE_ALL_SERIES
                i = utils.CalcSEQSeriesValue(s);
                SEQvalue += i;
#endif
                s.size = s.bottom - s.top + 1;
                s.topCard.next = s.size;
                for (i = 1; i < s.size; i++)
                {
                    Cards[s.top + i].next = s.size - i;
                }

                ThisList = s;
                iSEQ++;
                LastSeries = s;
            }

            // new:  if CLUBS are suitable and a CLUB series is sequential then we must not score
            // the column high if there is a non CLUB below the series.
            // the above was not implemented
            // instead we will NOT use the SEQ value if the suit is not completable.  just use the count

#if true
            j = 0;
            for (i = ThisSeries.Count - 1; i >= 0; i--)
            {
                series s = ThisSeries[i];
                // jys the value is getting to 1000 and cannot do that
                // just use the largest value
                nValue = utils.CalcSEQSeriesValue(s, BoardState);
                if (j == 0)
                {
                    SEQvalue = nValue;
                    SaveSeries = s;
                }
                else
                {
                    if ((nValue > SEQvalue) && !bSuitable)
                    {
                        SEQvalue = nValue;
                        SaveSeries = s;
                    }
                }
                j++;
                if (j > NumEmptyColumns) break;
            }
#endif
            ThisSeries.Last().NextSeries = null;
            if (SEQvalue > 1000) SEQvalue = 1000;
            if (SaveSeries != null)
            {
                int b = (1 << SaveSeries.sSuit);
                // the following works at last deal ???  17nov2012
                // if (!((b & iSuitStatus) > 0)) SEQvalue = SaveSeries.size;
                if ((b & iSuitStatus) > 0) // we need this suit
                {
                    return;
                }
                else
                {
                    // we dont need that suit, but we are not in the last deal yet
                    if (bLastDeal)
                        SEQvalue = SaveSeries.size;
                }
            }
        }



        //  which series in seriesofseries is located where
        public void FormSubSeries()
        {
            series s, sos;
            int i, j;

            int CurrentSuit;
            int NumberSuitChanges;
            for (i = 0; i < ThisSOS.Count; i++)
            {
                sos = ThisSOS[i];
                for (j = 0; j < ThisSeries.Count; j++)
                {
                    s = ThisSeries[j];
                    if (s.top == sos.top)
                    {
                        sos.SubSeries = j;
                        break;
                    }
                }
                NumberSuitChanges = 0;
                CurrentSuit = sos.topCard.suit;
                for (i = sos.top; i <= sos.bottom; i++)
                {
                    if (CurrentSuit != Cards[i].suit)
                    {
                        NumberSuitChanges++;
                    }
                    CurrentSuit = Cards[i].suit;
                }
                sos.NumSubSeries = 1 + NumberSuitChanges;
            }
        }

        private void AddTopRanks(int sStart, ref List<cRankIndex> Ranks)
        {
            //int n = ThisSeries.Count;
            //for (int i = sStart; i < n; i++)
            {
                series s = ThisSeries[sStart];
                cRankIndex cri = new cRankIndex(s.topCard.rank,sStart);
                Ranks.Add(cri);
            }
            Ranks.Sort(delegate(cRankIndex r1, cRankIndex r2) { return r2.rank.CompareTo(r1.rank); });
        }



        private void FormSEQpatterns()
        {
            string strInxvars;
            series s;
            List<cRankIndex>Ranks = new List<cRankIndex>();
            int  n = ThisSeries.Count;
            if (n == 0) return;

            // pattern (3..2),(5..4) can exist but (5..4),(3..2) cant because it would have been spotted as 5..2
            // so the card above would be the 2 and the card below would be the 4
            //(7..6),(3..2),(5..4) has pattern CAB and 2/1
            //(5..4),(7..6),(3..2) has BCA and 2/1 also
            // the biggest card (7) determines where we start and we start with its own bottom card 6
            // 6->C and look for a 5->B  its bottom card is 4 so look for a 3->A
            // sort series in order 7-6,5-4,3-2 of biggest then see if they form any type of sequential series

            do
            {
                n--;
                s = ThisSeries[n];
                AddTopRanks(n, ref Ranks);
                s.bRankable = true; // ??? should be rankable if patter is not empty ie: ""
                for (int i = 0; i < Ranks.Count - 1; i++)
                {
                    int rankTOP = ThisSeries[Ranks[i].index].bottomCard.rank;
                    int rankBELOW = ThisSeries[Ranks[i+1].index].topCard.rank;
                    if (rankTOP - 1 == rankBELOW) continue;
                    s.bRankable = false;
                }
                if (s.bRankable)
                {
                    strInxvars = GlobalClass.cstrInxVars.Substring(0, Ranks.Count);
                    for (int i = 0; i < Ranks.Count; i++)
                        Ranks[i].PatLet = strInxvars.Substring(Ranks.Count - i - 1, 1);
                    s.pattern = "";
                    for (int i = 0; i < Ranks.Count; i++)
                    {
                        s.pattern += SEQPatLookup(n+i, ref Ranks);
                    }
                    s.PatternCount = new int[s.pattern.Length];
                    int j = 0;
                    for (int i = n; i < ThisSeries.Count; i++)
                    {
                        s.PatternCount[j++] = ThisSeries[i].size;
                    }
                }
                else s.pattern = "";
            } while (n > 0);
   
        }

        public string SEQPatLookup(int inx, ref List<cRankIndex> Ranks)
        {
            for (int i = 0; i < Ranks.Count; i++)
            {
                if (inx == Ranks[i].index) return Ranks[i].PatLet;
            }
            return "";
        }

        private int PositionInColumn(int iCol)
        {
            int n = 0;
            int TopValue = (iCol < 4) ? 5 : 4;
            foreach (card c in Cards)
            {
                if (c.bFaceUp)
                {
                    break;
                }
                n++;
            }
            return TopValue - n;
        }

        public int CalculateColumnValue(int iFromColumn, int nEmpties, int BoardState)
        {
            int i, n;
            int NumUnexposed;
            int jValue;
            this.BoardState = BoardState;
            bLastDeal = ((BoardState & 0x10) > 0);
            iSuitStatus = BoardState & 0xf;
            bSuitable = (iSuitStatus > 0);
            NumEmptyColumns = nEmpties;
            n = (Cards.Count);
            value = 0;
            Debug.Assert(n >= 0);
            NumUnexposed = SetTop();
            ThisSeries.Clear();
            ThisSOS.Clear();
            if (n == 0)
            {
                if (bLastDeal) value = 0;
                else value = GlobalClass.COL_WEIGHT;
                return value;
            }

            FormSEQseries(iFromColumn);
            FormSEQpatterns();
            //Debug.Assert(VerifyThisSeries());
            SOSvalue = FormSOSseries(iFromColumn, top, ref ThisSOS);
            FormSubSeries();


            //SOSvalue -= NumUnexposed;
            //SOSvalue += 4;
            //SEQvalue -= NumUnexposed;
            //SEQvalue += 4;


            for (i = 0; i < Cards.Count; i++)
            {
                Cards[i].iCard = i;
                Cards[i].iStack = iFromColumn;
                if (i == 0) Cards[i].GapAbove = 1;  // anything can fit at the top
                else
                {
                    Cards[i].GapAbove = Cards[i - 1].rank - Cards[i].rank;
                }
            }
            // this causes a problem with KD, QC, JC, etc when suiting clubs
            jValue = SEQvalue;
            if(!bLastDeal)// need to find if suiting but do not have that scope here
                jValue = (SOSvalue > SEQvalue) ? SOSvalue : SEQvalue;
            jValue += PositionInColumn(iFromColumn);
            if (jValue > 1000) jValue = 1000;
            value = (jValue);
            return value;
        }

    }
}
