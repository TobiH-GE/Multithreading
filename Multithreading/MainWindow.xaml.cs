using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Multithreading
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Progress<int> progressCom;
        Progress<int> progressCom1;
        Progress<int> progressCom2;
        Progress<int> progressCom3;
        Progress<int> progressCom4;

        Task<int> worker;
        Task<int>[] workers;
        CancellationTokenSource cancelTokenSource;

        int[] niceArray = new int[100000000];
        Random rnd = new Random();
        public MainWindow()
        {
            InitializeComponent();
            progressCom = new Progress<int>((x) => refreshProgressBar(x, pbBar));
            progressCom1 = new Progress<int>((x) => refreshProgressBar(x, pbBar1));
            progressCom2 = new Progress<int>((x) => refreshProgressBar(x, pbBar2));
            progressCom3 = new Progress<int>((x) => refreshProgressBar(x, pbBar3));
            progressCom4 = new Progress<int>((x) => refreshProgressBar(x, pbBar4));

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
        private async void StartCalcArraySegment_Click(object sender, RoutedEventArgs e)
        {
            tbOut.Text = "calculating segments...";
            tbOut.Background = Brushes.Red;

            ArraySegment<int> segement1Array = new ArraySegment<int>(niceArray,        0, 25000000);
            ArraySegment<int> segement2Array = new ArraySegment<int>(niceArray, 25000000, 25000000);
            ArraySegment<int> segement3Array = new ArraySegment<int>(niceArray, 50000000, 25000000);
            ArraySegment<int> segement4Array = new ArraySegment<int>(niceArray, 75000000, 25000000);

            cancelTokenSource = new CancellationTokenSource();
            workers = new Task<int>[4];
            workers[0] = new Task<int>(() => Calc(segement1Array, progressCom1, cancelTokenSource.Token));
            workers[1] = new Task<int>(() => Calc(segement2Array, progressCom2, cancelTokenSource.Token));
            workers[2] = new Task<int>(() => Calc(segement3Array, progressCom3, cancelTokenSource.Token));
            workers[3] = new Task<int>(() => Calc(segement4Array, progressCom4, cancelTokenSource.Token));
            workers[0].Start();
            workers[1].Start();
            workers[2].Start();
            workers[3].Start();
            await Task.WhenAll(workers);
            tbOut.Background = Brushes.Green;
            tbOut.Text = (workers[0].Result + workers[1].Result + workers[2].Result + workers[3].Result).ToString();
        }

        public int Calc(int[] array, IProgress<int> progress, CancellationToken CancelToken)
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

                    if (CancelToken.IsCancellationRequested)
                    {
                        return 0;
                    }
                }
            }
            return result;
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

                    if (CancelToken.IsCancellationRequested)
                    {
                        return 0;
                    }
                }
            }
            return result;
        }

        public int CreateRandomArray(ref int[] array, IProgress<int> progress, CancellationToken CancelToken)
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

                    if (CancelToken.IsCancellationRequested)
                    {
                        return 0;
                    }
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
