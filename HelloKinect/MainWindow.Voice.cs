using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * This class is the helper class to process voice command
 */
namespace HelloKinect
{
    partial class MainWindow
    {
        void StartVoiceCommander()
        {
            v_commander.Start(kinectSensor);
        }

        void voiceCommander_OrderDetected(string order)
        {
            Dispatcher.Invoke(new Action(() =>
            {

                switch (order)
                {
                    case "record":
                        Console.WriteLine("Talk record");
                        break;
                    case "stop":
                        Console.WriteLine("Talk stop");
                        break;
                    case "fly away":
                        Console.WriteLine("Talk fly away");
                        break;
                    case "flapping":
                        Console.WriteLine("Talk flapping");
                        break;
                }
            }));
        }
    }
}
