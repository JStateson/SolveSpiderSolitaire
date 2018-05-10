using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace spider
{
    public class cJoinSuited
    {
        private cSpinControl cSC;
        private List<pseudoCard> PsuedoMoveList = new List<pseudoCard>();
        private List<card> PlaceHolder = new List<card>();

        public cJoinSuited(ref cSpinControl cSC)
        {
            this.cSC = cSC;
        }


        // suits must be completable and some empties available BEFORE calling
        // call CombineLikeSuits before calling this.
        public bool SpinSuitedJoinables(ref List<board>WorkingSeries)
        {
            bool bAny = false;
            int Total = 0;
            int OriginalCount = WorkingSeries.Count;
            int BoardBeingWorked;
            board tb;
            if (cSC.bTrigger) Console.WriteLine(MethodBase.GetCurrentMethod().Name); 
            for (BoardBeingWorked = 0; BoardBeingWorked < WorkingSeries.Count; BoardBeingWorked++)
            {
                tb = WorkingSeries[BoardBeingWorked];
                if (tb.dead) continue;
                if (tb.BuildingThisSuit == 0)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        // just want several empty columns right now
                        //if (tb.bSuitsCompletable[i])
                        {
                            do
                            {
                                board nb = new board(ref tb);
                                nb.SuitedSEQ.Clear();
                                // sorting by rank puts the biggest first and no need to swap order to get
                                // find a card that fits under the first card
                                nb.SuitedSEQ = nb.BuildSortedSEQ(1 << i, GlobalClass.eSortedSEQtype.SortByRank);
                                nb.BuildingThisSuit = i + 1;
                                bAny = JoinAnyLikeSuits(ref nb);
                                if (bAny)
                                {
                                    Total++;
                                    WorkingSeries.Add(nb);
                                }
                            } while (bAny);
                        }
                    }
                }
            }
            return (Total > 0);
        }


        // dont call this unless it was sorted by rank
        // this does not handle card subsets that are in the same column
        public bool JoinAnyLikeSuits(ref board tb)
        {
            if (cSC.bTrigger) Console.WriteLine(MethodBase.GetCurrentMethod().Name); 
            series sDes, sSrc;
            int PossibleCostReduction;
            int RankWanted, LowestRank, RankWantedLoc;
            PsuedoMoveList.Clear();
            //foreach (series s1 in tb.SortedSEQ)
            for(int i = 0; i < tb.SortedSEQ.Count-1; i++)
            {
                sDes = tb.SortedSEQ[i]; // biggest is first in sorted order of ranks
                PossibleCostReduction = sDes.bRankable ? 1 : 0;
                
                //foreach (series s2 in tb.SortedSEQ)
                for(int j = i + 1; j < tb.SortedSEQ.Count; j++)
                {
                    sSrc = tb.SortedSEQ[j];
                    if (sSrc.sSuit != sDes.sSuit) continue; // in event more then one suit is in the sorted list
                    PossibleCostReduction+= sSrc.bRankable ? 1 : 0;
                    // the 1 below represents possibly a single available placeholder
                    if (sDes.nEmptiesToUnstack + sSrc.nEmptiesToUnstack > (tb.NumEmptyColumns + 1 + PossibleCostReduction)) return false;
                    if (sDes.iStack == sSrc.iStack) continue;
                    // make s1 (bottom) the destination and s2 (top) the source (ranks must be different)
                    if (sDes.topCard.rank == sSrc.topCard.rank) continue;   // nothing to join

                    // 9..6 could join to 6..3 by moveing only the 5..3 if it exists
                    RankWanted = sDes.bottomCard.rank-1;  // find a card in source to be mvoed that is this rank
                    LowestRank = sSrc.bottomCard.rank;
                    if(!(RankWanted  > LowestRank && sSrc.topCard.rank >= RankWanted))continue;
                    // figure out where that card is in the source to be moved
                    RankWantedLoc = sSrc.top + (1 + sSrc.topCard.rank - sDes.bottomCard.rank);
                    card cSrc = tb.ThisColumn[sSrc.iStack].Cards[RankWantedLoc];
                    Debug.Assert(RankWanted == cSrc.rank);
                    sSrc.size -= (1 + sSrc.topCard.rank - sDes.bottomCard.rank);
                    sSrc.topCard = cSrc;
                    sSrc.top = cSrc.iCard;

                    if (sSrc.nEmptiesToUnstack == 0 && sDes.nEmptiesToUnstack == 0)
                    {
                        tb.moveto(cSrc.iStack, cSrc.iCard, sSrc.bottomCard.iStack);
                        return true;
                    }
                    pseudoCard pCard = new pseudoCard(sDes.topCard.ID, sSrc.bottomCard.ID, true);
                    PsuedoMoveList.Insert(0, pCard);
                    if (sSrc.nEmptiesToUnstack > 0)
                        if (!UnstackBelow(ref sSrc)) return false;
                    if (sDes.nEmptiesToUnstack > 0)
                        if (!UnstackBelow(ref sDes)) return false;
                    return JoinPsudos();
                }
            }
            return false;
        }

        // use a pattern if one is available else just unstack anyway possible
        private bool UnstackBelow(ref series sKeep)
        {     
            return true;
        }
        private bool JoinPsudos()
        {
            return true;
        }

        // same as above JSS but does not consume any empty columns
        // the ranks must be in order (rankable under the series) and the gap above both top cards must be 1
        // this routine can be called at any time after building a sorted sequence (BuildSEQ) of any number of suits
        public bool JoinSuitedWhenRankable(ref board tb)
        {
            if (cSC.bTrigger) Console.WriteLine(MethodBase.GetCurrentMethod().Name); 
            series sDes, sSrc;
            series uDes;    // the unstacked part of the destination
            int PossibleCostReduction;
            int RankWanted, LowestRank, RankWantedLoc;
            PsuedoMoveList.Clear();
            tb.BuildPlaceholders(ref PlaceHolder);
            //foreach (series s1 in tb.SortedSEQ)
            for (int i = 0; i < tb.SortedSEQ.Count - 1; i++)
            {
                sDes = tb.SortedSEQ[i]; // biggest is first in sorted order of ranks
                if (sDes.topCard.GapAbove != 1 || sDes.pattern == "") continue;
                                // only want reversible moves for swapping purposes
                                // and there must be a pattern of else the series is not rankable
                PossibleCostReduction = sDes.bRankable ? utils.hasPlaceholder(ref sDes.topCard, ref tb, ref PlaceHolder) : 0;

                //foreach (series s2 in tb.SortedSEQ)
                for (int j = i + 1; j < tb.SortedSEQ.Count; j++)
                {
                    sSrc = tb.SortedSEQ[j];
                    if (sSrc.topCard.GapAbove != 1 || sSrc.pattern == "") continue;
                    if (sSrc.sSuit != sDes.sSuit) continue; // in event more then one suit is in the sorted list
                    PossibleCostReduction +=
                        sSrc.bRankable ? utils.hasPlaceholder(ref sSrc.topCard, ref tb, ref PlaceHolder) : 0;

                    utils.FreeExcludedPlaceholders(ref PlaceHolder);    // done with our PossibleCostReduction

                    // the 0 below represents possibly a single available placeholder ie; we assume NONE here
                    if (sDes.nEmptiesToUnstack + sSrc.nEmptiesToUnstack > (tb.NumEmptyColumns + PossibleCostReduction + 0)) return false;
                    if (sDes.iStack == sSrc.iStack) continue;
                    // make s1 (bottom) the destination and s2 (top) the source (ranks must be different)
                    if (sDes.topCard.rank == sSrc.topCard.rank) continue;   // nothing to join
                    
                    // 9..6 could join to 6..3 by moveing only the 5..3 if it exists
                    RankWanted = sDes.bottomCard.rank - 1;  // find a card in source to be mvoed that is this rank
                    LowestRank = sSrc.bottomCard.rank;
                    if (!(RankWanted > LowestRank && sSrc.topCard.rank >= RankWanted)) continue;
                    // figure out where that card is in the source to be moved
                    RankWantedLoc = sSrc.top + (1 + sSrc.topCard.rank - sDes.bottomCard.rank);
                    card cSrc = tb.ThisColumn[sSrc.iStack].Cards[RankWantedLoc];
                    Debug.Assert(RankWanted == cSrc.rank);
                    sSrc.size-= (1 + sSrc.topCard.rank - sDes.bottomCard.rank);
                    sSrc.topCard = cSrc;
                    sSrc.top = cSrc.iCard;
                    if (sSrc.nEmptiesToUnstack == 0 && sDes.nEmptiesToUnstack == 0)
                    {
                        tb.moveto(cSrc.iStack, cSrc.iCard, sDes.bottomCard.iStack);
                        return true;
                    }
                    //pseudoCard pCard = new pseudoCard(sDes.topCard.ID, sSrc.bottomCard.ID, true);
                    //PsuedoMoveList.Insert(0, pCard);
                    uDes = null;
                    if (sDes.nEmptiesToUnstack > 0)
                    {
                        uDes = sDes.NextSeries;
                        uDes.tag = sSrc.iStack;    // after moveing sSrc put the unstack from Des where Src came from
                            // the above works because the algorithm only moves rankables that are in order
                            // so as to not consume a stack. the tag is not updated by any move card operation
                        if (!MoveSeriesBelow(ref tb, ref sDes)) return false;
                        uDes.iStack = tb.tag;   // series locations are NOT updated by any cards that are moved
                            // uDes.iStack is where the unstacked stuff was stashed
                        tb.tag = uDes.tag;      // cannot use sSrc.iStack since Src was moved
                    }
                    tb.tag = sDes.iStack;
                    bool bAny = cSC.Suitable.Disassemble.MoveCollapsible(ref sSrc, ref tb);
                    if (!bAny) return false;
                    if (uDes == null) return true;
                    tb.tag = uDes.tag;
                    bAny = cSC.Suitable.Disassemble.MoveCollapsible(ref uDes, ref tb);
                    return bAny;
                }
            }
            return false;
        }
        
        private bool MoveSeriesBelow(ref board tb, ref series sKeep)
        {
            series sCollapse = sKeep.NextSeries;
            tb.tag = utils.GetBestMove(sCollapse.topCard, ref tb, ref PlaceHolder);
            // note that tag could be negative:  need to get an empty column
            bool bAny = cSC.Suitable.Disassemble.MoveCollapsible(ref sCollapse, ref tb);
            return bAny;
        }

        // this joins suited sequences that can be swapped (ie: reversible moves)        
        public bool JoinAllSuitedWhenRankable(ref board tb)
        {
            if (cSC.bTrigger) Console.WriteLine(MethodBase.GetCurrentMethod().Name);
            if(tb.NumEmptyColumns < 1)return false;
            bool bAny = false, bResults = false;
            do
            {
                tb.BuildSortedSEQ(0xf, GlobalClass.eSortedSEQtype.SortByRank);
                bResults = cSC.JoinSuited.JoinSuitedWhenRankable(ref tb);
                bAny |= bResults;
                tb.ReScoreBoard();
            } while (bResults);
            return bAny;
        }

    }
}
