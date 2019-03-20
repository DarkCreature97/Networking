using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace locationserver
{
    public static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeConsole();
        public static int ctimeout = 1000;

        static Dictionary<string, string> dictionary = new Dictionary<string, string>();

        [STAThread]

        
        public static int Main(string[] args)
        {
            bool consoleMode = true;

            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    switch (args[i])
                    {
                        case "-t": ctimeout = int.Parse(args[++i]); break;
                        case "-w": consoleMode = false; break;
                    }
                }
            }

            if (consoleMode == true)
            {
                runServer();
                return 0;
            }
            else

            {
                FreeConsole();
                var app = new App();
                return app.Run();
            }
        }

        static void runServer()
        {
            TcpListener listener;
            Socket connection;
            Handler RequestHandler;
            try
            {
                listener = new TcpListener(IPAddress.Any, 43);
                listener.Start();
                Console.WriteLine("Server started Listening");
                Console.WriteLine();

                while (true)
                {
                    connection = listener.AcceptSocket();
                    RequestHandler = new Handler();
                    Thread t = new Thread(() => RequestHandler.doRequest(connection, ctimeout));
                    t.Start();
                }
            }

            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.ToString());
            }
        }

        class Handler
        {
            public int ctimeout { get; private set; }

            public void doRequest(Socket connection, int ctimeout)
            {
                NetworkStream socketStream;
                socketStream = new NetworkStream(connection);
                Console.WriteLine("Conection Received");

                try
                {
                    StreamWriter sw = new StreamWriter(socketStream);
                    StreamReader sr = new StreamReader(socketStream);
                    socketStream.ReadTimeout = ctimeout;
                    socketStream.WriteTimeout = ctimeout;
                    sw.AutoFlush = true;
                    String line = sr.ReadLine().Trim();

                    while (sr.Peek() > -1)
                    {
                        line += (char)sr.Read();
                    }

                    Console.WriteLine("Respond Received: " + "\r\n" + line);
                    string[] sections = line.Split(new char[] { ' ', '\r', '\n' });
                    string[] sectionWhois = line.Split(new char[] { ' ' }, 2);
                    string username = null;
                    string location = null;
                    List<string> protocol = new List<string>();

                    #region ------------------------------------------------HTTP/0.9------------------------------------------------
                    if (!line.Contains("HTTP/1.0") && !line.Contains("HTTP/1.1") && line.Contains("/"))
                    {
                        username = sections[1].Trim('/', '?');

                        if (line.StartsWith("PUT /"))
                        {
                            sw.Write("HTTP/0.9 200 OK\r\nContent-Type: text/plain\r\n\r\n");
                            for (int i = 3; i < sections.Length; i++)
                            {
                                location += sections[i] + " ";
                            }

                            location = location.Trim();

                            if (dictionary.ContainsKey(username))
                            {
                                dictionary.Remove(username);
                                dictionary.Add(username, location);
                            }
                            else
                            {
                                dictionary.Add(username, location);
                                sw.WriteLine();
                            }
                        }

                        if (line.StartsWith("GET /"))
                        {
                            if (dictionary.ContainsKey(username))
                            {
                                sw.WriteLine("HTTP/0.9 200 OK\r\nContent-Type: text/plain\r\n\r\n" + dictionary[username] + "\r\n");
                                Console.WriteLine(username + " is " + dictionary[username]);
                            }
                            else
                            {
                                sw.WriteLine("HTTP/0.9 404 Not Found\r\nContent-Type: text/plain\r\n\r\n");
                                Console.WriteLine("ERROR: no entries found");
                            }
                        }
                    }
                    #endregion

                    #region ------------------------------------------------HTTP/1.0------------------------------------------------
                    else if (line.Contains("HTTP/1.0"))
                    {
                        username = sections[1].Trim('/', '?');
                        if (line.StartsWith("POST /"))
                        {
                            sw.Write("HTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n\r\n");
                            for (int i = 7; i < sections.Length; i++)
                            {
                                location += sections[i] + " ";
                            }

                            location = location.Trim();

                            if (dictionary.ContainsKey(username))
                            {
                                dictionary.Remove(username);
                                dictionary.Add(username, location);
                            }
                            else
                            {
                                dictionary.Add(username, location);
                                sw.WriteLine();
                            }
                        }

                        if (line.StartsWith("GET /"))
                        {
                            if (dictionary.ContainsKey(username))
                            {
                                sw.WriteLine("HTTP/1.0 200 OK\r\nContent-Type: text/plain\r\n\r\n" + dictionary[username] + "\r\n");
                                Console.WriteLine(username + " is " + dictionary[username]);
                            }
                            else
                            {
                                sw.WriteLine("HTTP/1.0 404 Not Found\r\nContent-Type: text/plain\r\n\r\n");
                                Console.WriteLine("ERROR: no entries found");
                            }
                        }
                    }
                    #endregion

                    #region ------------------------------------------------HTTP/1.1------------------------------------------------
                    else if (line.Contains("HTTP/1.1"))
                    {
                        if (line.StartsWith("POST /"))
                        {
                            username = sections[1].Trim('/', '?');
                            sw.Write("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n");
                            for (int i = 10; i < sections.Length; i++)
                            {
                                location += sections[i] + " ";
                            }

                            location = location.Trim();

                            location = location.Replace("name=", "§");
                            location = location.Replace("&location=", "§");

                            string[] newSection = location.Split(new char[] { '§' });

                            if (dictionary.ContainsKey(newSection[1]))
                            {
                                dictionary.Remove(newSection[1]);
                                dictionary.Add(newSection[1], newSection[2]);
                            }
                            else
                            {
                                dictionary.Add(newSection[1], newSection[2]);
                                sw.WriteLine();
                            }
                        }

                        if (line.StartsWith("GET /"))
                        {
                            sections[1] = sections[1].Replace("name=", "§");
                            string[] newSection = sections[1].Split(new char[] { '§' });
                            username = newSection[1];

                            if (dictionary.ContainsKey(username))
                            {
                                sw.WriteLine("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n" + dictionary[username] + "\r\n");
                                Console.WriteLine(username + " is " + dictionary[username]);
                            }
                            else
                            {
                                sw.WriteLine("HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\n\r\n");
                                Console.WriteLine("ERROR: no entries found");
                            }
                        }
                    }
                    #endregion

                    #region ------------------------------------------------Whois------------------------------------------------
                    else if (sectionWhois.Length == 2)
                    {
                        sw.Write("OK\r\n");
                        //sw.Flush();
                        if (dictionary.ContainsKey(sectionWhois[0]))
                        {
                            dictionary.Remove(sectionWhois[0]);
                            dictionary.Add(sectionWhois[0], sectionWhois[1]);
                        }
                        else
                        {
                            dictionary.Add(sectionWhois[0], sectionWhois[1]);
                            sw.WriteLine();
                        }
                    }

                    else if (sectionWhois.Length == 1)
                    {
                        if (dictionary.ContainsKey(sections[0]))
                        {
                            Console.WriteLine(sections[0] + " is " + dictionary[sections[0]]);
                            sw.WriteLine(/*sections[0] + " is " + */dictionary[sections[0]]);
                            //sw.Flush();
                        }
                        else
                        {
                            Console.WriteLine("ERROR: no entries found");
                            sw.WriteLine("ERROR: no entries found");
                            //sw.Flush();
                        }
                    }
                    #endregion

                    foreach (KeyValuePair<string, string> userLocation in dictionary)
                    {
                        Console.WriteLine("User = {0}, Location = {1}", userLocation.Key, userLocation.Value);
                        Console.WriteLine();
                    }

                }

                catch (Exception e)
                {
                    Console.WriteLine("Error! - Something went wrong!" + e);
                    Console.WriteLine();
                }
                finally
                {
                    socketStream.Close();
                    connection.Close();
                }

            }

        }
    }

}