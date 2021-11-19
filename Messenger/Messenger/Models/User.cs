﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;

namespace Messenger.Models
{
    public class User : INotifyPropertyChanged
    {
        TcpClient client;
        bool _connectionEnded;
        public User()
        {
            _port = 14000;
            _iP = "127.0.0.1";
            _displayName = "Hadi";
            _connectionEnded = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region Fields
        private String _displayName;

        public String DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; OnPropertyChanged("DisplayName"); }
        }

        private String _iP;
        public String IP
        {
            get { return _iP; }
            set { _iP = value; OnPropertyChanged("IP"); }
        }


        private int _port;
        public int Port
        {
            get { return _port; }
            set { _port = value; OnPropertyChanged("Port"); }
        }

        private Message _message;

        public Message Message
        {
            get { return _message; }
            set { _message = value; OnPropertyChanged("Message"); }
        }
        

        private bool _showInvitationMessageBox;

        public bool ShowInvitationMessageBox
        {
            get { return _showInvitationMessageBox; }
            set { _showInvitationMessageBox = value; OnPropertyChanged("ShowInvitationMessageBox"); }
        }

        private bool _acceptRequest;

        public bool AcceptRequest
        {
            get { return _acceptRequest; }
            set { _acceptRequest = value; }
        }

        private bool _showSocketExceptionMessageBox;

        public bool ShowSocketExceptionMessageBox
        {
            get { return _showSocketExceptionMessageBox; }
            set 
            { 
                _showSocketExceptionMessageBox = value;
                Console.WriteLine("Showing SocketException MessageBox...");
                OnPropertyChanged("ShowSocketExceptionMessageBox"); 
            }
        }

        private bool _responseToRequest;

        public bool ResponseToRequest
        {
            get { return _responseToRequest; }
            set { _responseToRequest = value; OnPropertyChanged("ResponseToRequest"); }
        }
        #endregion

        private void OnPropertyChanged(string PropertyName ="")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
        }

        public void Listen()
        {
            Action action = () =>
            {
                TcpListener server = null;
                Console.WriteLine("Listen clicked...");
                try
                {
                    Int32 port = Port;
                    IPAddress localAddr = IPAddress.Parse(IP);

                    server = new TcpListener(localAddr, port);

                    server.Start();

                    Byte[] bytes = new Byte[256];
                    String data = null;

                    _connectionEnded = false;
                    // Enter the listening loop.
                    while (!_connectionEnded)
                    {
                        Console.Write(DisplayName.ToString() + " waiting for a connection... ");

                        client = server.AcceptTcpClient();
                        Console.WriteLine("Connected!");

                        data = null;
                        NetworkStream stream = client.GetStream();
                        int i;

                        // Loop to receive all the data sent by the client.
                        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                            Message Msg = JsonConvert.DeserializeObject<Message>(data);
                            if (Msg.RequestType == "Establish")
                            {
                               
                                ShowInvitationMessageBox = true;
                                
                                if (AcceptRequest)
                                {                                    
                                    // Send back a response.
                                    Message response = new Message("RequestAccepted", DisplayName, new DateTime(), "");
                                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(response));
                                    stream.Write(msg, 0, msg.Length);
                                }
                                else
                                {
                                    // Send back a response.
                                    Message response = new Message("RequestDenied", DisplayName, new DateTime(), " ");
                                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(response));
                                    stream.Write(msg, 0, msg.Length);
                                    client.Close();
                                    break;
                                }

                            }
                            else if (Msg.RequestType == "Chat")
                            {
                                Message = Msg;
                            }
                            else if(Msg.RequestType == "EndConnection")
                            {
                                Msg = new Message("Chat", Msg.Sender, new DateTime(), "Left the room.");
                                Message = Msg;
                                _connectionEnded = true;
                                client.Close();
                                break;
                            }
                        }
                    }
                }
                #region Exception
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }
                catch (ArgumentNullException e)
                {
                    Console.WriteLine("Insert a valid IP: {0}", e);
                }
                catch (FormatException e)
                {
                    Console.WriteLine("Insert a valid IP: {0}", e);
                }
                catch(System.IO.IOException e)
                {
                    Console.WriteLine("Connection ended: {0}", e);
                }
                finally
                {
                    // Stop listening for new clients.
                    Console.WriteLine("Done listening ...");
                    server.Stop();
                }
                #endregion
            };

            Task.Factory.StartNew(action);
        }

        public void Connect()
        {
            Message Msg = new Message("Establish", DisplayName, new DateTime(), "");
            string message = JsonConvert.SerializeObject(Msg);
            
            Action action = () =>
            {
                Console.WriteLine("Connect clicked...");
                try
                {
                    Int32 port = Port;
                    client = new TcpClient(IP, port);
                    Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                    NetworkStream stream = client.GetStream();
                    stream.Write(data, 0, data.Length);
                    data = new Byte[256];
                    String ResponseData = String.Empty;
                    Message ResponseObj = null;
                    Int32 bytes;

                    bytes = stream.Read(data, 0, data.Length);
                    ResponseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                    ResponseObj = JsonConvert.DeserializeObject<Message>(ResponseData);

                    if (ResponseObj.RequestType == "RequestDenied")
                    {
                        // Feedback here
                        ResponseToRequest = false;
                        // Close everything.
                        stream.Close();
                        client.Close();
                        _connectionEnded = true;
                    }
                    else
                    {
                        // Feedback here
                        ResponseToRequest = true;

                        _connectionEnded = false;

                        // Read the batch of the TcpServer response bytes.
                        while ((bytes = stream.Read(data, 0, data.Length)) != 0)
                        {
                            ResponseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                            ResponseObj = JsonConvert.DeserializeObject<Message>(ResponseData);
                            if (ResponseObj.RequestType == "Chat")
                            {
                                Message = ResponseObj;
                            }
                            else if (ResponseObj.RequestType == "EndConnection")
                            {
                                _connectionEnded = true;
                                client.Close();
                                ResponseObj = new Message("Chat", ResponseObj.Sender, new DateTime(), "Left the room.");
                                Message = ResponseObj;
                                Console.WriteLine("Done connecting...");
                                break;
                            }

                        }
                    }   
                }
                #region exception
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                    ShowSocketExceptionMessageBox = true;

                }
                catch (ArgumentNullException e)
                {
                    Console.WriteLine("ArgumentNullException: {0}", e);
                }
                catch (System.IO.IOException e)
                {
                    Console.WriteLine("Connection ended: {0}", e);
                }
                #endregion
            };

           Task.Factory.StartNew(action);
        }

        public void Chat(string message)
        {
            try
            {
                Message Msg = new Message("Chat", DisplayName, new DateTime(), message);
                message = JsonConvert.SerializeObject(Msg);

                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                NetworkStream stream = client.GetStream();

                stream.Write(data, 0, data.Length);
                Msg.Sender = "Me";
                Message = Msg;
            }
            #region Exception
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            #endregion
        }

        public void TearDownConnection()
        {
            try
            {
                if(client !=null && client.Connected)
                {
                    Message Msg = new Message("EndConnection", DisplayName, new DateTime(), "");
                    Byte[] data = System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(Msg));
                    NetworkStream stream = client.GetStream();
                    stream.Write(data, 0, data.Length);
                    client.Close();
                }
                _connectionEnded = true;
            }
            #region Exception
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            #endregion
        }
    }
}
