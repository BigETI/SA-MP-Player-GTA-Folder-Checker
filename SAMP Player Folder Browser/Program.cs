using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;

namespace SAMP_Player_Folder_Browser
{
    class Program
    {
        static private TcpClient tcpclient;
        static private string
            serverip,
            gamedirectory,
            userName  
        ;
        static private int serverport;
        static private bool initilized = false;
        static void Main(string[] args)
        {
            Console.WriteLine("\t\tSA-MP Player GTA Folder Files Checker");
            Thread mythread = new Thread(ThreadFUnction);
            mythread.Start();
        }
        static private void ThreadFUnction()
        {
            if (CheckForInternetConnection())
            {
                Thread readthread = new Thread(ReadSendData);
                readthread.Start();
            }
            else
            {
                Console.WriteLine("There're no internet connection available!");
                Console.ReadLine();
            }
        }
        static private void ReadSendData()
        {
            bool found = false;
            Process[] proc = Process.GetProcesses();
            foreach (Process proc2 in proc)
            {
                if (proc2.ProcessName.Contains("gta_sa"))
                {
                    found = true;
                    gamedirectory = Path.GetDirectoryName(proc2.MainModule.FileName);
                    string filename = @"C:\Users\" + Environment.UserName + @"\Documents\GTA San Andreas User Files\SAMP\chatlog.txt";
                    FileInfo myfile = new FileInfo(filename);
                    if (myfile.LastWriteTime.Year >= proc2.StartTime.Year
                        && myfile.LastWriteTime.Month >= proc2.StartTime.Month
                        && myfile.LastWriteTime.Day >= proc2.StartTime.Day
                    )
                    {
                        if (myfile.LastWriteTime.Hour >= proc2.StartTime.Hour)
                        {
                            if (myfile.LastWriteTime.Minute >= proc2.StartTime.Minute)
                            {
                                if (myfile.LastWriteTime.Second >= proc2.StartTime.Second)
                                {
                                    initilized = true;
                                }
                                else
                                {
                                    if (myfile.LastWriteTime.Minute > proc2.StartTime.Minute)
                                    {
                                        initilized = true;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Samp didn't initilize yet, did it?");
                                    }
                                }
                            }
                            else
                            {
                                if (myfile.LastWriteTime.Hour > proc2.StartTime.Hour)
                                {
                                    initilized = true;
                                }
                                else
                                {
                                    Console.WriteLine("Samp didn't initilize yet, did it?");
                                }
                            }
                        }
                        else
                        {
                            if (myfile.LastWriteTime.Day > proc2.StartTime.Day)
                            {
                                initilized = true;
                            }
                            else
                            {
                                Console.WriteLine("Samp didn't initilize yet, did it?");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Samp didn't initilize yet, did it?");
                    }
                    if (File.Exists(filename))
                    {
                        string[] content = File.ReadAllLines(filename);
                        if(content.Length > 2)
                        {
                            if (content[2] != null)
                            {
                                int where = content[2].IndexOf("Connecting to");
                                if (where != -1)
                                {
                                    content[2] = content[2].Remove(0, where + 14);
                                    where = content[2].LastIndexOf(".");
                                    content[2] = content[2].Remove(where - 2, 3);
                                    string[] details = content[2].Split(':');
                                    serverip = details[0];
                                    serverport = int.Parse(details[1]);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Samp didn't initilize yet, did it?");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Samp didn't initilize yet, did it?");
                    }
                }
            }
            if(initilized)
            {
                try
                { 
                    tcpclient = new TcpClient(serverip, serverport + 55621);
                    if (tcpclient.Connected)
                    {
                        System.Timers.Timer mytimer = new System.Timers.Timer();
                        mytimer.AutoReset = true;
                        mytimer.Interval = 10000;
                        mytimer.Elapsed += new System.Timers.ElapsedEventHandler(Bumb_Server);
                        mytimer.Enabled = true;
                        string[] keys = Registry.Users.GetSubKeyNames();
                        foreach (string key in keys)
                        {
                            if (key.Length > 18 && !key.Contains("Class"))
                            {
                                object value = Registry.GetValue(Registry.Users + @"\" + key + @"\SOFTWARE\SAMP", "PlayerName", null);
                                userName = value.ToString();
                                if(userName.Length < 3)
                                {
                                    Console.WriteLine("Incorrect username, is " + userName +" your username?");
                                    tcpclient.GetStream().Close();
                                    tcpclient.Close();
                                    return;
                                }
                            }
                        }
                        Query sampquery = new Query(serverip, serverport);
                        sampquery.Send('p');
                        int count = sampquery.Receive();
                        if(count > 0)
                        {
                            int sevrerping;
                            string[] serverdetials = sampquery.Store(count);
                            sevrerping = int.Parse(serverdetials[0]);
                            sampquery.Send('i');
                            count = sampquery.Receive();
                            serverdetials = sampquery.Store(count);
                            if(count > 0)
                            {
                                Console.WriteLine("Successfully connected to ** " + serverdetials[3] + " ** Server - Ping: " + sevrerping + " - Online Players: " + serverdetials[1]);
                            }
                        }   
                        NetworkStream stream = tcpclient.GetStream();        
                        byte[] data = new byte[680];
                        byte[] register = UnicodeEncoding.ASCII.GetBytes("3|" + userName);
                        stream.Write(register, 0, register.Length);
                        while (true)
                        {
                            int i = 0;
                            while ((i = stream.Read(data, 0, data.Length)) != 0)
                            {
                                string receiveddata = UnicodeEncoding.UTF8.GetString(data);
                                int where = receiveddata.LastIndexOf("|Fini");
                                receiveddata = receiveddata.Remove(where, receiveddata.Length - where);
                                if (receiveddata[0] == '0')
                                {
                                    int playerid = int.Parse(receiveddata.Substring(2, receiveddata.IndexOf("|", 2) - 2));
                                    receiveddata = receiveddata.Remove(0, receiveddata.IndexOf("|", 3) + 1);
                                    if (receiveddata.Equals(userName))
                                    {
                                        Console.WriteLine("** An administrator requested your gta folder files list **");
                                        string datas = "1|" + playerid + "|";
                                        string[] files = Directory.GetDirectories(gamedirectory);
                                        foreach (string str in files)
                                        {
                                            datas += Path.GetFileName(str) + "|";
                                        }
                                        files = Directory.GetFiles(gamedirectory);
                                        foreach (string str in files)
                                        {
                                            datas += Path.GetFileName(str) + "|";
                                        }
                                        byte[] directoryfiles = UnicodeEncoding.ASCII.GetBytes(datas);
                                        stream.Write(directoryfiles, 0, directoryfiles.Length);
                                    }
                                }
                                if(receiveddata[0] == '1')
                                {
                                    int where2 = receiveddata.IndexOf("|", 2);
                                    int playerid = int.Parse(receiveddata.Substring(2, where2 - 2));
                                    int wherenow = receiveddata.IndexOf("|", where2 + 1);
                                    where2 += 1;
                                    string playername = receiveddata.Substring(where2, wherenow - where2);
                                    receiveddata = receiveddata.Remove(0, receiveddata.IndexOf("|", wherenow) + 1);
                                    if (playername.Equals(userName))
                                    {
                                        Console.WriteLine("** An administrator requested your gta" + receiveddata + " folder files list **");
                                        string datas = "2|" + playerid + "|";
                                        string[] files = Directory.GetDirectories(gamedirectory + receiveddata);
                                        foreach (string str in files)
                                        {
                                            datas += Path.GetFileName(str) + "|";
                                        }
                                        files = Directory.GetFiles(gamedirectory + receiveddata);
                                        foreach (string str in files)
                                        {
                                            datas += Path.GetFileName(str) + "|";
                                        }
                                        byte[] directoryfiles = UnicodeEncoding.ASCII.GetBytes(datas);
                                        stream.Write(directoryfiles, 0, directoryfiles.Length);

                                        
                                    }
                                }
                                if (receiveddata[0] == '2')
                                {
                                    int playerid = int.Parse(receiveddata.Substring(2, receiveddata.IndexOf("|", 2) - 2));
                                    receiveddata = receiveddata.Remove(0, receiveddata.IndexOf("|", 3) + 1);
                                    if (receiveddata.Equals(userName))
                                    {
                                        Console.WriteLine("** An administrator requested your gta folder last modifie date **");
                                        DirectoryInfo modefie = new DirectoryInfo(gamedirectory);
                                        byte[] moddate = UnicodeEncoding.ASCII.GetBytes("4|" + playerid + "|{00b3b3}Game Folder Created On: {ff0000}" + modefie.CreationTimeUtc + " {00b3b3}Last Modified On: {ff0000}" + modefie.LastWriteTimeUtc);
                                        stream.Write(moddate, 0, moddate.Length);
                                    }
                                }
                                Array.Clear(data, 0, data.Length);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("This server seems to be missing the server-sided system.");
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message.Replace((serverport + 55621).ToString(), serverport.ToString()));
                    Console.WriteLine(e.StackTrace);
                    Console.ReadLine();
                }
            }
            else
            {
                if(!found)
                {
                    Console.WriteLine("GTA isn't running, is it?");
                }
                Console.ReadLine();
            }
        }
        static private void Bumb_Server(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                byte[] writer = UnicodeEncoding.ASCII.GetBytes("0|");
                tcpclient.GetStream().Write(writer, 0, writer.Length);
            }
            catch
            {
                if(tcpclient.Connected)
                {
                    tcpclient.GetStream().Close();
                    tcpclient.GetStream().Dispose();
                    tcpclient.Close();
                }
            }
        }
        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (var stream = client.OpenRead("http://www.google.com"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
    class Query
    {
        Socket qSocket;
        IPAddress address;
        int _port = 0;

        string[] results;
        int _count = 0;

        DateTime[] timestamp = new DateTime[2];

        public Query(string IP, int port)
        {
            qSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            qSocket.SendTimeout = 5000;
            qSocket.ReceiveTimeout = 5000;

            try
            {
                address = Dns.GetHostAddresses(IP)[0];
            }

            catch
            {

            }

            _port = port;
        }

        public bool Send(char opcode)
        {
            try
            {
                EndPoint endpoint = new IPEndPoint(address, _port);

                using (MemoryStream stream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write("SAMP".ToCharArray());

                        string[] SplitIP = address.ToString().Split('.');

                        writer.Write(Convert.ToByte(Convert.ToInt32(SplitIP[0])));
                        writer.Write(Convert.ToByte(Convert.ToInt32(SplitIP[1])));
                        writer.Write(Convert.ToByte(Convert.ToInt32(SplitIP[2])));
                        writer.Write(Convert.ToByte(Convert.ToInt32(SplitIP[3])));

                        writer.Write((ushort)_port);

                        writer.Write(opcode);

                        if (opcode == 'p')
                            writer.Write("8493".ToCharArray());

                        timestamp[0] = DateTime.Now;
                    }

                    if (qSocket.SendTo(stream.ToArray(), endpoint) > 0)
                        return true;
                }
            }

            catch
            {
                return false;
            }

            return false;
        }

        public int Receive()
        {
            try
            {
                _count = 0;

                EndPoint endpoint = new IPEndPoint(address, _port);

                byte[] rBuffer = new byte[3402];
                qSocket.ReceiveFrom(rBuffer, ref endpoint);

                timestamp[1] = DateTime.Now;

                using (MemoryStream stream = new MemoryStream(rBuffer))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        if (stream.Length <= 10)
                            return _count;

                        reader.ReadBytes(10);

                        switch (reader.ReadChar())
                        {
                            case 'i': // Information
                                {
                                    results = new string[6];

                                    results[_count++] = Convert.ToString(reader.ReadByte());

                                    results[_count++] = Convert.ToString(reader.ReadInt16());

                                    results[_count++] = Convert.ToString(reader.ReadInt16());

                                    int hostnamelen = reader.ReadInt32();
                                    results[_count++] = new string(reader.ReadChars(hostnamelen));

                                    int gamemodelen = reader.ReadInt32();
                                    results[_count++] = new string(reader.ReadChars(gamemodelen));

                                    int mapnamelen = reader.ReadInt32();
                                    results[_count++] = new string(reader.ReadChars(mapnamelen));

                                    return _count;
                                }

                            case 'r': // Rules
                                {
                                    int rulecount = reader.ReadInt16();

                                    results = new string[rulecount * 2];

                                    for (int i = 0; i < rulecount; i++)
                                    {
                                        int rulelen = reader.ReadByte();
                                        results[_count++] = new string(reader.ReadChars(rulelen));

                                        int valuelen = reader.ReadByte();
                                        results[_count++] = new string(reader.ReadChars(valuelen));
                                    }

                                    return _count;
                                }

                            case 'c': // Client list
                                {
                                    int playercount = reader.ReadInt16();

                                    results = new string[playercount * 2];

                                    for (int i = 0; i < playercount; i++)
                                    {
                                        int namelen = reader.ReadByte();
                                        results[_count++] = new string(reader.ReadChars(namelen));

                                        results[_count++] = Convert.ToString(reader.ReadInt32());
                                    }

                                    return _count;
                                }

                            case 'd': // Detailed player information
                                {
                                    int playercount = reader.ReadInt16();

                                    results = new string[playercount * 4];

                                    for (int i = 0; i < playercount; i++)
                                    {
                                        results[_count++] = Convert.ToString(reader.ReadByte());

                                        int namelen = reader.ReadByte();
                                        results[_count++] = new string(reader.ReadChars(namelen));

                                        results[_count++] = Convert.ToString(reader.ReadInt32());
                                        results[_count++] = Convert.ToString(reader.ReadInt32());
                                    }

                                    return _count;
                                }

                            case 'p': // Ping
                                {
                                    results = new string[1];

                                    results[_count++] = timestamp[1].Subtract(timestamp[0]).Milliseconds.ToString();

                                    return _count;
                                }

                            default:
                                return _count;
                        }
                    }
                }
            }

            catch
            {
                return _count;
            }
        }

        public string[] Store(int count)
        {
            string[] rString = new string[count];

            for (int i = 0; i < count && i < _count; i++)
                rString[i] = results[i];

            _count = 0;

            return rString;
        }
    }
}
