using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace PerniciousGames.OpenFileInSolution
{
    public class ProjectItemWrapper : IEquatable<ProjectItemWrapper>
    {
        public string Filename { get; set; }
        public string Project { get; set; }
        public string Path { get; set; }
        public ProjectItem ProjItem;

        private ProjectItemWrapper()
        {

        }

        public ProjectItemWrapper(ProjectItem inItem)
        {
            ProjItem = inItem;
            Path = inItem.FileNames[1];
            Filename = System.IO.Path.GetFileName(Path);
            Project = ProjItem.ContainingProject.Name;
        }

        public bool Equals(ProjectItemWrapper other)
        {
            return Path == other.Path;
        }
    }

    public class ProjectItemComparer : IEqualityComparer<ProjectItemWrapper>
    {
        public bool Equals(ProjectItemWrapper x, ProjectItemWrapper y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(ProjectItemWrapper obj)
        {
            return obj.Path.GetHashCode();
        }
    }

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
    [InstalledProductRegistration("OpenFileInSolution", "Displays a dialog to quick-open any file in the solution", "ID")]
    [Guid(GuidList.guidOpenFileInSolutionPkgString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class OpenFileInSolutionPackage : Package
    {
        public abstract class EnvDTEProjectKinds
        {
            public const string vsProjectKindSolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
        }

        static readonly Guid ProjectFileGuid = new Guid("6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C");
        static readonly Guid ProjectFolderGuid = new Guid("6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C");
        static readonly Guid ProjectVirtualFolderGuid = new Guid("6BB5F8F0-4483-11D3-8BCF-00C04F8EC28C");
        static readonly List<string> FileEndingsToSkip = new List<string>()
            {
                ".vcxproj.filters",
                ".vcxproj"
            };

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require
        /// any Visual Studio service because at this point the package object is created but
        /// not sited yet inside Visual Studio environment. The place to do all the other
        /// initialization is the Initialize method.
        /// </summary>
        public OpenFileInSolutionPackage()
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

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidOpenFileInSolutionCmdSet, 0x100);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);
            }
            // todo: add option to jump between h and cpp?
        }
        #endregion

        public static DTE GetActiveIDE()
        {
            // Get an instance of currently running Visual Studio IDE.
            DTE dte2 = Package.GetGlobalService(typeof(DTE)) as DTE;
            return dte2;
        }

        public static IList<Project> GetProjects()
        {
            Projects projects = GetActiveIDE().Solution.Projects;
            List<Project> list = new List<Project>();
            var item = projects.GetEnumerator();
            while (item.MoveNext())
            {
                var project = item.Current as Project;
                if (project == null)
                {
                    continue;
                }

                if (project.Kind == EnvDTEProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(project));
                }

                list.Add(project);
            }

            return list;
        }

        private static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            List<Project> list = new List<Project>();
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                // If this is another solution folder, do a recursive call, otherwise add
                if (subProject.Kind == EnvDTEProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(subProject));
                }

                list.Add(subProject);
            }
            return list;
        }

        private IEnumerable<ProjectItemWrapper> EnumerateProjectItems(ProjectItems items)
        {
            if (items != null)
            {
                for (int i = 1; i <= items.Count; i++)
                {
                    var itm = items.Item(i);

                    foreach (var res in EnumerateProjectItems(itm.ProjectItems))
                    {
                        yield return res;
                    }

                    try
                    {
                        var itmGuid = Guid.Parse(itm.Kind);
                        if (itmGuid.Equals(ProjectVirtualFolderGuid)
                            || itmGuid.Equals(ProjectFolderGuid))
                        {
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                        // itm.Kind may throw an exception with certain node types like WixExtension (COMException)
                    }

                    for (short j = 0; itm != null && j < itm.FileCount; j++)
                    {
                        bool bSkip = false;
                        foreach (var ending in FileEndingsToSkip)
                        {
                            if (itm.FileNames[1] == null || itm.FileNames[1].EndsWith(ending))
                            {
                                bSkip = true;
                                break;
                            }
                        }

                        if (!bSkip)
                        {
                            yield return new ProjectItemWrapper(itm);
                        }
                    }
                }
            }
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            var projItems = new Dictionary<string, ProjectItemWrapper>(StringComparer.Ordinal);
            foreach (var proj in GetProjects())
            {
                foreach (var item in EnumerateProjectItems(proj.ProjectItems))
                {
                    if (!projItems.ContainsKey(item.Path))
                    {
                        projItems.Add(item.Path, item);
                    }
                }
            }

            var wnd = new ListFiles(projItems.Values);
            wnd.Owner = HwndSource.FromHwnd(new IntPtr(GetActiveIDE().MainWindow.HWnd)).RootVisual as System.Windows.Window;
            wnd.Width = wnd.Owner.Width / 2;
            wnd.Height = wnd.Owner.Height / 3;
            wnd.ShowDialog();
        }
    }
}
