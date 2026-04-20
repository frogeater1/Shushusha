using System;
using System.Linq;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using FairyGUI;
using ShushushaServer;
using UnityEngine;

namespace UI
{
    public partial class UI_Lobby
    {
        partial void Init()
        {
            Game.Instance.uilobby = this;
            Debug.Log("lobby init");
            m_waiting.text = " ";
            m_创建.onClick.Set(() => OnCreate().Forget());
            m_加入.onClick.Set(() => { OnJoin().Forget(); });
            m_准备.onClick.Set(() => { OnReady().Forget(); });
            m_开始.onClick.Set(() => { OnGameStart().Forget(); });
        }


        private async UniTaskVoid OnCreate()
        {
            m_waiting.visible = true;
            m_waiting.text = "创建房间中...";
            var msgData = await Request.CreateRoom();
            Debug.Log(msgData.ResCode.ToString());
            switch (msgData.ResCode)
            {
                case ResCode.Success:
                    Game.Instance.OnCreateRoomSuccess(msgData);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async UniTaskVoid OnJoin()
        {
            var msgData = await Request.JoinRoom(int.Parse(m_房间号输入.text));
            switch (msgData.ResCode)
            {
                case ResCode.Success:
                    Game.Instance.OnJoinRoomSuccess(msgData);
                    break;
                case ResCode.CantFindRoom:
                    OnJoinRoomFail("找不到该房间");
                    Network.Disconnect();
                    break;
                case ResCode.RoomIsFull:
                    OnJoinRoomFail("房间已满");
                    Network.Disconnect();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async UniTaskVoid OnReady()
        {
            var msgData = await Request.Ready();
            var member = (UI_shark_avatar_lobby)m_memberlist.GetChildAt(msgData.Player.IdInRoom);
            member.m_准备.visible = true;
        }

        private async UniTaskVoid OnGameStart()
        {
            var msgData = await Request.GameStart();
            if (msgData.ResCode == ResCode.Success)
            {
            }
        }


        public void OnCreateRoomSuccess(create_room_s2c msgData)
        {
            m_waiting.text = " ";
            m_房间号.text = msgData.RoomId.ToString("0000");
            m_Page.selectedIndex = Page.房间;

            m_开始.visible = true;

            var member = (UI_shark_avatar_lobby)m_memberlist.AddItemFromPool();
            member.icon = UIPackage.GetItemURL("UI", "玩家头像");
            member.m_准备.visible = false;
            member.m_id_in_room.text = "0";
        }

        public void OnCreateRoomFail(string tip)
        {
            m_waiting.text = "创建房间失败";
            Debug.LogError(tip);
        }

        public void OnJoinRoomSuccess(join_room_s2c msgData)
        {
            m_waiting.text = " ";
            m_房间号.text = msgData.RoomId.ToString("0000");
            m_Page.selectedIndex = Page.房间;

            m_开始.visible = false;

            foreach (var player in msgData.Players.OrderBy(x => x.IdInRoom))
            {
                var member = (UI_shark_avatar_lobby)m_memberlist.AddItemFromPool();
                member.icon = UIPackage.GetItemURL("UI", "玩家头像");
                member.m_准备.visible = false;
                member.m_id_in_room.text = player.IdInRoom.ToString();
            }
        }

        public void OnJoinRoomFail(string tip)
        {
            m_waiting.text = tip;
            Debug.LogError(tip);
        }

        public void OnJoinRoom(JoinRoom msgData)
        {
            var member = (UI_shark_avatar_lobby)m_memberlist.AddItemFromPool();
            member.icon = UIPackage.GetItemURL("UI", "玩家头像");
            member.m_准备.visible = false;
            member.m_id_in_room.text = msgData.Player.IdInRoom.ToString();
        }

        public void OnPlayerLeft(PlayerLeft msgData)
        {
            var memberIndex = FindMemberIndex(msgData.Player.IdInRoom);
            if (memberIndex < 0)
            {
                Debug.LogWarning($"玩家离开，但未找到大厅头像: {msgData.Player.IdInRoom}");
                return;
            }

            m_memberlist.RemoveChildToPoolAt(memberIndex);
        }

        private int FindMemberIndex(int idInRoom)
        {
            for (var i = 0; i < m_memberlist.numChildren; i++)
            {
                var member = (UI_shark_avatar_lobby)m_memberlist.GetChildAt(i);
                if (int.TryParse(member.m_id_in_room.text, out var memberIdInRoom) && memberIdInRoom == idInRoom)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
