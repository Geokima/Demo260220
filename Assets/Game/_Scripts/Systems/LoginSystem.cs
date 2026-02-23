using Framework;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Systems
{
    public class LoginSystem : AbstractSystem
    {
        private string _token;
        private int _userId;
        private bool _isLoggingIn;
        
        public string Token => _token;
        public int UserId => _userId;
        public bool IsLoggedIn => !string.IsNullOrEmpty(_token);
        public bool IsLoggingIn => _isLoggingIn;
        
        public void SetLoginInfo(string token, int userId)
        {
            _token = token;
            _userId = userId;
        }
        
        public void SetLoggingIn(bool value)
        {
            _isLoggingIn = value;
        }
        
        public void Logout()
        {
            if (_isLoggingIn)
            {
                Debug.LogWarning("[Login] Cannot logout while logging in");
                return;
            }
            
            _token = null;
            _userId = 0;
            Debug.Log("[Login] Logout");
        }
    }
}
