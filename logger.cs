﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace GPVersionDifferences
{
    public class FileLogger
    {
        private static FileLogger singleton;
        private StreamWriter logWriter;

        public static FileLogger Instance
        {
            get { return singleton ?? (singleton = new FileLogger()); }
        }

        public void Open(string filePath, bool append)
        {
            if (logWriter != null)
                throw new InvalidOperationException(
                    "Logger is already open");

            logWriter = new StreamWriter(filePath, true);
            logWriter.AutoFlush = true;
        }

        public void Close()
        {
            if (logWriter != null)
            {
                logWriter.Close();
                logWriter = null;
            }
        }

        public void CreateEntry(string entry)
        {
            if (this.logWriter == null)
                throw new InvalidOperationException(
                    "Logger is not open");
            logWriter.WriteLine("{0} - {1}",
                 DateTime.Now.ToString("ddMMyyyy hh:mm:ss"),
                 entry);
        }
    }
}
