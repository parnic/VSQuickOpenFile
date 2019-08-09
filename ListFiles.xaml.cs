using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace PerniciousGames.OpenFileInSolution
{
    public partial class ListFiles : System.Windows.Window
    {
        internal class EnvDTEConstants
        {
            public const string vsWindowKindSolutionExplorer = "{3AE79031-E1BC-11D0-8F78-00A0C9110057}";
        }

        public ObservableCollection<ProjectItemWrapper> items { get; set; }

        private CollectionViewSource viewSource;
        public bool bSearchFullPath { get; set; }

        private string[] filterStrings;

        public static string FilterText { get; set; }

        public ListFiles(IEnumerable<ProjectItemWrapper> inItems)
        {
            items = new ObservableCollection<ProjectItemWrapper>(inItems.Distinct());
            viewSource = new CollectionViewSource();
            viewSource.Source = items;

            InitializeComponent();

            viewSource.Filter += FilterProjectItems;
            lstFiles.ItemsSource = viewSource.View;

            txtFilter.Focus();
            txtFilter.SelectAll();
            if (!string.IsNullOrEmpty(FilterText))
            {
                txtFilterChanged(txtFilter, TextChangedEventArgs.Empty as TextChangedEventArgs);
            }
        }

        protected void Window_SourceInitialized(object sender, System.EventArgs e)
        {
            LoadWindowSettings();
        }

        private void FilterProjectItems(object sender, FilterEventArgs e)
        {
            e.Accepted = true;
            if (!string.IsNullOrEmpty(FilterText))
            {
                foreach (var filter in filterStrings)
                {
                    var searchStr = (e.Item as ProjectItemWrapper).Filename.ToLower();
                    if (!bSearchFullPath)
                    {
                        searchStr = Path.GetFileName(searchStr);
                    }

                    if (!string.IsNullOrWhiteSpace(filter) && !searchStr.Contains(filter.ToLower()))
                    {
                        e.Accepted = false;
                        break;
                    }
                }
            }
        }

        private string GetSelectedFilename()
        {
            var selectedFile = (lstFiles.SelectedItem as ProjectItemWrapper);
            var selectedFilename = string.Empty;
            if (selectedFile != null)
            {
                selectedFilename = selectedFile.Filename;
                if (!bSearchFullPath)
                {
                    selectedFilename = Path.GetFileName(selectedFilename);
                }
            }

            return selectedFilename;
        }

        private void txtFilterChanged(object sender, TextChangedEventArgs e)
        {
            filterStrings = (sender as TextBox).Text.Split(' ');
            viewSource.View.Refresh();
            if (lstFiles.Items.Count > 0)
            {
                if (filterStrings.Length > 0)
                {
                    var selectedFilename = GetSelectedFilename();

                    foreach (var fileItem in lstFiles.Items)
                    {
                        var file = (fileItem as ProjectItemWrapper).Filename;
                        if (!bSearchFullPath)
                        {
                            file = Path.GetFileName(file.ToString());
                        }

                        if (string.IsNullOrEmpty(selectedFilename) || file.Length < selectedFilename.Length)
                        {
                            lstFiles.SelectedItem = fileItem;
                            selectedFilename = file;
                        }
                    }
                }

                if (lstFiles.SelectedIndex == -1)
                {
                    lstFiles.SelectedIndex = 0;
                }
            }
        }

        private void OpenSelectedFiles(bool bInSolutionExplorer)
        {
            foreach (var item in lstFiles.SelectedItems)
            {
                if (!bInSolutionExplorer)
                {
                    try
                    {
                        var w = (item as ProjectItemWrapper).ProjItem.Open();
                        w.Visible = true;
                        w.Activate();
                    }
                    catch (Exception)
                    {
                        var w = OpenFileInSolutionPackage.GetActiveIDE().ItemOperations.OpenFile((item as ProjectItemWrapper).Path);
                        w.Visible = true;
                        w.Activate();
                    }
                }
                else
                {
                    var projItem = (item as ProjectItemWrapper).ProjItem;
                    var ide = OpenFileInSolutionPackage.GetActiveIDE();
                    ide.Windows.Item(EnvDTEConstants.vsWindowKindSolutionExplorer).Activate();
                    projItem.ExpandView();
                    //((Microsoft.VisualStudio.PlatformUI.UIHierarchyMarshaler)ide.ActiveWindow.Object).GetItem(projItem.Name).Select(vsUISelectionType.vsUISelectionTypeSelect);
                    break;
                }
            }
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowSettings();
        }

        private void txtFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (lstFiles.Items.Count > 0)
            {
                if (e.Key == Key.Down)
                {
                    e.Handled = true;
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        var lastSelectedIndex = -1;
                        if (lstFiles.SelectedItems.Count > 0)
                        {
                            lastSelectedIndex = lstFiles.SelectedItems.Cast<object>().Max(x => lstFiles.Items.IndexOf(x));
                        }

                        if (lstFiles.Items.Count > lastSelectedIndex + 1 && lastSelectedIndex >= -1)
                        {
                            lstFiles.SelectedItems.Add(lstFiles.Items[lastSelectedIndex + 1]);
                        }
                    }
                    else
                    {
                        if (lstFiles.SelectedIndex == lstFiles.Items.Count - 1)
                        {
                            lstFiles.SelectedIndex = 0;
                        }
                        else
                        {
                            lstFiles.SelectedIndex++;
                        }
                    }
                    lstFiles.ScrollIntoView(lstFiles.SelectedItems[lstFiles.SelectedItems.Count - 1]);
                }
                else if (e.Key == Key.Up)
                {
                    e.Handled = true;
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        var firstSelectedIndex = lstFiles.Items.Count;
                        if (lstFiles.SelectedItems.Count > 0)
                        {
                            firstSelectedIndex = lstFiles.SelectedItems.Cast<object>().Min(x => lstFiles.Items.IndexOf(x));
                        }

                        if (firstSelectedIndex > 0)
                        {
                            lstFiles.SelectedItems.Add(lstFiles.Items[firstSelectedIndex - 1]);
                        }
                    }
                    else
                    {
                        if (lstFiles.SelectedIndex == 0)
                        {
                            lstFiles.SelectedIndex = lstFiles.Items.Count - 1;
                        }
                        else
                        {
                            lstFiles.SelectedIndex--;
                        }
                    }
                    lstFiles.ScrollIntoView(lstFiles.SelectedItems[0]);
                }
            }
        }

        private void lstFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstFiles.SelectedItems.Count > 0)
            {
                OpenSelectedFiles(false);
            }
        }

        private void lstFiles_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                OpenSelectedFiles(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
            }
            else if (e.Key == Key.Tab)
            {
                var NewSelectedIndex = lstFiles.SelectedIndex + 1;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    NewSelectedIndex -= 2;
                }
                if (NewSelectedIndex >= 0)
                {
                    e.Handled = true;
                    if (NewSelectedIndex < lstFiles.Items.Count)
                    {
                        lstFiles.SelectedIndex = NewSelectedIndex;
                    }
                    else
                    {
                        lstFiles.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                    }
                }
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (grdSettings.Visibility == Visibility.Collapsed)
            {
                grdSettings.Visibility = Visibility.Visible;
            }
            else
            {
                grdSettings.Visibility = Visibility.Collapsed;
            }
        }

        private void chkSearchFullPath_Checked(object sender, RoutedEventArgs e)
        {
            viewSource.View.Refresh();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                OpenSelectedFiles(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C || e.Key == Key.Insert)
            {
                var lCtrlState = Keyboard.GetKeyStates(Key.LeftCtrl);
                var rCtrlState = Keyboard.GetKeyStates(Key.LeftCtrl);
                if (lCtrlState.HasFlag(KeyStates.Down)
                    || rCtrlState.HasFlag(KeyStates.Down))
                {
                    if (!txtFilter.IsFocused || string.IsNullOrEmpty(txtFilter.SelectedText))
                    {
                        var copyStr = string.Empty;
                        foreach (var item in lstFiles.SelectedItems)
                        {
                            var projItem = item as ProjectItemWrapper;
                            if (projItem != null)
                            {
                                copyStr += projItem.Filename + System.Environment.NewLine;
                            }
                        }
                        if (!string.IsNullOrEmpty(copyStr))
                        {
                            Clipboard.SetText(copyStr);
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Restores the window's previous size and position
        /// </summary>
        private void LoadWindowSettings()
        {
            try
            {
                if (Properties.Settings.Default.Left > 0 && Properties.Settings.Default.Top > 0)
                {
                    var bottomBound = System.Windows.Forms.Screen.AllScreens.Max(s => s.Bounds.Bottom);
                    var rightBound = System.Windows.Forms.Screen.AllScreens.Max(s => s.Bounds.Right);

                    // only apply left setting if it is visible on current screen(s)
                    if (Properties.Settings.Default.Left < rightBound)
                    {
                        this.Left = Properties.Settings.Default.Left;
                    }

                    // only apply top setting if it is visible on current screen(s)
                    if (Properties.Settings.Default.Top < bottomBound)
                    {
                        this.Top = Properties.Settings.Default.Top;
                    }

                    this.Width = Properties.Settings.Default.Width;
                    this.Height = Properties.Settings.Default.Height;
                    this.WindowState = (WindowState)Properties.Settings.Default.WindowState;
                }
            }
            catch (System.Exception)
            {
                // swallow exception if settings fail to load
            }
        }

        /// <summary>
        /// Saves the window's current size and position
        /// </summary>
        private void SaveWindowSettings()
        {
            Properties.Settings.Default.Width = RestoreBounds.Width;
            Properties.Settings.Default.Height = RestoreBounds.Height;
            Properties.Settings.Default.Top = RestoreBounds.Top;
            Properties.Settings.Default.Left = RestoreBounds.Left;
            Properties.Settings.Default.WindowState = (int)this.WindowState;
        }
    }
}
