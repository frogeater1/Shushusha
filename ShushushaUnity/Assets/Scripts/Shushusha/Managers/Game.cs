using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ShushushaServer;
using UI;
using UnityEngine;
using Utils;
using Unity.Multiplayer.PlayMode;

public class Game : MonoSingletonBase<Game>
{
    public UI_Lobby uilobby;
    public UI_Main uiMain;

    public Player me;

    protected override void Awake()
    {
        base.Awake();
        UIBinder.BindAll();
        if (CurrentPlayer.Tags.Contains("Player1"))
        {
            me = new Player
            {
                Uid = 1,
            };
        }
        else
        {
            me = new Player
            {
                Uid = 2
            };
        }
    }

    public void Start()
    {
        Debug.Log("Start game");
    }

    private void Update()
    {
        Dispacher.Distribute();
    }


    public void JoinRoom(JoinRoom msgData)
    {
        uilobby.OnJoinRoom(msgData);
    }

    public void PlayerLeft(PlayerLeft msgData)
    {
        uilobby.OnPlayerLeft(msgData);
    }

    public void OnCreateRoomSuccess(create_room_s2c msgData)
    {
        me.IdInRoom = 0;
        me.Ready = false;
        uilobby.OnCreateRoomSuccess(msgData);
    }

    public void OnJoinRoomSuccess(join_room_s2c msgData)
    {
        me.IdInRoom = msgData.Players.First(x => x.Uid == me.Uid).IdInRoom;
        uilobby.OnJoinRoomSuccess(msgData);
    }

    public void GameStart(GameStart msgData)
    {
        uilobby.visible = false;
        uiMain.visible = true;
        uiMain.m_name.text = msgData.Mouse.Uid == me.Uid ? "我是鼠鼠" : "我是鲨鲨";
    }
}
