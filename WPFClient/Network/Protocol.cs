using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BigeumTalkClient.Pages;

namespace BigeumTalkClient.Network
{
    public enum PROTOCOL : ushort
    {
        C_VERSION_CHECK,
        S_VERSION_CHECK,
        C_LOGIN,
        S_LOGIN,
        C_ENTER_ROOM,
        S_ENTER_ROOM,
        C_LEAVE_ROOM,
        S_LEAVE_ROOM,
        C_CHAT,
        S_CHAT,
        S_OTHER_ENTER,
        S_OTHER_LEAVE
    }

    public enum RESULTCODE : ushort
    {
        LOGIN_EXIST = 2000,
        LOGIN_SUCCESS,
        ENTER_ROOM_FAIL,
        ENTER_ROOM_SUCCESS,
        CHAT_FAIL,
        CHAT_OK,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1), Serializable]
    struct PacketHeader
    {
        public ushort size;
        public PROTOCOL id;

        internal void ParseByte(byte[] bytes)
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            PacketHeader header = (PacketHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketHeader));
            handle.Free();
            this = header;
        }
    }

    public class Chat
    {
        public string nickname { get; set; }
        public ulong userId { get; set; }
        public string message { get; set; }
        public bool continuous { get; set; }
        public bool serverMessage { get; set; }
        public DateTime dateTime { get; set; }

        public Chat(string nickname, ulong userId, string message, bool continuous, bool serverMessage, ulong dateTime)
        {
            this.nickname = nickname;
            this.userId = userId;
            this.message = message;
            this.continuous = continuous;
            this.serverMessage = serverMessage;

            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            this.dateTime = dt.AddSeconds((double)dateTime).ToLocalTime();
        }

        public bool IsContinuous(string name)
        {
            return nickname == name;
        }
    }

    public class User
    {
        public string nickname { get; set; }
        public ulong userId { get; set; }

        public User(string nickname, ulong userId)
        {
            this.nickname = nickname;
            this.userId = userId;
        }
    }

    public class PKT_C_LOGIN
    {
        public string nickname { get; set; }
    }

    public class PKT_S_LOGIN
    {
        public ushort resultCode { get; set; }
        public ulong userId { get; set; }
    }

    public class PKT_C_ENTER_ROOM
    {
        public ulong userId { get; set; }
        public ushort roomId { get; set; }
    }

    public class PKT_S_ENTER_ROOM
    {
        public ushort resultCode { get; set; }
        public ushort roomId { get; set; }
        public string roomName { get; set; }
        public List<User> users { get; set; }
    }

    public class PKT_C_CHAT
    {
        public ulong userId { get; set; }
        public string nickname { get; set; }
        public string message { get; set; }
    }

    public class PKT_S_CHAT
    {
        public ushort resultCode { get; set; }
        public bool serverMessage { get; set; }
        public ulong userId { get; set; }
        public string nickname { get; set; }
        public string message { get; set; }
        public ulong timestamp { get; set; }
    }

    public class PKT_S_OTHER_ENTER
    {
        public User user { get; set; }
        public ulong timestamp { get; set; }
    };

    public class PKT_S_OTHER_LEAVE
    {
        public User user { get; set; }
        public ulong timestamp { get; set; }
    };

}
