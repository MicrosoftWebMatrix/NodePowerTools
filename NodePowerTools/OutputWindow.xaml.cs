using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
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

        private string _filePath;
        private StreamReader _fileStreamReader;
        private ISiteFileWatcherService _siteFileWatcherService;
        private long _logLength;

        


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
        public void Initialize(string filePath, ISiteFileWatcherService siteFileWatcherService, ITheme theme)
        {
            _filePath = Path.GetFullPath(filePath);
            _siteFileWatcherService = siteFileWatcherService;
                
            if (File.Exists(_filePath))            
                this.PerformInitialRead();
            
            _siteFileWatcherService.RegisterForSiteNotifications(WatcherChangeTypes.All, new FileSystemEventHandler(FileSystemEvent), null);

            outy.Foreground = theme.DefaultFormat.ForeColor;
            outy.Background = theme.DefaultFormat.BackColor;                
        }
        #endregion

        #region PerformInitialRead
        /// <summary>
        /// 
        /// </summary>
        private void PerformInitialRead()
        {
            _fileStreamReader = new StreamReader(new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8);
            _logLength = _fileStreamReader.BaseStream.Length;
            this.WriteToLog(_fileStreamReader.ReadToEnd());
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
            if (e.FullPath.ToLower() == _filePath.ToLower())
            {
                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                    this.PerformInitialRead();             
                }
                else if (e.ChangeType == WatcherChangeTypes.Changed)
                {
                    if (_fileStreamReader.BaseStream.Length < _logLength)
                    {
                        // if the log has decreased in size, it got reset.  
                        this.PerformInitialRead();
                    }
                    else
                    {
                        // there was an additive change to the log
                        _logLength = _fileStreamReader.BaseStream.Length;
                        var line = "";
                        while ((line = _fileStreamReader.ReadLine()) != null)
                        {
                            this.WriteToLog(line);
                        }
                    }
                }
            }            
        }
        #endregion       
    }
}
