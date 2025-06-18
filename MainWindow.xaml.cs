using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Collections.Generic;
using PlayerTracker.Models;
using OfficeOpenXml;
using System.IO;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.ComponentModel;
using LicenseContext = OfficeOpenXml.LicenseContext;
using System.Windows.Controls.Primitives;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;




/* NOTES for development:
 * Figure out how to better situation the datagrid and the form
 *  idea 1: make whole window scrollable + datagrid scrollable for easier navigation
 * 
 * 
 * 
 * 
 */



/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
namespace PlayerTracker
{
    public partial class MainWindow : Window
    {
        private Notifier notifier;
        private ObservableCollection<Player> players;
        public MainWindow()
        {
            InitializeComponent();
            LoadPlayersFromFile();
            playerDataGrid.ItemsSource = players;
            DataContext = this;
            notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: this,
                    corner: Corner.TopLeft,
                    offsetX: 10,
                    offsetY: 10);
                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(TimeSpan.FromSeconds(3), MaximumNotificationCount.FromCount(5));
                cfg.Dispatcher = Application.Current.Dispatcher;
            });
        }
        
        private void ShowNotification(string message)
        {
            notifier.ShowInformation(message);
        }

        private void LoadPlayersFromFile()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    var json = File.ReadAllText(SaveFilePath);
                    var loadedPlayers = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<Player>>(json);
                    players = loadedPlayers ?? new ObservableCollection<Player>();
                }
                else
                {
                    players = new ObservableCollection<Player>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading players: {ex.Message}");
                players = new ObservableCollection<Player>();
            }
        }

        private void AddPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtAge.Text, out int age) ||
               !int.TryParse(txtMinPotential.Text, out int minPotential) ||
               !int.TryParse(txtMaxPotential.Text, out int maxPotential) ||
               !int.TryParse(txtOverall.Text, out int overall))
            {
                ShowNotification("Please enter valid numeric values for age, overall, and potentials.");
                //MessageBox.Show("Please enter valid numeric values for age, overall, and potentials.");
                return;
            }
            if (chkUpdate.IsChecked == true && playerDataGrid.SelectedItem is Player selectedPlayer)
            {
                selectedPlayer.History.Insert(0, new PlayerStatVersion
                {
                    Timestamp = DateTime.Now,
                    Age = age,
                    MinPotential = minPotential,
                    MaxPotential = maxPotential,
                    Overall = overall
                });
                selectedPlayer.Name = txtName.Text;
                selectedPlayer.Position = txtPosition.Text;
                selectedPlayer.Nationality = txtNationality.Text;
                selectedPlayer.Promoted = chkPromoted.IsChecked ?? false;
                selectedPlayer.Released = chkReleased.IsChecked ?? false;

                playerDataGrid.Items.Refresh();
                ShowNotification($"Player '{selectedPlayer.Name}' updated successfully!");
                //MessageBox.Show($"Player '{selectedPlayer.Name}' updated successfully!");
            }
            else
            {
                var newPlayer = new Player()
                {
                    Name = txtName.Text,
                    Position = txtPosition.Text,
                    Nationality = string.IsNullOrWhiteSpace(txtNationality.Text) ? "Unknown" : txtNationality.Text,
                    Promoted = chkPromoted.IsChecked ?? false,
                    Released = chkReleased.IsChecked ?? false,
                    History = new List<PlayerStatVersion>
                {
                    new PlayerStatVersion
                    {
                        Timestamp = DateTime.Now,
                        Age = age,
                        MinPotential = minPotential,
                        MaxPotential = maxPotential,
                        Overall = overall
                    }
                }
                };

                players.Add(newPlayer);
                ShowNotification($"Player '{newPlayer.Name}' updated successfully!");
                //MessageBox.Show($"Player '{newPlayer.Name}' added successfully!");
            }
            ClearForm();
        }

        private void ClearForm()
        {
            txtName.Clear();
            txtPosition.Clear();
            txtNationality.Clear();
            txtAge.Clear();
            txtMinPotential.Clear();
            txtMaxPotential.Clear();
            txtOverall.Clear();
            chkPromoted.IsChecked = false;
            chkReleased.IsChecked = false;
            //Took out Update checkbox, might add in later if necessary
            //chkUpdate.IsChecked = false;
        }

        private void btnToggleForm_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerForm.Visibility == Visibility.Visible)
            {
                PlayerForm.Visibility = Visibility.Collapsed;
                btnToggleForm.Content = "Add New Player";
            }
            else
            {
                PlayerForm.Visibility = Visibility.Visible;
                btnToggleForm.Content = "Hide Form";
            }
        }

        private void playerDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (playerDataGrid.SelectedItem is Player selectedPlayer && selectedPlayer.History.Count > 0)
            {
                txtName.Text = selectedPlayer.Name;
                txtPosition.Text = selectedPlayer.Position;
                txtNationality.Text = selectedPlayer.Nationality;
                txtAge.Text = selectedPlayer.History[0].Age.ToString();
                txtOverall.Text = selectedPlayer.History[0].Overall.ToString();
                txtMinPotential.Text = selectedPlayer.History[0].MinPotential.ToString();
                txtMaxPotential.Text = selectedPlayer.History[0].MaxPotential.ToString();
                chkPromoted.IsChecked = selectedPlayer.Promoted;
                chkReleased.IsChecked = selectedPlayer.Released;
                chkUpdate.IsChecked = true; // Enable update checkbox when a player is selected
            }
        }

        private void ImportFromExcel_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Select an Excel file to import players"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(fileInfo))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row < rowCount; row++)
                    {
                        if (!int.TryParse(worksheet.Cells[row, 4].Text, out int age) ||
                            !int.TryParse(worksheet.Cells[row, 5].Text, out int overall) ||
                            !int.TryParse(worksheet.Cells[row, 6].Text, out int minPotential) ||
                            !int.TryParse(worksheet.Cells[row, 7].Text, out int maxPotential))
                            continue;


                        var player = new Player
                        {
                            Name = worksheet.Cells[row, 1].Text,
                            Position = worksheet.Cells[row, 2].Text,
                            Nationality = worksheet.Cells[row, 3].Text,
                            Promoted = bool.TryParse(worksheet.Cells[row, 8].Text, out bool p) && p,
                            Released = bool.TryParse(worksheet.Cells[row, 9].Text, out bool r) && r,
                            History = new List<PlayerStatVersion>
                            {
                                new PlayerStatVersion
                                {
                                    Timestamp = DateTime.Now,
                                    Age = age,
                                    Overall = overall,
                                    MinPotential = minPotential,
                                    MaxPotential = maxPotential
                                }
                            }
                        };
                        players.Add(player);
                    }
                    playerDataGrid.Items.Refresh();
                    ShowNotification($"Players imported successfully!");
                    //MessageBox.Show("Players imported successfully");
                }


            }
        }

        private readonly string SaveFilePath = "players.json";

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            SavePlayersToFile();
        }
        private void SavePlayersToFile()
        {
            try
            {
                var json = JsonConvert.SerializeObject(players);
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception ex)
            {

                MessageBox.Show($"Error saving players: {ex.Message}");
            }
        }

        /*
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var positions = players.Select(p => p.Position).Distinct().ToList();
            positions.Insert(0, "All");
            //PositionFilter.ItemsSource = positions;
            //PositionFilter.SelectedIndex = 0; // Default to "All"
            //SortSelector.SelectedIndex = 0;
            //ApplyFiltersAndSorting();

        }*/

        private void FilterToggles_Changed(object sender, RoutedEventArgs e)
        {
            if (playerDataGrid == null || players == null) return;

            bool filteredPromoted = TogglePromoted.IsChecked == true;
            bool filteredYouth = ToggleYouth.IsChecked == true;
            bool filteredReleased = ToggleReleased.IsChecked == true;

            var filtered = players.Where(p =>
                (!filteredPromoted && !filteredReleased && !filteredYouth) ||
                (filteredPromoted && p.Promoted) ||
                (filteredYouth && !p.Promoted && !p.Released) ||
                (filteredReleased && p.Released)).ToList();

            playerDataGrid.ItemsSource = null;
            playerDataGrid.ItemsSource = filtered;
        }

        /* Commenting out all sorting
         private void SortSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSorting();
        }
        */

        /* Removing ApplyFiltersAndSorting method for now
         private void ApplyFiltersAndSorting()
         {
             if (players == null) return;
             IEnumerable<Player> filtered = players;

             if (PositionFilter.SelectedItem is string selectedPosition && selectedPosition != "All")
             {
                 filtered = filtered.Where(p => p.Position == selectedPosition);
             }
             // Non necessary sorting code commented out for now
             var selectedSort = (SortSelector.SelectedItem as ComboBoxItem)?.Content as string;
             switch (selectedSort)
             {
                 case "Name":
                     filtered = filtered.OrderBy(p => p.Name);
                     break;
                 case "Overall":
                     filtered = filtered.OrderBy(p => p.History.FirstOrDefault()?.Overall ?? 0);
                     break;
                 case "Age":
                     filtered = filtered.OrderBy(p => p.History.FirstOrDefault()?.Age ?? 0);
                     break;
                 case "Min Potential":
                     filtered = filtered.OrderBy(p => p.History.FirstOrDefault()?.MinPotential ?? 0);
                     break;
                 case "Max Potential":
                     filtered = filtered.OrderBy(p => p.History.FirstOrDefault()?.MaxPotential ?? 0);
                     break;
             } 
             //playerDataGrid.ItemsSource = null;
             playerDataGrid.ItemsSource = filtered.ToList();
         }*/

        /*private void RefreshUI()
        {
            var positions = players.Select(p=> p.Position).Distinct().OrderBy(p => p).ToList();
            positions.Insert(0, "All");
            PositionFilter.ItemsSource = positions;
            PositionFilter.SelectedIndex = 0; // Default to "All"

            //SortSelector.SelectedIndex = 0; // Default to "Name"
            ApplyFiltersAndSorting();
        }*/
    }
}
