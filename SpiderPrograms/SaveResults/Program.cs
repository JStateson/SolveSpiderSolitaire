using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

// program saves all results indirectory in "2018" or "2018\fails"
// all standard seed and deal files are deleted as if "removefiles" was executed.

namespace SaveResults
{
    class Program
    {
        static void ClearDir(string strDir)
        {
            foreach (string sFile in Directory.GetFiles(strDir, "DEAL*.SpiderSolitaireSave-ms"))
            {
                File.Delete(sFile);
            }
            foreach (string sFile in Directory.GetFiles(strDir, "Deal*_mov.txt"))
            {
                File.Delete(sFile);
            }
            foreach (string sFile in Directory.GetFiles(strDir, "a?FirstEmptyColumn*.*"))
            {
                File.Delete(sFile);
            }
        }

        public static class KnownFolderFinder
        {
            private static readonly Guid CommonDocumentsGuid = new Guid("{FDD39AD0-238F-46AF-ADB4-6C85480369C7}");

            [Flags]
            public enum KnownFolderFlag : uint
            {
                KF_FLAG_DEFAULT = 0x00000000,
                KF_FLAG_SIMPLE_IDLIST = 0x00000100,
                KF_FLAG_NOT_PARENT_RELATIVE = 0x00000200,
                KF_FLAG_DEFAULT_PATH = 0x00000400,
                KF_FLAG_INIT = 0x00000800,
                KF_FLAG_NO_ALIAS = 0x00001000,
                KF_FLAG_DONT_UNEXPAND = 0x00002000,
                KF_FLAG_DONT_VERIFY = 0x00004000,
                KF_FLAG_CREATE = 0x00008000,
                KF_FLAG_NO_PACKAGE_REDIRECTION = 0x00010000,
                KF_FLAG_NO_APPCONTAINER_REDIRECTION = 0x00010000,
                KF_FLAG_FORCE_PACKAGE_REDIRECTION = 0x00020000,
                KF_FLAG_FORCE_APPCONTAINER_REDIRECTION = 0x00020000,
                KF_FLAG_RETURN_FILTER_REDIRECTION_TARGET = 0x00040000,
                KF_FLAG_FORCE_APP_DATA_REDIRECTION = 0x00080000,
                KF_FLAG_ALIAS_ONLY = 0x80000000
            }

            [DllImport("shell32.dll")]
            static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);

            public static string GetFolderFromKnownFolderGUID(Guid guid)
            {
                return pinvokePath(guid, KnownFolderFlag.KF_FLAG_DEFAULT_PATH);
            }

            public static void EnumerateKnownFolders()
            {
                KnownFolderFlag[] flags = new KnownFolderFlag[] {
            KnownFolderFlag.KF_FLAG_DEFAULT,
            KnownFolderFlag.KF_FLAG_ALIAS_ONLY | KnownFolderFlag.KF_FLAG_DONT_VERIFY,
            KnownFolderFlag.KF_FLAG_DEFAULT_PATH | KnownFolderFlag.KF_FLAG_NOT_PARENT_RELATIVE,
            };


                foreach (var flag in flags)
                {
                    Console.WriteLine(string.Format("{0}; P/Invoke==>{1}\n", flag, pinvokePath(CommonDocumentsGuid, flag)));
                }
                Console.ReadLine();
            }

            private static string pinvokePath(Guid guid, KnownFolderFlag flags)
            {
                IntPtr pPath;
                SHGetKnownFolderPath(guid, (uint)flags, IntPtr.Zero, out pPath); // public documents

                string path = System.Runtime.InteropServices.Marshal.PtrToStringUni(pPath);
                System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pPath);
                return path;
            }
        }

        static void Main(string[] args)
        {
            int iDirNum = 0;
            string strGaneEnd = ""; //w for in or l for loss
            bool bIsThere = false;
            string strSpiderBin0 = "";
            string PathToDirectory;
            string strSpiderBin;
            string strDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string strResultName = DateTime.Now.Year.ToString();
            string PathToResult = strDesktop + "\\" + strResultName;
            string dFile = "";
            string stTemp = System.Reflection.Assembly.GetEntryAssembly().Location; // path to executable
                                                                                    // KnownFolderFinder.EnumerateKnownFolders();   // this was used for testing purposes
                                                                                    // GlobalClass.InitExceptions();
                                                                                    // look for file in current director and use that one
            string strName;
            string strFrom;

            strSpiderBin0 = System.IO.Path.GetDirectoryName(stTemp) + "\\Spider Solitaire.SpiderSolitaireSave-ms";
            bIsThere = File.Exists(strSpiderBin0);
            if (!bIsThere)
            {

                //Attempt to find where the saved spider file is located.  Normally at c:\user\username but the windows MOVE property might
                //have been used to relocat the file.  In addition,  OneDrive may or may not be in the path returned by SpecialFolder

                strSpiderBin0 = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                // the above is usually user\stateson\documents
                strSpiderBin = strSpiderBin0.Replace("Documents", "Saved Games\\Microsoft Games\\Spider Solitaire\\Spider Solitaire.SpiderSolitaireSave-ms");
                bIsThere = File.Exists(strSpiderBin);
                if (!bIsThere)
                {
                    // user has made it hard to find, try looking in the registry in case the windows 10 "MOVE" relocated Documents.
                    // we need to find the original Documents location, not the new one
                    PathToDirectory = KnownFolderFinder.GetFolderFromKnownFolderGUID(new Guid("{FDD39AD0-238F-46AF-ADB4-6C85480369C7}"));
                    stTemp = PathToDirectory;
                    strSpiderBin = PathToDirectory.Replace("Documents", "Saved Games\\Microsoft Games\\Spider Solitaire\\Spider Solitaire.SpiderSolitaireSave-ms");
                    PathToResult = stTemp.Replace("Documents", strResultName);
                    bIsThere = File.Exists(strSpiderBin);
                    if (!bIsThere)
                    {
                        string strEXE;
                        Console.WriteLine("nothing here also " + strSpiderBin + "\n");
                        Console.WriteLine("Trying local directory where this .exe program resides\n");
                        strEXE = System.Reflection.Assembly.GetEntryAssembly().Location;
                        PathToDirectory = System.IO.Path.GetDirectoryName(strSpiderBin0);
                        Console.WriteLine("Looking here: " + PathToDirectory + "\n");
                        strSpiderBin0 = PathToDirectory + "\\Spider Solitaire.SpiderSolitaireSave-ms";
                        if (!File.Exists(strSpiderBin0))
                        {
                            Console.WriteLine("Giving up, cannot find " + strSpiderBin0);
                            Console.ReadLine();
                            Environment.Exit(0);
                        }
                        else strSpiderBin = strSpiderBin0;
                    }
                    else strSpiderBin0 = strSpiderBin;
                }
                else strSpiderBin0 = strSpiderBin;
            }
            else strSpiderBin = strSpiderBin0;
            PathToDirectory = Path.GetDirectoryName(strSpiderBin) + "\\";

            Directory.CreateDirectory(PathToResult);    // create 2018 if not exist
            while(true)
            {
                iDirNum++;
                stTemp = PathToResult + "\\" + iDirNum.ToString();
                bIsThere = Directory.Exists(stTemp);
                if (bIsThere) continue;
                bIsThere = Directory.Exists(stTemp + "-L");
                if (bIsThere) continue;
                break;
            }

            Console.WriteLine("enter W- for win or L- for loss plus any comment\n");
            Console.WriteLine("enter R to rename or enter A to add text when renaming\n");
            Console.WriteLine("enter F for first or 0..5 for seeds and G for original\n");
            Console.WriteLine("  example:  R S2C1    A -0f-   A -2SEED-\n");
            string strReply = Console.ReadLine();
            string strSuffix = strReply.Substring(0,1).ToLower();
            string strLRename = "f012345g";
            string[] strLs = { "first","0seed", "1seed", "2seed", "3seed", "4seed", "5seed", "ORIGINAL" };
            int iCmd = strLRename.IndexOf(strSuffix);
            if (iCmd >= 0)
            {
                strName = "Spider Solitaire.SpiderSolitaireSave-ms";
                strFrom = PathToDirectory + strName;
                if (File.Exists(strFrom))
                {
                    string strPathTo = strFrom;
                    string strTo = strLs[iCmd];
                    strName = "Spider Solitaire" + "-" + strTo + "-.";

                    System.IO.File.Move(strFrom, strPathTo.Replace("Spider Solitaire.", strName));
                    Environment.Exit(0);
                }
            }

            if (strSuffix == "r" || strSuffix == "a")
            {
                strName = "Spider Solitaire.SpiderSolitaireSave-ms";
                strFrom = PathToDirectory + strName;
                if (File.Exists(strFrom))
                {
                    string strPathTo = strFrom;
                    string strTo = strReply.Substring(1) + ".";
                    if (strSuffix.ToLower() == "a")
                        strName = "Spider Solitaire" + strTo.Trim();
                    else strName = strTo.Trim();
                    
                    System.IO.File.Move(strFrom, strPathTo.Replace("Spider Solitaire.", strName));
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("cannot find save file\n");
                    Console.ReadLine();
                    Environment.Exit(0);
                }
            }
            strSuffix = "";
            if (strReply.Substring(0, 1).ToLower() == "l")
            {
                strSuffix = "-L";
            }
            else
            {
                // no other commands other than L or W
                if(strReply.Substring(0, 1).ToLower() != "w")
                {
                    Console.WriteLine("Invalid command, try again\n");
                    Console.ReadLine();
                    Environment.Exit(0);
                }
            }
            ClearDir(PathToDirectory); 
            stTemp += strSuffix;
            Directory.CreateDirectory(stTemp);
            foreach (string sFile in Directory.GetFiles(PathToDirectory, "*.SpiderSolitaireSave-ms"))
            {
                if (Path.GetFileName(sFile) == "Spider Solitaire.SpiderSolitaireSave-ms" && strSuffix == "") continue;
                dFile = stTemp + "\\" + Path.GetFileName(sFile);
                System.IO.File.Move(sFile, dFile);
            }
            if (strReply != "")
            {
                dFile = stTemp + "\\notes.txt";
                System.IO.File.WriteAllText(dFile,strReply);
            }
            // need to create a new spider game
            // no simple way since program could be anywhere
        }
    }
}
