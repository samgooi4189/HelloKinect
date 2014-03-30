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
using Kinect.Toolbox;
using Kinect.Toolbox.Voice;
using Microsoft.Kinect;
using Kinect.Toolbox.Record;
using System.Timers;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;

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
        LinkedList<CoordinateContainer> rightList = new LinkedList<CoordinateContainer>();
        LinkedList<CoordinateContainer> leftList = new LinkedList<CoordinateContainer>();
        LinkedList<CoordinateContainer> gesture1_right = new LinkedList<CoordinateContainer>();
        LinkedList<CoordinateContainer> gesture1_left = new LinkedList<CoordinateContainer>();
        //Voice control
        //VoiceCommander v_commander;
        RecordingStatus status = RecordingStatus.STOP;
        //file stream for gesture
        GestureIO fileManager;

        public MainWindow()
        {
            InitializeComponent();
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
            //Add keywords that you wan to detect
            //v_commander = new VoiceCommander("record", "stop", "fly away", "flapping", "start", "finish", "write");

            kinectSensor.Start();
            //kinectDisplay.DataContext = colorManager;

            //v_commander.OrderDetected += voiceCommander_OrderDetected;
            //StartVoiceCommander();

            fileManager = new GestureIO();
            fileManager.loadGesture();
        }

        public void Clean() {
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

                //timer for recording and wait
                if (!timerWait.IsRunning && (timerRec.Elapsed.Seconds > 2 || !timerRec.IsRunning))
                {
                    timerRec.Stop();
                    timerRec.Reset();
                    Console.WriteLine("Please wait while we process the gesture......");
                    strBuilder.Append("Please wait while we process the gesture......\n");
                    ProcessGesture();
                    timerWait.Reset();
                    timerWait.Start();
                }
                if (timerWait.Elapsed.Seconds >= 5 && !timerRec.IsRunning)
                {
                    Console.WriteLine("Gesture recorded, NEXT GESTURE!");
                    strBuilder.Append("Gesture recorded, NEXT GESTURE!\n");
                    timerWait.Stop();
                    timerRec.Start();
                }
                /*contextTracker.Add(skeleton.Position.ToVector3(), skeleton.TrackingId);
                stabilities.Add(skeleton.TrackingId, contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId) ? "Stable" : "Non stable");
                if (!contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId))
                    continue;
                */

                foreach (Joint joint in skeleton.Joints)
                {
                    if (status == RecordingStatus.STOP)
                        break;

                    if (!timerRec.IsRunning) {
                        continue;
                    }

                    if (joint.TrackingState != JointTrackingState.Tracked)
                        continue;
                    //strBuilder.Append(joint.JointType.ToString());

                    if (status == RecordingStatus.RECORD)
                    {
                        if (joint.JointType.Equals(JointType.HandRight))
                        {
                            Console.WriteLine(String.Format("Right [{0}, {1}, {2}]", joint.Position.X, joint.Position.Y, joint.Position.Z));
                            strBuilder.Append(String.Format("\nRight [{0}, {1}, {2}]", joint.Position.X, joint.Position.Y, joint.Position.Z));
                            rightList.AddLast(new CoordinateContainer(joint.Position.X, joint.Position.Y));
                        }
                        else if (joint.JointType.Equals(JointType.HandLeft))
                        {
                            Console.WriteLine(String.Format("Left [{0}, {1}, {2}]", joint.Position.X, joint.Position.Y, joint.Position.Z));
                            strBuilder.Append(String.Format("\nLeft [{0}, {1}, {2}]", joint.Position.X, joint.Position.Y, joint.Position.Z));
                            leftList.AddLast(new CoordinateContainer(joint.Position.X, joint.Position.Y));
                        }
                    }
                    else if (status == RecordingStatus.USE) {
                        double cur_pos = joint.Position.Y;
                        bool touched = false;
                        if (joint.JointType.Equals(JointType.HandRight)) {
                            Console.WriteLine(String.Format("Using right: [{0}, {1}]", joint.Position.X, joint.Position.Y));
                            strBuilder.Append(String.Format("\nUsing right: [{0}, {1}]", joint.Position.X, joint.Position.Y));
                            //assume gesture array have only 6 elements
                            foreach (CoordinateContainer container in gesture1_right)
                            {
                                touched |= IsWithinRange(joint.Position.Y, container.getY()) || IsWithinRange(joint.Position.X, container.getX());
                            }
                            if (touched)
                            {
                                Console.WriteLine("Right Hand gesture detected");
                                strBuilder.Append("\nRight Hand gesture detected");
                            }
                        }
                        else if (joint.JointType.Equals(JointType.HandLeft)) {
                            Console.WriteLine(String.Format("Using left: [{0}, {1}]", joint.Position.X, joint.Position.Y));
                            strBuilder.Append(String.Format("\nUsing left: [{0}, {1}]", joint.Position.X, joint.Position.Y));
                            //assume gesture array have only 6 elements
                            foreach (CoordinateContainer container in gesture1_left)
                            {
                                touched |= IsWithinRange(joint.Position.Y, container.getY()) || IsWithinRange(joint.Position.X, container.getX());
                            }
                            if (touched)
                            {
                                Console.WriteLine("Left Hand gesture detected");
                                strBuilder.Append("\nLeft Hand gesture detected");
                            }
                        }
                    }
                }

                //MessageBox.Show(strBuilder.ToString());

                //this is the place to draw sitting position or not
                skeletonDisplayManager.Draw(frame.Skeletons, false);
                Output.Text = strBuilder.ToString();
                //we only care about 1 skeleton
                break;
            }
        }

        //Get the treshold of the movement
        void ProcessGesture() {
            if (rightList.Count == 0 || leftList.Count == 0) {
                return;
            }
            //insert into result array
            /*double rightSUM =0, leftSUM=0;
            foreach (double r_val in rightList)
                rightSUM += r_val;
            rightSUM /= rightList.Count;
            foreach (double l_val in leftList)
                leftSUM += l_val;
            leftSUM /= leftList.Count;
            gesture1_right.AddLast(rightSUM);
            gesture1_left.AddLast(leftSUM);*/
            gesture1_right.AddLast(rightList.Last());
            gesture1_left.AddLast(leftList.Last());

            //only consider last coordinate
            double rightXDiff = Math.Abs(rightList.Last().getX() - rightList.Last().getX());
            double rightYDiff = Math.Abs(rightList.Last().getY() - rightList.Last().getY());
            double leftXDiff = Math.Abs(leftList.Last().getX() - leftList.Last().getX());
            double leftYDiff = Math.Abs(leftList.Last().getY() - leftList.Last().getY());
            rightList.Clear();
            leftList.Clear();

            Console.WriteLine(String.Format("Right Diff: {0}, Left Diff: {1} ", (rightXDiff+rightYDiff)/2.00, (leftXDiff+leftYDiff)/2.00 ));
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
            double range = 0.02;
            if (cur < stored + range && cur > stored - range) {
                return true;
            }
            return false;
        }

        void ButtonOnClick(Object sender, RoutedEventArgs e) {
            //MessageBox.Show("Button Clicked");
            KinectCircleButton btn = (KinectCircleButton)sender;
            //MessageBox.Show(btn.Content.ToString());
            if (btn.Content.ToString() == "Record") {
                OnStartRecord();
            }
            else if (btn.Content.ToString() == "Use")
            {
                OnTestGesture();
            }
            else if (btn.Content.ToString() == "Stop")
            {
                OnStopRecord();
            }
            else if (btn.Content.ToString() == "Export") {
                //Here you will be link gesture to syntax
                OnWriteGesture("gesture1");
            }
        }

        //Voice trigger events
        void OnStartRecord() {
            status = RecordingStatus.RECORD;
            gesture1_left.Clear();
            gesture1_right.Clear();

        }

        void OnStopRecord() {
            status = RecordingStatus.STOP;
        }

        void OnTestGesture() {
            status = RecordingStatus.USE;
        }

        void OnWriteGesture(String gesture_syntax) {
            fileManager.saveGesture(gesture_syntax, gesture1_right, gesture1_left);
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

    }
}
