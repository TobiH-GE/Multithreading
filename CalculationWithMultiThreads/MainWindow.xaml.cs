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

        int _threads = 1;
        Int64 _numbers = 100000015;
        Int64[] niceArray;
        List<ProgressBar> listWithPBars;
        Progress<int>[] progressComs;
        Task<Int64>[] workers;
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
            workers = new Task<Int64>[] { new Task<Int64>(() => CreateRandomArray(ref niceArray, progressComs[0], cancelTokenSource.Token)) };
            workers[0].Start();
            await Task.WhenAll(workers[0]);

            tbOut.Text = workers[0].Result.ToString(); tbOut.Background = Brushes.Green;

            Threads = 4;
        }
        private void refreshProgressBar(int reportedProgress, ProgressBar pb)
        {
            pb.Value = reportedProgress;
        }
        private async void StartCalcMultiThreadQueue_Click(object sender, RoutedEventArgs e) //TODO: correct code, code not working
        {
            tbOut.Text = "calculating segments queued..."; tbOut.Background = Brushes.Red;

            for (int i = 0; i < niceArray.Length / segmentSize; i++)
            {
                ListWithSegments.Add(new ArraySegment<Int64>(niceArray, i * segmentSize, segmentSize));
            }
            ListWithSegments.Add(new ArraySegment<Int64>(niceArray, niceArray.Length - (niceArray.Length % segmentSize) - 1, niceArray.Length % segmentSize));

            cancelTokenSource = new CancellationTokenSource();
            workers = new Task<Int64>[_threads];

            for (int i = 0; i < _threads; i++)
            {
                int j = i;
                workers[i] = new Task<Int64>(() => Calc(ListWithSegments[j], progressComs[j], cancelTokenSource.Token));
                workers[i].Start();
            }
            
            while (ListWithSegments.Count > 0) // TODO: get result, remove blocking, NOT WORKING
            {
                Task<Int64> finishedTask = await Task.WhenAny(workers);
                for (int i = 0; i < _threads; i++)
                {
                    if (finishedTask == workers[i])
                    {
                        int j = i;
                        workers[i] = new Task<Int64>(() => Calc(ListWithSegments[ListWithSegments.Count - 1], progressComs[j], cancelTokenSource.Token));
                        workers[i].Start();
                        ListWithSegments.RemoveAt(ListWithSegments.Count - 1);
                    }
                }
            }

            Int64 result = 0;
            for (int i = 0; i < _threads; i++)
            {
                result += workers[i].Result;
            }

            tbOut.Background = Brushes.Green; tbOut.Text = "sum: " + result.ToString() + " / avg: " + (result / Numbers).ToString();
        }
        
        private async void StartCalcMultiThread_Click(object sender, RoutedEventArgs e)
        {
            tbOut.Text = "calculating segments..."; tbOut.Background = Brushes.Red;

            ArraySegment<Int64>[] segments = new ArraySegment<Int64>[_threads];

            cancelTokenSource = new CancellationTokenSource();
            workers = new Task<Int64>[_threads];

            for (int i = 0; i < _threads; i++)
            {
                int j = i;
                if (niceArray.Length % _threads != 0 && i == _threads - 1)
                    segments[i] = new ArraySegment<Int64>(niceArray, (niceArray.Length / _threads) * i, niceArray.Length / _threads + niceArray.Length % _threads);
                else
                    segments[i] = new ArraySegment<Int64>(niceArray, (niceArray.Length / _threads) * i, niceArray.Length / _threads);
                workers[i] = new Task<Int64>(() => Calc(segments[j], progressComs[j], cancelTokenSource.Token));
                workers[i].Start();
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
        public int CreateRandomArray(ref Int64[] array, IProgress<int> progress, CancellationToken CancelToken)
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
