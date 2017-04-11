using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.CommandBars;
using System.Linq;
using EnvDTE;
using CommandBuild;
using SolutionBuild.Construction;
using Microsoft.Build.Execution;
using EnvDTE80;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace Microsoft.Clover_BuildSolution
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    //[ProvideAutoLoad(VSConstants.EmptySolution)]
    [ProvideAutoLoad("{99b8fa2f-ab90-4f57-9c32-949f146f1914}")]
    [Guid(GuidList.guidClover_BuildSolutionPkgString)]
    public sealed class Clover_BuildSolutionPackage : Package
    {
        public CommandBarPopup BuildProjectMenu { get; set; }
        public CommandBarPopup BuildSolutionMenu { get; set; }
        public CommandBarPopup ToolMenu { get; set; }
        public stdole.StdPicture BuildProjectPicture { get; set; }
        public stdole.StdPicture BuildSolutionPicture { get; set; }
        public DTE DTE { get; set; }



        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public Clover_BuildSolutionPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            InitializeCustomMenu();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                var BuildSolutionMenuID = new CommandID(GuidList.guidClover_BuildSolutionCmdSet, (int)PkgCmdIDList.BuildSolutionMenu);
                var BuildSolutionMenuItem = new OleMenuCommand(MenuItemCallback, BuildSolutionMenuID);
                BuildSolutionMenuItem.BeforeQueryStatus += menuItem_BeforeQueryStatus;
                mcs.AddCommand(BuildSolutionMenuItem);

                var BuildProjectMenuID = new CommandID(GuidList.guidClover_BuildSolutionCmdSet, (int)PkgCmdIDList.BuildProjectMenu);
                var BuildProjectMenuItem = new OleMenuCommand(MenuItemCallback, BuildProjectMenuID);
                BuildProjectMenuItem.BeforeQueryStatus += menuItem_BeforeQueryStatus;
                mcs.AddCommand(BuildProjectMenuItem);

                var OpenSolutionButtonID = new CommandID(GuidList.guidClover_BuildSolutionCmdSet, (int)PkgCmdIDList.cmdidOpenSolution);
                var OpenSolutionButtonItem = new OleMenuCommand(MenuItemCallback, OpenSolutionButtonID);
                OpenSolutionButtonItem.BeforeQueryStatus += menuItem_BeforeQueryStatus;
                mcs.AddCommand(OpenSolutionButtonItem);
            }
        }

        void menuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menu = sender as OleMenuCommand;
            if (null == menu) return;

            var versionControlInstance = new VersionControlInstance(this);
            if (versionControlInstance.IsSlnFile())
            {
                var slnFile = versionControlInstance.GetSlnFile();

                var solutionInstance = new SolutionInstance(slnFile);
                switch (menu.CommandID.ID)
                {
                    case (int)PkgCmdIDList.BuildProjectMenu:
                        if (null == BuildProjectMenu) return;
                        BuildProjectMenu.Reset();
                        foreach (var projectInsolution in solutionInstance.Solution.Projects)
                        {
                            if (projectInsolution.ProjectType == SolutionProjectType.SolutionFolder) continue;
                            var projectMenu = BuildProjectMenu.Controls.Add(MsoControlType.msoControlPopup, Type.Missing, Type.Missing, Type.Missing, false) as CommandBarPopup;

                            projectMenu.Caption = projectInsolution.ProjectName;

                            foreach (var projectConfig in solutionInstance.GetProjectConfig(projectInsolution))
                            {
                                var buildConfigButton = projectMenu.Controls.Add(MsoControlType.msoControlButton, Type.Missing, Type.Missing, Type.Missing, false) as CommandBarButton;
                                buildConfigButton.Caption = projectConfig.Value.FullName;
                                buildConfigButton.Click += buildConfigButton_Click;
                                buildConfigButton.Parameter = solutionInstance.SolutionFile + ";" + projectConfig.Key + ";" + projectInsolution.AbsolutePath + ";" + projectConfig.Value.FullName;
                                buildConfigButton.Tag = "Build Project";
                                buildConfigButton.Style = MsoButtonStyle.msoButtonIconAndCaption;
                                buildConfigButton.Picture = BuildProjectPicture;
                            }
                        }
                        break;
                    case (int)PkgCmdIDList.BuildSolutionMenu:
                        if (null == BuildSolutionMenu) return;
                        BuildSolutionMenu.Reset();
                        foreach (var solutionConfig in solutionInstance.GetSolutionConfig())
                        {
                            var buildConfigButton = BuildSolutionMenu.Controls.Add(MsoControlType.msoControlButton, Type.Missing, Type.Missing, Type.Missing, false) as CommandBarButton;
                            buildConfigButton.Caption = solutionConfig.FullName;
                            buildConfigButton.Click += buildConfigButton_Click;
                            buildConfigButton.Parameter = solutionInstance.SolutionFile + ";" + solutionConfig.FullName;
                            buildConfigButton.Tag = "Build Solution";
                            buildConfigButton.Style = MsoButtonStyle.msoButtonIconAndCaption;
                            buildConfigButton.Picture = BuildSolutionPicture;
                        }
                        break;
                    case (int)PkgCmdIDList.cmdidOpenSolution:
                        menu.ParametersDescription = solutionInstance.SolutionFile;
                        break;
                }
                menu.Enabled = true;
            }
            else
            {
                switch (menu.CommandID.ID)
                {
                    case (int)PkgCmdIDList.BuildSolutionMenu:
                        BuildSolutionMenu.Reset();
                        break;
                    case (int)PkgCmdIDList.BuildProjectMenu:
                        BuildProjectMenu.Reset();
                        break;
                    default:
                        menu.Enabled = false;
                        break;
                }
            }
        }

        void buildConfigButton_Click(CommandBarButton Ctrl, ref bool CancelDefault)
        {
            var paramters = Ctrl.Parameter.Split(';');
            var devenvInstance = new DevenvInstance();
            devenvInstance.SetCurrentDevenvFullName(DTE.FullName);

            var outputWindow = (DTE as DTE2).ToolWindows.OutputWindow;
            var buildOutputPane = outputWindow.OutputWindowPanes.Item(VSConstants.OutputWindowPaneGuid.BuildOutputPane_string);
            var sortedBuildOutputPane = outputWindow.OutputWindowPanes.Item(VSConstants.OutputWindowPaneGuid.SortedBuildOutputPane_string);

            var buildLog = new SortedDictionary<string, List<string>>();

            sortedBuildOutputPane.Activate();
            buildOutputPane.Activate();

            devenvInstance.Starting += (sender, e) =>
            {
                buildOutputPane.Clear();
                sortedBuildOutputPane.Clear();

                DTE.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationBuild);
                DTE.StatusBar.Text = "已启动全部重新生成...";
            };

            devenvInstance.Exited += (sender, e) =>
            {
                foreach (var log in buildLog)
                {
                    log.Value.ForEach(m => sortedBuildOutputPane.OutputString(m + Environment.NewLine));
                }
                DTE.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationBuild);
                DTE.StatusBar.Text = buildLog.Values.SelectMany(m => m).Any(m => m.Contains("error")) ? "重新生成失败" : "全部重新生成已成功";
            };

            devenvInstance.OutputString += (sender, e) =>
            {
                if (!Regex.IsMatch(e.Data, "^[\\d|==]")) return;
                buildOutputPane.OutputString(e.Data + Environment.NewLine);

                if (Regex.IsMatch(e.Data, @"^(?<ID>\d)+"))
                {
                    var key = Regex.Match(e.Data, @"^(?<ID>\d)+").Groups["ID"].Value;
                    if (buildLog.ContainsKey(key))
                    {
                        buildLog[key].Add(e.Data);
                    }
                    else
                    {
                        buildLog.Add(key, new List<string>() { e.Data });
                    }
                }
                else
                {
                    buildLog.Add("result", new List<string>() { e.Data });
                }
            };

            switch (Ctrl.Tag)
            {
                case "Build Solution":
                    devenvInstance.ReBuild(paramters[0], paramters[1]);
                    break;
                case "Build Project":
                    devenvInstance.ReBuildProject(paramters[0], paramters[1], paramters[2], paramters[3]);
                    break;
            }
        }
        #endregion


        /// <summary>
        /// 初始化自定义菜单
        /// </summary>
        private void InitializeCustomMenu()
        {
            DTE = (DTE)GetService(typeof(DTE));

            var commandBars = DTE.CommandBars as CommandBars;
            var sourceControlExplorer = commandBars["Source Control Explorer"];
            BuildProjectMenu = sourceControlExplorer.Controls["Build Project"] as CommandBarPopup;
            BuildSolutionMenu = sourceControlExplorer.Controls["Build Solution"] as CommandBarPopup;


            var buildMenu = (commandBars["MenuBar"].Controls["Build"] as CommandBarPopup);
            BuildSolutionPicture = (buildMenu.Controls["Build Solution"] as CommandBarButton).Picture;
            BuildProjectPicture = (buildMenu.Controls["Build Selection"] as CommandBarButton).Picture;
        }


        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var menu = sender as OleMenuCommand;
            if (null == menu) return;

            var devenvInstance = new DevenvInstance();
            devenvInstance.SetCurrentDevenvFullName(DTE.FullName);
            devenvInstance.Open(menu.ParametersDescription);
        }



    }
}