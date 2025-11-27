using UnityEngine.InputSystem;

namespace LostSpells.Core.Utilities
{
    /// <summary>
    /// 키 입력 관련 유틸리티
    /// - 여러 클래스에서 중복 사용되던 ParseKey 메서드 통합
    /// </summary>
    public static class KeyInputUtility
    {
        /// <summary>
        /// 키 문자열을 Key enum으로 변환
        /// Options의 GetKeyDisplayName과 반대 역할
        /// </summary>
        /// <param name="keyString">키 이름 문자열 (예: "Space", "A", "LShift")</param>
        /// <param name="defaultKey">파싱 실패 시 반환할 기본 키</param>
        /// <returns>변환된 Key enum 값</returns>
        public static Key ParseKey(string keyString, Key defaultKey)
        {
            if (string.IsNullOrEmpty(keyString))
                return defaultKey;

            // 특수 키 매핑 (GetKeyDisplayName의 역변환)
            switch (keyString)
            {
                case "Space": return Key.Space;
                case "LShift": return Key.LeftShift;
                case "RShift": return Key.RightShift;
                case "LCtrl": return Key.LeftCtrl;
                case "RCtrl": return Key.RightCtrl;
                case "LAlt": return Key.LeftAlt;
                case "RAlt": return Key.RightAlt;
                case "Tab": return Key.Tab;
                case "Enter": return Key.Enter;
                case "Backspace": return Key.Backspace;
                case "Escape": return Key.Escape;
                case "Delete": return Key.Delete;
                case "Home": return Key.Home;
                case "End": return Key.End;
                case "PageUp": return Key.PageUp;
                case "PageDown": return Key.PageDown;
                default:
                    // 일반 키는 Enum.TryParse 시도
                    if (System.Enum.TryParse<Key>(keyString, true, out Key key))
                    {
                        return key;
                    }
                    return defaultKey;
            }
        }

        /// <summary>
        /// Key enum을 표시용 문자열로 변환
        /// Options의 GetKeyDisplayName 로직과 동일
        /// </summary>
        public static string GetKeyDisplayName(Key key)
        {
            switch (key)
            {
                case Key.Space: return "Space";
                case Key.LeftShift: return "LShift";
                case Key.RightShift: return "RShift";
                case Key.LeftCtrl: return "LCtrl";
                case Key.RightCtrl: return "RCtrl";
                case Key.LeftAlt: return "LAlt";
                case Key.RightAlt: return "RAlt";
                case Key.Tab: return "Tab";
                case Key.Enter: return "Enter";
                case Key.Backspace: return "Backspace";
                case Key.Escape: return "Escape";
                case Key.Delete: return "Delete";
                case Key.Home: return "Home";
                case Key.End: return "End";
                case Key.PageUp: return "PageUp";
                case Key.PageDown: return "PageDown";
                default: return key.ToString();
            }
        }
    }
}
