using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MMICSharp;

namespace MMILauncher
{
    class WPFLogger : Logger
    {
        public MainWindow WPFRef;

        public WPFLogger(MainWindow reference)
        {
            WPFRef = reference;
            System.Console.SetOut(new WPFConsoleWriter());
        }

       
        protected override void CreateErrorLog(string text)
        {
            WPFRef.LogOutputBlock.Dispatcher.Invoke(() => {
                WPFRef.LogOutputBlock.Inlines.Add($"Error: {DateTime.Now} ----> {text}\n");
                //WPFRef.LogScroller.ScrollToBottom();
                }
            ) ;
        }

        protected override void CreateInfoLog(string text)
        {
            WPFRef.LogOutputBlock.Dispatcher.Invoke(() => {
                WPFRef.LogOutputBlock.Inlines.Add($"Info: {DateTime.Now} ----> {text}\n");
               // WPFRef.LogScroller.ScrollToBottom();
            }
            );
        }

        protected override void CreateDebugLog(string text)
        {
            WPFRef.LogOutputBlock.Dispatcher.Invoke(() => {
                WPFRef.LogOutputBlock.Inlines.Add($"Debug: {DateTime.Now} ----> {text}\n");
               // WPFRef.LogScroller.ScrollToBottom();
            }
            );
        }

        protected override void CreateWarningLog(string text)
        {
            WPFRef.LogOutputBlock.Dispatcher.Invoke(() => {
                WPFRef.LogOutputBlock.Inlines.Add($"Warning: {DateTime.Now} ----> {text}\n");
                // WPFRef.LogScroller.ScrollToBottom();
            }
            );
        }

    }


    class WPFConsoleWriter : System.IO.TextWriter
    {
        public override void Write(char value)
        {
            base.Write(value);
            WPFLogger.Log(Log_level.L_INFO, $"Forwarded from console: {value}");
        }

        public override void Write(string value)
        {
            base.Write(value);
            WPFLogger.Log(Log_level.L_INFO, $"Forwarded from console: {value}");
        }
        public override void WriteLine(string value)
        {
            base.WriteLine(value);
            WPFLogger.Log(Log_level.L_INFO, $"Forwarded from console: {value}");
        }
        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }
    }

}
