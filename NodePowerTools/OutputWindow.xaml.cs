using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Windows.Controls;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Extensibility.Editor;

namespace NodePowerTools
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    public partial class OutputWindow : UserControl
    {
        //--------------------------------------------------------------------------
        //
        //	Variables
        //
        //--------------------------------------------------------------------------

        #region Variables

        private DirectoryInfo _logDirectory;
        private ISiteFileWatcherService _siteFileWatcherService;
        private List<ConsoleLog> _readers = new List<ConsoleLog>();

        #endregion

        //--------------------------------------------------------------------------
        //
        //	Constructors
        //
        //--------------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public OutputWindow()
        {            
            InitializeComponent();
        }
        #endregion

        //--------------------------------------------------------------------------
        //
        //	Methods
        //
        //--------------------------------------------------------------------------

        #region Initialize
        /// <summary>
        /// monitor the log file for changes, set up the initial read
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="siteFileWatcherService"></param>
        public void Initialize(string logDirectory, ISiteFileWatcherService siteFileWatcherService, ITheme theme)
        {
            _logDirectory = new DirectoryInfo(logDirectory);
            _siteFileWatcherService = siteFileWatcherService;
                                    
            _siteFileWatcherService.RegisterForSiteNotifications(WatcherChangeTypes.Created | WatcherChangeTypes.Changed, new FileSystemEventHandler(FileSystemEvent), null);

            //outy.Foreground = theme.DefaultFormat.ForeColor;
            //outy.Background = theme.DefaultFormat.BackColor;
        }
        #endregion

        #region PerformInitialRead
        /// <summary>
        /// 
        /// </summary>
        private void PerformInitialRead(string path)
        {            
            var fileStreamReader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8);
            WriteToLog(fileStreamReader.ReadToEnd());
            _readers.Add(new ConsoleLog() { FileStream=fileStreamReader, Path=path });
        }
        #endregion

        #region WriteToLog
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        private void WriteToLog(string text)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.outy.Text += text + "\r\n";
                scrollViewer.ScrollToEnd();
            }));
        }
        #endregion
        
        //--------------------------------------------------------------------------
        //
        //	Event Handlers
        //
        //--------------------------------------------------------------------------

        #region FileSystemEvent
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileSystemEvent(object sender, FileSystemEventArgs e)
        {
            // make sure the change was in the iisnode/ path
            if (Path.GetDirectoryName(e.FullPath) == _logDirectory.FullName && e.FullPath.EndsWith(".txt"))
            {                                
                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                    // looks like a new log got dropped - read that now                    
                    this.PerformInitialRead(e.FullPath);
                }
                else if (e.ChangeType == WatcherChangeTypes.Changed)
                {
                    // check if we have an open stream for this log
                    var readers = _readers.Where(x => x.Path.ToLower() == e.FullPath.ToLower());
                    if (readers.Count() == 0)
                    {
                        this.PerformInitialRead(e.FullPath);
                    }
                    else
                    {
                        // something strange happened - re-read the file
                        var reader = readers.First();
                        if (reader.FileStream.BaseStream.Length < reader.LogLegnth)
                        {
                            // if the log has decreased in size, it got reset.  
                            this.PerformInitialRead(reader.Path);
                        }
                        else
                        {
                            // there was an additive change to the log
                            reader.LogLegnth = reader.FileStream.BaseStream.Length;
                            var line = "";
                            while ((line = reader.FileStream.ReadLine()) != null)
                            {
                                this.WriteToLog(line);
                            }
                        }
                    }
                }
            }
           
        }
        #endregion
    }
}
