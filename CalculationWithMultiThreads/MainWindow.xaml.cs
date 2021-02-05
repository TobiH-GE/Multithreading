using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CalculationWithMultiThreads
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        CancellationTokenSource cancelTokenSource;

        private readonly object IndexLock = new object();

        int _threads = 1;
        Int64 _numbers = 100000015;
        Int64[] niceArray;
        List<ProgressBar> listWithPBars;
        Progress<int>[] progressComs;
        List<Task<Int64>> workers = new List<Task<Int64>>();
        List<ArraySegment<Int64>> ListWithSegments = new List<ArraySegment<Int64>>();
        int segmentSize = 1000000;
        Random rnd = new Random();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            StartGeneratingNumbers();
        }
        public int Threads
        {
            get { return _threads; }
            set
            {
                if (_threads != value && value != -1)
                {
                    _threads = value;
                    listWithPBars = new List<ProgressBar>();

                    for (int i = 0; i < _threads; i++)
                    {
                        listWithPBars.Add(new ProgressBar() { Style = FindResource("ProgressBarStyle") as Style });
                    }
                    CreateAndShowPBars(listWithPBars);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Threads))); ;
                }
            }
        }
        public Int64 Numbers
        {
            get { return _numbers; }
            set
            {
                if (_numbers != value && value != -1)
                {
                    _numbers = value;
                    StartGeneratingNumbers();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Numbers)));
                }
            }
        }
        private void CreateAndShowPBars(List<ProgressBar> listWithPBars) // TODO: return Progress<int>[] ?
        {
            spBars.Children.Clear();
            progressComs = new Progress<int>[listWithPBars.Count];
            for (int i = 0; i < listWithPBars.Count; i++)
            {
                spBars.Children.Add(listWithPBars[i]);
                int j = i;
                progressComs[i] = new Progress<int>((x) => refreshProgressBar(x, listWithPBars[j]));
            }
        }
        private async void StartGeneratingNumbers()
        {
            niceArray = new Int64[_numbers];

            CreateAndShowPBars(new List<ProgressBar>() { new ProgressBar() { Style = FindResource("ProgressBarStyle") as Style } });

            tbOut.Text = "generating random numbers ..."; tbOut.Background = Brushes.Red;

            cancelTokenSource = new CancellationTokenSource();
            workers.Add(Task<Int64>.Run(() => CreateRandomArray(ref niceArray, progressComs[0], cancelTokenSource.Token)));
            await Task.WhenAll(workers[0]);

            tbOut.Text = workers[0].Result.ToString(); tbOut.Background = Brushes.Green;

            Threads = 4;
        }
        private void refreshProgressBar(int reportedProgress, ProgressBar pb)
        {
            pb.Value = reportedProgress;
        }
        private async void StartCalcMultiThreadSeg_Click(object sender, RoutedEventArgs e)
        {
            tbOut.Text = "calculating segments queued..."; tbOut.Background = Brushes.Red;
            ListWithSegments.Clear();
            for (int i = 0; i < niceArray.Length / segmentSize; i++)
                ListWithSegments.Add(new ArraySegment<Int64>(niceArray, i * segmentSize, segmentSize));
            if (niceArray.Length % segmentSize > 0)
                ListWithSegments.Add(new ArraySegment<Int64>(niceArray, niceArray.Length - (niceArray.Length % segmentSize), niceArray.Length % segmentSize));

            cancelTokenSource = new CancellationTokenSource();
            workers.Clear();

            Int64 result = 0;
            int counter = 0;

            while (counter < ListWithSegments.Count || workers.Count > 0) // TODO: optimize code
            {
                while (workers.Count <= _threads && counter < ListWithSegments.Count) //  - workers.Count
                {
                    int pID = counter % _threads;
                    int j = counter++;
                    workers.Add(Task<Int64>.Run(() => Calc(ListWithSegments[j], progressComs[pID], cancelTokenSource.Token)));
                }
                Task<Int64> finishedTask = await Task.WhenAny(workers);
                result += finishedTask.Result;
                workers.Remove(finishedTask);
            }
            tbOut.Text = "sum: " + result.ToString() + " / avg: " + (result / Numbers).ToString(); tbOut.Background = Brushes.Green;
        }
        
        private async void StartCalcMultiThread_Click(object sender, RoutedEventArgs e)
        {
            tbOut.Text = "calculating segments..."; tbOut.Background = Brushes.Red;

            ArraySegment<Int64>[] segments = new ArraySegment<Int64>[_threads];

            cancelTokenSource = new CancellationTokenSource();
            workers.Clear();

            for (int i = 0; i < _threads; i++)
            {
                int j = i;
                if (niceArray.Length % _threads != 0 && i == _threads - 1)
                    segments[i] = new ArraySegment<Int64>(niceArray, (niceArray.Length / _threads) * i, niceArray.Length / _threads + niceArray.Length % _threads);
                else
                    segments[i] = new ArraySegment<Int64>(niceArray, (niceArray.Length / _threads) * i, niceArray.Length / _threads);
                workers.Add(Task<Int64>.Run(() => Calc(segments[j], progressComs[j], cancelTokenSource.Token)));
            }
            await Task.WhenAll(workers);

            Int64 result = 0;
            for (int i = 0; i < _threads; i++)
            {
                result += workers[i].Result;
            }

            tbOut.Background = Brushes.Green; tbOut.Text = "sum: " + result.ToString() + " / avg: " + (result / Numbers).ToString();
        }
        private void StartCalcSingleThread_Click(object sender, RoutedEventArgs e)
        {
            Threads = 1;
            StartCalcMultiThread_Click(sender, e);
        }
        public Int64 Calc(ArraySegment<Int64> segementarray, IProgress<int> progress, CancellationToken CancelToken)
        {
            Int64 result = 0;
            int counter = 0;
            int divider = segementarray.Count / 1000;

            while (counter < segementarray.Count)
            {
                result += segementarray[counter] + rnd.Next(1); // + rnd.Next(1) add some extra work
                counter++;
                if (divider > 0 && counter % 1000 == 0)
                {
                    progress.Report(counter / divider);
                    if (CancelToken.IsCancellationRequested) return 0;
                }
            }
            return result;
        }
        public Int64 CreateRandomArray(ref Int64[] array, IProgress<int> progress, CancellationToken CancelToken)
        {
            int counter = 0;
            int divider = array.Length / 1000;

            while (counter < array.Length)
            {
                array[counter] = rnd.Next(100);
                counter++;
                if (counter % divider == 0)
                {
                    progress.Report(counter / divider);
                    if (CancelToken.IsCancellationRequested) return -1;
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
