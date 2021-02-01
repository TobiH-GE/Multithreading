using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CalculationWithOneThread
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Progress<int> progressCom;

        Task<int> worker;
        CancellationTokenSource cancelTokenSource;

        int[] niceArray = new int[100000000];
        Random rnd = new Random();
        public MainWindow()
        {
            InitializeComponent();

            progressCom = new Progress<int>((x) => refreshProgressBar(x, pbBar));

            StartGeneratingNumbers();
        }
        private async void StartGeneratingNumbers()
        {
            tbOut.Text = "generating random numbers ...";
            tbOut.Background = Brushes.Red;
            cancelTokenSource = new CancellationTokenSource();
            worker = new Task<int>(() => CreateRandomArray(ref niceArray, progressCom, cancelTokenSource.Token));
            worker.Start();
            await Task.WhenAll(worker);
            tbOut.Background = Brushes.Green;
            tbOut.Text = worker.Result.ToString();
        }

        private void refreshProgressBar(int reportedProgress, ProgressBar pb)
        {
            pb.Value = reportedProgress;
        }

        private async void StartCalc_Click(object sender, RoutedEventArgs e)
        {
            tbOut.Text = "calculating ...";
            tbOut.Background = Brushes.Red;
            cancelTokenSource = new CancellationTokenSource();
            worker = new Task<int>(() => Calc(niceArray, progressCom, cancelTokenSource.Token));
            worker.Start();
            await Task.WhenAll(worker);
            tbOut.Background = Brushes.Green;
            tbOut.Text = worker.Result.ToString();
        }
        public int Calc(int[] array, IProgress<int> progress, CancellationToken CancelToken)
        {
            int result = 0;
            int counter = 0;
            int divider = array.Length / 1000;

            while (counter < array.Length)
            {
                result += array[counter];
                counter++;
                if (counter % divider == 0)
                {
                    progress.Report(counter / divider);
                    if (CancelToken.IsCancellationRequested) return 0;
                }
            }
            return result;
        }
        public int CreateRandomArray(ref int[] array, IProgress<int> progress, CancellationToken CancelToken)
        {
            ;
            int counter = 0;
            int divider = array.Length / 1000;

            while (counter < array.Length)
            {
                array[counter] = rnd.Next(100);
                counter++;
                if (counter % divider == 0)
                {
                    progress.Report(counter / divider);
                    if (CancelToken.IsCancellationRequested) return 0;
                }
            }
            return 1;
        }
        private void StopCalc_Click(object sender, RoutedEventArgs e)
        {
            cancelTokenSource.Cancel();
        }
    }
}
