using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CalculationWithMultiThreads
{
    public partial class MainWindow : Window
    {
        const int threads = 4;

        Progress<int>[] progressComs = new Progress<int>[threads];
        List<ProgressBar> listWithPBars = new List<ProgressBar>();

        Task<int>[] workers;
        CancellationTokenSource cancelTokenSource;

        int[] niceArray = new int[100000015];
        Random rnd = new Random();
        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < threads; i++)
            {
                listWithPBars.Add(new ProgressBar() { Width = 300, Height = 30, Maximum = 1000 });
                spBars.Children.Add(listWithPBars[listWithPBars.Count - 1]);
                int j = listWithPBars.Count - 1;
                progressComs[i] = new Progress<int>((x) => refreshProgressBar(x, listWithPBars[j]));
            }
            StartGeneratingNumbers();
        }
        private async void StartGeneratingNumbers()
        {
            tbOut.Text = "generating random numbers ...";
            tbOut.Background = Brushes.Red;
            cancelTokenSource = new CancellationTokenSource();
            workers = new Task<int>[1];
            workers[0] = new Task<int>(() => CreateRandomArray(ref niceArray, progressComs[0], cancelTokenSource.Token));
            workers[0].Start();
            await Task.WhenAll(workers[0]);
            tbOut.Background = Brushes.Green;
            tbOut.Text = workers[0].Result.ToString();
        }
        private void refreshProgressBar(int reportedProgress, ProgressBar pb)
        {
            pb.Value = reportedProgress;
        }
        private async void StartCalcMultiThread_Click(object sender, RoutedEventArgs e)
        {
            tbOut.Text = "calculating segments...";
            tbOut.Background = Brushes.Red;

            ArraySegment<int>[] segments = new ArraySegment<int>[threads];

            cancelTokenSource = new CancellationTokenSource();
            workers = new Task<int>[threads];

            for (int i = 0; i < threads; i++)
            {
                int j = i;
                if (niceArray.Length % threads != 0 && i == threads - 1)
                    segments[i] = new ArraySegment<int>(niceArray, (niceArray.Length / threads) * i, niceArray.Length / threads + niceArray.Length % threads);
                else
                    segments[i] = new ArraySegment<int>(niceArray, (niceArray.Length / threads) * i, niceArray.Length / threads);
                workers[i] = new Task<int>(() => Calc(segments[j], progressComs[j], cancelTokenSource.Token));
                workers[i].Start();
            }
            await Task.WhenAll(workers);
            tbOut.Background = Brushes.Green;

            int result = 0;
            for (int i = 0; i < threads; i++)
            {
                result += workers[i].Result;
            }

            tbOut.Text = result.ToString();
        }
        private async void StartCalcSingleThread_Click(object sender, RoutedEventArgs e)
        {
            tbOut.Text = "calculating 1 segment...";
            tbOut.Background = Brushes.Red;

            ArraySegment<int>[] segments = new ArraySegment<int>[1];

            cancelTokenSource = new CancellationTokenSource();
            workers = new Task<int>[1];

            segments[0] = new ArraySegment<int>(niceArray, 0, niceArray.Length);
            workers[0] = new Task<int>(() => Calc(segments[0], progressComs[0], cancelTokenSource.Token));
            workers[0].Start();

            await Task.WhenAll(workers);
            tbOut.Background = Brushes.Green;
            tbOut.Text = workers[0].Result.ToString();
        }
        public int Calc(ArraySegment<int> segementarray, IProgress<int> progress, CancellationToken CancelToken)
        {
            int result = 0;
            int counter = 0;
            int divider = segementarray.Count / 1000;

            while (counter < segementarray.Count)
            {
                result += segementarray[counter];
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
