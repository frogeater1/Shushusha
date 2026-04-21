using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Cysharp.Threading.Tasks;
using ShushushaServer;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Utils;
using Unity.Multiplayer.PlayMode;

public class Game : MonoSingletonBase<Game>
{
    public UI_Lobby uilobby;
    public UI_Main uiMain;

    public Player me;
    public PlayerIdentity Identity { get; private set; } = PlayerIdentity.None;
    public GameStage CurrentStage { get; private set; } = GameStage.None;

    public GameObject 指示物Prefab;
    private GameObject indicatorInstance;

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
        else if (CurrentPlayer.Tags.Contains("Player2"))
        {
            me = new Player
            {
                Uid = 2
            };
        }
        else
        {
            me = new Player
            {
                Uid = 3
            };
        }
    }

    public void Start()
    {
        Debug.Log("Start game");
    }

    private void Update()
    {
        Dispatcher.Distribute();
        TryPlaceHideStageIndicator();
    }


    public void JoinRoom(JoinRoom msgData)
    {
        uilobby.OnJoinRoom(msgData);
    }

    public void PlayerLeft(PlayerLeft msgData)
    {
        uilobby.OnPlayerLeft(msgData);
    }

    public void Ready(Ready msgData)
    {
        uilobby.OnReady(msgData);
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
        if (msgData.Mouse.Uid == me.Uid)
        {
            Identity = PlayerIdentity.Mouse;
        }
        else if (msgData.SharkKing.Uid == me.Uid)
        {
            Identity = PlayerIdentity.SharkKing;
        }
        else
        {
            Identity = PlayerIdentity.Shark;
        }

        uilobby.visible = false;
        uiMain.visible = true;
        uiMain.m_name.text = Identity switch
        {
            PlayerIdentity.Mouse => "我是鼠鼠",
            PlayerIdentity.SharkKing => "我是鲨王",
            _ => "我是鲨鲨"
        };
        uiMain.m_确定.visible = Identity is PlayerIdentity.Shark or PlayerIdentity.SharkKing;
        uiMain.m_技能.visible = Identity is PlayerIdentity.SharkKing;
        uiMain.m_确定.onClick.Set(() => SendIndicatorPosition().Forget());
    }

    public void ChangeStage(ChangeStage msgData)
    {
        CurrentStage = msgData.Stage;
        uiMain.m_round.SetVar("count", msgData.Round.ToString()).FlushVars();
        uiMain.m_stage.text = $"{msgData.Stage}阶段";

        switch (msgData.Stage)
        {
            case GameStage.Hide:
                if (Identity is PlayerIdentity.Shark or PlayerIdentity.SharkKing)
                {
                    uiMain.m_确定.enabled = true;
                }

                if (Identity == PlayerIdentity.Mouse)
                {
                    HideNonUiElements();
                    BlackoutGameCamera();
                }

                break;
        }
    }

    private void TryPlaceHideStageIndicator()
    {
        if (CurrentStage != GameStage.Hide || Identity == PlayerIdentity.Mouse)
        {
            return;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || IsPointerOnUi())
        {
            return;
        }

        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        var ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!TryGetSceneClickPoint(ray, out var point))
        {
            return;
        }

        if (indicatorInstance != null)
        {
            Destroy(indicatorInstance);
        }

        if (指示物Prefab == null)
        {
            Debug.LogWarning("未设置指示物Prefab");
            return;
        }

        indicatorInstance = Instantiate(指示物Prefab, point, Quaternion.identity);
    }

    private async UniTaskVoid SendIndicatorPosition()
    {
        if (CurrentStage != GameStage.Hide || Identity == PlayerIdentity.Mouse)
        {
            return;
        }

        if (indicatorInstance == null)
        {
            Debug.LogWarning("尚未放置指示物");
            return;
        }

        var msgData = await Request.HideIndicator(indicatorInstance.transform.position);
        if (msgData.ResCode != ResCode.Success)
        {
            Debug.LogWarning($"发送指示物坐标失败: {msgData.ResCode}");
            return;
        }

        uiMain.m_确定.enabled = false;
    }

    public void HideIndicator(HideIndicator msgData)
    {
        if (msgData.IdInRoom == me.IdInRoom)
        {
            return;
        }

        if (指示物Prefab == null)
        {
            Debug.LogWarning("未设置指示物Prefab");
            return;
        }

        Instantiate(指示物Prefab, new Vector3(msgData.X, msgData.Y, msgData.Z), Quaternion.identity);
    }

    private static bool IsPointerOnUi()
    {
        var touchTarget = FairyGUI.GRoot.inst.touchTarget;
        return touchTarget != null && touchTarget != FairyGUI.GRoot.inst;
    }

    private bool TryGetSceneClickPoint(Ray ray, out Vector3 point)
    {
        foreach (var hit in Physics.RaycastAll(ray).OrderBy(x => x.distance))
        {
            if (indicatorInstance != null && hit.transform.IsChildOf(indicatorInstance.transform))
            {
                continue;
            }

            if (IsUiObject(hit.collider.gameObject))
            {
                continue;
            }

            point = hit.point;
            return true;
        }

        point = default;
        return false;
    }

    private void HideNonUiElements()
    {
        foreach (var renderer in FindObjectsOfType<Renderer>(true))
        {
            if (IsUiObject(renderer.gameObject))
            {
                continue;
            }

            renderer.enabled = false;
        }

        foreach (var light in FindObjectsOfType<Light>(true))
        {
            if (IsUiObject(light.gameObject))
            {
                continue;
            }

            light.enabled = false;
        }
    }

    private static void BlackoutGameCamera()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = Color.black;
        mainCamera.cullingMask = 0;
    }

    private bool IsUiObject(GameObject target)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (!target.transform.IsChildOf(root.transform))
            {
                continue;
            }

            return root == gameObject || root.GetComponent<FairyGUI.UIPanel>() != null ||
                   root.GetComponent<FairyGUI.StageCamera>() != null;
        }

        return false;
    }
}

public enum PlayerIdentity
{
    None = 0,
    Mouse = 1,
    Shark = 2,
    SharkKing = 3
}
