using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


/*
 * deal to  1 empty coluumn gets about 8 boards
 * with 2 empty columns about 64
 * with 3 empty probably ??
 * was getting about 28,000 for 4 empty columns
 * */

namespace spider
{
    class cDealStrategy
    {

        private List<board> NewBoards;
        private int StartOfDealPtr;
        board nb;   // new board
        List<int> OEmpties;
        public int[] stats = new int[10];
        private int NumCreated = 0;
        private cSpinControl cSC;


        public int GetBoards(ref cSpinControl cSC)
        {
            int i, j=0;
            int OriginalCount = NewBoards.Count;
            board nb;
            cSC.ThisBoardSeries.Clear();
            for (i = 0; i < NewBoards.Count; i++)
            {
                nb = NewBoards[i];
                //if (nb.NumEmptyColumns == 0 || i > OriginalCount)
                if(nb.bWasDealtTo)
                {
                    //bs.Suitable.PerformSuitability(ref nb, ref NewBoards);
                    //if (j == 0)
                    //{
                    //    bs.SetSuitableValues(nb.bIsCompletable);
                    //}
                    nb.ID = cSC.ThisBoardSeries.Count;
                    cSC.ThisBoardSeries.Add(nb);
                    j++;
                }
            }
            NewBoards.Clear();
            Console.WriteLine("Added " + j + " boards with this new deal");
            return j;
        }

        public cDealStrategy(ref cSpinControl cSC)
        {
            NewBoards = new List<board>();
            OEmpties = new List<int>();
            StartOfDealPtr = 0;
            this.cSC = cSC;
        }

        // this spins only a series of cards of same suit
        private bool SpinCardInto(ref board tb, int ColumnToFill)
        {
            int i;
            series s;

            Debug.Assert(tb.ThisColumn[ColumnToFill].Cards.Count== 0);
            // do not move a card into a column that contains something

            for (i = 0; i < 10; i++)
            {
                if (OEmpties.Contains(i)) continue;
                // do not move from empty column because it is either empty
                // or we just put something in it so as to make it not empty

                if (tb.ThisColumn[i].ThisSeries.Count == 1 && tb.ThisColumn[i].ThisSeries.Last().top == 0) continue;
                // do not create an empty column just to fill another one

                nb = new board(ref tb);
                nb.dead = false;
                s = nb.ThisColumn[i].ThisSeries.Last();
                //stats[i]++;

                nb.moveto(i, s.top, ColumnToFill);
                //nb.ThisColumn[i].CalculateColumnValue(i);
                //nb.ThisColumn[ColumnToFill].CalculateColumnValue(ColumnToFill);
                nb.ReScoreBoard();
                NewBoards.Add(nb);                
            }
            return false;
        }

        private bool OutOfCardsOrDone(ref board tb)
        {
            if (tb.TotalCardsOnBoard < 10)
            {
                if (tb.score >= GlobalClass.MaxScore) return true;
                Console.WriteLine("ERROR:  Not enough remaining cards to cover empty columns");
                throw GlobalClass.No_Cards_Left;
            }
            if (tb.DealCounter == 5)
            {
                Debug.Assert(tb.ThisColumn[10].Cards.Count == 0);
                return true;
            }
            return false;
        }

        // 20nov2012 need to prevent too many boards from being created
        // problem arises when there are more then 1 empty column
        // Do not return more than NumWanted boards total
        // spread those number across the number of empty columns using decimation
        // by multipling by NumEmptys and then using only every 1 out of NumEmptys

        public int DealToBoard(ref board tb, DateTime TimeOfLastDeal, int NumBoardsTotal)
        {
            int i,n, NumEmpties, NonEmpties, nbPtr = NewBoards.Count;
            bool bMore=true;
            int lBest = tb.score;
            int NumWantedMax = 32;
            int NumWanted = tb.NumEmptyColumns * NumWantedMax * NumBoardsTotal;
            int NumGenerated = 0;
            int DecimationCnt = NumWanted / NumWantedMax;
            int[] NumDealtTo;
            if (DecimationCnt == 0) DecimationCnt = 1;
            tb.bWasDealtTo = false;

            if(OutOfCardsOrDone(ref tb))return 0;
            StartOfDealPtr = nbPtr;
            tb.CopyWorkingInts(ref OEmpties, GlobalClass.WorkingType.tEmpties);            
            tb.score = lBest;

            NumGenerated += AddBoard(ref tb, TimeOfLastDeal);
            bMore = (NumGenerated < NumWanted) ;
            // we reduced this board to a series that have 1 column less (if it has an empty column)
            while (bMore)
            {
                while (nbPtr < NewBoards.Count)
                {
                    if (!NewBoards[nbPtr].bWasDealtTo)
                    {
                        board nb = NewBoards[nbPtr];
                        nb.score = lBest;
                        NumGenerated += AddBoard(ref nb, TimeOfLastDeal);
                    }
                    nbPtr++;
                    if (NumGenerated >= NumWanted)
                    {
                        bMore = false;
                        break;
                    }
                }
                bMore = false;
                //NonEmpties = 0;
                //for (i = StartOfDealPtr; i < NewBoards.Count; i++)
                //{
                //    if (NewBoards[i].NumEmptyColumns == 0)
                //    {
                //        NonEmpties++;
                //    }
                //}
                //bMore = bMore && (NonEmpties < NumWanted);
                //nbPtr = StartOfDealPtr;
            }
            NumDealtTo = new int[NumGenerated];
            n = 0;
            for (i = StartOfDealPtr; i < NewBoards.Count; i++)
            {
                if (NewBoards[i].bWasDealtTo)
                {
                    NumDealtTo[n] = i;
                    NewBoards[i].bWasDealtTo = false;
                    n++;
                }
            }
            for (i = 0; i < n; i += DecimationCnt)
            {
                NewBoards[NumDealtTo[i]].bWasDealtTo = true;
                NumCreated++;
            }
            //for(i=StartOfDealPtr;i<NewBoards.Count;i++)
            //{
            //    board nb = NewBoards[i];
            //    if (nb.NumEmptyColumns == 0)
            //    {
            //        //nb.dead = true;  // finished with this one also
            //        // deal does not add a board so it needs to be marked dead ...but...
            //        // the check for empty columns is the same as check for dead
            //        NumCreated++;
            //        //GetBetterMoves(ref nb);
            //        // 11nov2012 somehow we are dealing a 2nd time so dont deal to a dead board
            //        if(!nb.bWasDealtTo)
            //        {
            //            Debug.Assert(false);    // dead is not used anymore ???
            //            nb.deal(lBest, TimeOfLastDeal);
            //            nb.bWasDealtTo = true;
            //        }
            //    }
            //}
            return NumCreated;
        }

        private void GetBetterMoves(ref board tb)
        {
            int iSeries = -1;
            int NumMoves = cSC.stlookup.AnyFewerMoves(ref tb, ref iSeries);
            if (NumMoves < tb.MyMoves.TheseMoves.Count)
            {
                board nb = cSC.ThisBoardSeries[iSeries];
                tb.MyMoves.CopyMoves(ref nb.MyMoves);
            }
        }

        private int AddBoard(ref board tb, DateTime TimeOfLastDeal)
        {

            int ColumnToFill;

            if (tb.Empties.Count == 0)
            {
                //GetBetterMoves(ref tb);
                tb.deal(tb.score, TimeOfLastDeal);
                tb.bWasDealtTo = true;
                NewBoards.Add(tb);
                return 1;
            }
            // have empty columns, fill on column and add that board to the new series
            // note that if several columns are empty then we must call addboard again
            for (int i = 0; i < tb.Empties.Count; i++)
            {
                ColumnToFill = tb.Empties[i];
                SpinCardInto(ref tb, ColumnToFill);
            }
            return 0;
        }

    }
}
