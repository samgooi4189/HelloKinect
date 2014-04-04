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

        public Dictionary<String, String> LoadGesture()
        {
            Dictionary<String, String> gestureDic = new Dictionary<String, String>();
            String txt_path = String.Format("{0}/gestures.kges", dir_path);
            if (File.Exists(txt_path))
            {
                using (StreamReader reader = new StreamReader(txt_path))
                {
                    String input = "";
                    String name = "";
                    List<String> keys = new List<String>();
                    while ((input = reader.ReadLine()) != null)
                    {
                        if (input == "" || input == "/n")
                        {
                            //save out any existing values, we have hit a new series
                            if (keys.Count != 0)
                            {
                                String pathKey = "";
                                foreach (String str in keys)
                                {
                                    pathKey += str;
                                }
                                gestureDic.Add(pathKey, name);
                            }

                            input = reader.ReadLine();
                            if (input == null) break;

                            name = input;
                            keys = new List<String>();
                        }
                        else
                        {
                            keys.Add(input);
                        }
                    }
                    reader.Close();
                }
            }
            return gestureDic;
        }

    public bool SaveGesture(String gesture, List<String> tileKeys) {
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
                        streamWriter.WriteLine(gesture);
                        foreach (String str in tileKeys)
                        {
                            streamWriter.WriteLine(str);
                        }
                        streamWriter.Write(System.Environment.NewLine);
                        streamWriter.Close();
                    }
                }
                else {
                    using (StreamWriter streamWriter = File.AppendText(txt_path)) {
                        streamWriter.WriteLine(gesture);
                        foreach (String str in tileKeys)
                        {
                            streamWriter.WriteLine(str);
                        }
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
