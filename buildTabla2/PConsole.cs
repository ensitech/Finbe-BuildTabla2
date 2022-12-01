using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace buildTabla2
{
    public class PConsole
    {
        private static PluginConsole console;
        private static bool enabled;
        private static string id;
        public static void init(string id, string ipAddress, int port, bool enabled)
        {
            PConsole.enabled = enabled;
            if (enabled)
            {

                console = new PluginConsole(ipAddress, port, PluginConsole.CLIENT);
                PConsole.id = id;
            }
        }
        public static void close()
        {
            //console.close();
        }

        public static void writeLine(string line)
        {
            if (enabled)
            {
                try
                {
                    var data = DataParser.parseData(PConsole.id, line);
                    console.writeLine(data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("eX: " + ex.Message);
                    enabled = false;
                }
            }

        }

        public static string readLine()
        {
            string line = "";
            //if (enabled)
            //{
            //    try
            //    {
            //        //var data = DataParser.parseData(PConsole.id, line);

            //        line = console.readLine();
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine("eX: " + ex.Message);
            //        enabled = false;
            //    }
            //}
            return line;

        }
    }
}
