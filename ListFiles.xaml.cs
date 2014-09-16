using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace PerniciousGames.OpenFileInSolution
{
    /// <summary>
    /// Interaction logic for ListFiles.xaml
    /// </summary>
    public partial class ListFiles : System.Windows.Window
    {
        public ObservableCollection<ProjectItemWrapper> items { get; set; }

        private CollectionViewSource viewSource;

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
            // i don't like rebuilding this array for each element in the list, but if i make it a class property, it's garbage generation all the way down
            string[] searchFilters;
            if (!string.IsNullOrEmpty(txtFilter.Text) && txtFilter.Text.Contains(" "))
            {
                searchFilters = txtFilter.Text.Split(' ');
            }
            else
            {
                searchFilters = new string[1] { txtFilter.Text };
            }

            e.Accepted = true;
            foreach (var filter in searchFilters)
            {
                if (!string.IsNullOrWhiteSpace(filter) && !(e.Item as ProjectItemWrapper).Filename.ToLower().Contains(filter))
                {
                    e.Accepted = false;
                    break;
                }
            }
        }

        private void txtFilterChanged(object sender, TextChangedEventArgs e)
        {
            viewSource.View.Refresh();
        }

        private void txtFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                foreach (var item in lstFiles.SelectedItems)
                {
                    var w = (item as ProjectItemWrapper).ProjItem.Open();
                    w.Visible = true;
                }
                Close();
            }
            else if (lstFiles.Items.Count > 0)
            {
                if (e.Key == Key.Down)
                {
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        if (lstFiles.Items.Count > lstFiles.SelectedIndex)
                        {
                            lstFiles.SelectedItems.Add(lstFiles.Items[lstFiles.SelectedIndex + 1]);
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
                        if (lstFiles.SelectedIndex > 0)
                        {
                            lstFiles.SelectedItems.Add(lstFiles.Items[lstFiles.SelectedIndex - 1]);
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
    }
}
