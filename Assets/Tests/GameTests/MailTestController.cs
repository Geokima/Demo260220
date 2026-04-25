using Framework;
using Game.Auth;
using Game.Base;
using Game.DTOs;
using Game.Mail;
using UnityEngine;

namespace Game.Tests
{
    public class MailTestController : MonoBehaviour, IController
    {
        public IArchitecture Architecture { get; set; } = GameArchitecture.Instance;

        private Rect _panelRect = new Rect(700, 100, 400, 500);
        private bool _showPanel = true;
        private GUIStyle _panelStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private bool _initialized;
        private Vector2 _scrollPosition;

        private void Awake()
        {
            this.RegisterEvent<LoginSuccessEvent>(OnLoginSuccess);
            // 订阅唯一的对齐事件
            this.RegisterEvent<MailSyncEvent>(OnMailSync);
        }

        private void OnLoginSuccess(LoginSuccessEvent e) => this.SendCommand(new GetMailListCommand());

        private void OnMailSync(MailSyncEvent e) { /* 自动响应 */ }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                _showPanel = !_showPanel;
            }
        }

        private void InitGUIStyles()
        {
            _panelStyle = new GUIStyle(GUI.skin.window) { fontSize = 12, normal = { background = CreateTex(400, 500, new Color(0.2f, 0.2f, 0.2f, 1f)) } };
            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true };
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, normal = { background = CreateTex(2, 2, new Color(0.35f, 0.35f, 0.35f, 1f)) }, hover = { background = CreateTex(2, 2, new Color(0.45f, 0.45f, 0.45f, 1f)) } };
            _initialized = true;
        }

        private Texture2D CreateTex(int width, int height, Color color)
        {
            var tex = new Texture2D(width, height);
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) tex.SetPixel(x, y, color);
            tex.Apply(); return tex;
        }

        private void OnGUI()
        {
            if (!_showPanel) return;

            if (!_initialized) InitGUIStyles();
            var accountModel = this.GetModel<AccountModel>();
            if (accountModel == null || !accountModel.IsLoggedIn) return;
            _panelRect = GUI.Window(2, _panelRect, DrawMailWindow, "邮件系统 (F2开关)", _panelStyle);
        }

        private void DrawMailWindow(int windowId)
        {
            var mailModel = this.GetModel<MailModel>();
            if (mailModel == null) return;

            GUILayout.BeginVertical();
            GUILayout.Label("邮件系统", _labelStyle);
            
            GUILayout.Label($"未读: {mailModel.UnreadCount.Value}", _labelStyle);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            
            // 确保你的 MailModel 里有 GetAllMails 方法
            var allMails = mailModel.GetAllMails(); 
            
            foreach (var mail in allMails)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label($"[{mail.MailId}] {mail.Title}", _labelStyle);
                
                GUILayout.Label(mail.Content, _labelStyle); 

                GUILayout.BeginHorizontal();
                if (!mail.IsRead && GUILayout.Button("读取", _buttonStyle, GUILayout.Width(60)))
                    this.SendCommand(new ReadMailCommand { MailId = mail.MailId });

                if (mail.Attachments?.Count > 0 && !mail.IsReceived && GUILayout.Button("领取", _buttonStyle, GUILayout.Width(60)))
                    this.SendCommand(new ReceiveAttachmentCommand { MailId = mail.MailId });

                if (GUILayout.Button("删除", _buttonStyle, GUILayout.Width(60)))
                    this.SendCommand(new DeleteMailCommand { MailId = mail.MailId });
                
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("手动刷新", _buttonStyle)) this.SendCommand(new GetMailListCommand());
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, _panelRect.width, 25));
        }

        public T GetModel<T>() where T : class, IModel => GameArchitecture.Instance.GetModel<T>();
        public void SendCommand<T>(T command) where T : ICommand => GameArchitecture.Instance.SendCommand(this, command);
    }
}