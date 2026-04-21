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
        private Window _tipWindow;
        private UI_Tip _tipView;

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
                case ResCode.AlreadyInRoom:
                    OnJoinRoomFail("已经在房间中");
                    Network.Disconnect();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async UniTaskVoid OnReady()
        {
            var msgData = await Request.Ready();
            if (msgData.ResCode != ResCode.Success)
            {
                OnReadyFail("准备失败");
            }
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
            m_waiting.text = " ";
            ShowTip(tip);
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
            m_waiting.text = " ";
            ShowTip(tip);
            Debug.LogError(tip);
        }

        private void OnReadyFail(string tip)
        {
            ShowTip(tip);
            Debug.LogError(tip);
        }

        private void ShowTip(string tip)
        {
            if (_tipWindow == null)
            {
                _tipView = UI_Tip.CreateInstance();
                _tipView.m_close.onClick.Set(CloseTipWindow);
                _tipView.m_confirm.onClick.Set(CloseTipWindow);

                _tipWindow = new Window
                {
                    modal = true,
                    contentPane = _tipView
                };
            }

            _tipView.m_content.text = tip;
            _tipWindow.CenterOn(GRoot.inst, true);
            _tipWindow.Show();
        }

        private void CloseTipWindow()
        {
            if (_tipWindow == null)
            {
                return;
            }

            _tipWindow.Hide();
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

        public void OnReady(Ready msgData)
        {
            SetMemberReady(msgData.Player.IdInRoom);
        }

        private void SetMemberReady(int idInRoom)
        {
            var memberIndex = FindMemberIndex(idInRoom);
            if (memberIndex < 0)
            {
                Debug.LogWarning($"玩家准备，但未找到大厅头像: {idInRoom}");
                return;
            }

            var member = (UI_shark_avatar_lobby)m_memberlist.GetChildAt(memberIndex);
            member.m_准备.visible = true;
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
