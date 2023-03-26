using BigeumTalkClient.Pages;
using BigeumTalkClient;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Windows.Navigation;
using System.IO;

namespace BigeumTalkClient.Network
{
    public static class Global
    {
        public static string nickName = "";
    }

    public class Session
    {
        public Socket socket = null;
        public const int BUFFER_SIZE = 0x1000;
        public byte[] buffer = new byte[BUFFER_SIZE];
        public IPEndPoint endPoint = null;
        public ulong userId = 0;
        public int tryCount = 0;
        public string nickname = "";
        public ulong currentRoomId = 0;
    }

    public class SocketNetwork
    {
        private static Session session;
        public SocketNetwork()
        {
            session = GetSession();

            Connect();
        }

        public static Session GetSession()
        {
            if(session == null)
            {
                session = new Session();
            }
            return session;
        }

        private void Connect()
        {
            if (GetSession().socket != null && GetSession().socket.Connected == true)
            {
                return;
            }
            session.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            session.endPoint = new IPEndPoint(IPAddress.Parse(MainWindow.mainWindow.settings.IP), MainWindow.mainWindow.settings.PORT);
            session.socket.Blocking = false;

            session.socket.BeginConnect(session.endPoint, new AsyncCallback(OnConnect), session);

        }
        private void OnConnect(IAsyncResult ar)
        {
            Session session = (Session)ar.AsyncState;
            Socket socket = session.socket;
            session.tryCount++;

            try
            {
                if (socket.Connected)
                {
                    socket.BeginReceive(session.buffer, 0, Session.BUFFER_SIZE, 0, OnRecv, session);
                    Dispatcher(new Action(delegate
                    {
                        LoginPage.loginPage.SigninError.Visibility = Visibility.Hidden;
                        LoginPage.loginPage.SigninButton.IsEnabled = true;
                        LoginPage.loginPage.NicknameText.IsEnabled = true;
                    }));
                }
                else
                {
                    if(session.tryCount < 10)
                    {
                        Dispatcher(new Action(delegate
                        {
                            LoginPage.loginPage.SigninError.Text = "서버와 연결되지 않았습니다. 다시 시도합니다. (" + session.tryCount.ToString() + ")";
                            LoginPage.loginPage.SigninError.Visibility = Visibility.Visible;
                        }));
                        session.socket.BeginConnect(session.endPoint, new AsyncCallback(OnConnect), session);
                    }
                    else
                    {
                        Dispatcher(new Action(delegate
                        {
                            LoginPage.loginPage.SigninError.Text = "서버가 응답하지 않습니다.";
                            LoginPage.loginPage.SigninError.Visibility = Visibility.Visible;
                        }));
                    }

                }
            }
            catch (Exception) { }
        }

        private void OnRecv(IAsyncResult ar)
        {
            Session session = (Session)ar.AsyncState;
            Socket socket = session.socket;

            int numOfBytes = 0;
            try
            {
                numOfBytes = socket.EndReceive(ar);
            }
            catch (Exception) { }

            
            
            if(numOfBytes > 0)
            {
                int headerSize = Marshal.SizeOf(new PacketHeader());
                if (numOfBytes <= headerSize)
                {
                    goto Register;
                }

                PacketHeader header = new PacketHeader();
                header.ParseByte(session.buffer);

                if(numOfBytes < header.size)
                {
                    goto Register;
                }

                HandlePacket(header.id, session.buffer.Skip(headerSize).ToArray(), (ushort)(header.size - headerSize));
            }
            else
            {
                Dispatcher(new Action(delegate
                {
                    MainWindow.mainWindow.fr.Content = LoginPage.loginPage;

                    LoginPage.loginPage.SigninError.Text = "서버가 연결이 끊겼습니다.";
                    LoginPage.loginPage.SigninError.Visibility = Visibility.Visible;
                }));
            }

            Register:
            socket.BeginReceive(session.buffer, 0, Session.BUFFER_SIZE, 0, OnRecv, session);
        }

        /* Handle Packet */

        private void HandlePacket(PROTOCOL protocol, byte[] buffer, ushort numOfBytes)
        {
            switch (protocol)
            {
                case PROTOCOL.S_LOGIN:
                    Handle_S_LOGIN(buffer, numOfBytes);
                    break;
                case PROTOCOL.S_ENTER_ROOM:
                    Handle_S_ENTER_ROOM(buffer, numOfBytes);
                    break;
                case PROTOCOL.S_CHAT:
                    Handle_S_CHAT(buffer, numOfBytes); 
                    break;
                case PROTOCOL.S_OTHER_ENTER:
                    Handle_S_OTHER_ENTER(buffer, numOfBytes);
                    break;
                case PROTOCOL.S_OTHER_LEAVE:
                    Handle_S_OTHER_LEAVE(buffer, numOfBytes);
                    break;
                default:
                    break;
            }
        }

        private void Handle_S_LOGIN(byte[] buffer, ushort numOfBytes)
        {
            PKT_S_LOGIN pkt = Deserialize<PKT_S_LOGIN>(buffer, numOfBytes);

            if((RESULTCODE)pkt.resultCode == RESULTCODE.LOGIN_SUCCESS)
            {
                session.userId = pkt.userId;

                Dispatcher(new Action(delegate
                {
                    session.nickname = LoginPage.loginPage.NicknameText.Text;

                    if (ChatPage.chatPage == null)
                    {
                        ChatPage.chatPage = new ChatPage();
                    }
                    MainWindow.mainWindow.fr.Content = ChatPage.chatPage;
                    ChatPage.chatPage.UserNickname.Text = session.nickname;
                }));
            }
            else
            {
                Dispatcher(new Action(delegate
                {
                    LoginPage.loginPage.SigninError.Text = "이미 사용중인 닉네임입니다.";
                    LoginPage.loginPage.SigninError.Visibility = Visibility.Visible;
                }));
            }
        }

        private void Handle_S_ENTER_ROOM(byte[] buffer, ushort numOfBytes)
        {
            PKT_S_ENTER_ROOM pkt = Deserialize<PKT_S_ENTER_ROOM>(buffer, numOfBytes);
            
            if ((RESULTCODE)pkt.resultCode == RESULTCODE.ENTER_ROOM_SUCCESS)
            {
                session.currentRoomId = pkt.roomId;
                Dispatcher(new Action(delegate
                {
                    ChatPage.chatPage.RoomNameBlock.Text = "#" + pkt.roomName.ToString();
                    
                    pkt.users.ForEach(user => ChatPage.chatPage.usersList.Add(user));
                }));
            }
            else
            {
                session.currentRoomId = 0;
                Dispatcher(new Action(delegate
                {
                    ChatPage.chatPage.RoomNameBlock.Text = "채팅방 연결에 실패했습니다.";
                }));
            }
        }

        private void Handle_S_CHAT(byte[] buffer, ushort numOfBytes)
        {
            PKT_S_CHAT pkt = Deserialize<PKT_S_CHAT>(buffer, numOfBytes);

            Dispatcher(new Action(delegate
            {
                if ((RESULTCODE)pkt.resultCode == RESULTCODE.CHAT_OK)
                {
                    bool isContinuous = false;
                    if (ChatPage.chatPage.chats._collection.Count > 0)
                    {
                        isContinuous = ChatPage.chatPage.chats._collection.Last().IsContinuous(pkt.nickname);
                    }
                    ChatPage.chatPage.chats.Add(new Chat(pkt.nickname, pkt.userId, pkt.message, isContinuous, pkt.serverMessage, pkt.timestamp));
                }
                else
                {
                    ChatPage.chatPage.chats.Add(new Chat("ERROR", 0, "서버와 연결이 불안정합니다.", false, true, 0));
                }
            }));
        }

        private void Handle_S_OTHER_ENTER(byte[] buffer, ushort numOfBytes)
        {
            PKT_S_OTHER_ENTER pkt = Deserialize<PKT_S_OTHER_ENTER>(buffer, numOfBytes);

            if (pkt.user.nickname == session.nickname)
            {
                return;
            }

            Dispatcher(new Action(delegate
            {
                ChatPage.chatPage.usersList.Add(pkt.user);
                ChatPage.chatPage.chats.Add(new Chat("SERVER", 0, pkt.user.nickname + "님이 입장했습니다.",false, true, pkt.timestamp));
            }));
        }

        private void Handle_S_OTHER_LEAVE(byte[] buffer, ushort numOfBytes)
        {
            PKT_S_OTHER_LEAVE pkt = Deserialize<PKT_S_OTHER_LEAVE>(buffer, numOfBytes);

            Dispatcher(new Action(delegate
            {
                ChatPage.chatPage.usersList.Remove(ChatPage.chatPage.usersList.Where(i => i.userId == pkt.user.userId).Single());
                ChatPage.chatPage.chats.Add(new Chat("SERVER", 0, pkt.user.nickname + "님이 퇴장했습니다.",false, true, pkt.timestamp));
            }));
        }

        /* Send Packet */

        public void Send_C_LOGIN()
        {
            if(session.socket == null)
            {
                return;
            }

            if(session.socket.Connected == false)
            {
                Connect();
            }
            PKT_C_LOGIN pkt = new PKT_C_LOGIN();
            pkt.nickname = LoginPage.loginPage.NicknameText.Text.Trim();

            byte[] pktBodt = Serialize(pkt);

            PacketHeader header = new PacketHeader();
            header.id = PROTOCOL.C_LOGIN;
            header.size = (ushort)(Marshal.SizeOf(new PacketHeader()) + pktBodt.Length);
            
            byte[] data = GetBytes(header).Concat(pktBodt).ToArray();

            session.socket.Send(data);

            Dispatcher(new Action(delegate
            {
                LoginPage.loginPage.SigninButton.IsEnabled = true;
                LoginPage.loginPage.NicknameText.IsEnabled = true;
            }));
            
        }

        public void Send_C_ENTER_ROOM()
        {
            if (session.socket == null || session.socket.Connected == false)
            {
                return;
            }

            PKT_C_ENTER_ROOM pkt = new PKT_C_ENTER_ROOM();
            pkt.userId = session.userId;
            pkt.roomId = 1;

            byte[] pktBodt = Serialize(pkt);
            PacketHeader header = new PacketHeader();
            header.id = PROTOCOL.C_ENTER_ROOM;
            header.size = (ushort)(Marshal.SizeOf(new PacketHeader()) + pktBodt.Length);

            byte[] data = GetBytes(header).Concat(pktBodt).ToArray();

            session.socket.Send(data);
        }

        public void Send_C_CHAT(string message)
        {
            if (session.socket == null || session.socket.Connected == false)
            {
                return;
            }

            PKT_C_CHAT pkt = new PKT_C_CHAT();
            pkt.userId = session.userId;
            pkt.nickname = session.nickname;
            pkt.message = message;

            byte[] pktBodt = Serialize(pkt);
            PacketHeader header = new PacketHeader();
            header.id = PROTOCOL.C_CHAT;
            header.size = (ushort)(Marshal.SizeOf(new PacketHeader()) + pktBodt.Length);

            byte[] data = GetBytes(header).Concat(pktBodt).ToArray();

            session.socket.Send(data);
        }


        /* Utils */
        private byte[] GetBytes(object obj)
        { 
            int size = Marshal.SizeOf(obj);
            byte[] bytes = new byte[size];

            IntPtr ptr = IntPtr.Zero;

            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(obj, ptr, true);
                Marshal.Copy(ptr, bytes, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return bytes;
        }

        private T? Deserialize<T>(byte[]buffer, ushort numOfByte)
        {
            string body = Encoding.Unicode.GetString(buffer.Take(numOfByte).ToArray());

            return JsonConvert.DeserializeObject<T>(body);
            // TODO 예외 처리
        }

        private byte[] Serialize(object obj)
        {
            string body = JsonConvert.SerializeObject(obj);
            return Encoding.Unicode.GetBytes(body);
        }

        private void Dispatcher(Action action)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
        }
    }


}
