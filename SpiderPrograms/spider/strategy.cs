using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace spider
{
    public class cStrategy
    {
        private class cPossibleMoves
        {
            public card From;
            public card To;
            public bool bSameSuit;
            public cPossibleMoves(ref card from, ref card to)
            {
                From = from;
                To = to;
                bSameSuit = (from.suit == to.suit);

            }
        }

        class cUnstackOrder
        {
            public int e;
            public int SizeSeries;
            public cUnstackOrder(int ie, int iSize)
            {
                e = ie;
                SizeSeries = iSize;
            }
        }


        public int OurLastBest;
        public int RtnCode;
        private column srcCol;
        private List<card> xTopMost;     // first card of the series of series
        private List<card> xBottomMost;  // last card of the series of series = last card of any stack
        public List<int> xEmpties;
        public List<int> xNonEmpties;
        private List<card> CardsToMove;
        private List<card> srcCardsToMove;
        private List<card> desCardsToMove;
        private List<board> NewBoards = new List<board>(32);
        private cDisassemble DisAssemble;
        private cSpinControl cSC;
        private List<card> PlaceHolder = new List<card>();

        public cStrategy(ref cSpinControl cSC, ref cDisassemble DisAssemble)
        {
            this.cSC = cSC;
            this.DisAssemble = DisAssemble;
            OurLastBest = 0;
            //DisAssemble = new cDisassemble();
            RtnCode = 0;
            CardsToMove = new List<card>();
            desCardsToMove = new List<card>();
            srcCardsToMove = new List<card>();
            xBottomMost = new List<card>();
            xEmpties = new List<int>();
            xNonEmpties = new List<int>();
            xTopMost = new List<card>();
        }

        public int FirstStrategy(ref board oldb)
        {
            int TotalAny = 0, Any = 0;
            int NumSOS;
            board tb;
            OurLastBest = oldb.LastBest;
            if (oldb.NumEmptyColumns >= 1)
                do
                {
                    Any = ExposeTop(ref oldb);
                    TotalAny += Any;
                } while (Any > 0);

            if (!(oldb.RunEndgame())) return 0;

            Any = SOSExposeTop(ref oldb);
            TotalAny += Any;
            NumSOS = NewBoards.Count;   // SOSexpose added this many
            if (oldb.bIsCompletable)
            {
                Any += RunJoinables(ref oldb, false);       // process the original board
                for (int i = 0; i < NumSOS; i++)    // process any that SOSexpose created
                {
                    tb = NewBoards[i];
                    board nb = new board(ref tb);
                    Any += RunJoinables(ref nb, false);
                    TotalAny += Any;
                }
            }
            return TotalAny;
        }

        public int SOSExposeTop(ref board tb)
        {
            bool bAnySameSuit = false;
            List<series> TopSeries = new List<series>();
            List<cPossibleMoves> ActualMoves = new List<cPossibleMoves>();
            NewBoards.Clear();
            TryUnstackAll(ref tb);

            tb.CopyWorkingCards(ref xBottomMost, GlobalClass.WorkingType.tBottomMost);
            foreach (column cCol in tb.ThisColumn)
            {
                if (cCol.iStack > 9) break;
                if (cCol.ThisSOS.Count == 1) TopSeries.Add(cCol.ThisSOS[0]);
            }
            foreach (card bm in xBottomMost)
            {
                foreach (series s in TopSeries)
                {
                    card tm = s.topCard;
                    if ((tm.rank + 1) == bm.rank)
                    {
                        card bbm = bm;
                        cPossibleMoves cpm = new cPossibleMoves(ref tm, ref bbm);
                        ActualMoves.Add(cpm);
                    }
                }
            }

            foreach (cPossibleMoves cpm in ActualMoves)
            {
                if (cpm.bSameSuit)
                {
                    bAnySameSuit = true;
                    break;
                }
            }

            foreach (cPossibleMoves cpm in ActualMoves)
            {
                column cCol;
                card cCrd;
                board nb;
                series SOS;
                bool bCan;
                List<series> ListSEQ;
                List<int> yEmpties = new List<int>();
                List<card> yBottomMost = new List<card>();
                if (bAnySameSuit && !cpm.bSameSuit) continue;
                nb = new board(ref tb);
                nb.AssignCompletedID(GlobalClass.TypeCompletedBy.ID_SOSET);
                CardsToMove.Clear();
                cCol = nb.ThisColumn[cpm.From.iStack];
                SOS = cCol.ThisSOS[0];
                ListSEQ = cCol.ThisSeries;    // the top cards in the SEQ are the ones to move in the SOS
                for (int i = 0; i < SOS.NumSubSeries; i++)
                {
                    cCrd = ListSEQ[i].topCard;
                    CardsToMove.Add(cCrd);
                }
                nb.tag = cpm.To.iStack;
                nb.CopyWorkingInts(ref yEmpties, GlobalClass.WorkingType.tEmpties);
                nb.CopyWorkingCards(ref yBottomMost, GlobalClass.WorkingType.tBottomMost);
                bCan = DisAssemble.bCanDisassembleSOS(SOS.NumSubSeries, ref nb, ref yEmpties, ref yBottomMost, ref CardsToMove);
                if (bCan)
                {
                    NewBoards.Add(nb);
                    nb.AssignCompletedID();
                }
            }
            return NewBoards.Count;
        }





        // this just checks empty columns for space right now
        // and only looks at SEQ
        public int ExposeTop(ref board tb)
        {
            int i, j, n, nT, nE;
            int sCost;  // cost of all the series that need to be unstacked to expose the facedown card
                        // is the sum of all the SEQ costs plus 1
            int tlCount = 0;
            column cCol;
            series sCollapse = new series();   // out of sequence series we want to collapse to a series of series

            for (i = 0; i < 10; i++)
            {
                nE = tb.NumEmptyColumns;
                cCol = tb.ThisColumn[i];
                n = cCol.Cards.Count;
                if (n < 2 || cCol.top == 0) continue;
                sCost = 1;
                for (j = 0; j < cCol.ThisSeries.Count; j++)
                {
                    sCost += cCol.ThisSeries[j].nEmptiesToUnstack;
                }
                if (sCost <= nE)    // series is moveable using empties
                {
                    sCollapse.bottomCard = cCol.Cards[n - 1];
                    nT = cCol.top - 1;  // point to first un-exposed card
                    sCollapse.topCard = cCol.Cards[nT];
                    sCollapse.size = cCol.Cards.Count - nT;
                    sCollapse.iStack = sCollapse.topCard.iStack;
                    sCollapse.tag = nT; // this is destination
                    if (utils.bRankable(ref sCollapse, ref tb))
                    {
                        if (true)
                        {
                            if (!DisAssemble.TryExpose(ref sCollapse, ref tb)) continue;
                            tlCount += 1;
                            break;
                        }
                    }
                }

            }
            if (tlCount > 0)
                tb.ReScoreBoard();
            return tlCount;
        }

        // this gets rid of as many facedown cards as possible
        // this should be run only if suits are locked out and only a few suits are remaining
        public int TryUnstackAll(ref board oldtb)
        {
            int Any;
            int Total = 0;
            int ne = oldtb.NumEmptyColumns;
            int ru = oldtb.UnexposedTypes.Count;
            int rs = oldtb.ComputeRemainingSuits();
            if (rs > 3) return 0;   // the works best with only a few suits
            if (ru > ne) return 0; // need a bunch of empty columns for sure
            if (oldtb.DealCounter < 5) return 0;    // only on last deal
            board tb = new board(ref oldtb);

            tb.AssignCompletedID(GlobalClass.TypeCompletedBy.ID_USTK);
            //tb.ShowBoard();

            Any = TryUnstackOne(ref tb, ref Total);

            //do
            //{
            //    Any = TryUnstackOne(ref tb);
            //    if (Any > 0)
            //    {
            //        tb.ReScoreBoard();
            //        Total += Any;
            //    }
            //} while (Any > 0);
            //tb.ExplainBoard("did unstack");
            tb.AssignCompletedID();
            NewBoards.Add(tb);
            return Total;
        }

        public int TryUnstackOne(ref board tb, ref int total)
        {
            int Any;
            total = 0;
            do
            {
                Any = UnstackOneSeries(ref tb);
                total += Any;
                tb.ReScoreBoard();
            } while (Any > 0);
            return total;
        }

        private int UnstackOneSeries(ref board tb)
        {
            int Any = 0;
            cUnstackOrder[] cUO;
            column cCol;
            card CardExposed;
            PlaceHolder.Clear();
            cUO = new cUnstackOrder[tb.NumFacedownColumns()];
            int i = 0;
            tb.CopyWorkingCards(ref xBottomMost, GlobalClass.WorkingType.tBottomMost);
            tb.CopyWorkingInts(ref xEmpties, GlobalClass.WorkingType.tEmpties);
            tb.BuildPlaceholders(ref PlaceHolder);


            // get best unstack order in case we cannot unstack all of them
            i = 0;
            foreach (int e in tb.NonEmpties)
            {
                cCol = tb.ThisColumn[e];
                if (cCol.top == 0) continue;
                int nSEQ = cCol.ThisSeries.Count;
                int nSOS = cCol.ThisSOS.Count;
                cUO[i++] = new cUnstackOrder(e, nSEQ);
            }
            Array.Sort(cUO, delegate(cUnstackOrder c1, cUnstackOrder c2)
            {
                return c1.SizeSeries.CompareTo(c2.SizeSeries);
            });

            foreach (cUnstackOrder uo in cUO)
            {
                cCol = tb.ThisColumn[uo.e];
                for (i = uo.SizeSeries - 1; i >= 0; i--)
                {
                    series s = cCol.ThisSeries[i];
                    int iStack = utils.GetBestMove(s.topCard, ref tb, ref PlaceHolder);
                    if (iStack < 0)
                    {
                        if (xEmpties.Count == 0) return Any;
                        iStack = xEmpties[0];
                        xEmpties.RemoveAt(0);
                    }
                    CardExposed = tb.PerformMove(s.iStack, s.top, iStack, s.size);
                    RecoverPlaceholder(ref tb, ref CardExposed);
                    Any++;
                }
                if (Any > 0) return Any;
            }
            return Any;
        }

        private void RecoverPlaceholder(ref board tb, ref card CardExposed)
        {
            if (CardExposed != null && CardExposed.rank > 1)
            {
                PlaceHolder.Add(CardExposed);
            }
        }


        public int RunJoinables(ref board oldb, bool bInPlace)
        {
            int Any = 0;
            int Score;
            board tb;
            bool bIsDone = false;

            if (!(oldb.RunEndgame())) return 0;

            if (bInPlace)
            {
                tb = oldb;
            }
            else
            {
                tb = new board(ref oldb);
            }
            tb.AssignCompletedID(GlobalClass.TypeCompletedBy.ID_JSS);
            while (JoinSameSuits(ref tb, true))
            {
                Any++;
                Score = tb.ReScoreBoard();
                CheckSuitComplete(ref tb);
                bIsDone = (Score >= GlobalClass.MaxScore);
                if (bIsDone) break;
            }

            if (!bIsDone)
            {
  
                while (JoinSuitedJoinables(ref tb) && !bIsDone)
                {
                   
                    Any++;
                    bIsDone = CheckSuitComplete(ref tb);
                }

            }
            if (bIsDone)
            {
                Console.WriteLine("Completed board!");
                Environment.Exit(0);
            }
            if (Any > 0 && !bInPlace) NewBoards.Add(tb);
            tb.AssignCompletedID();
            return Any;
        }

        public bool JoinSuitedJoinables(ref board tb)
        {
            return false;
        }

        private bool CheckSuitComplete(ref board ThisBoard)
        {
            if (OurLastBest < ThisBoard.score)
            {
                OurLastBest = ThisBoard.score;
                ThisBoard.ExplainBoard("From Strategy");
                ThisBoard.LastBest = ThisBoard.score;
            }
            for (int i = 0; i < ThisBoard.Completed.Count; i++)
            {
                ThisBoard.AddSuitCompleted(ThisBoard.Completed[i].suit);
            }
            return (ThisBoard.score >= GlobalClass.MaxScore);
        }

        /*
         * This just moves the bottom most card to another stack that has the same suit.
         * The move is not made unless it can be reversed (if bReversible is set)
         */

        public bool JoinSameSuits(ref board tb, bool bReversible)
        {
            int n = tb.BottomMost.Count;
            card tC, bC, aC;
            int i, j, k;
            int RankAbove;

            for (i = 0; i < n; i++)
            {
                bC = tb.BottomMost[i];
                series s2 = tb.ThisColumn[bC.iStack].ThisSeries.Last();
                for (j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    tC = tb.TopMost[j];
                    if (tC.suit == bC.suit)
                    {
                        // we have a candidate to join
                        srcCol = tb.ThisColumn[tC.iStack];
                        series s = srcCol.ThisSeries.Last();
                        // s is the series that might be moved under the bottom card
                        if (s.topCard.rank >= (bC.rank - 1) &&
                            s.bottomCard.rank < bC.rank &&
                            s2.topCard.rank > s.topCard.rank)
                        // size of destination must be 
                        //s2.size > s.size)
                        {
                            // we can move all or part of that series under that bottom card
                            for (k = 0; k < s.size; k++)
                            {
                                aC = srcCol.Cards[k + tC.iCard];
                                if (aC.rank + 1 == bC.rank)
                                {
                                    if (aC.iCard > 0)
                                    {
                                        // is not at top of stack
                                        // if Reversibility is required, then do not make the move
                                        // unless the the rank above allows reversing the move
                                        RankAbove = srcCol.Cards[aC.iCard - 1].rank;
                                        if ((RankAbove - 1) != aC.rank && bReversible)
                                        {
                                            return false;
                                        }
                                    }
                                    tb.moveto(tC.iStack, aC.iCard, bC.iStack);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }


        /*
          THIS DOES NOT WORK EXCEPT FOR VERY SIMPLE SEQ SERIES JYS !!!! need UnStack and to use patterns
         * This joins one SEQ series under another so as to concatanate the two series
         * */



        /*
 * this just exposes the top card and check to see if it can be recovered
 * jys this needs to be able to expose all cards that are exposable
 * (8s,6h,7D) expose to 8S,7D,6H
 * 
 * right now, the series underneath must be SEQ, use the SOS one for that, but it does not use placeholders
 * 
 * */
        public int ExposeOneTop(ref board tb)
        {
            int e, n;
            int LocAbove;   // location of card just above the top card
            card CardAbove;
            int Any = 0;
            int OriginalSource;

            foreach (card cT in tb.TopMost)
            {
                LocAbove = cT.iCard - 1;
                if (LocAbove <= 0) continue;    // nothing to expose
                CardAbove = tb.ThisColumn[cT.iStack].Cards[LocAbove];
                if (CardAbove.bFaceUp) continue;    // already exposed
                // we want only unexposed cards and the series underneath must be SEQ

                // we can expose this card by moving the one below it (our card cT)
                if (CardAbove.rank - 1 != cT.rank) continue;    // nope, wrong rank, we want to move it back

                n = tb.ThisColumn[cT.iStack].Cards.Count - cT.iCard;    // number of cards to move
                // the below does no error checking nor any statistics computation


                if (tb.NumEmptyColumns > 0)
                {
                    e = tb.Empties[0];
                    OriginalSource = cT.iStack;
                    tb.PerformMove(cT.iStack, cT.iCard, e, n);
                    tb.PerformMove(e, 0, OriginalSource, n);
                    Any++;
                }
                else
                {
                    // we will have to find a card that can hold it temporarily
                    foreach (card bC in tb.BottomMost)
                    {
                        if (bC.iStack == cT.iStack) continue;
                        if (bC.rank - 1 == cT.rank)
                        {
                            OriginalSource = cT.iStack;
                            n = tb.ThisColumn[cT.iStack].Cards.Count - cT.iCard;
                            tb.PerformMove(cT.iStack, cT.iCard, bC.iStack, n);
                            tb.PerformMove(bC.iStack, bC.iCard + 1, OriginalSource, n);
                            Any++;
                        }
                    }
                }
                if (Any > 0) tb.ThisColumn[cT.iStack].CalculateColumnValue(cT.iStack, tb.NumEmptyColumns, tb.BoardState());
            }

            if (Any > 0)
            {
                tb.ReScoreBoard(); // alternately CalcBoardScore and then GetSortedTops
                return ExposeOneTop(ref tb);
            }
            return Any;
        }

        public bool ConsiderStopRunning(ref int RepeatDepth)
        {
            if (cSC.bExceededONEcolLimit || cSC.bExceededTWOcolLimit || cSC.bExceededThreecolLimit)
            {
                // instead of just timeing out while running, look at how many events have occurred
                RepeatDepth++;
                if (RepeatDepth < 3)
                {
                    return false;
                }
                return true;
            }
            // 18nov2012 may want to let a few of these transpire before signaling exceed
            // for now just return true
            return false;
        }

        public int GetBoards()
        {
            int i, j = 0;
            for (i = 0; i < NewBoards.Count; i++)
            {
                board nb = NewBoards[i];
                cSC.ThisBoardSeries.Add(nb);
                j++;
            }
            NewBoards.Clear();
            Console.WriteLine("Added " + j + " boards with this SOSexpose strategy"); ;
            return j;
        }


    }
}
