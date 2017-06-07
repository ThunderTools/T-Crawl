using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WTGrabber
{
    class Program
    {
        private static string currentPlane = "";
        private static List<DataElement> apiData;
        static void Main(string[] args)
        {
            WriteColoredLine("Grabber started", ConsoleColor.Yellow);
            if (!Directory.Exists("data")) Directory.CreateDirectory("data");
            while (true)
            {
                //Try to fetch the JSONs from the game
                JObject jIndicators, jState;
                try
                {
                    jIndicators = JObject.Parse(GetIndicatorJson());
                    jState = JObject.Parse(GetStateJson());
                }
                catch
                {
                    Thread.Sleep(500);
                    continue;
                }

                //Get all the json keys into a list
                IList<string> listIndicatorsApi = jIndicators.Properties().Select(p => p.Name).ToList();
                IList<string> listStateApi = jState.Properties().Select(p => p.Name).ToList();

                try
                {
                    if (currentPlane == jIndicators["type"].Value<string>()) continue;
                }
                catch
                {
                    continue;
                }

                try
                {
                    currentPlane = jIndicators["type"].Value<string>();
                }
                catch
                {
                    currentPlane = "grabber:EMPTY"; //Mosty happens with the states in the menu
                }

                if (File.Exists(@"data\" + currentPlane)) continue;
                WriteColoredLine("++ " + currentPlane, ConsoleColor.DarkGreen);
                apiData = new List<DataElement>();


                //Scan the indicators
                foreach (string ind in listIndicatorsApi)
                {
                    apiData.Add(new DataElement(EvaluateType(jIndicators[ind].Value<string>()), ind, "indicators"));
                    WriteColoredLine("+i " + ind, ConsoleColor.DarkGreen);
                }

                //Scan the states
                foreach (string stat in listStateApi)
                {
                    apiData.Add(new DataElement(EvaluateType(jState[stat].Value<string>()), stat, "state"));
                    WriteColoredLine("+s " + stat, ConsoleColor.DarkGreen);
                }
                Thread.Sleep(1000);
                SaveToFile(currentPlane, apiData);

            }
        }

        static void WriteColoredLine(string text, ConsoleColor color)
        {
            ConsoleColor prevColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = prevColor;
        }

        public static string GetIndicatorJson()
        {
            WebRequest request = WebRequest.Create("http://localhost:8111/indicators");
            request.Credentials = CredentialCache.DefaultCredentials;
            WebResponse response = request.GetResponse();
            //Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            reader.Close();
            response.Close();
            return responseFromServer;
        }

        public static string GetStateJson()
        {
            WebRequest request = WebRequest.Create("http://localhost:8111/state");
            request.Credentials = CredentialCache.DefaultCredentials;
            WebResponse response = request.GetResponse();
            //Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            reader.Close();
            response.Close();
            return responseFromServer;
        }

        static Type EvaluateType(string value)
        {
            int ignoreMe;
            double ignoreMe2;
            bool ignoreMe3;
            if (Int32.TryParse(value, out ignoreMe))
            {
                return Type.DoubleOrInt;
            }
            else if (double.TryParse(value, out ignoreMe2))
            {
                return Type.Double;
            }
            else if (bool.TryParse(value, out ignoreMe3))
            {
                return Type.Boolean;
            }
            else
            {
                return Type.String;
            }
        }
        public enum Type
        {
            String,
            Boolean,
            Double,
            DoubleOrInt
        }

        static void SaveToFile(string plane, List<DataElement> data)
        {
            File.WriteAllText(@"data\" + plane + ".json", JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        internal class DataElement
        {
            public Type Type { get; set; }
            public string Name { get; set; }
            public string InterfaceName { get; set; }

            public DataElement()
            {

            }
            public DataElement(Type type, string name, string interfaceName)
            {
                this.Type = type;
                this.Name = name;
                this.InterfaceName = interfaceName;
            }
        }
    }
}