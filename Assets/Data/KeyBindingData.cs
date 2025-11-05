using System;
using UnityEngine.InputSystem;

namespace LostSpells.Data
{
    /// <summary>
    /// 게임 내 모든 키 바인딩을 관리하는 클래스
    /// </summary>
    [Serializable]
    public class KeyBindingData
    {
        // UI 관련 키 바인딩
        public Key toggleSidebarKey = Key.Tab;
        public Key pauseMenuKey = Key.Escape;

        // 플레이어 이동 관련 키 바인딩
        public Key moveLeftKey = Key.A;
        public Key moveRightKey = Key.D;
        public Key jumpKey = Key.Space;

        // 스킬 관련 키 바인딩
        public Key skill1Key = Key.Q;
        public Key skill2Key = Key.W;
        public Key skill3Key = Key.E;
        public Key skill4Key = Key.R;

        /// <summary>
        /// 기본 키 바인딩으로 초기화
        /// </summary>
        public void ResetToDefault()
        {
            toggleSidebarKey = Key.Tab;
            pauseMenuKey = Key.Escape;
            moveLeftKey = Key.A;
            moveRightKey = Key.D;
            jumpKey = Key.Space;
            skill1Key = Key.Q;
            skill2Key = Key.W;
            skill3Key = Key.E;
            skill4Key = Key.R;
        }

        /// <summary>
        /// 특정 키가 현재 프레임에 눌렸는지 확인
        /// </summary>
        public bool IsKeyPressed(Key key)
        {
            if (Keyboard.current == null)
                return false;

            return Keyboard.current[key].wasPressedThisFrame;
        }

        /// <summary>
        /// 특정 키가 현재 눌려있는지 확인 (연속 입력)
        /// </summary>
        public bool IsKeyHeld(Key key)
        {
            if (Keyboard.current == null)
                return false;

            return Keyboard.current[key].isPressed;
        }

        /// <summary>
        /// 사이드바 토글 키가 눌렸는지 확인
        /// </summary>
        public bool IsToggleSidebarPressed()
        {
            return IsKeyPressed(toggleSidebarKey);
        }

        /// <summary>
        /// 일시정지 메뉴 키가 눌렸는지 확인
        /// </summary>
        public bool IsPauseMenuPressed()
        {
            return IsKeyPressed(pauseMenuKey);
        }
    }
}
