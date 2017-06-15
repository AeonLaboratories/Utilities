using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Utilities
{
    public class LogFile
    {
        static long instanceCount = 0;
        string FileName;
		Queue<string> Q;
        Thread FlusherThread;
        Stopwatch sw = new Stopwatch();

        public long ElapsedMilliseconds { get { return sw.ElapsedMilliseconds; } }
        public string TimeStampFormat { get; set; }
        public string TimeStamp()
		{ return DateTime.Now.ToString(TimeStampFormat); }

		public bool ArchiveDaily { get; set; }
		public string LogFolder { get; set; }
		public string ArchiveFolder { get; set; }
		string FullFileName { get { return LogFolder + FileName; } }
		public string Header = "";

		public LogFile()
		{
			TimeStampFormat = "MM/dd/yyyy HH:mm:ss.fff\t";
			LogFolder  = @".\";
			ArchiveFolder = @"archive\";
		}

        public LogFile(string filename, bool archiveDaily) : this()
        {
            ArchiveDaily = archiveDaily;
            instanceCount++;
            FileName = filename;
            Q = new Queue<string>();
            FlusherThread = new Thread(flusher);
            FlusherThread.Name = "Log FlusherThread " + instanceCount.ToString();

            FlusherThread.IsBackground = true;
            FlusherThread.Start();
            sw.Restart();
        }

        public LogFile(string filename) : this(filename, false) {}

        ~LogFile() { flush(); }

        public void Close()
        {
            flush();
        }

        void flusher()
        {
			try
			{
				while (true)
				{
					Thread.Sleep(5000);     // wait 5 seconds 
					flush();
				}
			}
			catch (Exception e)
			{ MessageBox.Show(e.ToString()); }
		}

        /// <summary>
        /// Write the entry into the log file with no newline added.
        /// </summary>
        /// <param name="entry"></param>
        public void Write(string entry)
        {
            lock (Q) { Q.Enqueue(entry); }
            sw.Restart();
        }

        /// <summary>
        /// Writes a one-line entry into the logfile (adds "\r\n").
        /// </summary>
        /// <param name="entry"></param>
        public void WriteLine(string entry)
        { Write(entry + "\r\n"); }

        /// <summary>
        /// Writes a one-line timestamp and entry to the log file
        /// </summary>
        /// <param name="entry">Text to appear after the time stamp</param>
        public void Record(string entry)
        { WriteLine(TimeStamp() + entry); }

        string recordedEntry = "";
        string culledTimestamp = "";
		/// <summary>
		/// Writes a one-line timestamp and entry to the log file if
		/// the entry is not the same as the last entry.
		/// </summary>
		/// <param name="entry">Text to appear after the time stamp</param>
		public void LogParsimoniously(string entry)
        {
            if (entry == recordedEntry)
                culledTimestamp = TimeStamp();
            else
            {
                if (culledTimestamp != "")
                {
                    WriteLine(culledTimestamp + recordedEntry);
                    culledTimestamp = "";
                }
                WriteLine(TimeStamp() + entry);
                recordedEntry = entry;
            }
        }

        void archive()
        {
            try
            {
                int dot = FileName.IndexOf(".");
                if (dot < 0) dot = FileName.Length;
                string archiveFileName = ArchiveFolder + FileName.Insert(dot, DateTime.Now.AddDays(-1).ToString(" yyyyMMdd"));
                if (File.Exists(FullFileName) && !File.Exists(archiveFileName))
					File.Move(FullFileName, archiveFileName);
            }
            catch { }
        }

        Boolean flush()
        {
            if (Q.Count > 0)
            {
                lock (Q)
                {
                    try
                    {
                        if (ArchiveDaily) archive();
						bool newFile = !File.Exists(FullFileName);
                        StreamWriter logFile = new StreamWriter(FullFileName, true);
						if (newFile && !string.IsNullOrEmpty(Header))
							logFile.WriteLine(Header);
                        while (Q.Count > 0)
                            logFile.Write(Q.Dequeue());
                        logFile.Close();
                    }
                    catch { return false; }
                }
            }
            return true;
        }
    }
}
