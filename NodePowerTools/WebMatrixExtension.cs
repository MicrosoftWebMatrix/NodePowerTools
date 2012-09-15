using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Microsoft.WebMatrix.Extensibility;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Windows.Media;
using System.Windows;
using Microsoft.WebMatrix.Extensibility.Editor;
using System.Collections.Generic;
using System.ComponentModel;

namespace NodePowerTools
{
    /// <summary>
    /// A sample WebMatrix extension.
    /// </summary>
    [Export(typeof(Extension))]
    public class WebMatrixExtension : Extension
    {       

        //--------------------------------------------------------------------------
        //
        //	Variables
        //
        //--------------------------------------------------------------------------

        #region Variables

        /// <summary>
        /// Stores a reference to the small star image.
        /// </summary>
        private readonly BitmapImage _starImageSmall = new BitmapImage(new Uri("pack://application:,,,/NodePowerTools;component/Star_16x16.png", UriKind.Absolute));

        /// <summary>
        /// Stores a reference to the large star image.
        /// </summary>
        private readonly BitmapImage _starImageLarge = new BitmapImage(new Uri("pack://application:,,,/NodePowerTools;component/Star_32x32.png", UriKind.Absolute));

        /// <summary>
        /// launches chrome with the address for node-inspector 
        /// </summary>
        private readonly DelegateCommand _launchDebuggerCommand;

        /// <summary>
        /// Node control ribbon group - is there a way to grab existing ones?
        /// </summary>
        private RibbonGroup _ribbonGroup;

        /// <summary>
        /// determines if the current site is node or not
        /// </summary>
        private bool _isNodeSite;

        /// <summary>
        /// reference to the host object used to speak with WebMatrix
        /// </summary>
        protected IWebMatrixHost _host;


        private static readonly Guid _outputTaskPanelId = new Guid("2f09fa84-888f-47c9-b333-b3501a0055b4");        
        private IEditorTaskPanelService _editorTaskPanel;                                                          
        private ISiteFileWatcherService _siteFileWatcher;                                                          
        private OutputWindow _outputWindow;                                                                        
        private string _mainScriptPath;                                                                            
                                                                                                                   
        [Import(ThemeKeys.DefaultTheme, typeof(ITheme))]                                                           
        private ITheme DefaultTheme { get; set; }                                                                                                                                                                                                                                                                
                                                                                                                   
        [Import(typeof(IEditorTaskPanelService))]                                                                  
        private IEditorTaskPanelService EditorTaskPanelService                                                     
        {                                                                                                          
            get                                                                                                    
            {                                                                                                      
                return _editorTaskPanel;                                                                           
            }                                                                                                      
            set                                                                                                    
            {
                _editorTaskPanel = value;
            }
        }

        [Import(typeof(ISiteFileWatcherService))]
        private ISiteFileWatcherService SiteFileWatcherService
        {
            get
            {
                return _siteFileWatcher;
            }
            set
            {
                _siteFileWatcher = value;
            }
        }

        #endregion

        //--------------------------------------------------------------------------
        //
        //	Constructors
        //
        //--------------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the NodePowerTools class.
        /// </summary>
        public WebMatrixExtension()
            : base("WebMatrixExtension")
        {
            _launchDebuggerCommand = new DelegateCommand(p =>
            {
                // check if google chrome is installed                
                if (!IsChromeInstalled())
                {
                    _host.ShowNotification("Chrome is not installed!  Node Inspector requires Chrome.");
                }
                else
                {
                    try
                    {
                        _mainScriptPath = this.GetMainFileName();
                        var nodeInspectorUrl = string.Format("{0}/{1}/debug", _host.WebSite.Uri.ToString(), _mainScriptPath);
                        Process.Start("chrome", nodeInspectorUrl);
                    }
                    catch (Win32Exception ex)
                    {
                        _host.ShowNotification("Chrome is not installed!  Node Inspector requires Chrome.");
                    }
                    catch (Exception ex)
                    {
                        _host.ShowExceptionMessage("Node Power Tools", "There was a problem launching chrome. Are you sure it's installed?", ex);
                    }
                    //Process.Start("chrome", _host.WebSite.Uri.ToString());
                }
            });
        }
        #endregion



        //--------------------------------------------------------------------------
        //
        //	Event Handlers
        //
        //--------------------------------------------------------------------------      

        #region Initialize
        /// <summary>
        /// Called when the WebMatrixHost property changes.
        /// </summary>
        /// <param name="Host">Host used to communicate with WebMatrix</param>
        protected override void Initialize(IWebMatrixHost host, ExtensionInitData data)
        {
            // Get new values
            _host = host;
            if (host != null)
            {
                host.WorkspaceChanged += new EventHandler<WorkspaceChangedEventArgs>(WebMatrixHost_WorkspaceChanged);
                host.WebSiteChanged += new EventHandler<EventArgs>(WebMatrixHost_WebSiteChanged);

                // Add a simple button to the Ribbon
                _ribbonGroup = new RibbonGroup(
                        "Node",
                        new RibbonItem[]
                        {
                            new RibbonButton(
                                "Debug",
                                _launchDebuggerCommand,
                                null,
                                _starImageSmall,
                                _starImageLarge)
                        });
                data.RibbonItems.Add(_ribbonGroup);
                _editorTaskPanel.PageChanged += InitializeLogTab;

                // handle the right click event the tree
                host.ContextMenuOpening += new EventHandler<ContextMenuOpeningEventArgs>(host_ContextMenuOpening);

                // if this is the first time the extension is installed, this method will be called                                               
                if (_host.WebSite != null && !String.IsNullOrEmpty(_host.WebSite.Path))
                {
                    _isNodeSite = IsNodeProject();
                    _ribbonGroup.IsVisible = _host.Workspace is IEditorWorkspace && _isNodeSite;
                    InitializeLogTab(this, EventArgs.Empty);
                }
            }            
        }
        #endregion

        #region InitializeLogTab
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InitializeLogTab(object sender, EventArgs e)
        {
            if (_isNodeSite && _host.WebSite != null && !String.IsNullOrEmpty(_host.WebSite.Path)
                && !_editorTaskPanel.TaskTabExists(_outputTaskPanelId))
            {
                _mainScriptPath = this.GetMainFileName();
                _outputWindow = new OutputWindow();
                _outputWindow.Initialize(Path.Combine(_host.WebSite.Path, "iisnode"), _siteFileWatcher, DefaultTheme);
                _editorTaskPanel.AddTaskTab(_outputTaskPanelId, new TaskTabItemDescriptor(null, "Output", _outputWindow, Brushes.DarkOliveGreen));
            }
            else
            {
                if (_editorTaskPanel.TaskTabExists(_outputTaskPanelId))
                {
                    _editorTaskPanel.RemoveTaskTab(_outputTaskPanelId);
                }
            }
        }
        #endregion


        #region WebMatrixHost_WorkspaceChanged
        /// <summary>
        /// Called when the WebMatrixHost's WorkspaceChanged event fires.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void WebMatrixHost_WorkspaceChanged(object sender, WorkspaceChangedEventArgs e)
        {
            _ribbonGroup.IsVisible = e.NewWorkspace is IEditorWorkspace && _isNodeSite;
        }
        #endregion

        #region WebMatrixHost_WebSiteChanged
        /// <summary>
        /// Called when the WebMatrixHost's WorkspaceChanged event fires.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void WebMatrixHost_WebSiteChanged(object sender, EventArgs e)
        {
            _isNodeSite = IsNodeProject();
        }
        #endregion

        #region host_ContextMenuOpening
        /// <summary>
        /// add an option to open a console here if it's a directory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void host_ContextMenuOpening(object sender, ContextMenuOpeningEventArgs e)
        {
            // we only show the context menu if every item selected in the tree is valid to be compiled 
            IList<string> paths = new List<string>();
            var showContextMenu = e.Items.Count > 0;
            foreach (ISiteItem item in e.Items)
            {
                if (item == null || !(item is ISiteFolder))
                {
                    showContextMenu = false;
                    break;
                }
                else
                {
                    paths.Add((item as ISiteFolder).Path);
                }
            }

            // if all of the files in the list are valid, show the command window option
            if (showContextMenu)
            {
                var menuItem = new ContextMenuItem("Open Command Window", null, new DelegateCommand(new Action<object>(OpenCMD)), paths);
                e.AddMenuItem(menuItem);
            }
        }
        
        #endregion

        //--------------------------------------------------------------------------
        //
        //	Methods
        //
        //--------------------------------------------------------------------------

        #region IsChromeInstalled
        /// <summary>
        /// check the registry to see if google chrome is installed, it's required to use
        /// node-inspector
        /// </summary>
        /// <returns></returns>
        protected bool IsChromeInstalled()
        {
            // older installs don't have the clients reg, so check uninstall
            var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
            if (key != null)
            {
                foreach (string name in key.GetSubKeyNames())
                {
                    if (name == "Google Chrome") return true;
                }
            }

            // try it this way
            key = Registry.CurrentUser.OpenSubKey(@"Software\Google\Update\Clients");
            if (key == null) return false;

            var subkeys = key.GetSubKeyNames();
            if (subkeys == null) return false;

            foreach (string name in key.GetSubKeyNames())
            {
                using (RegistryKey tkey = key.OpenSubKey(name))
                {
                    if (tkey != null && tkey.GetValue("name") != null && tkey.GetValue("name").ToString() == "Google Chrome") 
                        return true;
                }
            }

            return false;
        }
        #endregion

        #region IsNodeProject
        /// <summary>
        /// determine if the current project is likely to be node
        /// TODO:  make sure randall does the real implementation of this
        /// </summary>
        /// <returns></returns>
        protected bool IsNodeProject()
        {
            // for now depend on the web.config
            if (_host.WebSite != null && !String.IsNullOrEmpty(_host.WebSite.Path))
            {
                var path = _host.WebSite.Path;
                if (File.Exists(path + @"\web.config"))
                {
                    using (var sr = new StreamReader(path + @"\web.config"))
                    {
                        string content = sr.ReadToEnd();
                        return (content.Contains("<add name=\"iisnode\""));
                    }
                }
            }
            return false;
        }
        #endregion

        #region GetMainFileName
        /// <summary>
        /// get the name of the main js entry point for the node app (assumes this is a node app)
        /// </summary>
        /// <returns></returns>
        protected string GetMainFileName()
        {
            // check for the precense of package.json, and look for a main entry point
            try
            {
                string packagePath = _host.WebSite.Path + @"\package.json";
                if (File.Exists(packagePath))
                {
                    using (var sr = new StreamReader(packagePath))
                    {
                        var content = sr.ReadToEnd();
                        var packageContent = JObject.Parse(content);
                        var main = packageContent["main"];
                        if (main != null) return main.Value<string>();
                    }
                }
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                Debug.Fail("Error reading package.json", ex.ToString());
            }

            // check for an server.js
            if (File.Exists(_host.WebSite.Path + @"\server.js"))
                return "server.js";

            // check for an app.js
            if (File.Exists(_host.WebSite.Path + @"\app.js"))
                return "app.js";

            // if all else fails, assume server.js
            return "server.js";
        }
        #endregion

        #region OpenCMD
        /// <summary>
        /// opens a cmd window at the current directory
        /// </summary>
        /// <param name="e"></param>
        protected void OpenCMD(object e)
        {
            IList<string> paths = (IList<string>)e;
            foreach (var path in paths)
            {
                ProcessStartInfo info = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = path
                };
                Process.Start(info);
            }
        }

        #endregion
    }
}
