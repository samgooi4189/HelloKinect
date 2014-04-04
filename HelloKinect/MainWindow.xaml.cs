﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using Kinect.Toolbox;
using Kinect.Toolbox.Voice;
using Microsoft.Kinect;
using Kinect.Toolbox.Record;
using System.Timers;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace HelloKinect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public KinectSensor kinectSensor;
        private KinectSensorChooser sensorChooser;
        SkeletonDisplayManager skeletonDisplayManager;
        readonly ColorStreamManager colorManager = new ColorStreamManager();
        readonly DepthStreamManager depthManager = new DepthStreamManager();
        bool displayDepth = false;
        private Skeleton[] skeletons;
        //Stop watch for record and wait
        Stopwatch timerRec = new Stopwatch();
        Stopwatch timerWait = new Stopwatch();
        //linked list for right and left hand
        List<CoordinateContainer> rightList = new List<CoordinateContainer>();
        List<CoordinateContainer> leftList = new List<CoordinateContainer>();
        List<CoordinateContainer> gesture1_right = new List<CoordinateContainer>();
        List<CoordinateContainer> gesture1_left = new List<CoordinateContainer>();
        //file stream for gesture
        GestureIO fileManager = new GestureIO();

        bool m_isInRecordMode;

        //IronPython Variables
        private string m_codeString = "";
        private string m_consoleString = "";
        private ScriptEngine m_engine = Python.CreateEngine();
        private ScriptScope m_scope = null;
        private MemoryStream m_ms = new MemoryStream();

        //buffer variables (between gestures during coding mode)
        private const double k_secondsToBuffer = 5.0;

        //PHASE 6
        private Queue<KinectTileButton> m_tileQueue;
        private Dictionary<String, String> m_gestureDictionary;

        public MainWindow()
        {
            InitializeComponent();
            m_isInRecordMode = true;
        }

        /**
         * This is the function to check whether Kinect is unpluged or not
         * 
         * */
        void Kinects_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (kinectSensor == null)
                    {
                        kinectSensor = e.Sensor;
                        Initialize();
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (kinectSensor == e.Sensor)
                    {
                        Clean();
                        MessageBox.Show("Kinect was disconnected");
                    }
                    break;
                case KinectStatus.NotReady:
                    break;
                case KinectStatus.NotPowered:
                    if (kinectSensor == e.Sensor)
                    {
                        Clean();
                        MessageBox.Show("Kinect is no more powered");
                    }
                    break;
                default:
                    MessageBox.Show("Unhandled Status: " + e.Status);
                    break;
            }
            
        }

        /*
         * This is been called from MainWindow.xaml
         */
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooser.KinectChanged += SensorChooserOnKinectChanged;
            this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.Start();

            try
            {
                //listen to any status change for Kinects
                KinectSensor.KinectSensors.StatusChanged += Kinects_StatusChanged;

                //loop through all the Kinects attached to this PC, and start the first that is connected without an error.
                foreach (KinectSensor kinect in KinectSensor.KinectSensors)
                {
                    if (kinect.Status == KinectStatus.Connected)
                    {
                        kinectSensor = kinect;
                        break;
                    }
                }
                if (KinectSensor.KinectSensors.Count == 0)
                    MessageBox.Show("No Kinect found");
                else
                    Initialize();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /*
         * This function is been called from MainWindow.xaml
         */
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Clean();
        }

        /*
         * Get everything initialized
         */
        public void Initialize() {
            if (kinectSensor == null)
                return;

            kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinectSensor.ColorFrameReady += kinectRuntime_ColorFrameReady;

            kinectSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            kinectSensor.DepthFrameReady += kinectSensor_DepthFrameReady;

            kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.5f,
                Prediction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            });

            kinectSensor.SkeletonFrameReady += kinectRuntime_SkeletonFrameReady;
            skeletonDisplayManager = new SkeletonDisplayManager(kinectSensor, kinectCanvas);
            kinectSensor.Start();

            
            //load in our gestures from file
            tile_1_Copy3.Background = new SolidColorBrush(Color.FromArgb(100, 82, 29, 143));
            tile_1.Background = new SolidColorBrush(Color.FromArgb(100, 82, 29, 143));
            tile_1_Copy.Background = new SolidColorBrush(Color.FromArgb(100, 82, 29, 143));
            tile_1_Copy2.Background = new SolidColorBrush(Color.FromArgb(100, 82, 29, 143));
            tile_1_Copy1.Background = new SolidColorBrush(Color.FromArgb(100, 82, 29, 143));
            tile_1_Copy4.Background = new SolidColorBrush(Color.FromArgb(100, 82, 29, 143));
            tile_1_Copy5.Background = new SolidColorBrush(Color.FromArgb(100, 82, 29, 143));
            tile_1_Copy6.Background = new SolidColorBrush(Color.FromArgb(100, 82, 29, 143));
            tile_1_Copy7.Background = new SolidColorBrush(Color.FromArgb(100, 82, 29, 143));

            m_gestureDictionary = fileManager.LoadGesture();
        }

        public void Clean()
        {
            //v_commander.OrderDetected -= voiceCommander_OrderDetected;
            if (kinectSensor != null)
            {
                kinectSensor.DepthFrameReady -= kinectSensor_DepthFrameReady;
                kinectSensor.SkeletonFrameReady -= kinectRuntime_SkeletonFrameReady;
                kinectSensor.ColorFrameReady -= kinectRuntime_ColorFrameReady;
                kinectSensor.Stop();
                kinectSensor = null;
            }
        }

        void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {

            using (var frame = e.OpenDepthImageFrame())
            {
                if (frame == null)
                    return;

                if (!displayDepth)
                    return;

                depthManager.Update(frame);
            }
        }

        void kinectRuntime_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {

            using (var frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;


                if (displayDepth)
                    return;

                colorManager.Update(frame);
            }
        }

        void kinectRuntime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                frame.GetSkeletons(ref skeletons);

                if (skeletons.All(s => s.TrackingState == SkeletonTrackingState.NotTracked))
                    return;

                ProcessFrame(frame);
            }
        }

        void ProcessFrame(ReplaySkeletonFrame frame)
        {
            Dictionary<int, string> stabilities = new Dictionary<int, string>();
            foreach (var skeleton in frame.Skeletons)
            {
                StringBuilder strBuilder = new StringBuilder();

                if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
                    continue;

                skeletonDisplayManager.Draw(frame.Skeletons, false);
                Output.Text = strBuilder.ToString();
                //we only care about 1 skeleton
                break;
            }
        }

        //Get the treshold of the movement
        void ProcessGesture() {

        }

        void replay_DepthImageFrameReady(object sender, ReplayDepthImageFrameReadyEventArgs e)
        {
            if (!displayDepth)
                return;

            depthManager.Update(e.DepthImageFrame);
        }

        void replay_ColorImageFrameReady(object sender, ReplayColorImageFrameReadyEventArgs e)
        {
            if (displayDepth)
                return;

            colorManager.Update(e.ColorImageFrame);
        }

        void replay_SkeletonFrameReady(object sender, ReplaySkeletonFrameReadyEventArgs e)
        {
            ProcessFrame(e.SkeletonFrame);
        }

        /*
         * This function check the coordinate within range
         */
        bool IsWithinRange(double cur, double stored) {
            double range = 0.01;
            if (cur < stored + range && cur > stored - range) {
                return true;
            }
            return false;
        }

        void ButtonOnClick(Object sender, RoutedEventArgs e) {
            KinectTileButton btn = (KinectTileButton)sender;
            
            if (btn == recordButton) {
                OnStartRecord();
            }
            else if (btn == useButton)
            {
                OnTestGesture();
            }
            else if (btn == stopButton)
            {
                OnStopRecord();
            }
            else if (btn == modeButton)
            {
                if (m_isInRecordMode)
                {
                    m_isInRecordMode = false;
                    recordButton.Visibility = System.Windows.Visibility.Hidden;
                    useButton.Visibility = System.Windows.Visibility.Hidden;
                    stopButton.Visibility = System.Windows.Visibility.Hidden;
                    exportButton.Visibility = System.Windows.Visibility.Hidden;

                    OnTestGesture();

                    codeView.Visibility = System.Windows.Visibility.Visible;
                    consoleView.Visibility = System.Windows.Visibility.Visible;
                    runButton.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    m_isInRecordMode = true;
                    
                    recordButton.Visibility = System.Windows.Visibility.Visible;
                    useButton.Visibility = System.Windows.Visibility.Visible;
                    stopButton.Visibility = System.Windows.Visibility.Visible;
                    exportButton.Visibility = System.Windows.Visibility.Visible;

                    OnStopRecord();
                    m_codeString = "";
                    codeView.Content = m_codeString;

                    codeView.Visibility = System.Windows.Visibility.Hidden;
                    consoleView.Visibility = System.Windows.Visibility.Hidden;
                    runButton.Visibility = System.Windows.Visibility.Hidden;
                }
            }
            else if (btn == exportButton)
            {
                //todo
            }
        }

        void GOTO_newFrame() {
            //Here you will be link gesture to syntax
            SelectionWindow window = new SelectionWindow();
            Clean();
            this.Close();
            window.OpenWindow(m_tileQueue);
            //OnWriteGesture("gesture1");
        }

        //Voice trigger events
        void OnStartRecord() {
            //status = RecordingStatus.RECORD;
            gesture1_left.Clear();
            gesture1_right.Clear();

        }

        void OnStopRecord() {
            //status = RecordingStatus.STOP;
        }

        void OnTestGesture() {
            //status = RecordingStatus.USE;
        }

        public void OpenMain(GesturePackage package) {
            fileManager.SaveGesture(package.getName(), package.GetTileKeys());
            this.Show();
        }

        enum RecordingStatus { 
           RECORD, STOP, USE 
        }

        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
        {
            bool error = false;
            if (args.OldSensor != null)
            {
                try
                {
                    args.OldSensor.DepthStream.Range = DepthRange.Default;
                    args.OldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    args.OldSensor.DepthStream.Disable();
                    args.OldSensor.SkeletonStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                    error = true;
                }
            }

            if (args.NewSensor != null)
            {
                try
                {
                    args.NewSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    args.NewSensor.SkeletonStream.Enable();

                    try
                    {
                        args.NewSensor.DepthStream.Range = DepthRange.Near;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;
                        args.NewSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                        //error = true;
                    }
                }
                catch (InvalidOperationException)
                {
                    error = true;
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            if (!error)
                kinectRegion.KinectSensor = args.NewSensor;
        }

        private void OnRunPython(object sender, RoutedEventArgs e)
        {
            m_ms = new MemoryStream();
            m_engine.Runtime.IO.SetOutput(m_ms, new StreamWriter(m_ms));
            m_scope = m_engine.CreateScope();

            ScriptSource source = m_engine.CreateScriptSourceFromString(m_codeString, SourceCodeKind.Statements);
            object result = source.Execute(m_scope);

            string str = ReadFromStream(m_ms);
            WriteToIDE(str);
            m_ms.Close(); 
        }

        private string ReadFromStream(MemoryStream ms)
        {
            int length = (int)ms.Length;
            Byte[] bytes = new Byte[length];

            ms.Seek(0, SeekOrigin.Begin);
            ms.Read(bytes, 0, (int)ms.Length);

            return Encoding.GetEncoding("utf-8").GetString(bytes, 0, (int)ms.Length);
        }

        private void WriteToIDE(string output)
        {
            m_consoleString += output;
            consoleView.Content = m_consoleString;
        }

        private void OnTileHovered(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectTileButton btn = (KinectTileButton)sender;
            Console.WriteLine(btn.Name.ToString());

            if (m_tileQueue == null)
            {
                m_tileQueue = new Queue<KinectTileButton>();
            }

            if (!m_tileQueue.Contains(btn))
            {
                m_tileQueue.Enqueue(btn);
                btn.Background = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
            }

            if (m_tileQueue.Count > 3)
            {
                KinectTileButton popped = m_tileQueue.Dequeue();
                popped.Background = new SolidColorBrush(Color.FromArgb(100, 82, 29, 143));
            }

            int scalar = 1;
            foreach (KinectTileButton button in m_tileQueue)
            {
                button.Background = new SolidColorBrush(Color.FromArgb(255, 0, (byte)(250 * scalar / 6) , 0));
                scalar++;
            }
        }

        private void OnTileClicked(object sender, RoutedEventArgs e)
        {
            KinectTileButton btn = (KinectTileButton)sender;
            if (m_isInRecordMode)
            {
                GOTO_newFrame();
                //m_isInRecordMode = false;
            }
            else
            {
                Queue<KinectTileButton> toCheck = new Queue<KinectTileButton>(m_tileQueue);
                String potentialKey = "";
                while (toCheck.Count != 0)
                {
                    potentialKey += toCheck.Dequeue().Name;
                }

                if (m_gestureDictionary.ContainsKey(potentialKey))
                {
                    m_codeString += m_gestureDictionary[potentialKey];
                    codeView.Content = m_codeString;
                }
            }
        }
    }
}
