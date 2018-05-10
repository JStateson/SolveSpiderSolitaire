using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace spider
{
    
    public class cReduce
    {
        static int OnCounter = 0;
        public class cPotentialExpose
        {
            public card Placeholder;
            public card CardBlocking;
            public cPotentialExpose(ref card cPlaceholder, ref card cCardBlocking)
            {
                Placeholder = cPlaceholder;
                CardBlocking = cCardBlocking;
            }
        }

        public class cMoveOrder
        {
            public card From;
            public card To;
            public int EmptyID;
            public bool bUseEmpty;
            public cMoveOrder(ref card iFrom)
            {
                From = iFrom;
            }
        }

        public cReduce(ref cSpinControl cSC, ref cDisassemble Disassemble, ref cStrategy Strategy)
        {
            this.Disassemble = Disassemble;
            this.Strategy = Strategy;
            this.cSC = cSC;
        }

        private cStrategy Strategy;
        private int BoardBeingWorked;
        private DateTime TimeLastBest, TimeSinceFilterRan;
        private int OriginalCount;
        

        private cSpinControl cSC;
        private List<card> PlaceHolders = new List<card>();
                                                    // remaining columns that hav a bottom card rank > ace 
        private List<card> PermanentPlaceHolders = new List<card>();
                                                    // any remaining column that does  not contain a stackable
                                                    // remaining means the columns that do not contain the card
                                                    // that is being built


        private List<int> DoNotUse = new List<int>();
        private List<card> Stackables = new List<card>();   // all 13, hold so we can unmark them
        private List<int> StackableIDs = new List<int>();
        // when unstacking to free up a stackable, if any of the ones
        // unstacked were joined to another stackable, then  we need
        // to remove that card from our list of joinables as it was
        // joined


        public cDisassemble Disassemble;
        public List<cPotentialExpose> PotentialExpose;
        private List<int> xEmpties = new List<int>();
        public List<int> PartOfMovables;    // if a card is part of the movables then it cannot
                                            // be used as a placeholder until it has moved.  After it has
                                            // has moved then fStack must be used, not iStack

        private bool IsTopMost(ref card thisCard, ref column cCol)
        {
            int i;
            if (thisCard.iCard == cCol.top) return true;
            for (i = thisCard.iCard - 1; i >= cCol.top; i--)
            {
                foreach (card c in Stackables)
                {
                    if (c == cCol.Cards[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void GetAdjacentStackables(ref card thisCard, ref column cCol, ref List<card> CardsAbove, ref List<card> CardsBelow)
        {
            int i,j;
            CardsBelow.Clear();
            CardsAbove.Clear();

            if (thisCard.iCard > cCol.top)
            {
                for (i = thisCard.iCard - 1; i >= cCol.top; i--)
                {

                    for (j = 0; j < Stackables.Count; j++)
                    {
                        if (cCol.Cards[i] == Stackables[j])
                            CardsAbove.Add(Stackables[j]);
                    }
                }
            }
            else if(thisCard.iCard != (cCol.Cards.Count-1))
            {
                for (i = thisCard.iCard + 1; i < cCol.Cards.Count; i++)
                {
                    for (j = 0; j < Stackables.Count; j++)
                    {
                        if (cCol.Cards[i] == Stackables[j])
                            CardsBelow.Add(Stackables[j]);
                    }
                }
            }
        }


        public void InitializeReduction(ref board tb, ref card cNext)
        {
            int i, j = 13;
            tb.ReScoreBoard();
            Stackables.Clear();
            StackableIDs.Clear();
            DoNotUse.Clear();
            cNext = tb.SuitedSEQ[0].topCard;
            int suit = cNext.suit;

            foreach (series s in tb.SuitedSEQ)
            {
                DoNotUse.Add(s.iStack); // not a permanent placeholder till that card is moved (if it is moved)
                for (i = cNext.iCard; i <= s.bottom; i++)
                {
                    column cCol = tb.ThisColumn[s.iStack];
                    Stackables.Add(cCol.Cards[i]);
                    cCol.Cards[i].PartOfStackables = true;
                    Debug.Assert(j == cCol.Cards[i].rank && suit == cCol.Cards[i].suit);
                    j--;
                }
                cNext = s.AssemblyLink; // link to next card in "s" assembly
                if (cNext != null)
                    StackableIDs.Add(cNext.ID);
            }
            cNext = tb.SuitedSEQ[0].topCard;
        }

        // if there is a card above ThisCard that is a member of the series we are trying to stack, then
        // ThisCard must be moved.  Only king has this problem when stacking
        private bool bStackablesAbove(ref board tb, ref card ThisCard)
        {
            column cCol = tb.ThisColumn[ThisCard.iStack];
            for (int i = ThisCard.iCard - 1; i >= cCol.top; i++)
            {
                if (cCol.Cards[i].PartOfStackables) return true;
            }
            return false;
        }

        public bool ReduceSuits(ref board oldtb)
        {
            bool bSuccess = true;
            card cNext = null;
            card ExposedCard = null;
            int e, CardID, LastID;
            card CardExposed;
            board tb = new board(ref oldtb);
            tb.AssignCompletedID(GlobalClass.TypeCompletedBy.ID_RSI);
            OnCounter++;
            InitializeReduction(ref tb, ref cNext);
            Debug.Assert(cNext.rank == 13);
            GetPlaceholders(ref tb, cNext.iStack);

            if (bStackablesAbove(ref tb, ref cNext))
            {
                bSuccess = UnstackSeriesBelow(ref tb, ref cNext);
                if (bSuccess)
                {
                    e = tb.Empties[0];
                    tb.Empties.RemoveAt(0);
                    CardExposed = tb.moveto(cNext.iStack, cNext.iCard, e);    // have to move the king
                    RecoverPlaceholder(ref tb, ref CardExposed);
                    tb.tag = e;
                    InitializeReduction(ref tb, ref cNext);
                }
                else
                {
                    tb.AssignCompletedID();
                    return false;  // jys !!! may want to try a second or lower ranked card
                }
            }
            else
            {
                tb.tag = cNext.iStack;
                GetPlaceholders(ref tb, tb.tag);
            }

            // unstack below the last card (cNext) and the next card

            while(bSuccess)
            {
                bSuccess = UnstackSeriesBelow(ref tb, ref cNext);
                if (bSuccess)
                {
                    if (StackableIDs.Count > 0)
                    {
                        CardID = StackableIDs[0];   // next card to gather in
                        StackableIDs.RemoveAt(0);
                        LastID = cNext.ID;
                        cNext = utils.FindCardFromID(ref tb, CardID);
                        bSuccess = UnstackSeriesBelow(ref tb, ref cNext);
                        if (bSuccess)
                        {
                            ExposedCard = tb.moveto(cNext.iStack, cNext.iCard, tb.tag);
                            RecoverPlaceholder(ref tb, ref ExposedCard);
                        }
                        else
                        {
                            tb.AssignCompletedID();
                            return false;
                        }
                    }
                    else
                    {
                        tb.ReScoreBoard();
                        cSC.Suitable.PerformSuitabilityScreening(ref tb);
                        if (tb.bIsCompletable) cSC.NextBoardSeries.Add(tb);
                        else cSC.ThisBoardSeries.Add(tb);
                        tb.AssignCompletedID();
                        return true;
                    }
                }
                else
                {
                    tb.AssignCompletedID();
                    return false;
                }
            }
            tb.AssignCompletedID();
            return false;
        }

        private void RecoverPlaceholder(ref board tb, ref card CardExposed)
        {
            if (CardExposed != null && CardExposed.rank > 1)
            {
                if (!CardExposed.PartOfStackables)
                    PermanentPlaceHolders.Add(CardExposed);                    
                PlaceHolders.Add(CardExposed);
            }
        }

 

        private bool UnstackSeriesBelow(ref board tb, ref card cNext)
        {
            bool bSuccess = true;
            series sCollapse;
            column cCol = tb.ThisColumn[cNext.iStack];
            int n = cCol.Cards.Count;
            int LastSeries = cCol.ThisSeries.Count - 1;
            Debug.Assert(LastSeries >= 0);
            if (cNext.WhichSEQSeries == LastSeries) return true;

                    // if only one to unstack then use a simple unstack
            if (cNext.WhichSEQSeries == (LastSeries - 1))
            {
                series ts = cCol.ThisSeries[cNext.WhichSEQSeries];
                bSuccess = UnstackOne(ref tb, ref ts);
                return bSuccess;
            }
                    // if more then one to unstack then try to move all of them using a pattern match
            sCollapse = new series();
            sCollapse.bottomCard = cCol.Cards[n - 1];
            sCollapse.topCard = cNext;
            sCollapse.tag = cNext.tag;  // this is destination
            sCollapse.size = cCol.Cards.Count - cNext.iCard;
            sCollapse.iStack = cNext.iStack;
            if (utils.bRankable(ref sCollapse, ref tb))
            {
                bSuccess = Disassemble.bCanUnstackCollapseable(ref sCollapse, ref tb, ref PermanentPlaceHolders);
                return bSuccess;
            }

            // hmm - not rankable  JYS !!! could try a simple move SOS assumeing all are sequential 
            // if that worked then mode the ABCD pattern counf of 4 max to account for simple sequential move
            // and use FillDS instead of SOS

            // the following will work but is not optimum

            int TopOfSeries = cNext.WhichSEQSeries + 1;
                // start at bottom series, work upward
            for (int i = LastSeries; i >= TopOfSeries && bSuccess; i--)
            {
                series ts = cCol.ThisSeries[i];
                bSuccess = UnstackOne(ref tb, ref ts);                
            }
            return bSuccess;
        }

        // this looks for available spots to stuff a card and if it cannot find one it suggests an empty stack
        private int GetBestMove(ref card c, ref board tb)
        {
            int i, n;
            int iSameSuit = -1;
            int iAnySuit = -1;
            int ToStack = -1;
            List<card> PlaceHolder = PermanentPlaceHolders;
            if (c.PartOfStackables) PlaceHolder = PlaceHolders;
            n = PlaceHolder.Count;
            for (i = 0; i < n; i++)
            {
                if (PlaceHolder[i].rank - 1 == c.rank)
                {

                    if (PlaceHolder[i].suit == c.suit)
                    {
                        iSameSuit = i;
                        ToStack = PlaceHolder[i].iStack;
                        PlaceHolder.RemoveAt(i);
                        StackableIDs.Remove(c.ID);
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


        private bool UnstackOne(ref board tb, ref series ts)
        {
            int iStack = GetBestMove(ref ts.topCard, ref tb);
            if (iStack < 0)
            {
                if (tb.Empties.Count == 0) return false;
                iStack = tb.Empties[0];
                tb.Empties.RemoveAt(0);
            }
            tb.PerformMove(ts.iStack, ts.top, iStack, ts.size);
            return true;
        }



        private void ClearAnyBlocking(ref board tb, ref series s)
        {
            card cAbove;
            card JustMoved = s.topCard;
            card BottomCard = s.bottomCard;
            column cCol = tb.ThisColumn[JustMoved.iStack];  // we moved from here so use iStack, not fStack
            int iAbove = JustMoved.iCard - 1;
            if (iAbove < 0)
            {
                xEmpties.Add(JustMoved.iStack);
            }
            if (!BottomCard.PartOfStackables)
            {
                PlaceHolders.Add(BottomCard);   // this has the updated fStack
            }
            cAbove = cCol.Cards[iAbove];
            if(cAbove.PartOfStackables)return;
            if (PartOfMovables.Contains(cAbove.ID)) return;
            PlaceHolders.Add(cAbove);
        }

        
        private void GetPlaceholders(ref board tb, int AvoidStack)
        {
            column cCol;
            card LastCard;
            PlaceHolders.Clear();
            PermanentPlaceHolders.Clear();

            foreach(int i in tb.NonEmpties)
            {
                cCol = tb.ThisColumn[i];
                LastCard = cCol.Cards[cCol.Cards.Count - 1];
                if (LastCard.rank == 1) continue;   // an ACE cannot hold anything
                if (AvoidStack == i) continue;
                PlaceHolders.Add(LastCard);
                if (DoNotUse.Contains(i)) continue; // not a permanent placeholder if stackables are above it
                PermanentPlaceHolders.Add(LastCard);
            }
        }

        public bool RunFilter(int BoardsToSave)
        {


            TimeSpan ElapsedR;
            cSC.CountLimit = 0;
            BoardBeingWorked = 0;
            cSC.bSignalSpinDone = false;
            cSC.bExceededCountLimit = false;
            cSC.bExceededONEcolLimit = false;
            cSC.bExceededTWOcolLimit = false;
            cSC.bExceededThreecolLimit = false;
            cSC.bOutOfSpaceInSTLOOKUP = false;
            cSC.bGotOneSuitAtLeast = false;
            cSC.SortedScores.Clear();
            cSC.BestScoreIndex.Clear();
            cSC.TimeDealStarted = TimeLastBest;

            TimeLastBest = DateTime.Now;    // in event we never get a "best"
            TimeSinceFilterRan = TimeLastBest;
            OriginalCount = cSC.ThisBoardSeries.Count;

            while (BoardBeingWorked < cSC.NextBoardSeries.Count)
            {
                board nb = cSC.NextBoardSeries[BoardBeingWorked];
                if (nb.bIsCompletable)
                    ReduceSuits(ref nb);
                else cSC.ThisBoardSeries.Add(nb);   // pass back for AllPossibleMoves
                BoardBeingWorked++;

                if (0 == (BoardBeingWorked % 100) && ((GlobalClass.TraceBits & 4) > 0))
                {

                    ElapsedR = DateTime.Now.Subtract(TimeLastBest);
                    int iSecs = Convert.ToInt32(ElapsedR .TotalSeconds);
                    nb.ShowBoard();
                    Console.WriteLine("R(" + nb.score + ") " + "  BBW:" + BoardBeingWorked);
                }


            }
            // we did all boards
            cSC.NextBoardSeries.Clear();
            return false;
        }



    }
}
