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
using System.Net.Sockets;
using System.IO;
namespace Location
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TcpClient client = new TcpClient();
                string server = hostBox.Text;
                int port = int.Parse(portBox.Text);
                string username = usernameBox.Text;
                string location = locationBox.Text;
                int ctimeout = int.Parse(timeoutBox.Text);
                client.Connect(server, port);
                StreamWriter sw = new StreamWriter(client.GetStream());
                StreamReader sr = new StreamReader(client.GetStream());
                sw.AutoFlush = true;

                if (ctimeout > 0) //If the timeout is greater than 0 set the timeout value from the box
                {
                    client.ReceiveTimeout = ctimeout;
                    client.SendTimeout = ctimeout;
                }
                else if(ctimeout == 0)
                {
                    consoleBox.AppendText("Timeout has been disabled" + "\r\n");
                }

                if (username == "" && port != 80) // if the username box is blank and the port is not equal to 80 show error
                {
                    consoleBox.AppendText("Too few arguments ");
                    consoleBox.AppendText("\r\n");
                }

                if (whoisRadio.IsChecked == true)
                {

                    if (location == "") //If the location is empty show the location of the username searched
                    {
                        sw.WriteLine(username);
                        sw.Flush();
                        string response = sr.ReadToEnd();
                        consoleBox.AppendText(username + " is " + response);
                    }

                    else //Otherwise set the location for that username
                    {
                        sw.WriteLine(username + " " + location);
                        string response = sr.ReadLine();
                        if (response == "OK") //If the response from the server is OK
                        {
                            consoleBox.AppendText(username + " location changed to be " + location);
                            consoleBox.AppendText("\r\n");
                        }
                        else //Otherwise show error
                        {
                            consoleBox.AppendText("ERROR: Unexpected response: " + response);
                            consoleBox.AppendText("\r\n");
                        }
                    }
                }

                if (http09Radio.IsChecked == true)
                {
                    if (location == "") //If the protocol is set to -h9 and location is empty show the location of the username searched
                    {
                        sw.WriteLine("GET /" + username);
                        string line1 = sr.ReadLine();

                        if (line1.Contains("404 Not Found")) //If the first line starts with 404
                        {
                            consoleBox.AppendText(line1);
                            consoleBox.AppendText("\r\n");
                        }
                        else //Otherwise show the location for the user searched
                        {
                            line1 = sr.ReadLine();
                            line1 = sr.ReadLine();
                            consoleBox.AppendText(username + " is " + sr.ReadLine());
                            consoleBox.AppendText("\r\n");
                        }

                    }
                    else //Otherwise add user
                    {
                        sw.WriteLine("PUT /" + username + "\r\n" + "\r\n" + location); //Send this line to the server
                        string response = sr.ReadLine();
                        if (response.Contains("OK")) //If the line contain OK read all the lines and set the username's location to new location
                        {
                            while (sr.Peek() > -1)
                            {
                                response += sr.ReadLine() + "\r\n";
                            }

                            consoleBox.AppendText(username + " location changed to be " + location);
                            consoleBox.AppendText("\r\n");
                        }
                        else //Otherwise show error
                        {
                            consoleBox.AppendText("ERROR: Unexpected response: " + response);
                            consoleBox.AppendText("\r\n");
                        }
                    }
                }

                if (http10Radio.IsChecked == true)
                {
                    if (location == "") //If the protocol is set to -h0 and location is empty show the location of the username searched
                    {
                        sw.WriteLine("GET /?" + username + " HTTP/1.0\r\n");
                        string line1 = sr.ReadLine();
                        if (line1.Contains("404 Not Found")) //If the first line starts with 404
                        {
                            consoleBox.AppendText(line1);
                            consoleBox.AppendText("\r\n");
                        }
                        else //Otherwise show the location for the user searched
                        {
                            line1 = sr.ReadLine();
                            line1 = sr.ReadLine();
                            consoleBox.AppendText(username + " is " + sr.ReadLine());
                            consoleBox.AppendText("\r\n");
                        }
                    }
                    else //Otherwise add user
                    {
                        sw.Write("POST /" + username + " HTTP/1.0" + "\r\n" + "Content-Length: " + location.Length + "\r\n" + "\r\n" + location);
                        string response = sr.ReadLine();
                        if (response.Contains("OK")) //If the line contain OK read all the lines and set the username's location to new location
                        {
                            while (sr.Peek() > -1)
                            {
                                response += sr.ReadLine() + "\r\n";
                            }

                            consoleBox.AppendText(username + " location changed to be " + location);
                            consoleBox.AppendText("\r\n");
                        }
                        else //Otherwise send an error
                        {
                            consoleBox.AppendText("ERROR: Unexpected response: " + response);
                            consoleBox.AppendText("\r\n");
                        }
                    }
                }

                if (http11Radio.IsChecked == true)
                {
                    if (location == "") //If the protocol is set to -h1 and location is empty show the location of the username searched
                    {
                        sw.WriteLine("GET /" + "?name=" + username + " HTTP/1.1\r\n" + "Host: " + server + "\r\n");
                        string line1 = sr.ReadLine();
                        if (port == 80) //If the port is 80 print everything when it starts from a blank line all the way to the last blank line as a location
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
                            consoleBox.AppendText(username + " is " + outputLine);
                            consoleBox.AppendText("\r\n");
                        }

                        else if (line1.Contains("404 Not Found")) //Otherwise show error
                        {
                            consoleBox.AppendText(line1);
                            consoleBox.AppendText("\r\n");
                        }

                        else
                        {
                            line1 = sr.ReadLine();
                            line1 = sr.ReadLine();
                            line1 = sr.ReadLine();
                            consoleBox.AppendText(username + " is " + line1);
                            consoleBox.AppendText("\r\n");
                        }
                    }

                    else
                    {
                        int H1Length = username.Length + location.Length + 15;
                        sw.Write("POST / HTTP/1.1\r\n" + "Host: " + server + "\r\n" + "Content-Length: " + H1Length + "\r\n" + "\r\n" + "name=" + username + "&location=" + location);
                        string response = sr.ReadLine();
                        if (response.Contains("OK"))
                        {
                            while (sr.Peek() > -1)
                            {
                                response += sr.ReadLine() + "\r\n";
                            }

                            consoleBox.AppendText(username + " location changed to be " + location);
                            consoleBox.AppendText("\r\n");
                        }
                        else
                        {
                            consoleBox.AppendText("ERROR: Unexpected response: " + response);
                            consoleBox.AppendText("\r\n");
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                if (debugModeCheckBox.IsChecked == true)
                {
                    consoleBox.AppendText("ERROR: Unkown error" + "\r\n" + ex);
                    consoleBox.AppendText("\r\n");
                    System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(ex, true);
                    consoleBox.AppendText("Error in line: " + trace.GetFrame(0).GetFileLineNumber());
                    consoleBox.AppendText("\r\n");
                }
                else
                {
                    consoleBox.AppendText("ERROR: Unkown error" + "\r\n");
                }
            }
        }

        private void btnSetTimeout_Click(object sender, RoutedEventArgs e)
        {

        }

        private void consoleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            consoleBox.ScrollToEnd();
        }
    }
}
