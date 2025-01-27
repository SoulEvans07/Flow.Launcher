using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Flow.Launcher.Plugin.Program.Views.Models;
using Flow.Launcher.Plugin.Program.Views.Commands;
using Flow.Launcher.Plugin.Program.Programs;
using System.ComponentModel;
using System.Windows.Data;

namespace Flow.Launcher.Plugin.Program.Views
{
    /// <summary>
    /// Interaction logic for ProgramSetting.xaml
    /// </summary>
    public partial class ProgramSetting : UserControl
    {
        private PluginInitContext context;
        private Settings _settings;
        private GridViewColumnHeader _lastHeaderClicked;
        private ListSortDirection _lastDirection;

        // We do not save all program sources to settings, so using
        // this as temporary holder for displaying all loaded programs sources. 
        internal static List<ProgramSource> ProgramSettingDisplayList { get; set; }

        public bool EnableDescription
        {
            get => _settings.EnableDescription;
            set
            {
                Main.ResetCache();
                _settings.EnableDescription = value;
            }
        }
        public bool HideAppsPath
        {
            get => _settings.HideAppsPath;
            set
            {
                Main.ResetCache();
                _settings.HideAppsPath = value;
            }
        }

        public bool EnableRegistrySource
        {
            get => _settings.EnableRegistrySource;
            set
            {
                _settings.EnableRegistrySource = value;
                ReIndexing();
            }
        }

        public bool EnableStartMenuSource
        {
            get => _settings.EnableStartMenuSource;
            set
            {
                _settings.EnableStartMenuSource = value;
                ReIndexing();
            }
        }

        public string CustomizedExplorerPath
        {
            get => _settings.CustomizedExplorer;
            set => _settings.CustomizedExplorer = value;
        }

        public string CustomizedExplorerArg
        {
            get => _settings.CustomizedArgs;
            set => _settings.CustomizedArgs = value;
        }

        public ProgramSetting(PluginInitContext context, Settings settings, Win32[] win32s, UWP.Application[] uwps)
        {
            this.context = context;
            _settings = settings;
            Loaded += Setting_Loaded;
            InitializeComponent();
        }

        private void Setting_Loaded(object sender, RoutedEventArgs e)
        {
            ProgramSettingDisplayList = _settings.ProgramSources.LoadProgramSources();
            programSourceView.ItemsSource = ProgramSettingDisplayList;

            ViewRefresh();
        }

        private void ViewRefresh()
        {
            if (programSourceView.Items.Count == 0
                && btnProgramSourceStatus.Visibility == Visibility.Visible
                && btnEditProgramSource.Visibility == Visibility.Visible)
            {
                btnProgramSourceStatus.Visibility = Visibility.Hidden;
                btnEditProgramSource.Visibility = Visibility.Hidden;
            }

            if (programSourceView.Items.Count > 0
                && btnProgramSourceStatus.Visibility == Visibility.Hidden
                && btnEditProgramSource.Visibility == Visibility.Hidden)
            {
                btnProgramSourceStatus.Visibility = Visibility.Visible;
                btnEditProgramSource.Visibility = Visibility.Visible;
            }

            programSourceView.Items.Refresh();
        }

        private async void ReIndexing()
        {
            ViewRefresh();
            indexingPanel.Visibility = Visibility.Visible;
            await Main.IndexProgramsAsync();
            indexingPanel.Visibility = Visibility.Hidden;
        }

        private void btnAddProgramSource_OnClick(object sender, RoutedEventArgs e)
        {
            var add = new AddProgramSource(context, _settings);
            if (add.ShowDialog() ?? false)
            {
                ReIndexing();
            }

            ViewRefresh();
        }

        private void DeleteProgramSources(List<ProgramSource> itemsToDelete)
        {
            itemsToDelete.ForEach(t1 => _settings.ProgramSources
                                                    .Remove(_settings.ProgramSources
                                                                        .Where(x => x.UniqueIdentifier == t1.UniqueIdentifier)
                                                                        .FirstOrDefault()));
            itemsToDelete.ForEach(x => ProgramSettingDisplayList.Remove(x));

            ReIndexing();
        }

        private void btnEditProgramSource_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedProgramSource = programSourceView.SelectedItem as Settings.ProgramSource;
            if (selectedProgramSource != null)
            {
                var add = new AddProgramSource(selectedProgramSource, _settings);
                if (add.ShowDialog() ?? false)
                {
                    ReIndexing();
                }
            }
            else
            {
                string msg = context.API.GetTranslation("flowlauncher_plugin_program_pls_select_program_source");
                MessageBox.Show(msg);
            }
        }

        private void btnReindex_Click(object sender, RoutedEventArgs e)
        {
            ReIndexing();
        }

        private void BtnProgramSuffixes_OnClick(object sender, RoutedEventArgs e)
        {
            var p = new ProgramSuffixes(context, _settings);
            if (p.ShowDialog() ?? false)
            {
                ReIndexing();
            }
        }

        private void programSourceView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void programSourceView_Drop(object sender, DragEventArgs e)
        {
            var directories = (string[])e.Data.GetData(DataFormats.FileDrop);

            var directoriesToAdd = new List<ProgramSource>();

            if (directories != null && directories.Length > 0)
            {
                foreach (string directory in directories)
                {
                    if (Directory.Exists(directory) && !ProgramSettingDisplayList.Any(x => x.UniqueIdentifier == directory))
                    {
                        var source = new ProgramSource
                        {
                            Location = directory,
                            UniqueIdentifier = directory
                        };

                        directoriesToAdd.Add(source);
                    }
                }

                if (directoriesToAdd.Count() > 0)
                {
                    directoriesToAdd.ForEach(x => _settings.ProgramSources.Add(x));
                    directoriesToAdd.ForEach(x => ProgramSettingDisplayList.Add(x));

                    ViewRefresh();
                    ReIndexing();
                }
            }
        }

        private void btnLoadAllProgramSource_OnClick(object sender, RoutedEventArgs e)
        {
            ProgramSettingDisplayList.LoadAllApplications();

            ViewRefresh();
        }

        private void btnProgramSourceStatus_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedItems = programSourceView
                                .SelectedItems.Cast<ProgramSource>()
                                .ToList();

            if (selectedItems.Count() == 0)
            {
                string msg = context.API.GetTranslation("flowlauncher_plugin_program_pls_select_program_source");
                MessageBox.Show(msg);
                return;
            }

            if (selectedItems
                .Where(t1 => !_settings
                                .ProgramSources
                                .Any(x => t1.UniqueIdentifier == x.UniqueIdentifier))
                .Count() == 0)
            {
                var msg = string.Format(context.API.GetTranslation("flowlauncher_plugin_program_delete_program_source"));

                if (MessageBox.Show(msg, string.Empty, MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    return;
                }

                DeleteProgramSources(selectedItems);
            }
            else if (IsSelectedRowStatusEnabledMoreOrEqualThanDisabled(selectedItems))
            {
                ProgramSettingDisplayList.SetProgramSourcesStatus(selectedItems, false);

                ProgramSettingDisplayList.StoreDisabledInSettings();
            }
            else
            {
                ProgramSettingDisplayList.SetProgramSourcesStatus(selectedItems, true);

                ProgramSettingDisplayList.RemoveDisabledFromSettings();
            }

            if (selectedItems.IsReindexRequired())
                ReIndexing();

            programSourceView.SelectedItems.Clear();

            ViewRefresh();
        }

        private void ProgramSourceView_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            programSourceView.SelectedItems.Clear();
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    Sort(sortBy, direction);

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            var dataView = CollectionViewSource.GetDefaultView(programSourceView.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private bool IsSelectedRowStatusEnabledMoreOrEqualThanDisabled(List<ProgramSource> selectedItems)
        {
            return selectedItems.Where(x => x.Enabled).Count() >= selectedItems.Where(x => !x.Enabled).Count();
        }

        private void Row_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedItems = programSourceView
                .SelectedItems.Cast<ProgramSource>()
                .ToList();

            if (selectedItems
                .Where(t1 => !_settings
                                .ProgramSources
                                .Any(x => t1.UniqueIdentifier == x.UniqueIdentifier))
                .Count() == 0)
            {
                btnProgramSourceStatus.Content = context.API.GetTranslation("flowlauncher_plugin_program_delete");
                return;
            }

            if (IsSelectedRowStatusEnabledMoreOrEqualThanDisabled(selectedItems))
            {
                btnProgramSourceStatus.Content = "Disable";
            }
            else
            {
                btnProgramSourceStatus.Content = "Enable";
            }
        }
    }
}