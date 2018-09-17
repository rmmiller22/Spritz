﻿using CMD;
using Nett;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ToolWrapperLayer;
using WorkflowLayer;

namespace SpritzGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<RNASeqFastqDataGrid> rnaSeqFastqCollection = new ObservableCollection<RNASeqFastqDataGrid>();
        private ObservableCollection<InRunTask> dynamicTasksObservableCollection = new ObservableCollection<InRunTask>();
        private readonly ObservableCollection<PreRunTask> staticTasksObservableCollection = new ObservableCollection<PreRunTask>();
        private readonly ObservableCollection<SRADataGrid> sraCollection = new ObservableCollection<SRADataGrid>();

        public MainWindow()
        {
            InitializeComponent();

            dataGridRnaSeqFastq.DataContext = rnaSeqFastqCollection;
            workflowTreeView.DataContext = staticTasksObservableCollection;
            LbxSRAs.ItemsSource = sraCollection;
            if (!InstallationDialogAndCheck())
                Close();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                foreach (var draggedFilePath in files)
                {
                    if (Directory.Exists(draggedFilePath))
                    {
                        foreach (string file in Directory.EnumerateFiles(draggedFilePath, "*.*", SearchOption.AllDirectories))
                        {
                            AddAFile(file);
                        }
                    }
                    else
                    {
                        AddAFile(draggedFilePath);
                    }
                    dataGridRnaSeqFastq.Items.Refresh();
                }
            }
            UpdateOutputFolderTextbox();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void MenuItem_Wiki_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://github.com/smith-chem-wisc/Spritz/wiki");
        }

        private void MenuItem_Contact_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://github.com/smith-chem-wisc/Spritz");
        }

        private void RunWorkflowButton_Click(object sender, RoutedEventArgs e)
        {
            dynamicTasksObservableCollection = new ObservableCollection<InRunTask>();
            for (int i = 0; i < staticTasksObservableCollection.Count; i++)
            {
                dynamicTasksObservableCollection.Add(new InRunTask("Workflow" + (i + 1) + "-" + staticTasksObservableCollection[i].options.Command.ToString(), staticTasksObservableCollection[i].options));
            }
            workflowTreeView.DataContext = dynamicTasksObservableCollection;
            EverythingRunnerEngine a = new EverythingRunnerEngine(dynamicTasksObservableCollection.Select(b => new Tuple<string, Options>(b.DisplayName, b.options)).ToList(), OutputFolderTextBox.Text);
            var t = new Task(a.Run);
            t.Start();
            RunWorkflowButton.IsEnabled = false;
        }

        private void BtnAddRnaSeqFastq_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openPicker = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "FASTQ Files|*.fastq",
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = true
            };
            if (openPicker.ShowDialog() == true)
            {
                foreach (var filepath in openPicker.FileNames)
                {
                    AddAFile(filepath);
                }
            }
            dataGridRnaSeqFastq.Items.Refresh();
        }

        private void BtnClearRnaSeqFastq_Click(object sender, RoutedEventArgs e)
        {
            rnaSeqFastqCollection.Clear();
        }

        private void LoadTaskButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openPicker = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "TOML files(*.toml)|*.toml",
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = true
            };
            if (openPicker.ShowDialog() == true)
            {
                foreach (var tomlFromSelected in openPicker.FileNames)
                {
                    AddAFile(tomlFromSelected);
                }
            }
            UpdateTaskGuiStuff();
        }

        private void ClearTasksButton_Click(object sender, RoutedEventArgs e)
        {
            staticTasksObservableCollection.Clear();
            UpdateTaskGuiStuff();
        }

        private void RemoveLastTaskButton_Click(object sender, RoutedEventArgs e)
        {
            staticTasksObservableCollection.RemoveAt(staticTasksObservableCollection.Count - 1);
            UpdateTaskGuiStuff();
        }

        private void ResetTasksButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void AddNewRnaSeqFastq(object sender, StringListEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => AddNewRnaSeqFastq(sender, e)));
            }
            else
            {
                foreach (var uu in rnaSeqFastqCollection)
                {
                    uu.Use = false;
                }
                foreach (var newRnaSeqFastqData in e.StringList)
                {
                    rnaSeqFastqCollection.Add(new RNASeqFastqDataGrid(newRnaSeqFastqData));
                }
                UpdateOutputFolderTextbox();
            }
        }

        private void MenuItem_Setup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new InstallWindow();
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void BtnAddSRA_Click(object sender, RoutedEventArgs e)
        {
            if (TbxSRA.Text.Contains("SR"))
            {
                //TO DO: If exist, then pop box.
                if (true)
                {
                    SRADataGrid sraDataGrid = new SRADataGrid(TbxSRA.Text);
                    sraCollection.Add(sraDataGrid);
                }
            }
        }

        private void BtnWorkFlow_Click(object sender, RoutedEventArgs e)
        {
            if (sraCollection.Count == 0 && rnaSeqFastqCollection.Count == 0)
            {
                MessageBox.Show("Please add FASTQ files prior to choosing the workflow.");
                return;
            }
            var dialog = new WorkFlowWindow(OutputFolderTextBox.Text);
            if (dialog.ShowDialog() == true)
            {
                AddTaskToCollection(dialog.Options);
                UpdateTaskGuiStuff();
            }
        }

        private void BtnSaveRnaSeqFastqSet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WriteExperDesignToTsv(OutputFolderTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save experimental design!\n\n" + ex.Message);
                return;
            }
        }

        private void UpdateTaskGuiStuff()
        {
            if (staticTasksObservableCollection.Count == 0)
            {
                RunWorkflowButton.IsEnabled = false;
                RemoveLastTaskButton.IsEnabled = false;
                ClearTasksButton.IsEnabled = false;
                ResetTasksButton.IsEnabled = false;
            }
            else
            {
                RunWorkflowButton.IsEnabled = true;
                RemoveLastTaskButton.IsEnabled = true;
                ClearTasksButton.IsEnabled = true;
            }
        }

        private void AddTaskToCollection(Options ye)
        {
            PreRunTask te = new PreRunTask(ye);
            staticTasksObservableCollection.Add(te);
            staticTasksObservableCollection.Last().DisplayName = "Task" + (staticTasksObservableCollection.IndexOf(te) + 1);
        }

        private void UpdateOutputFolderTextbox()
        {
            if (rnaSeqFastqCollection.Any())
            {
                var MatchingChars =
                    from len in Enumerable.Range(0, rnaSeqFastqCollection.Select(b => b.FilePath).Min(s => s.Length)).Reverse()
                    let possibleMatch = rnaSeqFastqCollection.Select(b => b.FilePath).First().Substring(0, len)
                    where rnaSeqFastqCollection.Select(b => b.FilePath).All(f => f.StartsWith(possibleMatch, StringComparison.Ordinal))
                    select possibleMatch;

                OutputFolderTextBox.Text = Path.Combine(Path.GetDirectoryName(MatchingChars.First()));
            }
            else
            {
                OutputFolderTextBox.Clear();
            }
        }

        private void AddAFile(string filepath)
        {
            var theExtension = Path.GetExtension(filepath).ToLowerInvariant();
            switch (theExtension)
            {
                case ".fastq":
                case ".fastq.gz":
                    RNASeqFastqDataGrid rnaSeqFastq = new RNASeqFastqDataGrid(filepath);
                    rnaSeqFastqCollection.Add(rnaSeqFastq);
                    UpdateOutputFolderTextbox();
                    break;

                case ".toml":
                    TomlTable tomlFile = null;
                    try
                    {
                        tomlFile = Toml.ReadFile(filepath);
                    }
                    catch (Exception)
                    {
                        break;
                    }
                    var ye1 = Toml.ReadFile<Options>(filepath);
                    AddTaskToCollection(ye1);
                    break;
            }
        }

        private bool InstallationDialogAndCheck()
        {
            var exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            if (!WrapperUtility.CheckToolSetup(exePath))
            {
                try
                {
                    var dialog = new InstallWindow();
                    dialog.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            return WrapperUtility.CheckBashSetup() && WrapperUtility.CheckToolSetup(Environment.CurrentDirectory);
        }

        private void WriteExperDesignToTsv(string filePath)
        {
            using (StreamWriter output = new StreamWriter(filePath))
            {
                output.WriteLine("FileName\tCondition\tBiorep\tFraction\tTechrep");
                foreach (var aFastq in rnaSeqFastqCollection)
                {
                    output.WriteLine(aFastq.FileName +
                        "\t" + aFastq.Experiment +
                        "\t" + aFastq.MatePair);
                }
            }
        }

        private void workflowTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var a = sender as TreeView;
            if (a.SelectedItem is PreRunTask preRunTask)
            {
                var workflowDialog = new WorkFlowWindow(preRunTask.options);
                workflowDialog.ShowDialog();
                workflowTreeView.Items.Refresh();
            }
        }
    }
}