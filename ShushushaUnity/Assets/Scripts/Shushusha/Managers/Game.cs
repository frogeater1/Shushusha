using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.Json;
using Cysharp.Threading.Tasks;
using FairyGUI;
using ShushushaServer;
using UI;
using UnityEngine;
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

    public GameObject 指示物Prefab;
    public List<GameObject> 指示物列表 = new();
    private Window indicatorMenuWindow;
    private CancellationTokenSource stageCountdownCts;

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
        ApplyIndicators(msgData.Indicators);
        uiMain.m_round.SetCountText(msgData.Round);
        uiMain.m_stage.text = $"{msgData.Stage}阶段";
        CancelStageCountdown();

        if (msgData.StageSeconds > 0)
        {
            StartStageCountdown(msgData.StageSeconds).Forget();
        }
        else
        {
            uiMain.m_倒计时.text = string.Empty;
        }

        switch (msgData.Stage)
        {
            case GameStage.Observe:
                break;
            case GameStage.Hide:
                switch (Identity)
                {
                    case PlayerIdentity.Shark or PlayerIdentity.SharkKing:
                        uiMain.m_确定.enabled = true;
                        break;
                    case PlayerIdentity.Mouse:
                        BlackoutForMouse();
                        break;
                }

                break;
            case GameStage.Kill:
            default:
                break;
        }
    }

    private void SetFloor(int currentFloor)
    {
        CurrentFloor = currentFloor;
        uiMain.m_floor.SetCountText(CurrentFloor);
    }

    private void SetMagicVisible(bool visible)
    {
        uiMain.m_魔力txt.visible = visible;
        uiMain.m_魔力值.visible = visible;
    }

    private void SetMagic(int magic)
    {
        if (Identity != PlayerIdentity.Mouse)
        {
            return;
        }

        Magic = magic;
        uiMain.m_魔力值.m_魔力值.selectedIndex = magic;
    }

    private async UniTaskVoid StartStageCountdown(int seconds)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        stageCountdownCts = cts;

        try
        {
            for (var remaining = seconds; remaining >= 0; remaining--)
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

    public void OnIndicatorClicked(GameObject target)
    {
        if (CurrentStage != GameStage.Hide || Identity == PlayerIdentity.Mouse)
        {
            return;
        }

        if (IsPointerOnUi())
        {
            return;
        }

        var indicator = FindIndicator(target);
        if (indicator == null)
        {
            return;
        }

        ShowIndicatorMenu(indicator);
    }

    private async UniTaskVoid ChangeIndicator(GameObject indicator, IndicatorChangeKind kind)
    {
        if (CurrentStage != GameStage.Hide || Identity == PlayerIdentity.Mouse)
        {
            return;
        }

        var indicatorId = 指示物列表.IndexOf(indicator);
        if (indicatorId < 0)
        {
            Debug.LogWarning("指示物未配置到指示物列表");
            return;
        }

        var msgData = await Request.ChangeIndicator(indicatorId, kind);
        if (msgData.ResCode != ResCode.Success)
        {
            Debug.LogWarning($"发送指示物变化失败: {msgData.ResCode}");
            return;
        }
    }

    private void ShowIndicatorMenu(GameObject indicator)
    {
        if (indicatorMenuWindow == null)
        {
            var menu = UI_Menu.CreateInstance();
            menu.m_menu.onClickItem.Set(OnIndicatorMenuItemClicked);
            indicatorMenuWindow = new Window
            {
                modal = true,
                contentPane = menu
            };
        }

        indicatorMenuWindow.contentPane.data = indicator;
        var position = GRoot.inst.GlobalToLocal(Stage.inst.touchPosition);
        if (position.y > GRoot.inst.height - 300f)
        {
            position.y -= 300f;
        }

        indicatorMenuWindow.Show();
        indicatorMenuWindow.SetXY(position.x, position.y);
        GRoot.inst.modalLayer.onClick.Set(HideIndicatorMenu);
    }

    private void HideIndicatorMenu()
    {
        if (indicatorMenuWindow == null)
        {
            return;
        }

        indicatorMenuWindow.contentPane.data = null;
        GRoot.inst.modalLayer.onClick.Set((EventCallback0)null);
        indicatorMenuWindow.Hide();
    }

    private void OnIndicatorMenuItemClicked(EventContext context)
    {
        if (indicatorMenuWindow?.contentPane is not UI_Menu menu ||
            menu.data is not GameObject indicator)
        {
            return;
        }

        if (!TryGetIndicatorChangeKind(context, menu, out var kind))
        {
            return;
        }

        HideIndicatorMenu();
        ChangeIndicator(indicator, kind).Forget();
    }

    private static bool TryGetIndicatorChangeKind(EventContext context, UI_Menu menu, out IndicatorChangeKind kind)
    {
        var itemIndex = context.data is GObject item ? menu.m_menu.GetChildIndex(item) : -1;
        switch (itemIndex)
        {
            case 0:
                kind = IndicatorChangeKind.Position;
                return true;
            case 1:
                kind = IndicatorChangeKind.Color;
                return true;
            case 2:
                kind = IndicatorChangeKind.Rotation;
                return true;
            default:
                Debug.LogWarning($"未知指示物菜单选项: {itemIndex}");
                kind = default;
                return false;
        }
    }

    public void OnChangeIndicator(ChangeIndicator msgData)
    {
        ApplyIndicators(msgData.Indicators);
    }

    private static bool IsPointerOnUi()
    {
        var touchTarget = FairyGUI.GRoot.inst.touchTarget;
        return touchTarget != null && touchTarget != FairyGUI.GRoot.inst;
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

    private GameObject EnsureIndicator(int indicatorId)
    {
        var indicator = FindIndicator(indicatorId);
        if (indicator != null)
        {
            return indicator;
        }

        if (indicatorId < 0)
        {
            return null;
        }

        if (指示物Prefab == null)
        {
            Debug.LogWarning($"无法实例化指示物，缺少指示物Prefab: {indicatorId}");
            return null;
        }

        while (指示物列表.Count <= indicatorId)
        {
            指示物列表.Add(null);
        }

        if (指示物列表[indicatorId] == null)
        {
            var newIndicator = Instantiate(指示物Prefab);
            newIndicator.name = $"{指示物Prefab.name}_{indicatorId}";
            if (newIndicator.GetComponent<Indicator>() == null)
            {
                newIndicator.AddComponent<Indicator>();
            }

            指示物列表[indicatorId] = newIndicator;
        }

        return 指示物列表[indicatorId];
    }

    private void ApplyIndicators(List<ServerIndicator> indicators)
    {
        foreach (var serverIndicator in indicators)
        {
            var indicator = FindIndicator(serverIndicator.IndicatorId);
            if (indicator == null)
            {
                indicator = EnsureIndicator(serverIndicator.IndicatorId);
                if (indicator == null)
                {
                    Debug.LogWarning($"未找到指示物: {serverIndicator.IndicatorId}");
                    continue;
                }
            }

            ApplyIndicatorTransform(indicator, serverIndicator.Position, serverIndicator.Rotation, serverIndicator.Color);
        }
    }

    private static void ApplyIndicatorTransform(GameObject indicator, ServerVector3 position, ServerVector3 rotation, ServerColor color)
    {
        indicator.transform.position = new Vector3(position.X, position.Y, position.Z);
        indicator.transform.eulerAngles = new Vector3(rotation.X, rotation.Y, rotation.Z);

        var renderer = indicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(color.R, color.G, color.B, color.A);
        }
    }

    private void BlackoutForMouse()
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
