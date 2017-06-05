using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SplitFile
{
    class Program
    {
        public static StreamWriter CreateSplitFile(FileInfo org, long fileCount)
        {
            string mainpart = org.Name.Substring(0, org.Name.IndexOf(org.Extension));
            string currFile = string.Format("{0}_({1}){2}", mainpart, fileCount, org.Extension);
            return new StreamWriter(org.DirectoryName + "\\" + currFile, false, Encoding.UTF8);
        }

        static void Main(string[] args)
        {
            long currBytes = 0;
            long maxBytes = -1 ;
            long maxLines = -1;
            long fromLine = -1;
            long toLine = -1;
 
            var cmd = new Arguments(args);

            if (args.Length != 2 || cmd["INFILE"] == null)
            {
                Console.WriteLine("Usage: SplitFile ") ;
                return ;
            }

            if (cmd["MAXSIZE"] != null)
            {
                long tmp;
                if (long.TryParse(cmd["MAXSIZE"], out tmp) == true)
                {
                    maxBytes = ((tmp * 1024) * 1024);
                    Console.WriteLine("SplitFile: MAXSIZE = " + cmd["MAXSIZE"] + " -> " + maxBytes + " bytes");
                } 
            }

            if (cmd["MAXLEN"] != null)
            {
                long tmp;
                if (long.TryParse(cmd["MAXLEN"], out tmp) == true)
                {
                    maxLines = tmp;
                    Console.WriteLine("SplitFile: MAXLEN = " + cmd["MAXLEN"]);
                }
            }

            if (cmd["PICKLINES"] != null)
            {
                string[] items = cmd["PICKLINES"].Split(new char[] { ',' });
                long tmp;

                if (long.TryParse(items[0], out tmp) == true)
                    fromLine = tmp;
                else
                    fromLine = 0;

                Console.WriteLine("SplitFile: PICKLINES = " + cmd["PICKLINES"] + ". From = " + fromLine);

                if (long.TryParse(items[1], out tmp) == true)
                    toLine = tmp;
                else
                    toLine = long.MaxValue;

                Console.WriteLine("SplitFile: PICKLINES = " + cmd["PICKLINES"] + ". To = " + toLine);
            }

            if (maxLines == -1 && maxBytes == -1 && toLine == -1)
            {
                Console.WriteLine("SplitFile: Either MAXSIZE, MAXLEN, PICKLINES must be given");
                return;
            }

            var fi = new FileInfo(cmd["INFILE"]);
            if (fi.Exists == false)
            {
                Console.WriteLine("SplitFile: Input file '" + cmd["INFILE"] + "' doesn't exist");
                return;
            }

            Console.WriteLine("SplitFile: Processing input file '" + fi.Name + "'");

            long lineCount = 1;
            long fileCount = 1;
            DateTime tStart = DateTime.Now ;

            StreamWriter outFile = CreateSplitFile(fi, 0);
            bool isNew = false;

            using (StreamReader sr = new StreamReader(fi.FullName, Encoding.UTF8))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (isNew == true)
                    {
                        outFile = CreateSplitFile(fi, fileCount);
                        fileCount++;
                        isNew = false;
                    }

                    if ((fromLine >= 0) && (toLine >= 0))
                    {
                        if ((lineCount >= fromLine) && (lineCount <= toLine))
                        {
                            Console.WriteLine("SplitFile: Picking line #" + lineCount);
                            outFile.WriteLine(line);
                            currBytes += line.Length;
                        }

                        if (lineCount > toLine)
                        {
                            outFile.Close();
                            break;
                        }
                    }
                    else
                    {
                        outFile.WriteLine(line);
                        currBytes += line.Length;
                    }

                    lineCount++ ;

                    if ( (maxLines > 0 && (lineCount % maxLines) == 0) || ((maxBytes > 0) && (currBytes >= maxBytes)))
                    {
                        outFile.Close();
                        isNew = true;
                        currBytes = 0;

                        Console.WriteLine("SplitFile: Processed file #" + fileCount);
                    }
                }
            }

            TimeSpan elapsed = DateTime.Now - tStart ;
            Console.WriteLine("SplitFile: " + fileCount + " file(s) processed in " + elapsed.TotalSeconds.ToString("#0") + " seconds");
       }
    }
}
