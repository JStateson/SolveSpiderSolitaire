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


/*
 * 
 * The following addresses are 32 bit offsets from the origin
 * multiply index [x] by 4 for byte offset
 * [0] is origin   
 * [2] = 00002028
 * [3] = 0
 * [4] = 0
 * [5] = address "x" where x + 2028 + 6 is the origin of the xml information
 * 
 * */

namespace spider
{
    public class cXmlFromBoard
    {
        /*
         * if "" then we just save the binary file as it was called by makexml
         * if "DEAL_" then a got to write out DEAL_x and DEAL_x_y 
         * if "SUIT_" then same as "" but add suit info to SUIT_
         * tb.DealCounter is the actual deals which are only 1..5 or so
         * cSC.LocalDealCounter is the variation on the deal 0..511
         * format of output might be DEAL_2_3  for the 3rd variation of the 2nd deal
         * 
         * */
        private StringBuilder sbXML;
        public void ReCreateBinFile(ref board tb, ref cSpinControl cSC, string strPrefix)
        {
            long nLen1, nLen = 1048576;
            int i, j, n;
            int DealID = cSC.LocalDealCounter;
            string strFullpathname;
            string strName, DealName = strPrefix;

            //FileStream inStream = File.OpenRead(strFullpathname + ".hdr");
            //BinaryReader br = new BinaryReader(inStream);
            //nLen = inStream.Length;
            //Debug.Assert(nLen == 0x2028);

            if (strPrefix != "")
            {
                if(strPrefix == "FIRST")
                {
                    strFullpathname = GlobalClass.strSpiderDir + "FirstEmptyColumn" + GlobalClass.strSpiderExt;
                }
                else
                {
                    DealName += (1 + tb.DealCounter).ToString() + "_";
                    if (DealID > 0) DealName += DealID.ToString() + "_";
                    strName = DealName + Path.GetFileName(GlobalClass.strSpiderOutputBinary);
                    strFullpathname = GlobalClass.strSpiderDir + strName;
                }
                
            }
            else strFullpathname =GlobalClass.strSpiderOutputBinary;

            FileStream outStream = File.Create(strFullpathname);
            BinaryWriter bw = new BinaryWriter(outStream);


            // get the hdr
            byte[] outbuf = new byte[256000];
            for (j = 0; j < 0x2028; j++)
            {
                outbuf[j] = cSC.Hdr[j];
            }


            nLen = cSC.PngArray.Length;
            // get the png file
#if SAVE_PNG            
            inStream = File.OpenRead(strFullpathname + ".png");
            nLen = inStream.Length;
            br = new BinaryReader(inStream);
            br.Read(outbuf, 0x2028, (int)nLen);
            nLen1 = nLen + 0x2028;
#endif
            nLen1 = 0x2028;
            for (i = 0; i < nLen; i++)
            {
                outbuf[nLen1] = cSC.PngArray[i];
                nLen1++;
            }

            MakeXmlFile(strFullpathname, ref tb, ref cSC);
            n = sbXML.Length;
            string sbTemp = sbXML.ToString();
            j = (int)nLen1;
            // convert n into bytes and append ff fe so one could get "c6 18 01 00 ff fe"
            byte[] b6 = new byte[8];
            GetUnk6(ref b6, 6 + n * 2); // added 2 to fix problem and added another 2 for two more nulls but 0xa is missing

            for (i = 0; i < 6; i++)
            {
                outbuf[j] = b6[i];
                j++;
            }
            for (i = 0; i < n; i++)
            {
                outbuf[j] = (byte)sbTemp[i]; 
                j++;
                outbuf[j] = 0;
                j++;
            }
            outbuf[j] = 0;
            j++;
            outbuf[j] = 0x0a;
            j++;
            bw.Write(outbuf, 0, j+2);
            bw.Close();
        }

        private void GetUnk6(ref byte[] b6, int n)
        {
            // append ff fe to end of string
            ulong u6 = 0xfeff00000000;
            u6 |= (ulong)n;
            for (int i = 0; i <= 5; i++)
            {
                b6[i] = (byte)((u6 >> (i * 8)) & 0x000000FF);
            }
            //return u6.ToString("X");
        }

        //complete starts off with  x 110 y 380  then x goes to 140 (increments by 30

        private void MakeXmlFile(string strFullPathName, ref board tb, ref cSpinControl cSC)
        {
            bool bFaceUp;       // deck:  all are false except the last enabled
            bool bEnabled;      // complete: all are faceup and all enabled are false
            sbXML = new StringBuilder(16384);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = ("    ");
            settings.NewLineChars = "\n";
            settings.OmitXmlDeclaration = true;
            
            using (XmlWriter writer = XmlWriter.Create(sbXML,settings)) //(strFullPathName + "_bin.xml", settings))

//            using (XmlWriter writer = XmlWriter.Create(strFullPathName + "_bin.xml", settings))
            {
                // Write XML data.
                writer.WriteStartElement("Root");
                writer.WriteStartElement("GameState");
                writer.WriteElementString("Version", "7");
                writer.WriteElementString("GameSeed", cSC.GameSeed.ToString());
                writer.WriteElementString("Score", tb.score.ToString());        // 2018 tb.score.ToString());
                writer.WriteElementString("Moves", (1+tb.DealCounter).ToString());  // (tb.NumMoves + tb.MyMoves.TheseMoves.Count).ToString());
                writer.WriteEndElement();   // gamestate
                writer.WriteStartElement("CardTable");
                writer.WriteStartElement("CardStacks");
                cCardStack ccsPtr;
                int nCardStack = 0;
                int nDirection = 1;

                foreach (column col in tb.ThisColumn)
                {
                    ccsPtr = col.CardStack;   
                    int ccsPtrX = ccsPtr.X;
                    int ccsPtrY = ccsPtr.Y;
                    int j = 0;  // count cards as only last one is enabled
                    int dX=0, dY=0;
                    string stackName = "";
                    switch (nDirection)
                    {
                        case 1: stackName = "Stack" + (col.CardStack.stackid).ToString();
                            dY = 7;
                            dX = -7;
                            break;
                        case 2: stackName = "Deck";
                            dX = -6;
                            break;
                        case 3: stackName = "Complete";
                            dX = 30; // origin of X was 110
                            dY = 0;  // origin of Y was 380
                            break;

                    }
                    writer.WriteStartElement("CardStack");
                    writer.WriteElementString("NumEndCardsSpaced", ccsPtr.NumEndCardsSpaced.ToString());
                    writer.WriteElementString("X", ccsPtr.X.ToString());
                    writer.WriteElementString("Y", ccsPtr.Y.ToString());
                    writer.WriteElementString("Name", stackName);
                    writer.WriteElementString("Direction", nDirection.ToString());
                    writer.WriteStartElement("Cards");
                    foreach (card crd in col.Cards)
                    {
                        writer.WriteStartElement("Card");
                        writer.WriteElementString("X",ccsPtrX.ToString());
                        writer.WriteElementString("Y", ccsPtrY.ToString());
                        bFaceUp = crd.bFaceUp;
                        bEnabled = bFaceUp;

                        if (nDirection == 3)
                        {
                            bFaceUp = true;
                            bEnabled = false;
                        }
                        else if (nDirection == 2)
                        {
                            bFaceUp = false;
                            bEnabled = (j == (col.Cards.Count - 1));
                        }

                        if (nDirection == 1)
                        {
                            ccsPtrY += dY;
                        }
                        writer.WriteElementString("Name",GlobalClass.ProperName(crd));
                        writer.WriteElementString("Type", crd.type.ToString());
                        writer.WriteElementString("FaceUp", bFaceUp.ToString().ToLower());
                        writer.WriteElementString("Enabled", bEnabled.ToString());

                        if (bFaceUp && nDirection < 3) dY = 23;

                        if (((1 + j) % 13) == 0)
                        {
                            ccsPtrX += dX;
                            if (nDirection != 1)
                                ccsPtrY += dY;
                        }

                        j++;
                        writer.WriteEndElement();   // card
                    }
                    writer.WriteEndElement();   // cards
                    writer.WriteEndElement();  //CardStack
                    nCardStack++;
                    if(nCardStack == 10)nDirection = 2;
                    if(nCardStack == 11)nDirection = 3;
                    // need to write out the completed stack eventually!!  
                }
                writer.WriteEndElement();   // cardstacks
                writer.WriteEndElement();   // cardtable
                writer.WriteEndElement();   // root
                writer.Flush();
                if(strFullPathName.Contains("SUIT"))
                    Console.WriteLine(" Wrote out a binary suit file: " + strFullPathName);
                else if (strFullPathName.Contains("DEAL"))
                    Console.WriteLine(" Wrote out a binary deal file: " + strFullPathName);
                else Console.WriteLine(" replaced with a new binary deal file: " + strFullPathName); 
                foreach (cSuitedStatus cSS in tb.SuitStatus)
                {
                    for (int i = 0; i < cSS.NumCompletedAndRemoved; i++)
                        Console.WriteLine("Removed suit:" + GlobalClass.SuitNames[cSS.index]);
                }
            }
        }
    }

    public class cMergeXmlFile
    {
        private byte[] XMLtemplete;
        int j=1;
        public void LoadTemplate(int NumMoves, string strSrcDir, string XMLdata)
        {
            int ptr2xml = 0x2028 + 6;
            string strInFileName = strSrcDir + "\\" + NumMoves.ToString() + ".SpiderSolitaireSave-ms";
            string strOutFileName = strSrcDir + "\\bin-" + NumMoves.ToString() + ".SpiderSolitaireSave-ms"; 
            FileStream inStream = File.OpenRead(strInFileName);
            BinaryReader br = new BinaryReader(inStream);
            FileStream outStream = File.Create(strOutFileName);
            BinaryWriter bw = new BinaryWriter(outStream);
            int n = (int)inStream.Length;
            XMLtemplete = new byte[n+2];
            br.Read(XMLtemplete, 0, n);
            br.Close();
            for (int i = 0; i < 4; i++)
            {
                int r = XMLtemplete[20 + i];
                ptr2xml += (r * j);
                j *= 256;
            }
            if (XMLtemplete[ptr2xml] != 60 || XMLtemplete[ptr2xml + 2] != 82)
            {
                Console.WriteLine("Unable to find xml in spider binary file");
                Debug.Assert(false);
                bw.Close();
                return;
            }
            Encoding.Unicode.GetBytes(XMLdata, 0, XMLdata.Length, XMLtemplete, ptr2xml);
            bw.Write(XMLtemplete, 0, n);
            bw.Close();
        }


    }



}
