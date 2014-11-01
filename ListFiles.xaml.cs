using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using EnvDTE;

namespace PerniciousGames.OpenFileInSolution
{
    /// <summary>
    /// Interaction logic for ListFiles.xaml
    /// </summary>
    public partial class ListFiles : System.Windows.Window
    {
        public ObservableCollection<ProjectItemWrapper> items { get; set; }

        private CollectionViewSource viewSource;
        public bool bSearchFullPath { get; set; } // todo: config-ize me

        private string[] filterStrings;

        public static string FilterText { get; set; }

        // todo: save static list of last-entered strings. restore most recent one when opening.

        public ListFiles(IEnumerable<ProjectItemWrapper> inItems)
        {
            items = new ObservableCollection<ProjectItemWrapper>(inItems);
            viewSource = new CollectionViewSource();
            viewSource.Source = items;

            InitializeComponent();

            viewSource.Filter += FilterProjectItems;
            lstFiles.ItemsSource = viewSource.View;

            txtFilter.Focus();
            txtFilter.SelectAll();
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

                    if (!string.IsNullOrWhiteSpace(filter) && !searchStr.Contains(filter))
                    {
                        e.Accepted = false;
                        break;
                    }
                }
            }
        }

        private void txtFilterChanged(object sender, TextChangedEventArgs e)
        {
            filterStrings = (sender as TextBox).Text.Split(' ');
            viewSource.View.Refresh();
            if (lstFiles.SelectedIndex == -1 && lstFiles.Items.Count > 0)
            {
                lstFiles.SelectedIndex = 0;
            }
        }

        private void OpenSelectedFiles(bool bInSolutionExplorer)
        {
            foreach (var item in lstFiles.SelectedItems)
            {
                if (!bInSolutionExplorer)
                {
                    var w = (item as ProjectItemWrapper).ProjItem.Open();
                    w.Visible = true;
                }
                else
                {
                    var projItem = (item as ProjectItemWrapper).ProjItem;
                    var ide = OpenFileInSolutionPackage.GetActiveIDE();
                    ide.Windows.Item(Constants.vsWindowKindSolutionExplorer).Activate();
                    projItem.ExpandView();
                    //((Microsoft.VisualStudio.PlatformUI.UIHierarchyMarshaler)ide.ActiveWindow.Object).GetItem(projItem.Name).Select(vsUISelectionType.vsUISelectionTypeSelect);
                    break;
                }
            }
            Close();
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
    }
}
