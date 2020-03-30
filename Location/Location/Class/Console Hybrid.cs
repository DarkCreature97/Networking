using System;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.IO;

namespace Location.Class
{
    public static class Console_Hybrid
    {
        private static bool debug;

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeConsole();
        #region old kerner things
        /*[DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);*/
        #endregion
        [STAThread]
        public static int Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                try
                {
                    TcpClient client = new TcpClient();
                    //client.Connect("whois.net.dcs.hull.ac.uk", 43);
                    string server = "whois.net.dcs.hull.ac.uk";
                    int port = 43;
                    string protocol = "whois";
                    string username = null;
                    string location = null;
                    int ctimeout = 1000;
                    //bool debug = false;
                    for (int i = 0; i < args.Length; ++i)
                    {
                        switch (args[i])
                        {
                            case "-h": server = args[++i]; break;
                            case "-p": port = int.Parse(args[++i]); break;
                            case "-h9":
                            case "-h0":
                            case "-h1": protocol = args[i]; break;
                            case "-t": ctimeout = int.Parse(args[++i]); break;
                            case "-w": break;
                            case "-d": debug = true; break;
                            default:

                            if (username == null)
                            {
                                username = args[i];
                            }
                            else if (location == null)
                            {
                                location = args[i];
                            }
                            else
                            {
                                Console.WriteLine("Too many arguments");
                            }
                            break;
                        }
                    }

                    if (ctimeout > 0)
                    {
                        client.ReceiveTimeout = ctimeout;
                        client.SendTimeout = ctimeout;
                    }

                    if (username == null)
                    {
                        Console.WriteLine("Too few arguments");
                        return (0);
                    }


                    client.Connect(server, port);
                    StreamWriter sw = new StreamWriter(client.GetStream());
                    StreamReader sr = new StreamReader(client.GetStream());
                    sw.AutoFlush = true;
                    //string response = sr.ReadToEnd();

                    switch (protocol)
                    {
                        case "whois":
                        if (location == null)
                        {
                            sw.WriteLine(username);
                            sw.Flush();
                            string response = sr.ReadToEnd();
                            Console.Write(username + " is " + response);
                        }

                        else
                        {
                            sw.WriteLine(username + " " + location);
                            //sw.Flush();
                            string response = sr.ReadLine();
                            if(response == "OK")
                            {
                                Console.WriteLine(username + " location changed to be " + location); 
                            }
                            else
                            {
                                Console.WriteLine("ERROR: Unexpected response: " + response + args);
                            }
                        }
                        break;

                        case "-h9":
                        if (location == null)
                        {
                            sw.WriteLine("GET /" + username);
                            //sw.Flush();
                            string line1 = sr.ReadLine();
                            if (line1.Contains("404"))
                            {
                                Console.WriteLine(line1);
                                
                            }
                            else
                            {
                                line1 = sr.ReadLine();
                                line1 = sr.ReadLine();
                                Console.WriteLine(username + " is " + sr.ReadLine());
                            }

                        }
                        else
                        {
                            sw.WriteLine("PUT /" + username + "\r\n" + "\r\n" + location);
                            //sw.Flush();
                            string response = sr.ReadLine();
                            if (response.Contains("OK"))
                            {
                                while (sr.Peek() > -1)
                                {
                                    response += sr.ReadLine() + "\r\n";
                                }

                                Console.WriteLine(username + " location changed to be " + location);
                            }
                            else
                            {
                                Console.WriteLine("ERROR: Unexpected response: " + response + args);
                            }
                        }
                        break;

                        case "-h0":
                        if (location == null)
                        {
                            sw.WriteLine("GET /?" + username + " HTTP/1.0\r\n");
                            //sw.Flush();
                            string line1 = sr.ReadLine();
                            if (line1.Contains("404"))
                            {
                                Console.WriteLine(line1);
                            }
                            else
                            {
                                line1 = sr.ReadLine();
                                line1 = sr.ReadLine();
                                Console.WriteLine(username + " is " + sr.ReadLine());
                            }
                        }
                        else
                        {

                            sw.Write("POST /" + username + " HTTP/1.0" + "\r\n" + "Content-Length: " + location.Length + "\r\n" + "\r\n" + location);
                            //sw.Flush();
                            string response = sr.ReadLine();
                            if (response.Contains("OK"))
                            {
                                while (sr.Peek() > -1)
                                {
                                    response += sr.ReadLine() + "\r\n";
                                }

                                Console.WriteLine(username + " location changed to be " + location);
                            }
                            else
                            {
                                Console.WriteLine("ERROR: Unexpected response: " + response + args);
                            }
                        }
                        break;

                        case "-h1":
                        if (location == null)
                        {
                            sw.WriteLine("GET /" + "?name=" + username + " HTTP/1.1\r\n" + "Host: " + server + "\r\n");
                            //sw.Flush();
                            string line1 = sr.ReadLine();
                            if (port == 80)
                            {
                                string outputLine = "";
                                bool found = false;
                                while (sr.Peek() > -1)
                                {
                                    string stratsFrom = sr.ReadLine();
                                    if (stratsFrom == "")
                                    {
                                        found = true;
                                    }

                                    else if (found)
                                    {
                                        stratsFrom += "\r\n";
                                        outputLine += stratsFrom;
                                    }
                                }
                                Console.WriteLine(username + " is " +  outputLine);
                            }

                            else if (line1.Contains("404"))
                            {
                                Console.WriteLine(line1);
                            }

                            else
                            {
                                line1 = sr.ReadLine();
                                line1 = sr.ReadLine();
                                line1 = sr.ReadLine();
                                Console.WriteLine(username + " is " + line1);
                            }

                        }

                        else
                        {
                            int H1Length = username.Length + location.Length + 15;
                            sw.Write("POST / HTTP/1.1\r\n" + "Host: " + server + "\r\n" + "Content-Length: " + H1Length + "\r\n" + "\r\n" + "name=" + username + "&location=" + location);
                            //sw.Flush();
                            string response = sr.ReadLine();
                            if (response.Contains("OK"))
                            {
                                while (sr.Peek() > -1)
                                {
                                response += sr.ReadLine() + "\r\n";
                                }

                            Console.WriteLine(username + " location changed to be " + location);
                            }
                            else
                            {
                                Console.WriteLine("ERROR: Unexpected response: " + response + args);
                            }
                        }
                        break;

                    }
                }
                catch (Exception e)
                {
                    if (debug == true)
                    {
                        Console.WriteLine("ERROR: No arguments supplied" + "\r\n" + e);
                        System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(e, true);
                        Console.WriteLine("Error in line: " + trace.GetFrame(0).GetFileLineNumber());
                    }
                    else
                    {
                        Console.WriteLine("ERROR: No arguments supplied" + "\r\n");
                    }
                }
                return 0;
            }
            else
            {
                FreeConsole();
                var app = new App();
                return app.Run();
            }
        }
    }
}
