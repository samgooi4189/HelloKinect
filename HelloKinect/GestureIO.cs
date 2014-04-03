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
        char[] delimChar = {','};

        public GestureIO() {
            dir_path = Directory.GetCurrentDirectory();
            Console.WriteLine(dir_path);
        }

        public GestureIO(String target) {
            dir_path = target;
        }

        public Dictionary<String, GesturePackage> loadGesture() {
            Dictionary<String, GesturePackage> gestureDic = new Dictionary<string, GesturePackage>();
            String txt_path = String.Format("{0}/gestures.kges", dir_path);
            if (File.Exists(txt_path)) {
                //LinkedList<String> content = new LinkedList<string>();
                using (StreamReader reader = new StreamReader(txt_path)) { 
                    String input= "";
                    int line_counter = 0;
                    GesturePackage ges_pack;
                    while ((input = reader.ReadLine()) != null) {
                        //content.AddLast(input);
                        String[] str_array = input.Split(delimChar);
                        ges_pack = new GesturePackage(str_array[0]);
                        foreach(String str in str_array){
                            if (line_counter % 4 == 1 && ges_pack != null) {
                                String[] left_coordinates = str.Split(':');
                                for(int i=0; i<left_coordinates.Length; i+=2){
                                    ges_pack.setLeftCoordinates(new CoordinateContainer(Double.Parse(left_coordinates[i]), Double.Parse(left_coordinates[i+1])) );
                                }
                            }
                            else if (line_counter % 4 == 2 && ges_pack != null)
                            {
                                String[] right_coordinates = str.Split(':');
                                for (int i = 0; i < right_coordinates.Length; i += 2)
                                {
                                    ges_pack.setRightCoordinates(new CoordinateContainer(Double.Parse(right_coordinates[i]), Double.Parse(right_coordinates[i + 1])));
                                }
                            }
                        }
                        Console.WriteLine(input);
                        gestureDic.Add(ges_pack.getName(), ges_pack);
                        line_counter++;
                    }
                    reader.Close();
                }  
            }
            return gestureDic;
        }

        public bool saveGesture(String gesture_name, List<CoordinateContainer> right_coordinates, List<CoordinateContainer> left_coordinates) {
            try
            {
                if (!Directory.Exists(dir_path))
                {
                    Directory.CreateDirectory(dir_path);
                }

                String txt_path = String.Format("{0}/gestures.kges", dir_path);
                if (!File.Exists(txt_path))
                {
                    //create a file to write to
                    using (StreamWriter streamWriter = File.CreateText(txt_path))
                    {
                        streamWriter.WriteLine(gesture_name);
                        foreach (CoordinateContainer r in right_coordinates)
                        {
                            streamWriter.Write("{0}, ", r.ToString());
                        }
                        streamWriter.Write(System.Environment.NewLine);
                        foreach (CoordinateContainer l in left_coordinates)
                        {
                            streamWriter.Write("{0}, ", l.ToString());
                        }
                        streamWriter.Write(System.Environment.NewLine);
                        streamWriter.Write(System.Environment.NewLine);
                        streamWriter.Close();
                    }
                }
                else {
                    using (StreamWriter streamWriter = File.AppendText(txt_path)) {
                        streamWriter.WriteLine(gesture_name);
                        foreach (CoordinateContainer r in right_coordinates)
                        {
                            streamWriter.Write("{0}, ", r.ToString());
                        }
                        streamWriter.Write(System.Environment.NewLine);
                        foreach (CoordinateContainer l in left_coordinates)
                        {
                            streamWriter.Write("{0}, ", l.ToString());
                        }
                        streamWriter.Write(System.Environment.NewLine);
                        streamWriter.Write(System.Environment.NewLine);
                        streamWriter.Close();
                    }
                }
                //read file
                using (StreamReader reader = File.OpenText(txt_path)) {
                    string str = "";
                    while ((str = reader.ReadLine()) != null) {
                        //read file and cache to the program
                        Console.WriteLine(str);
                    }
                    reader.Close();
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
