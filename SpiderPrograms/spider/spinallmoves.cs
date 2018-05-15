using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace spider
{


    public class cBestScore
    {
        public int iptr;
        public int score;
        public int NumEmptyColumns;
        public int NumCompletedSuits;
        public bool bLastDeal;
        public int SuitJustCompleted;
        public cBestScore(int i, int sScore, int ncNumEmpties, int nsCompleted, bool nbLastDeal)
        {
            iptr = i;
            score = sScore;
            NumEmptyColumns = ncNumEmpties;
            NumCompletedSuits = nsCompleted;
            bLastDeal = nbLastDeal;
            SuitJustCompleted = 0;
        }
    }



    public class cCompleted
    {
        public int suit;
        public int score;
        public int iptr;
    }

    public class cMaxValues
    {
        public int MaxInsertsWhenSuiting;
        public int MinInserts;
        public int MaxTimeoutWhenSuiting;
        public int MinTimeout;
        public int MaxInserts;
        public int TimeOutBest;
        public cMaxValues(int MaxInsertsWhenSuiting, int MinInserts,int MaxTimeoutWhenSuiting,int MinTimeout)
        {
            this.MaxInserts = MinInserts;
            this.MinInserts = MinInserts;
            this.MinTimeout = MinTimeout;
            this.MaxTimeoutWhenSuiting = MaxTimeoutWhenSuiting;
            this.MaxInsertsWhenSuiting = MaxInsertsWhenSuiting;
            this.TimeOutBest = MinTimeout;
        }
    }

    public class cSpinAllMoves
    {
        private cSpinControl cSC;
        public int NumOne, NumTwo, Num3000;
        public int NumThreeOrBetter;
        private int BoardBeingWorked;
        private DateTime TimeLastBest;
        private int OriginalCount;
        private DateTime dtNow, dtLast, dtFirstBoard, TimeSinceFilterRan;
        private TimeSpan dtDiff, TimeSinceFilter;
        private int LastCount;
        private int LastBest;
        private int ActNumScoresUsed;
        private csuitable Suitable;
        private board LastDuplicateBoard = null;
        public int NumSuitsWritten;
        public int[] SuitsCompleted;
        public int RepeatDepth;
        string strSpinType = "";

        public cSpinAllMoves( ref cSpinControl cSC)
        {

//            Suitable = new csuitable(ref MaxValues);

            cSC.stlookup = new Cstlookup(cSC.MaxValues.MaxInsertsWhenSuiting*4);
            cSC.PrevSeed = new cSeed();
            cSC.ThisSeed = new cSeed();
            cSC.SortedScores = new List<cBestScore>(128);
            this.cSC = cSC;
            NumSuitsWritten = 0;
            SuitsCompleted = new int[4];
            ClearSeries();
            RepeatDepth = 0;
            strSpinType = "";
        }

        public void ClearNumSuitedInSeries()
        {
            for (int i = 0; i < 4; i++)
                SuitsCompleted[i] = 0;
        }

        // return true if was able to create a new board
        public bool SpinOneCard(ref board tb, int SrcCol, int SrcCard, int DesCol, int DesCard)
        {
            if (cSC.bTrigger) Console.WriteLine(MethodBase.GetCurrentMethod().Name); 
            int iSecs = 0;
            TimeSpan TimeSinceLastBest;
            
            tb.AssignCompletedID(GlobalClass.TypeCompletedBy.ID_BS);
            tb.moveto(SrcCol, SrcCard, DesCol);

            tb.ReScoreBoard();
            if (tb.NotifySuitJustCompleted)
            {
                NumSuitsWritten++;
                bool WriteOnce = true;
                string strJustCompleted = "_";
                int j = 1;
                for(int i = 0; i < 4;i++)
                {
                    if((j & tb.bitJustCompleted) > 0)
                    {
                        strJustCompleted += GlobalClass.cSuits[i] + "_";
                        SuitsCompleted[i]++;
                        if (SuitsCompleted[i] > 1) WriteOnce = false;
                    }
                    j = j << 1;
                }

                if (WriteOnce)
                {
                    cSC.Deck.SaveBoardAsBin(ref tb, "SUIT_" + NumSuitsWritten.ToString() + strJustCompleted + "_");
                }

            }
            if (tb.score >= cSC.BestScore)
            {
                if (tb.score == cSC.BestScore)
                {
                    LastDuplicateBoard = tb;
                }
                else
                {
                    cSC.BestScore = tb.score;
                    cBestScore cBS = new cBestScore(cSC.ThisBoardSeries.Count, tb.score, tb.NumEmptyColumns, tb.NumCompletedSuits, tb.bOnLastDeal);
                    if (tb.NotifySuitJustCompleted)
                        cBS.SuitJustCompleted |= tb.bitJustCompleted;
                    cSC.SortedScores.Add(cBS);
                    cSC.BestBoard = tb;
                    LastDuplicateBoard = null;
                }
            }
            else
            {
                if (tb.bOnLastDeal && false)
                {
                    cBestScore cBS = new cBestScore(cSC.ThisBoardSeries.Count, tb.score, tb.NumEmptyColumns, tb.NumCompletedSuits, tb.bOnLastDeal);
                    if (tb.NotifySuitJustCompleted)
                        cBS.SuitJustCompleted |= tb.bitJustCompleted;
                    cSC.SortedScores.Add(cBS);
                }
            }

            tb.NotifySuitJustCompleted = false;
            //tb.from = tb.ID;  // board was not spun off here
            tb.ID = cSC.ThisBoardSeries.Count;
            tb.AssignCompletedID();

            if ((GlobalClass.TraceBits & 8) > 0 || cSC.bTrigger) tb.ShowRawBoard();
            cSC.ThisBoardSeries.Add(tb);

            cSC.CountLimit++;
            TimeSinceLastBest = DateTime.Now.Subtract(TimeLastBest);
            if (cSC.bSignalSpinDone) return false;
            if (TimeSinceLastBest.TotalSeconds > cSC.MaxValues.TimeOutBest)
            {
                Console.WriteLine("Aborting due to timeing out");
                if ((GlobalClass.TraceBits & 2) > 0)
                {
                    Console.WriteLine("Best1: " + cSC.BestScore + " dups:" + cSC.NumberDuplicates + " BoardsScored:" + cSC.CountLimit + " LastBoardSpun:" + BoardBeingWorked);
                    Console.WriteLine("Suits made (dchs) " + SuitsCompleted[0].ToString() + " " +
                        SuitsCompleted[1].ToString() + " " +
                        SuitsCompleted[2].ToString() + " " +
                        SuitsCompleted[3].ToString() + " StringSuits:" + tb.strSuitsRemoved);
                    if (OriginalCount > BoardBeingWorked) Console.WriteLine("Did not visit " + (OriginalCount - BoardBeingWorked) + " boards from last deal");
                }
                cSC.bSpinTimedOut = true;
                cSC.bSignalSpinDone = true;
                return false;
            }
            if (tb.score > 3000)
            {
                Num3000++;
                if (tb.NotifySuitJustCompleted)
                {
                    tb.NotifySuitJustCompleted = false;
                    utils.SetSuitableValues(ref cSC, ref tb);
                    //cSC.bSignalSpinDone = true;
                    cSC.bGotOneSuitAtLeast = true;
                    //Console.WriteLine("Completed Suit");
                    //return true;
                }
            }
            else if (tb.NumEmptyColumns > 2)
            {
                NumThreeOrBetter++;
                if (NumThreeOrBetter > 100 && Num3000 == 0)
                {
                    cSC.bSignalSpinDone = true;
                    cSC.bExceededThreecolLimit = true;
                    Console.WriteLine("ABORT:  Too many Three Column events: " + NumThreeOrBetter);
                    return false;
                }
            }
            else if (tb.NumEmptyColumns > 1)
            {
                NumTwo++;
//                if (NumTwo > (cSC.MaxValues.MaxInserts / (1000 - 198 * tb.DealCounter)) && NumThreeOrBetter == 0)
                if (NumTwo > (1+tb.DealCounter)*200 && NumThreeOrBetter == 0)
                {
                    cSC.bSignalSpinDone = true;
                    cSC.bExceededTWOcolLimit = true;
                    Console.WriteLine("ABORT:  Too many Two Column events: " + NumTwo);
                    return false;
                }


            }
            else if (tb.NumEmptyColumns > 0)
            {
                NumOne++;
                //if (NumOne > ((1 + tb.DealCounter) * 1000) && NumTwo == 0)
                if (NumTwo == 0)
                {
                    if (NumOne > (tb.DealCounter < 2 ? 3000 : 6000))
                    {
                        cSC.bSignalSpinDone = true;
                        cSC.bExceededONEcolLimit = true;
                        Console.WriteLine("ABORT:  Too many One Column events: " + NumOne);
                        return false;
                    }
                }
            }

            if (0 == (cSC.CountLimit % 5000) && ((GlobalClass.TraceBits & 4) > 0))
            {
                dtNow = DateTime.Now;
                TimeSpan tc = dtNow.Subtract(tb.TimeOfLastDeal);
                string strSinceLD = "";
                if (tc.TotalSeconds < 60)
                    strSinceLD = Convert.ToInt32(tc.TotalSeconds).ToString("0") + "s";
                else strSinceLD = Convert.ToInt32(tc.TotalMinutes).ToString("0") + "m"; 

                dtDiff = dtNow.Subtract(dtLast);
                dtLast = dtNow;
                iSecs = Convert.ToInt32(dtDiff.TotalSeconds);
                tb.ShowBoard();
                Console.WriteLine("Q(" + cSC.SortedScores.Count + ") " + cSC.BestScore + " dups:" + cSC.NumberDuplicates + " limit:" + cSC.CountLimit/1000 + "k i:" + BoardBeingWorked + " et:" + strSinceLD + " dif:" + (cSC.CountLimit - BoardBeingWorked) + " 1c:" + NumOne + " 2c:" + NumTwo + " 3+" + NumThreeOrBetter + " " + strSpinType);
                cSC.stlookup.ShowBufferStats();
            }


            // if (MoveValue == board.DEALT_A_CARD) return bCreatedBoard;
            // no need to check timouts, etc as we just want to get this board into the system

            if (cSC.CountLimit >= cSC.MaxValues.MaxInserts)
            {
                cSC.bSignalSpinDone = true;
                cSC.bExceededCountLimit = true;
                Console.WriteLine("Aborting!  Exceeded limit when counting");
            }

            if (cSC.bSignalSpinDone && (GlobalClass.TraceBits & 2) > 0)
            {
                dtNow = DateTime.Now;
                dtDiff = dtNow.Subtract(dtLast);
                dtLast = dtNow;
                iSecs = Convert.ToInt32(dtDiff.TotalSeconds);
                tb.ShowBoard();
                Console.WriteLine("SpinDone - Best: " + cSC.BestScore + " dups:" + cSC.NumberDuplicates + " limit:" + cSC.CountLimit/1000 + "k i:" + BoardBeingWorked + " et:" + iSecs + " dif:" + (cSC.CountLimit - BoardBeingWorked) + " 1c:" + NumOne + " 2c:" + NumTwo + " 3+" + NumThreeOrBetter);
                return false;
            }

            if (cSC.bSignalSpinDone) return false;

            LastCount = cSC.ThisBoardSeries.Count;
            if (cSC.BestScore > 500)
            {
                if (cSC.BestScore > LastBest)
                {
                    if ((GlobalClass.TraceBits & 2) > 0)
                    {
                        TraceBoard(LastCount - 1);
                        dtDiff = DateTime.Now.Subtract(dtFirstBoard);
                        tb.ShowBoard();
                        TimeSinceFilter = DateTime.Now.Subtract(TimeSinceFilterRan);
                        Console.WriteLine("Extra:(" + tb.ExtraPoints + ") " + cSC.BestScore + " dups:" + cSC.NumberDuplicates + " limit:" + cSC.CountLimit + " i:" + BoardBeingWorked + " TO: " + cSC.MaxValues.TimeOutBest + " (Sec) MaxIter:" + cSC.MaxValues.MaxInserts + " TimeSinceFilterRan(sec):" + TimeSinceFilter.TotalSeconds.ToString("0"));
                    }
                }
                LastBest = cSC.BestScore;
            }
            return true; // bSignalSpinDone;
        }


        public void ClearSeries()
        {
            cSC.BestScore = 0;
            cSC.bSignalSpinDone = false;
            cSC.ThisSeed.Clear();
            cSC.PrevSeed.Clear();
            cSC.SortedScores.Clear();
            cSC.BestScoreIndex.Clear();
            cSC.ThisBoardSeries.Clear();
            dtLast = DateTime.Now;
        }


        // spin a suit at a time (causes problems, need to spin each card in turn)
        // do NOT move a king to an empty column unless the
        // exposed card frees up another column -OR-
        // there is a king above it that has a larger series value
        // nov2012 do not move king if suit is buildable ???? (maybe)
        // nov2012-1 king must be moved if it hides ALL others
        public bool SpinThisSuitedColumn(ref board tb, int iColumn)
        {
            if (cSC.bTrigger) Console.WriteLine(MethodBase.GetCurrentMethod().Name); 
            column c = tb.ThisColumn[iColumn];
            series s, ss;
            int i, cSeriesValue;
            int RankAbove;
            int n = c.ThisSeries.Count;
            int cntAbove;
            Debug.Assert(n > 0);

            s = c.ThisSeries.Last();
            if (s.topCard.rank == 13 && s.top == 0)
            {
                return true;   // do not move a king from one empty to another
            }
            // nov2012 do not move king if that suit is buildable
            if (s.topCard.rank == 13)
            {
                if(tb.SuitStatus[s.topCard.suit].bSuitsCompletable)
                {
                    return true;
                }
                // must move a king if all (or any?) above it are required!!!!
                if (tb.bOnLastDeal)
                {
                    cSC.Suitable.PerformSuitabiltyStudy(ref tb, s.topCard.suit);
                    if (cSC.Suitable.ExposesRequired.Count > 0)
                    {
                        return SpinTheseCards(ref tb, iColumn, s.top);
                    }
                }
            }



            if (!tb.SuitsLocked)
            {
                cntAbove = 1 + (c.ThisSeries[0].bottom - c.ThisSeries[0].top);
                if (s.topCard.rank == 13 && s.top > 0)  // do not move a king unless ...
                {
                    RankAbove = c.Cards[s.top - 1].rank;
                    if (tb.NumEmptyColumns > 0)
                    {
                        for (i = 0; i < 10; i++)
                        {
                            if (tb.ThisColumn[i].Cards.Count == 0) continue;
                            series t = tb.ThisColumn[i].ThisSeries.Last();  // bottom series
                            if ((RankAbove - 1) == t.topCard.rank)
                            {
                                int ThisCnt = 1 + (s.bottom - s.top);
                                if (((cntAbove + ThisCnt) > 7 && (s.topCard.suit == c.ThisSeries[0].topCard.suit)) || t.top == 0)
                                {
                                    // spinning this king can result in swapping one empty column
                                    // for another or building a series that is probably close enough
                                    return SpinTheseCards(ref tb, iColumn, s.top);
                                }

                            }
                        }
                    }
                    cSeriesValue = s.nValue;    // current series value
                    n--;    // move up above the bottom series
                    // n was is the count, if 0 then nothing more to process
                    if (n == 0) return true;
                    while (n >= 0)
                    {
                        ss = c.ThisSeries[n];
                        n--;
                        if (ss.topCard.rank == 13)
                        {
                            if (ss.nValue > cSeriesValue)
                                return SpinTheseCards(ref tb, iColumn, s.top);
                        }
                    }
                    return true;
                }
            }
            return SpinTheseCards(ref tb, iColumn, s.top);
        }

        // select only 1 card even if more than 1 in suit 
        // if more then 1 in a suit then try both of those card
        // repeat until the entire suit has been spun from this column
        public bool SpinThisColumn(ref board tb, int iColumn)
        {
            if (cSC.bTrigger) Console.WriteLine(MethodBase.GetCurrentMethod().Name); 
            int i, ThisSuit, ThisRank;
            series s = tb.ThisColumn[iColumn].ThisSeries.Last();
            card CardAbove;
            column c = tb.ThisColumn[iColumn];
            int n = c.Cards.Count;
            Debug.Assert(n > 0);
            n--;    // point to bottom most card
            ThisSuit = c.Cards[n].suit; // this suit and this rank are being moved
            ThisRank = c.Cards[n].rank;



            while (n >= s.top)
            {
                if (ThisRank == 13)
                {
                    if (tb.NumEmptyColumns == 0 || c.Cards[n].iCard == 0) return true;
                    // a king cannot be moved anywhere except an empty column
                    // never move a king if it is already in an otherwise empty column
                    // move it only if the card exposed can hold some other column
                    CardAbove = tb.ThisColumn[iColumn].Cards[n - 1];
                    for (i = 0; i < 10; i++)
                    {
                        if (tb.ThisColumn[i].ThisSeries.Count == 1)   // only 1 sequential series
                        {
                            s = tb.ThisColumn[i].ThisSeries[0];
                            if (s.topCard.rank == (CardAbove.rank - 1))
                            {
                                //nb = new board(ref tb);
                                if (!SpinTheseCards(ref tb, iColumn, n)) return false;
                            }
                        }
                    }
                }

                if (!SpinTheseCards(ref tb, iColumn, n)) return false;
                n--;
                if (n < 0 || cSC.bSignalSpinDone) break;
                ThisRank++; // one above it must be 1 higher and same rank
                if (ThisSuit != c.Cards[n].suit || ThisRank != c.Cards[n].rank)
                    break;

            }
            return true;
        }

        // suits are identical in series
        // (1) do NOT put a suited series into more than ONE empty column
        // (2)do NOT spin a suited series from the top of a column to
        // an empty column
        public bool SpinTheseCards(ref board tb, int iColumn, int LocInIcolumn)
        {
            if (cSC.bTrigger) Console.WriteLine(MethodBase.GetCurrentMethod().Name); 
            int i;
            bool bDidEmpty = false;
            int DesRank, DesLoc, SrcRank = tb.ThisColumn[iColumn].Cards[LocInIcolumn].rank;
            for (i = 0; i < 10; i++)
            {
                if (i == iColumn) continue; // dont put card back where it came from
                DesLoc = tb.ThisColumn[i].Cards.Count;
                if (DesLoc == 0)
                {
                    if (bDidEmpty) continue;    // already moved to an empty column (1)
                    if (LocInIcolumn == 0) continue;   // do not move from one empty to another empty (2)
                }
                if (DesLoc > 0)
                {
                    DesRank = tb.ThisColumn[i].Cards[DesLoc - 1].rank;
                    if (SrcRank != (DesRank - 1)) continue;
                }
                else bDidEmpty = true;  // dont put same stuff into 

                if (TrySpinOneCard(ref tb, iColumn, LocInIcolumn, i, DesLoc)) continue;
                return false;
            }
            return true;
        }


        public void TraceBoard(int iBoard)
        {
            cSC.ThisBoardSeries[iBoard].MyMoves.TraceBoard(null);
        }


        public bool ShrinkThisBoardSeries(int nToUse)
        {



            int tbsNum = cSC.ThisBoardSeries.Count;
            board tb;
            int nEmpties = 0;
            int i, j, n = GetBestScores(nToUse, ref nEmpties);


            if (n == 0)
            {  
                return false;
            }

            cSC.NextBoardSeries.Clear();

            if (nEmpties < 3 || true)   // jys !!!!
            {

                for (i = 0; i < n; i++)
                {
                    tb = cSC.ThisBoardSeries[cSC.BestScoreIndex[i]];
                    //int beforeJSS = tb.score;
                    //cSC.JoinSuited.JoinAllSuitedWhenRankable(ref tb);
                    //cSC.BestScore = tb.score;
                    //Debug.Assert(beforeJSS <= tb.score);
                    cSC.NextBoardSeries.Add(tb);
                    if ((GlobalClass.TraceBits & 1) > 0) tb.ShowBoard();
                }
            }
            else
            {
                // get the one(s) with the fewest moves when the boards are about the same
                j = 0;  // count "n" may be different
                tb = cSC.ThisBoardSeries[cSC.BestScoreIndex[0]];
                cSC.NextBoardSeries.Add(tb);
                j++;
                for (i = 1; i < n; i++)
                {
                    board nb = cSC.ThisBoardSeries[cSC.BestScoreIndex[i]];
                    if (tb.Comparable(ref nb) && nb.MyMoves.TheseMoves.Count < tb.MyMoves.TheseMoves.Count)
                    {
                        cSC.NextBoardSeries.RemoveAt(cSC.NextBoardSeries.Count - 1);
                        cSC.NextBoardSeries.Add(nb);
                        tb = nb;
                    }
                    else
                    {
                        cSC.NextBoardSeries.Add(nb);
                        j++;
                        tb = nb;
                    }
                }
                n = j;
            }




            if ((GlobalClass.TraceBits & 2) > 0)
            {
                Console.WriteLine("++++++++Shrank from " + tbsNum + " to " + n + "  and here is the best one...");
                DumpSorted(1);
                cSC.stlookup.ShowBufferStats();
            }
            cSC.ThisBoardSeries.Clear();
            cSC.CountLimit = 0;
            cSC.PrevSeed.CopySeeds(ref cSC.ThisSeed);
            cSC.ThisSeed.Clear();
            for (i = 0; i < n; i++)
            {
                tb = cSC.NextBoardSeries[i];
                tb.MyMoves.AddShrinkCode(ref cSC, n);
                tb.from = tb.ID;
                tb.ID = cSC.ThisBoardSeries.Count;
                cSC.ThisSeed.Add(ref tb);
                cSC.ThisBoardSeries.Add(tb);
                Debug.Assert(tb.ID == (cSC.ThisBoardSeries.Count - 1));
                //if (i == 0)
                //{
                //    cBestScore cBS = new cBestScore(0, tb.score, tb.NumEmptyColumns, tb.NumCompletedSuits);
                //    cSC.SortedScores.Add(cBS);
                //}
            }
            cSC.NextBoardSeries.Clear();
            cSC.BestScore = cSC.ThisBoardSeries[0].score;
            cSC.stlookup.ShrinkPerformed();

            return true;
        }

        public void DumpSorted(int nMax)
        {
            board tb;
            int i, iBoard, n = cSC.BestScoreIndex.Count;
            if (n == 0) return;
            if (n < nMax) nMax = n;
            for (i = 0; i < nMax; i++)
            {
                iBoard = cSC.BestScoreIndex[i];
                tb = cSC.ThisBoardSeries[iBoard];
                tb.ReScoreBoard();
                Console.WriteLine("FromBI(" + i + ") ID:" + tb.ID + " Empties:" + tb.NumEmptyColumns + " Score:" + tb.score + " Completed:" + tb.NumCompletedSuits + " Moves:" + tb.MyMoves.TheseMoves.Count + tb.GetSuitables());
                tb.MyMoves.TraceBoard();
                tb.ShowRawBoard();
            }
        }

        public int GetBestScores(int nToUse, ref int NumEmpties)
        {
            cBestScore cbs;
            NumEmpties = 0;
            cSC.BestScoreIndex = new List<int>(nToUse);
            int i, n = cSC.SortedScores.Count;
            int nBiggerFilter = 0;
            int tCount = 0;
            int ScoreFloor = 0;
            int ColValToUse = 500;  // but not on last deal
            
            ActNumScoresUsed = 0;

            for (i = 0; i < cSC.SortedScores.Count; i++)
            {
                if (NumEmpties < cSC.SortedScores[i].NumEmptyColumns)
                    NumEmpties = cSC.SortedScores[i].NumEmptyColumns; 
            }

            if (n < cSC.ThisSeed.Seeds.Count && cSC.bSpinDidAllBoards && NumEmpties < 3)
            {
                // if n is very small then deal to all the boards
                for (tCount = 0; tCount < n; tCount++)
                {
                    cSC.BestScoreIndex.Add(cSC.ThisSeed.SeedIndex[tCount]);
                }
                ActNumScoresUsed = tCount;
                return tCount;
            }
            if (n == 0) return 0;
//            cSC.SortedScores.Sort(delegate(cBestScore a, cBestScore b) { return a.score.CompareTo(b.score); });
            cbs = cSC.SortedScores.Last();
            
            if (cbs.NumCompletedSuits > 0)
            {
                ScoreFloor = 3000 * cbs.NumCompletedSuits;
                for (i = 0; i < n; i++)
                {
                    if (cSC.SortedScores[i].score > ScoreFloor)
                    {
                        if (cSC.SortedScores[i].NumEmptyColumns > nBiggerFilter)
                        {
                            if(!cSC.SortedScores[i].bLastDeal)
                            nBiggerFilter = cSC.SortedScores[i].NumEmptyColumns;
                        }
                    }
                }
                ScoreFloor += (nBiggerFilter * ColValToUse);
            }
            else
                ScoreFloor = cbs.NumEmptyColumns * ColValToUse;

            if (ScoreFloor == 0)
            {
                // need to have more boards to process than just the number of seeds
                if (n > 500) n = 500;
                nToUse = n;

            }

            tCount = 0;
            for (i = n - 1; i >= 0; i--)
            {
                cbs = cSC.SortedScores[i];
                if (cbs.score >= ScoreFloor)
                {
                    cSC.BestScoreIndex.Add(cbs.iptr);
                    tCount++;
                }
                else break;
                if (tCount == nToUse)
                {
                    ActNumScoresUsed = tCount;
                    return tCount;
                }
            }
            ActNumScoresUsed = tCount;
            return tCount;
        }
 

        public bool TrySpinOneCard(ref board tb, int SrcCol, int SrcCard, int DesCol, int DesCard)
        {
            if (cSC.bTrigger) Console.WriteLine(MethodBase.GetCurrentMethod().Name); 
            board nb = null;
            int[] des = new int[112];
            Int64 ChkWord = 0;
            int WouldGoHere = cSC.ThisBoardSeries.Count;
            bool bInsertFailed = false, bResult = false;
#if AINT
            int[] des1 = new int[112];
            Int64 ChkWord1 = 0, ChkWord2 = 0;
            int desptr1;
            bool bVerify = true;
#endif
            int desptr = tb.FormDupList(SrcCol, SrcCard, DesCol, DesCard, ref des, ref ChkWord);
            // check for a duplicate move before creating a new board
            // note that the number of actual moves is 1 less since we have not yet moved anything

            if (cSC.stlookup.bIsNewBoard(ChkWord, desptr, ref des, tb.MyMoves.TheseMoves.Count + 1, ref WouldGoHere, ref bInsertFailed))
            {
                if (bInsertFailed)
                {
                    cSC.bSignalSpinDone = true;
                    cSC.bOutOfSpaceInSTLOOKUP = true;
                    return false;
                }
                if (WouldGoHere != cSC.ThisBoardSeries.Count)
                {
                    // this indicates the lookup program found an identical board that had more moves
                    // replace that board with the moves from this one.  

                    //ThisBoardSeries[WouldGoHere].DoNotUse = true;
                    cSC.ThisBoardSeries.RemoveAt(WouldGoHere);
                    nb = new board(ref tb);
                    bResult = SpinOneCard(ref nb, SrcCol, SrcCard, DesCol, DesCard);
                    cSC.ThisBoardSeries.Insert(WouldGoHere, nb);
                    cSC.ThisBoardSeries.RemoveAt(cSC.ThisBoardSeries.Count - 1);
                    return bResult;
                }

                // either it was not found or it was found but we cannot update the queue
                // because the pointer WouldGoHere is invalid (board was shrunk)
                nb = new board(ref tb);
                bResult = SpinOneCard(ref nb, SrcCol, SrcCard, DesCol, DesCard);

#if AINT
                //bVerify = tbsCompare1(0, 12);
                //tbsDump(WouldGoHere, desptr, ref des);
                //if ((WouldGoHere == 12 && RunCounter == 2) || WouldGoHere == 0)
                //{
                //    tbsDump(WouldGoHere, desptr, ref des);
                //}
                //if (WouldGoHere == 13 || WouldGoHere == 102)
                //{
                //    tbsDump(WouldGoHere, desptr, ref des);
                //    desptr = tb.FormDupList(SrcCol, SrcCard, DesCol, DesCard, ref des, ref ChkWord2);
                //    desptr1 = nb.FormVerify(ref des1, ref ChkWord1);
                //    Console.WriteLine("WGH: " + WouldGoHere + " " + ChkWord.ToString("x16") + " " + ChkWord1.ToString("x16") + " " + ChkWord2.ToString("x16"));
                //}
                if((iTrace & 0x10) > 0)
                {
                    ChkWord1 = 0;
                    //desptr = tb.FormDupList(SrcCol, SrcCard, DesCol, DesCard, ref des, ref ChkWord);
                    desptr1 = nb.FormVerify(ref des1, ref ChkWord1);
                    bVerify = (desptr == desptr1);
                    bVerify |= (ChkWord == ChkWord1);
                    for (int ii = 0; ii < desptr; ii++)
                    {
                        bVerify |= (des[ii] == des1[ii]);
                    }
                    Debug.Assert(bVerify);
                }
#endif
                return bResult;

            }
            else
            {
                cSC.NumberDuplicates++;
            }
            return true;
        }


        public bool SpinOff(ref board tb)
        {
            int i, j;
            if(cSC.bTrigger)Console.WriteLine(MethodBase.GetCurrentMethod().Name);
            int n = tb.ThisColumn[10].Cards.Count;
            for (i = 0; i < 10; i++)
            {
                j = i; // cCO[i].ptr;
                if (tb.ThisColumn[j].Cards.Count == 0) continue;    // column is empty: nothing to spin
                if (tb.NumEmptyColumns > 2) // || tb.bOnLastDeal ||(n==10 && tb.NumEmptyColumns == 1 ))
                {
                    strSpinType = "Spin by suit!";
                    if (SpinThisSuitedColumn(ref tb, j)) continue;
                    return false;
                }
                else
                {
                    if (SpinThisColumn(ref tb, j)) continue;
                    return false;
                }
            }
            return true;
        }


        public void AddNewBoard(ref board ThisBoard)
        {
            ThisBoard.ReScoreBoard();
            ThisBoard.ID = cSC.ThisBoardSeries.Count;
            cSC.ThisBoardSeries.Add(ThisBoard);
        }


        // this is done from the startup program -or-
        // it could be done from DealStrategy where new boards are created
        // from a bunch of boards that have empty columns
        public void AddInitialBoard(ref board nb, int TypeInitialBoard, ref csuitable Suitable)
        {
            int desptr;
            int[] des = new int[112];
            Int64 ChkWord = 0;
            this.Suitable = Suitable;
            bool bInsertFailed = false; 
            switch (TypeInitialBoard)
            {
                case GlobalClass.FIRST_CARD:
                    int tsbCount = 0;
                    nb.ComputeRemainingSuits();
                    LastBest = nb.score - 1;
                    cSC.BestScore = LastBest;
                    nb.TimeOfLastDeal = DateTime.Now;
                    nb.DealCounter = 5 - nb.ThisColumn[10].Cards.Count / 10;
                    // if there are 20 cards left, we must have dealt 3 times
                    
                    nb.MyMoves.AddMoveInfo(TypeInitialBoard, nb.DealCounter, 0);
                    nb.from = -1;   // didnt come from another board
                    desptr = nb.FormVerify(ref des, ref ChkWord);
                    cSC.stlookup.bIsNewBoard(ChkWord, desptr, ref des, nb.MyMoves.TheseMoves.Count, ref tsbCount, ref bInsertFailed);
                    Debug.Assert(!bInsertFailed);
                    Debug.Assert(0 == tsbCount);
                    break;
                case GlobalClass.DEALT_A_CARD:

                    break;
            }
            AddNewBoard(ref nb);
        }

        public bool RunFilter(int BoardsToSave)
        {

            bool bCanSpinMore = true;
            int LastCount;
            cSC.CountLimit = 0;
            BoardBeingWorked = 0;
            board tb, OldBoard;
            bool bOnlyNotifyOnce = true;
            cSC.bSignalSpinDone = false;
            cSC.bExceededCountLimit = false;
            cSC.bExceededONEcolLimit = false;
            cSC.bExceededTWOcolLimit = false;
            cSC.bExceededThreecolLimit = false;
            cSC.bOutOfSpaceInSTLOOKUP = false;
            cSC.bGotOneSuitAtLeast = false;
            dtFirstBoard = DateTime.Now;
            cSC.SortedScores.Clear();
            cSC.BestScoreIndex.Clear();
            cSC.TimeDealStarted = TimeLastBest;
            NumOne = 0;
            NumTwo = 0;
            NumThreeOrBetter = 0;
            Num3000 = 0;
            TimeLastBest = DateTime.Now;    // in event we never get a "best"
            TimeSinceFilterRan = TimeLastBest;
            OriginalCount = cSC.ThisBoardSeries.Count;
            bool bAnyInShrink;
            //bool bAbleToReduce = false;
            ClearNumSuitedInSeries();
            while (BoardBeingWorked < cSC.ThisBoardSeries.Count)
            {
                OldBoard = cSC.ThisBoardSeries[BoardBeingWorked];
                tb = new board(ref OldBoard);
                tb.from = OldBoard.ID;
                //cSC.Strategy.ExposeOneTop(ref tb);
                tb.RunCounter = cSC.RunCounter++;                

                if (tb.NotifySuitJustCompleted && bOnlyNotifyOnce)
                {
                    tb.NotifySuitJustCompleted = false;
                    bOnlyNotifyOnce = tb.bIsCompletable;    // once all are done then no need for hi values
                    utils.SetSuitableValues(ref cSC, ref tb);
                }

               // cSC.JoinSuited.JoinAllSuitedWhenRankable(ref tb);
            // 12november2012 had to comment out the above because makeing wrong moves
                int any = cSC.Strategy.FirstStrategy(ref tb);

                //if (BoardBeingWorked < OriginalCount)
                //{
                //    int any = cSC.Strategy.FirstStrategy(ref tb);
                //    bAbleToReduce = (any > 0);
                //    if (bAbleToReduce)
                //    {
                //        cSC.Strategy.GetBoards();
                //    }
                //}
                //else
                //{
                //    int any = cSC.Strategy.RunJoinables(ref tb, true);
                //    bAbleToReduce = (any > 0);
                //}



                LastCount = cSC.ThisBoardSeries.Count;
                bCanSpinMore = SpinOff(ref tb);
                if (GlobalClass.bFoundFirstColunn)
                {
                    GlobalClass.bFoundFirstColunn = false;
                    cSC.cBD.SetCS(ref GlobalClass.FirstEmptyColumn);
                    cXmlFromBoard xtest = new cXmlFromBoard();
                    xtest.ReCreateBinFile(ref GlobalClass.FirstEmptyColumn, ref cSC, "FIRST");
                    cSC.Deck.SaveFirstBoardMoves(ref GlobalClass.FirstEmptyColumn);
                }
                OldBoard.nchild = cSC.ThisBoardSeries.Count - LastCount;
                BoardBeingWorked++;
                if (cSC.bSignalSpinDone)
                {
                    Console.WriteLine("Spin Done");
                    break;
                }
            }
            if (LastDuplicateBoard != null)
            {
                int LDB_ID = LastDuplicateBoard.ID;
                cBestScore cBS = new cBestScore(LDB_ID,
                    LastDuplicateBoard.score,
                LastDuplicateBoard.NumEmptyColumns,
                LastDuplicateBoard.NumCompletedSuits,
                LastDuplicateBoard.bOnLastDeal);
                cSC.SortedScores.Add(cBS);
                Debug.Assert(LastDuplicateBoard.score == cSC.ThisBoardSeries[LDB_ID].score);
                LastDuplicateBoard = null;
            }

            cSC.bSpinDidAllBoards = (BoardBeingWorked == cSC.ThisBoardSeries.Count);
            //if (cSC.bExceededONEcolLimit) cSC.bSpinDidAllBoards = true;
            cSC.bStopThisRun = cSC.Strategy.ConsiderStopRunning(ref RepeatDepth);
            if ((GlobalClass.TraceBits & 2) > 0) Console.WriteLine("DidAllBoards:" + cSC.bSpinDidAllBoards + " SpinDone:" + cSC.bSignalSpinDone + " ScoresSaved:" + cSC.SortedScores.Count + " NumSeeds:"+cSC.ThisSeed.SeedIndex.Count);
            bAnyInShrink = ShrinkThisBoardSeries(BoardsToSave);
            if (!bAnyInShrink)
            {
                int n = cSC.ThisSeed.SeedIndex.Count;
                Console.WriteLine("None in shrink but seed was:" + n.ToString());
                for (int i = 0; i < n; i++)
                {
                    cSC.ThisBoardSeries[cSC.ThisSeed.SeedIndex[i]].MyMoves.AddShrinkCode(ref cSC,n);
                }
            }
            return bAnyInShrink;
        }
    }
}
