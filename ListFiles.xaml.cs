using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Linq;

namespace PerniciousGames.OpenFileInSolution
{
    /// <summary>
    /// Interaction logic for ListFiles.xaml
    /// </summary>
    public partial class ListFiles : System.Windows.Window
    {
        public ObservableCollection<ProjectItemWrapper> items { get; set; }

        private CollectionViewSource viewSource;
        private bool bSearchFullPath; // todo: config-ize me

        // todo: save static list of last-entered strings. restore most recent one when opening.

        public ListFiles(IEnumerable<ProjectItemWrapper> inItems)
        {
            items = new ObservableCollection<ProjectItemWrapper>(inItems);
            viewSource = new CollectionViewSource();

            InitializeComponent();

            viewSource.Filter += FilterProjectItems;
            viewSource.Source = items;
            lstFiles.ItemsSource = viewSource.View;

            txtFilter.Focus();
        }

        private void FilterProjectItems(object sender, FilterEventArgs e)
        {
            e.Accepted = true;
            if (!string.IsNullOrEmpty(txtFilter.Text))
            {
                foreach (var filter in txtFilter.Text.Split(' '))
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
            viewSource.View.Refresh();
        }

        private void OpenSelectedFiles()
        {
            foreach (var item in lstFiles.SelectedItems)
            {
                var w = (item as ProjectItemWrapper).ProjItem.Open();
                w.Visible = true;
            }
            Close();
        }

        private void txtFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                OpenSelectedFiles();
            }
            else if (lstFiles.Items.Count > 0)
            {
                if (e.Key == Key.Down)
                {
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        var lastSelectedIndex = -1;
                        if (lstFiles.SelectedItems.Count > 0)
                        {
                            lastSelectedIndex = lstFiles.SelectedItems.Cast<object>().Max(x => lstFiles.Items.IndexOf(x));
                        }

                        if (lstFiles.Items.Count > lastSelectedIndex + 1 && lastSelectedIndex >= 0)
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
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        var firstSelectedIndex = -1;
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

        private void lstFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                OpenSelectedFiles();
            }
        }

        private void lstFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenSelectedFiles();
        }
    }
}
