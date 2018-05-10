using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace spider
{
    public class Cstlookup
    {
        private Int16[] STtable;
        Int64[] MainCheck;
        int[] MainCheckIndex;
        int[] BoardSeriesIndex; // this cannot be searched from 0 after a shrink is performed
        private int BSISearchStart;     // start searching from here after a shrink
        int MCptr, STptr;
        private int MaxInserts;
        double pctMC, pctST;
        bool bTrigger = false;

        public void SetDebugTrigger(bool bTrigger)
        {
            this.bTrigger = bTrigger;
        }

        public Cstlookup(int NumInserts)
        {
            MaxInserts = NumInserts;
            MainCheck = new Int64[MaxInserts];
            STtable = new Int16[MaxInserts * GlobalClass.STLOOKUP_MPLY];   // this holds all the moves
            MainCheckIndex = new int[MaxInserts];
            BoardSeriesIndex = new int[MaxInserts];
            Clear();
        }

        private int BoardLookup(Int64 mc, int nCardSeq, ref int[] CardSeq, ref int WhereInSeries)
        {
            if (bTrigger) Console.WriteLine(MethodBase.GetCurrentMethod().Name); 
            int i, j, n, jptr;
            int NumMoves = 9999;
            bool bFound = false;
            for (i = MCptr - 1; i >= 0; i--)
            {
                bFound = false;
                if (MainCheck[i] == mc)
                {
                    jptr = MainCheckIndex[i];
                    n = STtable[jptr++];
                    if (n != nCardSeq)
                    {
                        continue;
                    }
                    for (j = 0; j < nCardSeq; j++)
                    {
                        if (STtable[jptr++] != CardSeq[j])
                        {
                            bFound = false;
                            break;
                        }
                    }
                    if (bFound)
                    {
                        NumMoves = STtable[jptr];
                        WhereInSeries = BoardSeriesIndex[i];
                        return NumMoves;
                    }
                }
            }
            return NumMoves;
        }

        public int AnyFewerMoves(ref board tb, ref int WhereInSeries)
        {
            int[] des = new int[112];
            Int64 ChkWord = 0;
            int desptr;
            desptr = tb.FormVerify(ref des, ref ChkWord);
            return BoardLookup(ChkWord, desptr, ref des, ref WhereInSeries);

        }

        public bool cstCompare(int i1, int i2)
        {
            int stPtr1, stPtr2;
            int nCard1, nCard2;
            int i;
            bool bSame = false;
            stPtr1 = MainCheckIndex[i1];
            stPtr2 = MainCheckIndex[i2];
            nCard1 = STtable[stPtr1];
            nCard2 = STtable[stPtr2];
            if (nCard1 == nCard2)
            {
                for (i = 0; i < nCard1; i++)
                {
                    if (STtable[i + stPtr1] != STtable[i + stPtr2]) break;
                    if ((i + 1) == nCard1) bSame = true;
                }
            }
            return bSame;
        }

        // search backwards thru BoardSeriesIndex to the value BSISearchStart;
        // else we may get the wrong index into the BoardSeries
        // this is only used to update the BoardSeries when the number of moves of a new board
        // is better than the number of moves of an older one.
        public void ShrinkPerformed()
        {
            BSISearchStart = MCptr;
        }

        private bool stinsert(Int64 mc, int nCardSeq,  ref int[] CardSeq, int NumMoves, int BSIloc)
        {
            if (bTrigger) Console.WriteLine(MethodBase.GetCurrentMethod().Name); 
            int i;
            Debug.Assert(MCptr < MaxInserts);
            if (MCptr >= MaxInserts)
            {
                Console.WriteLine("ran out of index space and had to signal abort");
                return false;
            };
            MainCheck[MCptr] = mc;
            BoardSeriesIndex[MCptr] = BSIloc;
            MainCheckIndex[MCptr++] = STptr;
            Debug.Assert((STptr + nCardSeq + 2) < MaxInserts * GlobalClass.STLOOKUP_MPLY);
            if ((STptr + nCardSeq + 2) >= MaxInserts * GlobalClass.STLOOKUP_MPLY)
            {
                Console.WriteLine("ran of symbol space and had to signal abort");
                return false;
            }

            STtable[STptr++] = Convert.ToInt16(nCardSeq);

            for (i = 0; i < nCardSeq; i++)
            {
                STtable[STptr++] = Convert.ToInt16(CardSeq[i]);
            }
            STtable[STptr++] = Convert.ToInt16(NumMoves);
            return true;
        }
        
       

        // if we find something we set "true" ie: it was found.
        // however, it is NOT a new board if found in our lookup table
        // The lookup index is returned if 
        public bool bIsNewBoard(Int64 mc, int nCardSeq, ref int[] CardSeq, int NumMoves, ref int BSIloc, ref bool bInsertFailed)
        {
            if (bTrigger) Console.WriteLine(MethodBase.GetCurrentMethod().Name); 
            int i, j, n, jptr;
            bool bFound = false;
            for (i = MCptr-1; i >= 0; i--)
            {
                bFound = false;
                if (MainCheck[i] == mc)
                {
                    jptr = MainCheckIndex[i];
                    n = STtable[jptr++];
                    if (n != nCardSeq)
                    {
                        continue;
                    }
                    bFound = true;
                    for (j = 0; j < nCardSeq; j++)
                    {
                        if (STtable[jptr++] != CardSeq[j])
                        {
                            bFound = false;
                            break;
                        }
                    }
                    if (bFound)
                    {
                        if (NumMoves < STtable[jptr])
                        {
                            //STtable[jptr] = (Int16)NumMoves;
                            if (i >= BSISearchStart)
                            {
                                BSIloc = BoardSeriesIndex[i];
                            }
                            else
                            {
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            if (bFound) return false;
            bool bSpaceLeft = stinsert(mc,  nCardSeq, ref CardSeq, NumMoves, BSIloc);
            if (!bSpaceLeft)
            {
                bInsertFailed = true;
            }
            else bInsertFailed = false;
            return true;
        }


  

        public void ShowBufferStats()
        {
            double xMaxI = MaxInserts;
            if (MCptr > 0)
            {
                pctMC = 100.0 * MCptr / xMaxI;
                pctST = 100.0 * STptr / (Convert.ToDouble(GlobalClass.STLOOKUP_MPLY) * xMaxI);
                Console.WriteLine("% Symtables - index: " + pctMC.ToString("##.## and table: ") + pctST.ToString("##.##") + " MCptr:" + MCptr);
            }
        }
        public void Clear()
        {
            ShowBufferStats();
            MCptr = 0;
            STptr = 0;
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
