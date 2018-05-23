#define SAVE_PNG1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;


using System.Drawing;
using System.Windows.Forms;

/*
 * deck consists of 104 cards face down.  Top card goes face down to stack 1, next card to stack2
 * there are 10 stacks.  54 cards are dealt to the columns and the remaining 50 are kept in the deck
 * the first 4 stacks have 6 cards, remaining 6 stacks have 5 cards
 * 
layout from the xml file as follows
stack   x 	y
10	721	17
9	642	17-45 (step 7) but the non-hidden ones are 23 down
8	563	17-45 (step 7)
7	484
6	405
5	326
4	247	17-52 (step 7)
3	168
2	 89
1	 10

11	610	380 (13 cards)
	604	380 (13 cards)
	598	380 (13 cards)
	592	380 (11 cards)

 * 
 * 
complete (110,380)

x	y
110	380 (13 cards)
140	380 (13 cards)
repeat for all completed decks
all faceup, all disabled


types are 0..51 (1 for each card)

seems to be random but different boards had same values!
 * 
 * */


namespace spider
{

    public class cEventClass
    {
        public int ID;
        public int iStack;
        public int iCard;
        public int DesStack;
        public int Info_ID; // built suit, dealt card, etc
        public int Info_Val;
        public int score;
        private char A;     
        public char GetCode()
        {
            return A;
        }
        public void SetCode(char A)
        {
            this.A = A;
        }
    }

    
    public class cCardStack
    {
        public int X;
        public int Y;
        public int NumEndCardsSpaced;
        public int stackid;
    }


    public class cRankSuitType
    {
        public int rank;
        public int suit;
    }
    

    public class cGameState
    {
        public string Version;
        public string GameSeed;
        public string Score;
        public string Moves;
    }

    public class cBuildDeck
    {
        public string strCreatedXML;
        public string strUnix;
        public string strXMLout;
        public string strGatheredMoves;
        string strGatheredName;
        public class cEventStats
        {
            public int score;
            public int index;
            public string strCode;
            public bool ScoreLookup;
            public int TypeCode;
            public cEventStats()
            {
                score = -1;
                ScoreLookup = false;
                strCode = "";
                TypeCode = 0;
            }
        }
        cEventStats[] EventStatistics = new cEventStats[GlobalClass.MAXINFO];
        int RootCount = 0;

        public string strEventInfoPrompt = "";
        public List<cEventClass> AllEvents = new List<cEventClass>();
        private cSpinControl cSC;
        private card NewCard;
        //private string strSPloc = "";   // location of file below "nam"
        //private string strSPnam = "";   // name but not extension
        //private string SourceXML = "Spider Solitaire.SpiderSolitaireSave-ms";
        //private string DesXML = "Spider Solitaire.SpiderSolitaireSave-ms.xml";
        private string strValue;
        private int[,] DeckIndex = new int[6, 10];

        public Int16 CurrentStack = -1;
        public char CurrentSuit = ' ';
        public Int16 CurrentRank = 0;
        public Int16 CurrentOffset = 0;
        public int CurrentCode;
        public bool DoingDeck = false;
        public bool DoingComplete = false;
        public bool bFaceUp = false;
        public int CurrentType = 0;
        public int locXML;
        public cGameState GameState = new cGameState();
        private int CurrentCardStack = -1;
        cCardStack[] msCS = new cCardStack[12];
        private int[] KeepOrder = new int[12];
        private int OrderCounter = 0;
        public XmlDocument doc;
        private bool bJustReadNewXML = false;
        private static board OriginalSavedBoard;  // this is always the saved board from microsoft
//        private board InitialBoard;

        private bool LookForSubstituteXML(int XMLtoRead)
        {
            cSC.XML_Diag_filename = "";
            cSC.bJustReadXML = false;
            if (XMLtoRead > 0)
            {
                cSC.XML_Diag_filename = GlobalClass.strSpiderName + ".xml" + XMLtoRead;
                cSC.bJustReadXML = File.Exists(cSC.XML_Diag_filename);
                if (!cSC.bJustReadXML)
                {
                    Console.WriteLine("XML file specifed does not exist! " + cSC.XML_Diag_filename);
                    GlobalClass.Cannot_find_file.Data.Add("MSG", "XML file specifed does not exist! " + cSC.XML_Diag_filename);
                    throw GlobalClass.Cannot_find_file;
                }                
            }

            // the above will cause 1..6 to be read if 1..6 exists
            // 0 will always read the plain xml file
            // if file extension newxml exists it will alway be read in place of anything else

            bool bNewExists = File.Exists(GlobalClass.strSpiderName + ".newxml");
            if (bNewExists)
            {
                if (cSC.bJustReadXML)
                {
                    Console.WriteLine("A newxml file exists, but you also requested " + cSC.XML_Diag_filename);
                    GlobalClass.NewXML_file_exists.Data.Add("MSG", "A newxml file exists, but you also requested " + cSC.XML_Diag_filename);
                    throw GlobalClass.NewXML_file_exists;
                }
                cSC.XML_Diag_filename = GlobalClass.strSpiderName + ".newxml";
                cSC.bJustReadXML = true;
                bJustReadNewXML = true;
            }

            if(XMLtoRead == -1) // this is for MakeXML so I dont have to totally rewrite this routine like I should
            {
                cSC.XML_Diag_filename = GlobalClass.strSpiderName + ".xml";
                return false;
            }

            if (!cSC.bJustReadXML)
            {

                DirectoryInfo di = new DirectoryInfo(GlobalClass.strSpiderDir);
                FileInfo[] rgFiles = di.GetFiles(GlobalClass.SaveName + ".xml?");
                foreach (FileInfo fi in rgFiles)
                {
                    fi.Delete();
                }
                cSC.XML_Diag_filename = GlobalClass.strSpiderName + ".xml";
            }

            return cSC.bJustReadXML;
        }


        public cBuildDeck(string strLoc, int XMLtoRead, ref cSpinControl cSC)
        {
            int i;
            for (i = 0; i < GlobalClass.MAXINFO; i++)
                EventStatistics[i] = new cEventStats();

            GlobalClass.LocMeta = new cCardMeta[104];
            for (i = 0; i < 104; i++)
            {
                GlobalClass.LocMeta[i] = new cCardMeta();
            }

            this.cSC = cSC;
            cSC.cBD = this;
            //strSPloc = Path.GetDirectoryName(strLoc) + "\\";

            for (i = 0; i < 12; i++)
                msCS[i] = new cCardStack();
            GameState = new cGameState();
            LookForSubstituteXML(XMLtoRead);
        }

        //private string ProperName(card c)
        //{
        //    string suit = GlobalClass.SuitNames[c.suit];
        //    string name = GlobalClass.CardNames[c.rank - 1] + "Of" + suit;
        //    if (c.ID > 51) name += "1";
        //    return name;
        //}

        


 
    public static byte[] ConvertImage(System.Drawing.Image img)
    {
        Byte[] data;
        using(MemoryStream ms = new MemoryStream())
        {
            img.Save(ms, img.RawFormat);
            data = new byte[ms.Length];
            data = ms.ToArray();
        }
        return data;
    }



    public void WritePng()
    {
        FileStream outStream = File.Create("c:\\temp\\temp.png");
        BinaryWriter bw = new BinaryWriter(outStream);
        bw.Write(cSC.PngArray, 0, cSC.PngArray.Length);
        bw.Close();
    }

    private void Get4(ref byte[] b4, int iStart, int n)
    {
        for (int i = 0; i <= 3; i++)
        {
            b4[i + iStart] = (byte)((n >> (i * 8)) & 0x000000FF);
        }
    }

        // Fullpathname may not always include extension like it used to
        public void CreateXml()
        {
            int n, locPNG;
            int NLEN = 1048576;
            int kVal;
            int xmlsize = 0;
            int bLocXml = 0;    // location of the XML in bytes from the origin of the file
            strCreatedXML = "";
            byte[] inbuf; // = new byte[NLEN];
            
            FileStream inStream = File.OpenRead(GlobalClass.strSpiderBin);
            NLEN = Convert.ToInt32(inStream.Length);
            inbuf = new byte[NLEN + 1];
            BinaryReader br = new BinaryReader(inStream);
#if SAVE_PNG            
            FileStream outStream;
            BinaryWriter bw;
            outStream = File.Create(strFullpathname + ".png");
            bw = new BinaryWriter(outStream);
#else
            cSC.PngArray = ConvertImage(PngResource.jys33); //spiderpng);
#endif
            //WritePng();


            n = br.ReadInt32();         // RGMH
            n = br.ReadInt32();         // null but I did see a 01 00 00 00
            locPNG = br.ReadInt32();    // should be 2028
            n = br.ReadInt32();         // null
            n = br.ReadInt32();         // null
            kVal = br.ReadInt32();  // size of png stuff
            
            // the value for the size of the png is located at ---
            // 5 * 4  = 20 bytes into the file
            // this must be changed to the size of the png since the png is somehow changed by the 
            // resource handler

            bLocXml += (6 * 4);



            locXML = kVal + locPNG + 6;
            // locXML is where it starts, but we have already read 4*6 bytes

            br.ReadBytes(locXML - 24);
            xmlsize = NLEN - locXML;

            inbuf = new byte[xmlsize];
            n = br.Read(inbuf, 0, xmlsize - 1);
            if (inbuf[0] != 60 || inbuf[2] != 82)
            {
                Console.WriteLine("Unable to find xml in spider binary file:" + GlobalClass.strSpiderBin);
                Environment.Exit(0);
            }
            strCreatedXML = Encoding.Unicode.GetString(inbuf);
            File.WriteAllText(GlobalClass.strSpiderName+".xml", strCreatedXML);
            inStream.Seek(0x2028, SeekOrigin.Begin);
#if SAVE_PNG
            cSC.PngArray = new byte[kVal];
            br.Read(cSC.PngArray, 0, kVal);
            bw.Write(cSC.PngArray, 0, kVal);
            bw.Close();   
#else
            kVal = cSC.PngArray.Length;
#endif
            inStream.Seek(0, SeekOrigin.Begin);
            
            br.Read(cSC.Hdr, 0, 0x2028);
            Get4(ref cSC.Hdr,20,kVal);
            br.Close();
            return;

        }

        // read in a made up xml file and write out the new saved file
        public void WriteBoardMergingXML()
        {
            board ThisNewBoard = new board();
            FillDeck(ref ThisNewBoard, cSC.XML_Diag_filename);
            cSC.LocalDealCounter = 0; // probably 0 already
            SaveBoardAsBin(ref ThisNewBoard, eSavedType.eMERGE);
        }

        public void GetBoardFromSpiderSave(ref board InitialBoard)
        {
            if (!cSC.bJustReadXML)
            {   // normal execution path:  take a saved game and extract the xml for parsing
                CreateXml();
                //Debug.Assert(cSC.XML_Diag_filename == strSPloc + ".xml");  // not true anymore as we are allowing any name for the saved game
                FillDeck(ref InitialBoard, cSC.XML_Diag_filename);
                OriginalSavedBoard = new board(ref InitialBoard);
                cSC.GameSeed = Convert.ToInt32(GameState.GameSeed.ToString());
                SaveDeck(ref OriginalSavedBoard);
                return;
            }
            if (bJustReadNewXML) // that writeboardmerge does this stuff
            {   // for debug or testing we might want to take a fake xml and create a saved game
                FillDeck(ref InitialBoard, cSC.XML_Diag_filename);
                OriginalSavedBoard = new board(ref InitialBoard);
                cSC.GameSeed = Convert.ToInt32(GameState.GameSeed.ToString());
                return;
            }
            OriginalSavedBoard = new board();
            CreateXml();
            FillDeck(ref OriginalSavedBoard, GlobalClass.strSpiderName + ".xml");
            FillDeck(ref InitialBoard, cSC.XML_Diag_filename);
            InitialBoard.MyMoves.TraceBoard();
            cSC.GameSeed = Convert.ToInt32(GameState.GameSeed.ToString());
        }

        // there should only be two aces of spades as this program works only with 2 decks
        private void FillDeck(ref board tb, string XMLFullpathname)
        {
            int i, n, destStack, srcStack;
            column cCol, cCol2;
            card cCrd;
            List<int> Changed = new List<int>();
            List<string> WhatChanged = new List<string>();
            GlobalClass.cRST = new cRankSuitType[52];
            tb.NumMoves = 0;
            for (i = 0; i < 52; i++)
                GlobalClass.cRST[i] = new cRankSuitType();
            FillOneDeck(ref tb, XMLFullpathname);
            foreach (column cC in tb.ThisColumn)
            {
                if (cC.iStack > 9) break;
                foreach (card c in cC.Cards)
                {
                    if (c.bFaceUp) break;
                    tb.UnexposedTypes.Add(c.type);
                }
            }
            if (tb.ThisColumn[10].Cards.Count <= 10)
                tb.bOnLastDeal = true;
            tb.ComputeRemainingSuits();
            tb.ReScoreBoard();
            if (tb.DealCounter > 0) return;
            for (i = 0; i < 10; i++)
            {
                cCol = tb.ThisColumn[i];
                n = cCol.top;
                if (i < 4)
                {
                    if (n != 5 || cCol.Cards.Count != 6)
                    {
                        Changed.Add(i);
                        if (n == 5) WhatChanged.Add("Count");
                        else WhatChanged.Add("Top");
                        tb.NumMoves++;
                    }
                }
                if (i > 3)
                {
                    if (n != 4 || cCol.Cards.Count != 5)
                    {
                        Changed.Add(i);
                        if (n == 4) WhatChanged.Add("Count");
                        else WhatChanged.Add("Top");
                        tb.NumMoves++;
                    }
                }
            }

            if (tb.NumMoves < 3 && tb.NumMoves != 0)
            {

                cCol = tb.ThisColumn[Changed[0]];
                n = cCol.top;
                if (Changed.Count == 1)
                {
                    // card was moved back to its original place but the one above it is faceup
                    cCol.Cards[n].bFaceUp = false;
                    tb.ReScoreBoard();
                    return;
                }
                cCol2 = tb.ThisColumn[Changed[1]];

                if (WhatChanged[0] == "Top")
                {
                    destStack = Changed[0];
                    srcStack = Changed[1];
                }
                else
                {
                    destStack = Changed[1];
                    srcStack = Changed[0];
                }

                n = tb.ThisColumn[srcStack].Cards.Count;
                cCrd = tb.ThisColumn[srcStack].Cards[n - 1];
                tb.ThisColumn[srcStack].Cards.RemoveAt(n - 1);
                n = tb.ThisColumn[destStack].Cards.Count;
                tb.ThisColumn[destStack].Cards[n - 1].bFaceUp = false;
                tb.ThisColumn[destStack].Cards.Add(cCrd);
                tb.ReScoreBoard();
                tb.ShowBoard();
            }
        }


        // read the XML "decks" into the board
        // count number of spades, expect only 26 / 13 for 2 decks
        private board FillOneDeck(ref board tb, string XMLFullpathname)
        {
            int ID_counter = 0;
            RootCount = 0;
            CurrentCardStack = -1;
            OrderCounter = 0;
            DoingDeck = false;
            DoingComplete = false;
            bFaceUp = false;
            int NumSpades = 0;
            string strT;
            XmlTextReader reader = new XmlTextReader(XMLFullpathname);
            bool bMore = true;
            while (reader.Read() && bMore)
            {
                if (reader.Name == "MyRoot")
                {
                    reader.Read();
                    GetEventXml(reader);
                    tb.MyMoves.TraceBoard();
                    break;
                }
                if (reader.Name == "Root")
                {
                    RootCount++;
                    if (RootCount == 2)
                    {
                        break;
                    }
                }
                string strName;
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.



                        if (reader.Name == "GameState")
                        {
                            reader.Read();
                            reader.Read();
                            Debug.Assert(reader.Name == "Version");
                            reader.Read();
                            GameState.Version = reader.Value;
                            reader.Read();
                            reader.Read();
                            reader.Read();
                            Debug.Assert(reader.Name == "GameSeed");
                            reader.Read();
                            GameState.GameSeed = reader.Value;
                            reader.Read();
                            reader.Read();
                            reader.Read();
                            Debug.Assert(reader.Name == "Score");
                            reader.Read();
                            GameState.Score = reader.Value;
                            reader.Read();
                            reader.Read();
                            reader.Read();
                            Debug.Assert(reader.Name == "Moves");
                            reader.Read();
                            GameState.Moves = reader.Value;
                            reader.Read();
                        }

                        if (reader.Name == "CardStack")
                        {
                            CurrentCardStack++;
                            reader.Read();
                            reader.Read();
                            Debug.Assert(reader.Name == "NumEndCardsSpaced");
                            reader.Read();
                            strT = reader.Value;
                            msCS[CurrentCardStack].NumEndCardsSpaced = Convert.ToInt32(strT);
                            reader.Read();
                            reader.Read();
                            reader.Read();
                            Debug.Assert(reader.Name == "X");
                            reader.Read();
                            strT = reader.Value;
                            msCS[CurrentCardStack].X = Convert.ToInt32(strT);
                            reader.Read();
                            reader.Read();
                            reader.Read();
                            Debug.Assert(reader.Name == "Y");
                            reader.Read();
                            strT = reader.Value;
                            msCS[CurrentCardStack].Y = Convert.ToInt32(strT);
                            strT = reader.Value;
                        }

                        if (reader.Name == "Name")
                        {
                            reader.Read();
                            strName = reader.Value;
                            reader.Read();
                            if (strName == "Deck")
                            {
                                DoingDeck = true;
                                DoingComplete = false;
                                CurrentStack = 11;
                                CurrentOffset = 0;
                                KeepOrder[OrderCounter++] = CurrentStack;
                                msCS[CurrentCardStack].stackid = CurrentStack;
                            }
                            if (strName == "Complete")
                            {
                                DoingComplete = true;
                                DoingDeck = false;
                                CurrentStack = 12;
                                KeepOrder[OrderCounter++] = CurrentStack;
                                CurrentOffset = 0;
                                msCS[CurrentCardStack].stackid = CurrentStack;
                            }
                            if (strName.Contains("Stack"))
                            {
                                if (strName == "Stack1")
                                {
                                    CurrentStack = 1;
                                    CurrentOffset = 0;
                                }
                                if (strName == "Stack2")
                                {
                                    CurrentStack = 2;
                                    CurrentOffset = 0;
                                }
                                if (strName == "Stack3")
                                {
                                    CurrentStack = 3;
                                    CurrentOffset = 0;
                                }
                                if (strName == "Stack4")
                                {
                                    CurrentStack = 4;
                                    CurrentOffset = 0;
                                }
                                if (strName == "Stack5")
                                {
                                    CurrentStack = 5;
                                    CurrentOffset = 0;
                                }
                                if (strName == "Stack6")
                                {
                                    CurrentStack = 6;
                                    CurrentOffset = 0;
                                }
                                if (strName == "Stack7")
                                {
                                    CurrentStack = 7;
                                    CurrentOffset = 0;
                                }
                                if (strName == "Stack8")
                                {
                                    CurrentStack = 8;
                                    CurrentOffset = 0;
                                }
                                if (strName == "Stack9")
                                {
                                    CurrentStack = 9;
                                    CurrentOffset = 0;
                                }
                                if (strName == "Stack10")
                                {
                                    CurrentStack = 10;
                                    CurrentOffset = 0;
                                }
                                msCS[CurrentCardStack].stackid = CurrentStack;
                                KeepOrder[OrderCounter++] = CurrentStack;
                                break;
                            }

                            CurrentSuit = ' ';
                            if (strName.Contains("Spades"))
                            {
                                CurrentSuit = 's';
                                NumSpades++;
                            }
                            if (strName.Contains("Hearts"))
                            {
                                CurrentSuit = 'h';
                            }
                            if (strName.Contains("Diamonds"))
                            {
                                CurrentSuit = 'd';
                            }
                            if (strName.Contains("Clubs"))
                            {
                                CurrentSuit = 'c';
                            }
                            if (CurrentSuit == ' ')
                            {
                                if (strName == "Complete")
                                {
                                    //bMore = false;
                                    //break;
                                }
                                if (strName != "Deck" && strName != "Complete")
                                    Console.WriteLine("cannot parse suit " + strName);
                            }
                            else
                            {
                                if (strName.Contains("Six"))
                                {
                                    CurrentRank = 6;
                                }
                                if (strName.Contains("Five"))
                                {
                                    CurrentRank = 5;
                                }
                                if (strName.Contains("Four"))
                                {
                                    CurrentRank = 4;
                                }
                                if (strName.Contains("Three"))
                                {
                                    CurrentRank = 3;
                                }
                                if (strName.Contains("Two"))
                                {
                                    CurrentRank = 2;
                                }
                                if (strName.Contains("Ace"))
                                {
                                    CurrentRank = 1;
                                }
                                if (strName.Contains("Seven"))
                                {
                                    CurrentRank = 7;
                                }
                                if (strName.Contains("Eight"))
                                {
                                    CurrentRank = 8;
                                }
                                if (strName.Contains("Nine"))
                                {
                                    CurrentRank = 9;
                                }
                                if (strName.Contains("Ten"))
                                {
                                    CurrentRank = 10;
                                }
                                if (strName.Contains("Jack"))
                                {
                                    CurrentRank = 11;
                                }
                                if (strName.Contains("Queen"))
                                {
                                    CurrentRank = 12;
                                }
                                if (strName.Contains("King"))
                                {
                                    CurrentRank = 13;
                                }
                                if (strName.Contains("1"))
                                {
                                    CurrentCode = 52;       // types are 0..51 in the xml file
                                    //CurrentCode = 0;
                                }
                                else CurrentCode = 0;
                                if (CurrentRank == 0)
                                {
                                    if (strName == "Complete")
                                    {
                                        bMore = false;
                                        break;
                                    }
                                    Console.WriteLine("cannot parse rank " + strName);
                                }
                               // Console.WriteLine("Stack:" + CurrentStack + " " + CurrentRank + " " + CurrentSuit);
                               // ThisBoard.ThisColumn[CurrentStack - 1].add(CurrentRank, CurrentSuit, bFaceUp);
                            }
                        }
                        else // is not "Name"
                        {
                            if (reader.Name == "Type")
                            {
                                reader.Read();
                                strValue = reader.Value;
                                CurrentType = Convert.ToInt16(strValue);
                                reader.Read();
                            }
                            if (reader.Name == "FaceUp")
                            {
                                reader.Read();
                                strName = reader.Value;
                                reader.Read();
                                bFaceUp = (strName.ToLower() == "true");
                                NewCard = new card();
                                //NewCard.ID = ID_counter;
                                //ID_counter++;
                                NewCard.ID = CurrentType+CurrentCode;
                                // subtract 51
                                NewCard.bFaceUp = bFaceUp;

                                        //NewCard.type = (Int16)(CurrentType + CurrentCode);
                                GlobalClass.LocMeta[NewCard.ID].Type = CurrentType;

                                        //NewCard.SetSuitChar(CurrentSuit);
                                GlobalClass.LocMeta[NewCard.ID].Suit = SetSuitChar(CurrentSuit);
                                        //NewCard.rank = CurrentRank;
                                GlobalClass.LocMeta[NewCard.ID].Rank = CurrentRank;

                                if (DoingDeck) NewCard.bFaceUp = true;  // have to set it to true 
                                // to simplify the moveto 
                                //need to set false when writing table back to spider binary
                                if (DoingComplete) NewCard.bFaceUp = true;
                                NewCard.iStack = CurrentStack - 1;
                                tb.ThisColumn[CurrentStack - 1].Cards.Add(NewCard);
                                GlobalClass.cRST[CurrentType].rank = CurrentRank;
                               
                                GlobalClass.cRST[CurrentType].suit = GlobalClass.LocMeta[NewCard.ID].Suit;
                                ID_counter++;
                            }
                        }

                        break;
                }
            }
            reader.Close();
            NumSpades /= 13;
            if (NumSpades != 2)
            {
                Console.WriteLine("This program works only with 2 decks and all 4 suits\n");
                Environment.Exit(0);
            }
            tb.ReScoreBoard();
            return tb;
        }
        private int SetSuitChar(char ch)
        {
            char achar = char.ToLower(ch);
            return GlobalClass.cSuits.IndexOf(ch);
        }


       public void SetCS(ref board tb)
        {
            int i, j;
            column cC;
            for (i = 0; i < 12; i++)
            {
                cC = tb.ThisColumn[i];
                for (j = 0; j < 12; j++)
                {
                    if ((msCS[j].stackid - 1) == i)
                    {
                        cC.CardStack = msCS[j];
                        break;
                    }

                }
            }
        }


        public void IssueCardStack(ref column col, ref XmlNode n_append, int nCardStack)
        {
            int j;
            cCardStack ccsPtr;
            card crd;
            // for deck, every 13 cards subtract 6 from X
            // for cards, subtract 7 from X and add 7 to Y
            int dY = 0, dX = 0;

            bool bFaceUp;       // deck:  all are false except the last enabled
            bool bEnabled;      // complete: all are faceup and all enabled are false
            // cards: use the value of bFaceUP for bEnabled;

            string stackName = "";
            switch (nCardStack)
            {
                case 1: stackName = "Stack" + (col.CardStack.stackid).ToString();
                    dY = 7;
                    dX = -7;
                    break;
                case 2: stackName = "Deck";
                    dX = -6;
                    break;
                case 3: stackName = "Complete";
                    dY = 30;
                    break;

            }


            ccsPtr = col.CardStack;
            int ccsPtrX = ccsPtr.X;
            int ccsPtrY = ccsPtr.Y;

            XmlNode n_ct = doc.CreateElement("CardStack");
            n_append.AppendChild(n_ct);

            XmlNode n_ccs = doc.CreateElement("NumEndCardsSpaced");
            n_ccs.AppendChild(doc.CreateTextNode(ccsPtr.NumEndCardsSpaced.ToString()));
            n_ct.AppendChild(n_ccs);

            n_ccs = doc.CreateElement("X");
            n_ccs.AppendChild(doc.CreateTextNode(ccsPtr.X.ToString()));
            n_ct.AppendChild(n_ccs);

            n_ccs = doc.CreateElement("Y");
            n_ccs.AppendChild(doc.CreateTextNode(ccsPtr.Y.ToString()));
            n_ct.AppendChild(n_ccs);

            n_ccs = doc.CreateElement("Name");
            n_ccs.AppendChild(doc.CreateTextNode(stackName));
            n_ct.AppendChild(n_ccs);

            n_ccs = doc.CreateElement("Direction");
            n_ccs.AppendChild(doc.CreateTextNode(nCardStack.ToString()));
            n_ct.AppendChild(n_ccs);

            XmlNode n_cards = doc.CreateElement("Cards");
            n_ct.AppendChild(n_cards);

            for (j = 0; j < col.Cards.Count; j++)
            {



                crd = col.Cards[j];

                bFaceUp = crd.bFaceUp;
                bEnabled = bFaceUp;

                if (nCardStack == 3)
                {
                    bFaceUp = true;
                    bEnabled = false;
                }
                else if (nCardStack == 2)
                {
                    bFaceUp = false;
                    bEnabled = (j == (col.Cards.Count - 1));
                }

                if (nCardStack == 1)
                {
                    if (j > 0)
                    {
                        ccsPtrY += dY;
                    }
                }


                XmlNode n_card = doc.CreateElement("Card");
                n_cards.AppendChild(n_card);

                //n_ccs = doc.CreateElement("NumEndCardsSpaced");
                //n_ccs.AppendChild(doc.CreateTextNode(ccsPtr.NumEndCardsSpaced.ToString()));
                //n_card.AppendChild(n_ccs);

                n_ccs = doc.CreateElement("X");
                n_ccs.AppendChild(doc.CreateTextNode(ccsPtrX.ToString()));
                n_card.AppendChild(n_ccs);

                n_ccs = doc.CreateElement("Y");
                n_ccs.AppendChild(doc.CreateTextNode(ccsPtrY.ToString()));
                n_card.AppendChild(n_ccs);

                n_ccs = doc.CreateElement("Name");
                n_ccs.AppendChild(doc.CreateTextNode(GlobalClass.ProperName(crd)));
                n_card.AppendChild(n_ccs);

                n_ccs = doc.CreateElement("Type");
                n_ccs.AppendChild(doc.CreateTextNode((crd.type & 255).ToString()));
                n_card.AppendChild(n_ccs);

                n_ccs = doc.CreateElement("FaceUp");
                n_ccs.AppendChild(doc.CreateTextNode(bFaceUp.ToString().ToLower()));
                n_card.AppendChild(n_ccs);

                n_ccs = doc.CreateElement("Enabled");
                n_ccs.AppendChild(doc.CreateTextNode(bEnabled.ToString().ToLower()));
                n_card.AppendChild(n_ccs);

                if (bFaceUp) dY = 23;

                if (((1 + j) % 13) == 0)
                {
                    ccsPtrX += dX;
                    if (nCardStack != 1)
                        ccsPtrY += dY;
                }

            }
        }

        public void CopyBoardAsBin(ref board tb)
        {
            //SaveBoardAsXML(ref tb, null);
            //Environment.Exit(0);
            cSC.cMXF.LoadTemplate(tb.MyMoves.TheseMoves.Count, "c:\\spider", cSC.Deck.strXMLout);
        }


   //the following not used anymore
        private void SaveBoardAsXML(ref board tb, StreamWriter sw)
        {
            int i;
            byte[] bXMLtext;            
            char[] delim = new char[] { '\n' };
            column col;
            StringBuilder sb = new StringBuilder(16384);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = ("    ");
            settings.NewLineChars = "\n";
            settings.OmitXmlDeclaration = true; 
            XmlWriter writer;
            writer = XmlWriter.Create(sb, settings);

            SetCS(ref tb);

            //i = Convert.ToInt32(GameState.Moves);
            //j = Convert.ToInt32(GameState.Score);

            //i += tb.MyMoves.TheseMoves.Count;
            //j -= tb.MyMoves.TheseMoves.Count;



            GameState.Score = (GlobalClass.COL_WEIGHT - tb.MyMoves.TheseMoves.Count).ToString();        // j.ToString();

            // note true because we are makeing moves Debug.Assert(GameState.Moves == tb.MyMoves.TheseMoves.Count.ToString());
            GameState.Moves = tb.MyMoves.TheseMoves.Count.ToString();   // i.ToString();


            doc = new XmlDocument();
            XmlNode n_root = doc.CreateElement("Root");
            doc.AppendChild(n_root);
            XmlNode n_gamestate = doc.CreateElement("GameState");
            n_root.AppendChild(n_gamestate);

            XmlNode n_gsa = doc.CreateElement("Version");
            n_gsa.AppendChild(doc.CreateTextNode(GameState.Version));
            n_gamestate.AppendChild(n_gsa);

            n_gsa = doc.CreateElement("GameSeed");
            n_gsa.AppendChild(doc.CreateTextNode(GameState.GameSeed));
            n_gamestate.AppendChild(n_gsa);

            n_gsa = doc.CreateElement("Score");
            n_gsa.AppendChild(doc.CreateTextNode(GameState.Score));
            n_gamestate.AppendChild(n_gsa);

            n_gsa = doc.CreateElement("Moves");
            n_gsa.AppendChild(doc.CreateTextNode(GameState.Moves));
            n_gamestate.AppendChild(n_gsa);

            XmlNode n_tbl = doc.CreateElement("CardTable");
            n_root.AppendChild(n_tbl);
            XmlNode n_stks = doc.CreateElement("CardStacks");
            n_tbl.AppendChild(n_stks);
            for (i = 0; i < 10; i++)
            {
                col = tb.ThisColumn[KeepOrder[i] - 1];
                IssueCardStack(ref col, ref n_stks, 1);
            }
            col = tb.ThisColumn[10];
            IssueCardStack(ref col, ref n_stks, 2);
            col = tb.ThisColumn[11];
            IssueCardStack(ref col, ref n_stks, 3);

            doc.WriteContentTo(writer);
            writer.Close();
            sb.Replace(" />", "/>");
            if (sw == null)
            {
                strXMLout = sb.ToString();
                return; // leave the </Root> where it is because we are inserting unicode back into original bin file
            }
            sb.Replace("</Root>","");
            strXMLout = sb.ToString();
            //            sb.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "").Replace(" />", "/>");

            bXMLtext = new byte[2*(strXMLout.Length+1)];
           // i = Encoding.Unicode.GetBytes(strXMLout,0,strXMLout.Length,bXMLtext,0);
            i = Encoding.ASCII.GetBytes(strXMLout, 0, strXMLout.Length, bXMLtext, 0);
            sw.WriteLine(strXMLout);

        }

        // call this at any time to save the best board under the diag name
        // this file is ALWAYS read instead of the original save board
        public void SaveDiagBoard(ref board tb)
        {
            FileStream outStream = File.Create(cSC.XML_Diag_filename);
            StreamWriter sw = new StreamWriter(outStream);
            //SaveBoardAsXML(ref tb, sw);
            //SerializeMoveEvents(ref tb, sw);
            tb.MyMoves.TraceBoard(sw);
            tb.ShowRawBoard(sw,false,0);
            sw.Close();
        }

        public void SaveDeck(ref board tb)
        {
            string strLocation = System.IO.Path.GetDirectoryName(cSC.XML_Diag_filename);
            FileStream outStream = File.Create(strLocation + "\\adeck.txt");
            StreamWriter sw = new StreamWriter(outStream);
            //SerializeMoveEvents(ref tb, sw);
            tb.MyMoves.TraceBoard(sw);
            tb.ShowRawBoard(sw, false,1);
            sw.Close();
#if DEBUG
            outStream = File.Create(strLocation + "\\adeck2.txt");  // CARD IDs
            sw = new StreamWriter(outStream);
            tb.ShowRawBoard(sw, true, 1);
            sw.Close();
#endif
        }




        // this is called from the deal handler to save each deal
        public void SaveBoardAtDeal(GlobalClass.eSBAD eMove, ref board tb, int iLocalDeal)   // iLocalDeal is 0 on bestboard else is deal#
        {
            string strDC; ;
            string strMovID;
            string strName = "";
            StreamWriter sw;
            FileStream outStream;
            if (eMove == GlobalClass.eSBAD.eStartGather)
            {
                strName = SaveBoardAsBin(ref tb, eSavedType.eSEED);
                strDC = tb.DealCounter.ToString();
                strGatheredMoves = "=====" + strName + "=====\r\n";
                strGatheredMoves += tb.MyMoves.TraceBoard(null);
                strGatheredMoves += tb.AssembleRawBoard(null, false, 0);
                tb.DealString += strName + ",";
                strGatheredName = strName;
                return;
            }
            strName = SaveBoardAsBin(ref tb, eSavedType.eDEAL); 
            tb.DealString += strName + ",";
            strGatheredMoves += "\r\n===== " + strName + " =====\r\n";
            strGatheredMoves += tb.MyMoves.TraceBoard(null);
            if (eMove == GlobalClass.eSBAD.eAllGathered)
            {
                outStream = File.Create(GlobalClass.strSpiderDir + strGatheredName + "_mov.txt");
                sw = new StreamWriter(outStream);
                // tb.ShowRawBoard(sw, false, 0); 2018 cannot do this here, needs to be at best board"
                sw.WriteLine(strGatheredMoves);
                sw.Close();
            }

        }



        public void SaveBestBoardAsXml()
        {
            SaveDiagBoard(ref cSC.BestBoard);
        }

        public void ReTraceBestBoard()
        {
            TraceAllMoves(ref cSC.BestBoard, null);
        }

        public void SaveBoardMoves(ref board tb, string strName)
        {
            string strFullpathname = GlobalClass.strSpiderDir + strName + "_mov.txt";
            FileStream outStream = File.Create(strFullpathname);
            StreamWriter sw = new StreamWriter(outStream);
            //SerializeMoveEvents(ref tb, sw);
            tb.MyMoves.TraceBoard(sw);
            tb.ShowRawBoard(sw, false, 0);
            sw.Close();
        }

        //used only for testing
        public void SerializeMoveEvents(ref board nb, StreamWriter sw)
        {
            int i, n = nb.MyMoves.TheseMoves.Count;
            int LocalDealCounter = 0;

            AllEvents = new List<cEventClass>();
            AllEvents.Clear();
            cMoveInfo cMI;
            for (i = 0; i < n; i++)
            {
                cMoveData cMD = nb.MyMoves.TheseMoves[i];
                pseudoCard pSC = utils.CVTMoveValueToInfo(cMD);
                //card c = utils.FindCardFromID(ref nb, pSC.ID);
                cEventClass cEC = new cEventClass();    //(c.iStack, c.iCard, pSC.desID);
                cEC.ID = pSC.ID;
                cEC.score = pSC.score;
                cEC.DesStack = pSC.desID;
                
                if (cMD.WhereInfo >= 0)
                {
                    cMI = nb.MyMoves.ThisMoveInfo[cMD.WhereInfo];
                    cEC.Info_ID = cMI.id;
                    if (cMI.id == GlobalClass.DEALT_A_CARD)
                    {
                        cEC.Info_Val = ++LocalDealCounter;
                    }
                    if (cMI.id == GlobalClass.BUILT_SUIT)
                    {
                        cEC.Info_Val = cMI.suit;
                    }
                }
                AllEvents.Add(cEC);
            }
            //if (sw != null)
            //{
            //    sw.WriteLine("<MyRoot>");
            //    XmlWriterSettings settings = new XmlWriterSettings();
            //    settings.Indent = true;
            //    settings.IndentChars = ("    ");
            //    settings.NewLineChars = "\n";
            //    settings.OmitXmlDeclaration = true;
            //    XmlWriter writer;
            //    writer = XmlWriter.Create(sw, settings);                
            //    System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(AllEvents.GetType());
            //    x.Serialize(writer, AllEvents);
            //    sw.WriteLine("\n</MyRoot>\n</Root>");
            //}
        }



        private int GetLookupScore(string ID_CODE)
        {
            foreach(cEventStats cES in EventStatistics)
            {
                if (ID_CODE == cES.strCode.ToLower())
                {
                    if (cES.ScoreLookup)
                    {
                        return cES.score;
                    }
                }
            }
            return -1;
        }

       

        public bool AdvanceSaveBoard(string ID_CODE, out board tb)
        {
            if(AdvanceBoardTo(ID_CODE, out tb, 0, 0))
            {
                CopyBoardAsBin(ref tb);
                return true;
            }
            return false;
        }

        public bool AdvanceToPosition(int RequiredDeal, int RequiredOffset, out board tb)
        {
            if (AdvanceBoardTo("0", out tb, RequiredDeal, RequiredOffset))
            {
                CopyBoardAsBin(ref tb);
                return true;
            }
            return false;
        }

        public bool ShowAllBestScores(int RequiredDeal, out board tb)
        {
            int score = -1;
            cEventStats cES = EventStatistics[RequiredDeal - 1];
            if (cES.TypeCode == GlobalClass.DEALT_A_CARD)
                    score = cES.score;
            if (score < 0)
            {
                tb = null;
                return false;
            }
            return AdvanceBoardTo("1", out tb, RequiredDeal, score);
        }


        public bool AddBoardsToLookup(int DealCounter, int BreakIndex)
        {
            board tb = new board(ref OriginalSavedBoard);
            int[] des = new int[112];
            Int64 ChkWord = 0;
            int index = 0;
            int desptr, DesCard, WouldGoHere = 0;
            bool bInsertFailed = false;
            cSC.ThisBoardSeries.Clear();
            cSC.BestScore = 0;
            cSC.SortedScores.Clear();
            cSC.stlookup.Clear();
            int UniqueID = 0;
            foreach (cEventClass cEC in cSC.EventList)
            {
                card c = utils.FindCardFromID(ref tb, cEC.ID);
                DesCard = tb.ThisColumn[cEC.DesStack].Cards.Count;
                tb.ID = index++;
                desptr = tb.FormDupList(c.iStack, c.iCard, cEC.DesStack, DesCard, ref des, ref ChkWord);
                if (cSC.stlookup.bIsNewBoard(ChkWord, desptr, ref des, tb.MyMoves.TheseMoves.Count + 1, ref WouldGoHere, ref bInsertFailed))
                {
                    tb.moveto(c.iStack, c.iCard, cEC.DesStack);
                    tb.ReScoreBoard();
                    if (tb.score > cSC.BestScore)
                        cSC.BestScore = tb.score;
                    if (cEC.Info_ID >= 0)
                    {
                        if (cEC.Info_ID == GlobalClass.DEALT_A_CARD)
                        {
                            tb.deal(cSC.BestScore, DateTime.Now);
                            tb.ReScoreBoard();
                            tb.ID = index;
                            index = 0;
                            cSC.BestScore = 0;
                        }
                    }
                }
                else
                {
                    Debug.Assert(false);
                }
                if (WouldGoHere != 0) Console.WriteLine("WouldGoHere");
                if (index >= BreakIndex && DealCounter == (tb.DealCounter + 1))
                {
                    board nb = new board(ref tb);
                    nb.UniqueID = UniqueID++;
                    cSC.ThisBoardSeries.Add(nb);
                    nb.ShowBoard();
                    if (nb.UniqueID > 0)
                        utils.tbsCompareAny(ref cSC, 0, nb.UniqueID);
                }
            }
            return true;
        }

        // iterarate thru events till we get to the Following ID
        public bool AdvanceBoardTo(string ID_CODE, out board nb, int arg1, int arg2)
        {
            nb = OriginalSavedBoard;
            int index = 0;
            int OldScore = 0;
            //int ealCounter = 1;
            int UniqueID = 0;
            int ScoreLookup = GetLookupScore(ID_CODE);  // gets -1 for other than a..z
            foreach (cEventClass cEC in cSC.EventList)
            {
                card c = utils.FindCardFromID(ref nb, cEC.ID);
                nb.moveto(c.iStack, c.iCard, cEC.DesStack);
                nb.ReScoreBoard();
                nb.UniqueID = UniqueID++;
                nb.ID = index++;
                if (ScoreLookup == -1)
                {
                    if (ID_CODE == "0")
                    {
                        if ((nb.DealCounter+1) == arg1 && nb.ID == (arg2-1))
                        {
                            return true;
                        }
                    }
                    if (ID_CODE == "1")
                    {
                        if ((nb.DealCounter + 1) == arg1 && nb.score == arg2)
                        {
                            //nb.MyMoves.TraceBoard();
                            nb.ShowRawBoard();
                        }
                    }
                }

                if (ScoreLookup >= 0)
                    if (ScoreLookup == nb.score)
                    {
                        return true;
                    }
                if (nb.score > OldScore)
                    OldScore = nb.score;
                if (cEC.Info_ID >= 0)
                {
                    if (cEC.Info_ID == GlobalClass.DEALT_A_CARD)
                    {
                        nb.deal(OldScore, DateTime.Now);
                        nb.ReScoreBoard();
                        nb.ID = index;
                        nb.UniqueID = UniqueID++;
                        index = 0;

                        if (ID_CODE == "0" && ScoreLookup == -1)
                        {
                            if ((nb.DealCounter + 1) == arg1 && nb.ID == arg2)
                            {
                                return true;
                            }
                        }

                        //ealCounter++;
                        //nb.MyMoves.TraceBoard();
                        //nb.ShowRawBoard();
                        OldScore = nb.score;
                    }
                    if (cEC.Info_ID == GlobalClass.BUILT_SUIT)
                    {
                        
                    }
                    string a = cEC.GetCode().ToString().ToLower();
                    if (a == ID_CODE)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        // this re-makes the moves used to create the board "tb" - usually the BestBoard
        public void TraceAllMoves(ref board tb, StreamWriter sw)
        {
            int i,  n = tb.MyMoves.TheseMoves.Count;
            board osb = new board(ref OriginalSavedBoard);
            cMoveInfo cMI;
            int OldScore = 0;
            bool bIsDeal = false;
            if(tb.UniqueID == -1)
            {

                tb.ShowBoard();
            }
            for (i = 0; i < n; i++)
            {
                cMoveData cMD = tb.MyMoves.TheseMoves[i];
                if (!(cMD.Des == -1 && cMD.Src == -1))
                {   // yes, this is a bodge 11nov2012
                    pseudoCard pSC = utils.CVTMoveValueToInfo(cMD);
                    card c = utils.FindCardFromID(ref osb, pSC.ID);
                    osb.moveto(c.iStack, c.iCard, pSC.desID);
                    osb.ReScoreBoard();
                    osb.MyMoves.TraceBoard(sw);
                    osb.ShowRawBoard(sw,false,0);
                    if (osb.score > OldScore)
                        OldScore = osb.score;
                }
                bIsDeal = cMD.ShrinkCode.Contains("D-");
                if(bIsDeal)
                {
                    int iGot = 1;
                }
                if (cMD.Des == -1 && cMD.Src == -1)
                {
                    osb.deal(OldScore, DateTime.Now);
                    osb.ReScoreBoard();
                    osb.MyMoves.TraceBoard(sw);
                    osb.ShowRawBoard(sw, false,0);
                    OldScore = osb.score;
                }

                // if WhereInfo is > 0 then more than one event happened.  Usually a deal has no move so an immediate deal but
                // conceivable a suit could be formed by the deal.  this has happened but it is more likely the deal has no moves.
                if (cMD.WhereInfo >= 0)
                {
                    cMI = tb.MyMoves.ThisMoveInfo[cMD.WhereInfo];

                    if (cMI.id == GlobalClass.DEALT_A_CARD)
                    {
                        //Debug.Assert(OldScore == cMI.score);
                        // 11nov2012 score has local deal index instead of best score

                        // 11nov2012 if two deals in a row then it seems that WhereInfo is > 0
                        // and we must deal here twice????
                        // for (int j = 0; j < cMD.WhereInfo; j++)
                        //{
                        osb.deal(OldScore, DateTime.Now);
                        osb.ReScoreBoard();
                        osb.MyMoves.TraceBoard(sw);
                        osb.ShowRawBoard(sw, false, 0);
                        OldScore = osb.score;
                        //}
                    }
                    if (cMI.id == GlobalClass.BUILT_SUIT)
                    {

                    }
                }

            }
        }



        public string SaveBoardAsBin(ref board tb, eSavedType eST)
        {
            string strName = cSC.FormName(tb.DealCounter, eST);
            SetCS(ref tb);
            cXmlFromBoard xtest = new cXmlFromBoard();
            xtest.ReCreateBinFile(ref tb, ref cSC, strName);
            return strName;
        }

        //create the event list structure from the xml
        public void GetEventXml(XmlTextReader xR)
        {
            int i, A = Convert.ToInt32('A');
            char a='a';
            bool bTakeSuitStats = false;
            int SuitCnt = 0;
            int DealCnt = 0;
            bool bAny = false;
            strEventInfoPrompt = "Enter the code letter to advance to the event described\n";;
            string strInfo;
            int InfoVal;
            cSC.EventList.Clear();
            XmlSerializer mySerializer = new XmlSerializer(typeof(List<cEventClass>));
            cSC.EventList = (List<cEventClass>)mySerializer.Deserialize(xR);
            for (i = 0; i < EventStatistics.Length; i++)
                EventStatistics[i].score = 0;
            for (i = 0; i < cSC.EventList.Count; i++)
            {
                cEventClass cEC = cSC.EventList[i];
                InfoVal = cSC.EventList[i].Info_Val;
                strInfo = "";
                if (EventStatistics[DealCnt].score < cEC.score)
                {
                    EventStatistics[DealCnt].index = i;
                    EventStatistics[DealCnt].score = cEC.score;
                }
                if (bTakeSuitStats)
                {
                    if (EventStatistics[5 - 1 + SuitCnt].score < cEC.score)
                    {
                        EventStatistics[5 - 1 + SuitCnt].score = cEC.score;
                        EventStatistics[5 - 1 + SuitCnt].index = i;
                    }
                }
                switch (cSC.EventList[i].Info_ID)
                {
                    case GlobalClass.DEALT_A_CARD:
                        bTakeSuitStats = false;
                        a = Convert.ToChar(A++);
                        strInfo = "Dealt Card " + InfoVal + " code:" +  a.ToString();
                        cSC.EventList[i].SetCode(a);
                        DealCnt++;
                        break;
                    case GlobalClass.BUILT_SUIT:
                        bTakeSuitStats = true;
                        a = Convert.ToChar(A++);
                        strInfo = "Built " + GlobalClass.SuitNames[InfoVal] + " code:" + a.ToString();
                        cSC.EventList[i].SetCode(a);
                        SuitCnt++;
                        break;
                }
                //cSC.EventList[i].CurrentDeal = DealCnt;
                if (strInfo != "")
                {
                    bAny = true;
                    strEventInfoPrompt += strInfo + "\n";
                }
            }
            if (!bAny)
                strEventInfoPrompt = "";

            for (i = 0; i < DealCnt; i++)
            {
                if (EventStatistics[i].score >= 0)
                {
                    a = Convert.ToChar(A++);
                    EventStatistics[i].strCode = a.ToString();
                    EventStatistics[i].ScoreLookup = true;
                    strInfo = "Best at deal " + (i + 1).ToString() + " code:" + a.ToString();
                    strEventInfoPrompt += strInfo + "\n";
                    EventStatistics[i].TypeCode = GlobalClass.DEALT_A_CARD;
                }
                else break;
            }
            for (i = 0; i < SuitCnt; i++)
            {
                if (EventStatistics[5+i].score >= 0)
                {
                    a = Convert.ToChar(A++);
                    EventStatistics[5+i].strCode = a.ToString();
                    EventStatistics[5+i].ScoreLookup = true;
                    strInfo = "Best at suit " + (i + 1).ToString() + " code:" + a.ToString();
                    strEventInfoPrompt += strInfo + "\n";
                    EventStatistics[i].TypeCode = GlobalClass.BUILT_SUIT;
                }
                else break;
            }
        }


    }
}