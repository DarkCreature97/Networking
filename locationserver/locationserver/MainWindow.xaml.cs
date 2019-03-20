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
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace locationserver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Thread r = new Thread(() => runServer()); //Run thread
        public static TcpListener listener; //Declare tcp listener as public static


        public MainWindow()
        {
            InitializeComponent();
        }

        static Dictionary<string, string> dictionary = new Dictionary<string, string>(); //Make the structure of the Dictionary and declare it as a string dictionary

        public static int stimeout = 1000; //Set server timeout to a 1000 ms

        static void runServer()
        {
            Socket connection; //Declare the connaction to the socket
            Handler RequestHandler; // request the handler
            try
            {
                listener = new TcpListener(IPAddress.Any, 43);
                listener.Start(); //Start listening to the client
                Console.WriteLine("Server started Listening"); //Send a message to the console
                Console.WriteLine();

                while (true)
                {
                    connection = listener.AcceptSocket();
                    RequestHandler = new Handler();
                    Thread t = new Thread(() => RequestHandler.doRequest(connection, stimeout));  //Declare t as a new thread and run the connection and set the timeout
                    t.Start(); //Start the server
                }
            }

            catch (Exception e) //Else throw the exception
            {
                Console.WriteLine("Exception: " + e.ToString());
            }
        }

        class Handler
        {
            public void doRequest(Socket connection, int stimeout)
            {
                NetworkStream socketStream; //Declare the newtwork stream
                socketStream = new NetworkStream(connection);
                Console.WriteLine("Conection Received");

                try
                {
                    StreamWriter sw = new StreamWriter(socketStream); //Set sw as stream writer and sr as stream reader
                    StreamReader sr = new StreamReader(socketStream);
                    socketStream.ReadTimeout = stimeout; //Set the read and write timeout stimeout which has a set default value to 1000 ms
                    socketStream.WriteTimeout = stimeout;
                    sw.AutoFlush = true; //Flush whenever it is needed
                    String line = sr.ReadLine().Trim(); //Set line as every line and trim every line

                    while (sr.Peek() > -1) //As the value is greater than -1 read the lines received as characters
                    {
                        line += (char)sr.Read();
                    }

                    Console.WriteLine("Respond Received: " + "\r\n" + line);
                    string[] sections = line.Split(new char[] { ' ', '\r', '\n' }); //Split every line when encounter these characters
                    string[] sectionWhois = line.Split(new char[] { ' ' }, 2);
                    string username = null; //Set username and location as null
                    string location = null;
                    List<string> protocol = new List<string>();

                    #region ------------------------------------------------HTTP/0.9------------------------------------------------
                    if (!line.Contains("HTTP/1.0") && !line.Contains("HTTP/1.1") && line.Contains("/")) //If the line received does not contain these and contains a /
                    {
                        username = sections[1].Trim('/', '?'); //Split the 2nd argument by / and ?

                        if (line.StartsWith("PUT /")) //If the line starts with put send that line
                        {
                            sw.Write("HTTP/0.9 200 OK\r\nContent-Type: text/plain\r\n\r\n"); //Everytime reads the line add a space at then end
                            for (int i = 3; i < sections.Length; i++)
                            {
                                location += sections[i] + " ";
                            }

                            location = location.Trim(); //Trim the lines received

                            if (dictionary.ContainsKey(username)) //If the dictinary contains this username update the location
                            {
                                dictionary.Remove(username);
                                dictionary.Add(username, location);
                            }
                            else //Otherwise add username and location
                            {
                                dictionary.Add(username, location); 
                                sw.WriteLine();
                            }
                        }

                        if (line.StartsWith("GET /")) //If the line read starts with get 
                        {
                            if (dictionary.ContainsKey(username)) //If the dictinary contains this username update the location
                            {
                                sw.WriteLine("HTTP/0.9 200 OK\r\nContent-Type: text/plain\r\n\r\n" + dictionary[username] + "\r\n"); //Send this line to the client and print the username and the location for that username
                                Console.WriteLine(username + " is " + dictionary[username]);
                            }
                            else //Otherwise send this error line
                            {
                                sw.WriteLine("HTTP/0.9 404 Not Found\r\nContent-Type: text/plain\r\n\r\n");
                                Console.WriteLine("ERROR: no entries found");
                            }
                        }
                    }
                    #endregion

                    #region ------------------------------------------------HTTP/1.0------------------------------------------------
                    else if (line.Contains("HTTP/1.0")) //Otherwise if the line conntains this, go through the code
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

                        if (line.StartsWith("GET /")) //If the line contains, go through the code
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
                    else if (line.Contains("HTTP/1.1")) //Otherwise if the line contains this go through the code
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

                            location = location.Replace("name=", "§"); //Replace this characters from this line with another one
                            location = location.Replace("&location=", "§");

                            string[] newSection = location.Split(new char[] { '§' }); //Now split by this character

                            if (dictionary.ContainsKey(newSection[1])) //Update dictionary
                            {
                                dictionary.Remove(newSection[1]);
                                dictionary.Add(newSection[1], newSection[2]);
                            }
                            else //Otherwise add to dictionary
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
                    else if (sectionWhois.Length == 2) //Otherwise if the line received's length is 2
                    {
                        sw.Write("OK\r\n"); //Send an OK to the client
                        //sw.Flush();
                        if (dictionary.ContainsKey(sectionWhois[0])) //If the user is already in the dictionary Update
                        {
                            dictionary.Remove(sectionWhois[0]);
                            dictionary.Add(sectionWhois[0], sectionWhois[1]);
                        }
                        else //Otherwise add it to the dictionery
                        {
                            dictionary.Add(sectionWhois[0], sectionWhois[1]);
                            sw.WriteLine();
                        }
                    }

                    else if (sectionWhois.Length == 1) //Otherwise if the length is 1
                    {
                        if (dictionary.ContainsKey(sections[0])) //Send the location for that user
                        {
                            Console.WriteLine(sections[0] + " is " + dictionary[sections[0]]);
                            sw.WriteLine(/*sections[0] + " is " + */dictionary[sections[0]]);
                            //sw.Flush();
                        }
                        else //Otherwise send an error
                        {
                            Console.WriteLine("ERROR: no entries found");
                            sw.WriteLine("ERROR: no entries found");
                            //sw.Flush();
                        }
                    }
                    #endregion

                    foreach (KeyValuePair<string, string> userLocation in dictionary) //Each time you access the dictionary print these values
                    {
                        Console.WriteLine("User = {0}, Location = {1}", userLocation.Key, userLocation.Value);
                        Console.WriteLine();
                    }

                }

                catch (Exception e) //Otherwise throw an error with a message
                {
                    Console.WriteLine("Error! - Something went wrong!" + e);
                    Console.WriteLine();
                }
                finally //at last close the socket stream and the connection
                {
                    socketStream.Close();
                    connection.Close();
                }

            }

        }
        private void btn_Start(object sender, RoutedEventArgs e) //When the Start button is clicked try connecting to the server
        {
            try
            {
                stimeout = int.Parse(timeoutBox.Text);
                Thread r = new Thread(() => runServer());
                r.Start();
                consoleBox.AppendText("Server is listening but will not display any content");
                consoleBox.AppendText("\r\n");
            }
            catch (Exception ex) //If this fails throw a full detailed exeption
            {
                consoleBox.AppendText("Error: Server launch has failed" + ex);
                consoleBox.AppendText("\r\n");
            }
        }

        private void btn_Close(object sender, RoutedEventArgs e) //When the Close button is clicked Close the connection with the client
        {
            listener.Stop();
            consoleBox.AppendText("Server stopped listening");
            consoleBox.AppendText("\r\n");

        }

        private void ConsoleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            consoleBox.ScrollToEnd();
        }
    }
}
