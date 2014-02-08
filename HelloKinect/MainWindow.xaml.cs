using System;
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

namespace HelloKinect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public KinectSensor kinectSensor;
        SkeletonDisplayManager skeletonDisplayManager;
        readonly ColorStreamManager colorManager = new ColorStreamManager();
        readonly DepthStreamManager depthManager = new DepthStreamManager();
        bool displayDepth = false;
        private Skeleton[] skeletons;
        //Stop watch for record and wait
        Stopwatch timerRec = new Stopwatch();
        Stopwatch timerWait = new Stopwatch();
        //linked list for right and left hand
        LinkedList<double> rightList = new LinkedList<double>();
        LinkedList<double> leftList = new LinkedList<double>();
        //Voice control
        VoiceCommander v_commander;

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
            kinectSensor.DepthFrameReady -= kinectSensor_DepthFrameReady;
            kinectSensor.SkeletonFrameReady -= kinectRuntime_SkeletonFrameReady;
            kinectSensor.ColorFrameReady -= kinectRuntime_ColorFrameReady;
            kinectSensor.Stop();
            kinectSensor = null;
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
            v_commander = new VoiceCommander("record", "stop", "fly away", "flapping");

            kinectSensor.Start();
            kinectDisplay.DataContext = colorManager;

            v_commander.OrderDetected += voiceCommander_OrderDetected;
            StartVoiceCommander();
        }

        public void Clean() {
            v_commander.OrderDetected -= voiceCommander_OrderDetected;
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

                if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
                    continue;

                if (!timerWait.IsRunning && (timerRec.Elapsed.Seconds > 1 || !timerRec.IsRunning))
                {
                    timerRec.Stop();
                    timerRec.Reset();
                    Console.WriteLine("Start to wait........");
                    ProcessGesture();
                    timerWait.Reset();
                    timerWait.Start();
                }

                if (timerWait.Elapsed.Seconds >= 5 && !timerRec.IsRunning)
                {
                    Console.WriteLine("This is 5 sec, RECORD!");
                    timerWait.Stop();
                    timerRec.Start();
                }
                //if (eyeTracker == null)
                //    eyeTracker = new EyeTracker(kinectSensor);

                //eyeTracker.Track(skeleton);

                /*contextTracker.Add(skeleton.Position.ToVector3(), skeleton.TrackingId);
                stabilities.Add(skeleton.TrackingId, contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId) ? "Stable" : "Non stable");
                if (!contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId))
                    continue;
                */
                //if (eyeTracker.IsLookingToSensor.HasValue && eyeTracker.IsLookingToSensor == false)
                //    continue;
                //StringBuilder strBuilder = new StringBuilder();

                foreach (Joint joint in skeleton.Joints)
                {
                    if (!timerRec.IsRunning) {
                        continue;
                    }
                    if (joint.TrackingState != JointTrackingState.Tracked)
                        continue;
                    //strBuilder.Append(joint.JointType.ToString());

                    if (joint.JointType.Equals(JointType.HandRight))
                    {
                        Console.WriteLine(String.Format("Right [{0}, {1}, {2}]", joint.Position.X, joint.Position.Y, joint.Position.Z));
                        rightList.AddLast(joint.Position.Y);
                    }
                    if (joint.JointType.Equals(JointType.HandLeft))
                    {
                        Console.WriteLine(String.Format("Left [{0}, {1}, {2}]", joint.Position.X, joint.Position.Y, joint.Position.Z));
                        leftList.AddLast(joint.Position.Y);
                    }
                }

                //MessageBox.Show(strBuilder.ToString());

                //this is the place to draw sitting position or not
                skeletonDisplayManager.Draw(frame.Skeletons, false);

                //we only care about 1 skeleton
                break;
            }
        }

        //Get the treshold of the movement
        void ProcessGesture() {
            if (rightList.Count == 0 || leftList.Count == 0) {
                return;
            }

            double rightDiff = Math.Abs(rightList.Last() - rightList.First());
            double leftDiff = Math.Abs(leftList.Last() - leftList.First());

            Console.WriteLine(String.Format("Right Diff: {0}, Left Diff: {1} ", rightDiff, leftDiff));
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
    }
}
