using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Multithreading
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Progress<int> progressCom;
        Task<int> worker;
        int[] niceArray = new int[100000000];
        Random rnd = new Random();
        public MainWindow()
        {
            InitializeComponent();
            progressCom = new Progress<int>(refreshProgressBar);
            StartGeneratingNumbers();
        }
        private async void StartGeneratingNumbers()
        {
            tbOut.Text = "generating random numbers ...";
            tbOut.Background = Brushes.Red;
            worker = new Task<int>(() => CreateRandomArray(ref niceArray, progressCom));
            worker.Start();
            await Task.WhenAll(worker);
            tbOut.Background = Brushes.Green;
            tbOut.Text = worker.Result.ToString();
        }

        private void refreshProgressBar(int reportedProgress)
        {
            pbBar.Value = reportedProgress;
        }

        private async void StartCalc_Click(object sender, RoutedEventArgs e)
        {
            tbOut.Text = "calculating ...";
            tbOut.Background = Brushes.Red;
            worker = new Task<int>(() => Calc(niceArray, progressCom));
            worker.Start();
            await Task.WhenAll(worker);
            tbOut.Background = Brushes.Green;
            tbOut.Text = worker.Result.ToString();
        }

        public int Calc(int[] array, IProgress<int> progress)
        {
            
            int result = 0;
            int counter = 0;
            int divider = array.Length / 1000;

            while (counter < array.Length)
            {
                result+= array[counter];
                counter++;
                if (counter % divider == 0)
                {
                    progress.Report(counter / divider);
                }
            }
            return result;
        }

        public int CreateRandomArray(ref int[] array, IProgress<int> progress)
        {;
            int counter = 0;
            int divider = array.Length / 1000;

            while (counter < array.Length)
            {
                array[counter] = rnd.Next(100);
                counter++;
                if (counter % divider == 0)
                {
                    progress.Report(counter / divider);
                }
            }
            return 1;
        }
    }
}
