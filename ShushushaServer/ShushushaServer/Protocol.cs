using System.Collections.Generic;
using System.Text.Json;

namespace ShushushaServer
{
    public class JsonPacket
    {
        public MsgId MsgId { get; set; }
        public JsonElement Data { get; set; }
    }

    public enum MsgId
    {
        create_room_c2s = 1000,
        create_room_s2c = 1001,
        join_room_c2s = 1002, //这个是自己加入房间的请求
        join_room_s2c = 1003,
        ready_c2s = 1005,
        ready_s2c = 1006,
        game_start_s2c = 1007,
        game_start_c2s = 1008,
        hide_indicator_c2s = 1009,
        hide_indicator_s2c = 1010,
        JoinRoom = 2000, //这个是其他人加入房间时服务端主动发的
        GameStart = 2001,
        PlayerLeft = 2002,
        Ready = 2003,
        ChangeStage = 2004,
        HideIndicator = 2005,
    }

    public class create_room_s2c
    {
        public ResCode ResCode { get; set; }
        public int RoomId { get; set; }
    }

    public class create_room_c2s
    {
        public Player Player { get; set; } = null!;
    }

    public class join_room_s2c
    {
        public ResCode ResCode { get; set; }
        public int RoomId { get; set; }
        public List<Player> Players { get; set; } = new();
    }

    public class join_room_c2s
    {
        public int RoomId { get; set; }
        public Player Player { get; set; } = null!;
    }

    public class ready_c2s
    {
        public int RoomId { get; set; }
        public int IdInRoom { get; set; }
    }

    public class ready_s2c
    {
        public ResCode ResCode { get; set; }
    }

    public class game_start_c2s
    {
        public int RoomId { get; set; }
    }

    public class game_start_s2c
    {
        public ResCode ResCode { get; set; }
    }

    public class hide_indicator_c2s
    {
        public int RoomId { get; set; }
        public int IdInRoom { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class hide_indicator_s2c
    {
        public ResCode ResCode { get; set; }
    }


    public class JoinRoom
    {
        public Player Player { get; set; } = null!;
    }

    public class GameStart
    {
        public Player Mouse { get; set; } = null!;
        public Player SharkKing { get; set; } = null!;
    }


    public class PlayerLeft
    {
        public Player Player { get; set; } = null!;
    }

    public class Ready
    {
        public Player Player { get; set; } = null!;
    }

    public class ChangeStage
    {
        public int Round { get; set; }
        public GameStage Stage { get; set; }
    }

    public class HideIndicator
    {
        public int IdInRoom { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }


    public class Player
    {
        public int Uid { get; set; }
        public int IdInRoom { get; set; }

        public bool Ready { get; set; }
    }

    public enum ResCode
    {
        Success = 0,
        CantFindRoom = 1,
        RoomIsFull = 2,
        AlreadyInRoom = 3,
        GameAlreadyStarted = 4,
        NotRoomOwner = 5,
        NotEnoughPlayers = 6,
        NotAllPlayersReady = 7,
        InvalidRoomState = 8
    }

    public enum GameStage
    {
        None = 0,
        Hide = 1,
        Kill = 2
    }
}
