using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloKinect
{
    class GestureIO
    {

        String dir_path;
        public GestureIO() {
            dir_path = Directory.GetCurrentDirectory();
            Console.WriteLine(dir_path);
        }

        public GestureIO(String target) {
            dir_path = target;
        }

        public bool loadGesture() {
            String txt_path = String.Format("{0}/gestures.kges", dir_path);
            if (File.Exists(txt_path)) {
                LinkedList<String> content = new LinkedList<string>();
                using (StreamReader reader = new StreamReader(txt_path)) { 
                    String input= "";
                    while ((input = reader.ReadLine()) != null) {
                        content.AddLast(input);
                    }
                    Console.WriteLine(content.ToString());
                }
                return true;   
            }
            return false;
        }

        public bool saveGesture(String gesture_name, LinkedList<double> right_coordinates, LinkedList<double> left_coordinates) {
            try
            {
                if (!Directory.Exists(dir_path))
                {
                    Directory.CreateDirectory(dir_path);
                }

                String txt_path = String.Format("{0}/gestures.kges", dir_path);
                if (!File.Exists(txt_path)) { 
                    //create a file to write to
                    using (StreamWriter streamWriter = File.CreateText(txt_path)) {
                        streamWriter.WriteLine(gesture_name);
                        foreach (double r in right_coordinates) { 
                            streamWriter.Write("{0}, ", r.ToString());
                        }
                        streamWriter.Write(System.Environment.NewLine);
                        foreach (double l in left_coordinates)
                        {
                            streamWriter.Write("{0}, ", l.ToString());
                        }
                        streamWriter.Write(System.Environment.NewLine);
                        streamWriter.Write(System.Environment.NewLine);
                    }
                }
                //read file
                using (StreamReader reader = File.OpenText(txt_path)) {
                    string str = "";
                    while ((str = reader.ReadLine()) != null) {
                        Console.WriteLine(str);
                    }
                }

                return true;
            }
            catch (Exception e) {
                Console.WriteLine("The process had failed due to {0}.", e.ToString());
            }
            
            return false;
        }
    }
}
