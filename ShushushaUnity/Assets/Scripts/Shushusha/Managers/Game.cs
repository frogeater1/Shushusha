using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    public int CurrentFloor { get; private set; } = 1;
    public int Magic { get; private set; }

    public List<GameObject> 指示物列表 = new();
    private GameObject selectedIndicator;
    private CancellationTokenSource stageCountdownCts;

    private const int StageCountdownSeconds = 30;

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


    public void OnJoinRoom(JoinRoom msgData)
    {
        uilobby.OnJoinRoom(msgData);
    }

    public void OnPlayerLeft(PlayerLeft msgData)
    {
        uilobby.OnPlayerLeft(msgData);
    }

    public void OnReady(Ready msgData)
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
        var player = msgData.Players.First(x => x.Uid == me.Uid);
        me.IdInRoom = player.IdInRoom;
        me.Ready = false;
        uilobby.OnJoinRoomSuccess(msgData);
    }

    public void OnGameStart(GameStart msgData)
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
        uiMain.m_确定.visible = false;
        uiMain.m_技能.visible = Identity is PlayerIdentity.SharkKing;
        SetMagicVisible(Identity == PlayerIdentity.Mouse);
        SetMagic(0);
        SetFloor(1);
    }

    public void OnChangeStage(ChangeStage msgData)
    {
        CurrentStage = msgData.Stage;
        SetFloor(msgData.CurrentFloor);
        SetMagic(msgData.Magic);
        uiMain.m_round.SetVar("count", msgData.Round.ToString()).FlushVars();
        uiMain.m_stage.text = $"{msgData.Stage}阶段";
        CancelStageCountdown();

        switch (msgData.Stage)
        {
            case GameStage.Hide:
                StartStageCountdown().Forget();

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
            case GameStage.Kill:
                StartStageCountdown().Forget();
                break;
            default:
                uiMain.m_倒计时.text = string.Empty;
                break;
        }
    }

    private void SetFloor(int currentFloor)
    {
        CurrentFloor = currentFloor;
        uiMain.m_floor.SetVar("count", CurrentFloor.ToString()).FlushVars();
    }

    private void SetMagicVisible(bool visible)
    {
        uiMain.m_魔力txt.visible = visible;
        uiMain.m_魔力值.visible = visible;
    }

    private void SetMagic(int magic)
    {
        Magic = magic;
        if (Identity != PlayerIdentity.Mouse)
        {
            return;
        }

        var controller = uiMain.m_魔力值.m_魔力值;
        if (controller.pageCount <= 0)
        {
            return;
        }

        controller.selectedIndex = Mathf.Clamp(Magic, 0, controller.pageCount - 1);
    }

    private async UniTaskVoid StartStageCountdown()
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        stageCountdownCts = cts;

        try
        {
            for (var remaining = StageCountdownSeconds; remaining >= 0; remaining--)
            {
                uiMain.m_倒计时.text = remaining.ToString();

                if (remaining == 0)
                {
                    break;
                }

                await UniTask.Delay(1000, cancellationToken: cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (stageCountdownCts == cts)
            {
                stageCountdownCts = null;
            }

            cts.Dispose();
        }
    }

    private void CancelStageCountdown()
    {
        stageCountdownCts?.Cancel();
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
        if (!TryGetIndicator(ray, out var indicator))
        {
            return;
        }

        ChangeIndicator(indicator).Forget();
    }

    private async UniTaskVoid ChangeIndicator(GameObject indicator)
    {
        if (CurrentStage != GameStage.Hide || Identity == PlayerIdentity.Mouse)
        {
            return;
        }

        selectedIndicator = indicator;
        var indicatorId = 指示物列表.IndexOf(indicator);
        if (indicatorId < 0)
        {
            Debug.LogWarning("指示物未配置到指示物列表");
            return;
        }

        var msgData = await Request.ChangeIndicator(indicatorId, indicator.transform.position,
            indicator.transform.eulerAngles, GetIndicatorColor(indicator));
        if (msgData.ResCode != ResCode.Success)
        {
            Debug.LogWarning($"发送指示物变化失败: {msgData.ResCode}");
            return;
        }
    }

    public void OnChangeIndicator(ChangeIndicator msgData)
    {
        if (msgData.IdInRoom == me.IdInRoom)
        {
            return;
        }

        selectedIndicator = FindIndicator(msgData.IndicatorId);
        if (selectedIndicator == null)
        {
            Debug.LogWarning($"未找到指示物: {msgData.IndicatorId}");
            return;
        }

        ApplyIndicatorChange(selectedIndicator, msgData);
    }

    private static bool IsPointerOnUi()
    {
        var touchTarget = FairyGUI.GRoot.inst.touchTarget;
        return touchTarget != null && touchTarget != FairyGUI.GRoot.inst;
    }

    private bool TryGetIndicator(Ray ray, out GameObject indicator)
    {
        foreach (var hit in Physics.RaycastAll(ray).OrderBy(x => x.distance))
        {
            if (IsUiObject(hit.collider.gameObject))
            {
                continue;
            }

            var clickedIndicator = FindIndicator(hit.collider.gameObject);
            if (clickedIndicator != null)
            {
                indicator = clickedIndicator;
                return true;
            }
        }

        indicator = null;
        return false;
    }

    private GameObject FindIndicator(GameObject target)
    {
        return 指示物列表.FirstOrDefault(indicator =>
            indicator != null && target.transform.IsChildOf(indicator.transform));
    }

    private GameObject FindIndicator(int indicatorId)
    {
        if (indicatorId < 0 || indicatorId >= 指示物列表.Count)
        {
            return null;
        }

        return 指示物列表[indicatorId];
    }

    private static Color GetIndicatorColor(GameObject indicator)
    {
        var renderer = indicator.GetComponent<Renderer>();
        return renderer != null ? renderer.material.color : Color.white;
    }

    private static void ApplyIndicatorChange(GameObject indicator, ChangeIndicator msgData)
    {
        indicator.transform.position = new Vector3(msgData.Position.X, msgData.Position.Y, msgData.Position.Z);
        indicator.transform.eulerAngles = new Vector3(msgData.Rotation.X, msgData.Rotation.Y, msgData.Rotation.Z);

        var renderer = indicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(msgData.Color.R, msgData.Color.G, msgData.Color.B, msgData.Color.A);
        }
    }

    private void HideNonUiElements()
    {
        foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            if (IsUiObject(renderer.gameObject))
            {
                continue;
            }

            renderer.enabled = false;
        }

        foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Include))
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