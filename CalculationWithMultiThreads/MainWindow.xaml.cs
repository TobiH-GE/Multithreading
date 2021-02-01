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

        public int _threads;
        public int _numbers;
        int[] niceArray;

        Random rnd = new Random();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Threads = 4;
            Numbers = 100000015;
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Threads)));
                }
            }
        }
        public int Numbers
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

        private async void StartGeneratingNumbers()
        {
            niceArray = new int[_numbers];

            spBars.Children.Clear();

            Progress<int>[] progressComs = new Progress<int>[1];
            List<ProgressBar> listWithPBars = new List<ProgressBar>();
            Task<int>[] workers;

            listWithPBars.Add(new ProgressBar() { Width = 300, Height = 30, Maximum = 1000 });
            spBars.Children.Add(listWithPBars[listWithPBars.Count - 1]);
            progressComs[0] = new Progress<int>((x) => refreshProgressBar(x, listWithPBars[0]));

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
            spBars.Children.Clear();

            Progress<int>[] progressComs = new Progress<int>[_threads];
            List<ProgressBar> listWithPBars = new List<ProgressBar>();
            Task<int>[] workers;

            for (int i = 0; i < _threads; i++)
            {
                listWithPBars.Add(new ProgressBar() { Width = 300, Height = 30, Maximum = 1000 });
                spBars.Children.Add(listWithPBars[listWithPBars.Count - 1]);
                int j = listWithPBars.Count - 1;
                progressComs[i] = new Progress<int>((x) => refreshProgressBar(x, listWithPBars[j]));
            }

            tbOut.Text = "calculating segments...";
            tbOut.Background = Brushes.Red;

            ArraySegment<int>[] segments = new ArraySegment<int>[_threads];

            cancelTokenSource = new CancellationTokenSource();
            workers = new Task<int>[_threads];

            for (int i = 0; i < _threads; i++)
            {
                int j = i;
                if (niceArray.Length % _threads != 0 && i == _threads - 1)
                    segments[i] = new ArraySegment<int>(niceArray, (niceArray.Length / _threads) * i, niceArray.Length / _threads + niceArray.Length % _threads);
                else
                    segments[i] = new ArraySegment<int>(niceArray, (niceArray.Length / _threads) * i, niceArray.Length / _threads);
                workers[i] = new Task<int>(() => Calc(segments[j], progressComs[j], cancelTokenSource.Token));
                workers[i].Start();
            }
            await Task.WhenAll(workers);
            tbOut.Background = Brushes.Green;

            int result = 0;
            for (int i = 0; i < _threads; i++)
            {
                result += workers[i].Result;
            }

            tbOut.Text = result.ToString();
        }
        private void StartCalcSingleThread_Click(object sender, RoutedEventArgs e)
        {
            Threads = 1;
            StartCalcMultiThread_Click(sender, e);
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
