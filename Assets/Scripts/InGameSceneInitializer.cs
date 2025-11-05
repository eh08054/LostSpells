using UnityEngine;

namespace LostSpells
{
    /// <summary>
    /// InGame 씬 초기화 - 씬 시작 시 배경, Ground, Player, Enemy 오브젝트 자동 생성
    /// </summary>
    public class InGameSceneInitializer : MonoBehaviour
    {
        [Header("배경 이미지 (선택사항)")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite middlegroundSprite;

        private void Awake()
        {
            // 이미 오브젝트가 있으면 생성하지 않음
            if (GameObject.Find("Player") != null &&
                GameObject.Find("Enemy") != null)
            {
                // Debug.Log("[InGameSceneInitializer] 게임 오브젝트가 이미 존재합니다.");
                return;
            }

            CreateGameObjects();
        }

        private void CreateGameObjects()
        {
            // 배경 생성 (씬에 없으면 생성)
            if (GameObject.Find("Background") == null || GameObject.Find("Middleground") == null)
            {
                CreateBackground();
            }

            // 땅 생성 (씬에 없으면 생성)
            if (GameObject.Find("Ground") == null)
            {
                CreateGround();
            }

            // 플레이어 생성
            CreatePlayer();

            // 적 생성
            CreateEnemy();

            Debug.Log("[InGameSceneInitializer] InGame 오브젝트 생성 완료!");
        }

        private void CreateBackground()
        {
            // Background (하늘) 생성
            if (GameObject.Find("Background") == null)
            {
                GameObject background = new GameObject("Background");
                background.transform.position = new Vector3(0, 0, 0);

                SpriteRenderer sr = background.AddComponent<SpriteRenderer>();

                // Resources에서 배경 스프라이트 로드
                Sprite bgSprite = backgroundSprite;
                if (bgSprite == null)
                {
                    bgSprite = Resources.Load<Sprite>("Gothicvania-Town/Art/Environment/Background/background");
                }

                if (bgSprite != null)
                {
                    sr.sprite = bgSprite;

                    // 카메라 크기에 맞게 자동 스케일 조정
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        float screenHeight = cam.orthographicSize * 2f; // 높이 = 10
                        float screenWidth = screenHeight * cam.aspect; // 16:9 = 17.78

                        // 스프라이트 실제 크기 (Unity units)
                        float spriteHeight = bgSprite.bounds.size.y;
                        float spriteWidth = bgSprite.bounds.size.x;

                        // 화면을 채우기 위한 스케일 계산
                        float scaleX = screenWidth / spriteWidth;
                        float scaleY = screenHeight / spriteHeight;
                        float scale = Mathf.Max(scaleX, scaleY); // 화면을 완전히 채우도록

                        background.transform.localScale = new Vector3(scale, scale, 1f);

                        Debug.Log($"[InGameSceneInitializer] Background 스케일: {scale}, 화면: {screenWidth}x{screenHeight}, 스프라이트: {spriteWidth}x{spriteHeight}");
                    }
                }
                else
                {
                    // 배경 스프라이트가 없으면 하늘색 사각형 생성
                    sr.sprite = CreateSquareSprite();
                    sr.color = new Color(0.53f, 0.81f, 0.92f); // 하늘색
                    background.transform.localScale = new Vector3(20f, 12f, 1f);
                }

                sr.sortingOrder = 0; // 가장 뒤에 표시

                Debug.Log("[InGameSceneInitializer] Background 생성 완료");
            }

            // Middleground (건물) 생성
            if (GameObject.Find("Middleground") == null)
            {
                GameObject middleground = new GameObject("Middleground");
                middleground.transform.position = new Vector3(0, 0, 0);

                SpriteRenderer sr = middleground.AddComponent<SpriteRenderer>();

                // Resources에서 중경 스프라이트 로드
                Sprite mgSprite = middlegroundSprite;
                if (mgSprite == null)
                {
                    mgSprite = Resources.Load<Sprite>("Gothicvania-Town/Art/Environment/Background/middleground");
                }

                if (mgSprite != null)
                {
                    sr.sprite = mgSprite;

                    // 카메라 크기에 맞게 자동 스케일 조정
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        float screenHeight = cam.orthographicSize * 2f;
                        float screenWidth = screenHeight * cam.aspect;

                        float spriteHeight = mgSprite.bounds.size.y;
                        float spriteWidth = mgSprite.bounds.size.x;

                        float scaleX = screenWidth / spriteWidth;
                        float scaleY = screenHeight / spriteHeight;
                        float scale = Mathf.Max(scaleX, scaleY);

                        middleground.transform.localScale = new Vector3(scale, scale, 1f);

                        Debug.Log($"[InGameSceneInitializer] Middleground 스케일: {scale}, 화면: {screenWidth}x{screenHeight}, 스프라이트: {spriteWidth}x{spriteHeight}");
                    }
                }
                else
                {
                    // 중경 스프라이트가 없으면 어두운 회색 사각형 생성
                    sr.sprite = CreateSquareSprite();
                    sr.color = new Color(0.3f, 0.3f, 0.35f, 0.7f); // 반투명 어두운 회색
                    middleground.transform.localScale = new Vector3(20f, 12f, 1f);
                }

                sr.sortingOrder = 5; // Background와 Ground 사이

                Debug.Log("[InGameSceneInitializer] Middleground 생성 완료");
            }
        }

        private void CreateGround()
        {
            GameObject ground = new GameObject("Ground");
            ground.transform.position = new Vector3(0, -4, 0);

            SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = new Color(0.4f, 0.25f, 0.1f, 0f); // 완전 투명
            sr.sortingOrder = 10; // Background와 Middleground 뒤, Player/Enemy 앞

            ground.transform.localScale = new Vector3(20f, 1f, 1f);

            // BoxCollider2D 추가 (충돌 감지)
            BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();

            // Rigidbody2D 추가 (Static - 움직이지 않는 지형)
            Rigidbody2D rb = ground.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            Debug.Log("[InGameSceneInitializer] Ground 생성 완료 - Position: (0, -4, 0), Scale: (20, 1, 1)");
        }

        private void CreatePlayer()
        {
            if (GameObject.Find("Player") != null)
                return;

            // 플레이어 오브젝트 생성
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(-5, -3, 0);

            // Player 태그는 Unity 기본 태그이므로 안전하게 설정 가능
            try
            {
                player.tag = "Player";
            }
            catch (UnityException)
            {
                Debug.LogWarning("[InGameSceneInitializer] Player 태그가 정의되지 않았습니다.");
            }

            // SpriteRenderer 추가
            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = new Color(0.2f, 0.5f, 1f); // 파란색
            sr.sortingOrder = 20; // Ground보다 앞에 표시

            // 크기 조정
            player.transform.localScale = new Vector3(1f, 1.5f, 1f);

            // BoxCollider2D 추가
            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();

            // Rigidbody2D 추가 (중력 적용)
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 회전 방지

            Debug.Log("[InGameSceneInitializer] Player 생성 완료 - Position: (-5, -3, 0), Scale: (1, 1.5, 1)");
        }

        private void CreateEnemy()
        {
            if (GameObject.Find("Enemy") != null)
                return;

            // 적 오브젝트 생성
            GameObject enemy = new GameObject("Enemy");
            enemy.transform.position = new Vector3(5, -3, 0);

            // Enemy 태그는 사용자 정의 태그이므로 안전하게 처리
            // 태그 설정 없이도 오브젝트 이름으로 구분 가능

            // SpriteRenderer 추가
            SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = new Color(1f, 0.2f, 0.2f); // 빨간색
            sr.sortingOrder = 20; // Ground보다 앞에 표시

            // 크기 조정
            enemy.transform.localScale = new Vector3(1f, 1.5f, 1f);

            // BoxCollider2D 추가
            BoxCollider2D collider = enemy.AddComponent<BoxCollider2D>();

            // Rigidbody2D 추가 (중력 적용)
            Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 회전 방지

            // 체력바 생성
            CreateHealthBar(enemy);

            Debug.Log("[InGameSceneInitializer] Enemy 생성 완료 - Position: (5, -3, 0), Scale: (1, 1.5, 1)");
        }

        /// <summary>
        /// 적 오브젝트 위에 체력바 생성
        /// </summary>
        private void CreateHealthBar(GameObject parent)
        {
            // 체력바 컨테이너 (부모의 자식으로)
            GameObject healthBarContainer = new GameObject("HealthBar");
            healthBarContainer.transform.SetParent(parent.transform);
            healthBarContainer.transform.localPosition = new Vector3(0, 0.85f, 0); // 적 위쪽에 배치 (높이 낮춤)
            healthBarContainer.transform.localScale = Vector3.one;

            // 체력바 배경/테두리 (검은색)
            GameObject background = new GameObject("Background");
            background.transform.SetParent(healthBarContainer.transform);
            background.transform.localPosition = Vector3.zero;

            SpriteRenderer bgSr = background.AddComponent<SpriteRenderer>();
            bgSr.sprite = CreateSquareSprite();
            bgSr.color = new Color(0.1f, 0.1f, 0.1f, 1f); // 진한 검은색 (테두리)
            bgSr.sortingOrder = 21; // 적보다 앞에 표시

            background.transform.localScale = new Vector3(1.2f, 0.15f, 1f);

            // 체력바 전경 (녹색) - 왼쪽 정렬
            GameObject foreground = new GameObject("Foreground");
            foreground.transform.SetParent(healthBarContainer.transform);
            foreground.transform.localPosition = new Vector3(-0.08f, 0, 0); // 왼쪽 정렬

            SpriteRenderer fgSr = foreground.AddComponent<SpriteRenderer>();
            fgSr.sprite = CreateSquareSprite();
            fgSr.color = new Color(0.2f, 0.8f, 0.2f, 1f); // 녹색
            fgSr.sortingOrder = 22; // 배경보다 앞에 표시

            foreground.transform.localScale = new Vector3(1.04f, 0.11f, 1f); // 테두리가 보이도록 살짝 작게
        }

        /// <summary>
        /// 정사각형 Sprite 생성
        /// </summary>
        private Sprite CreateSquareSprite()
        {
            // 흰색 정사각형 텍스처 생성 (16x16 픽셀)
            Texture2D texture = new Texture2D(16, 16);
            Color[] pixels = new Color[16 * 16];

            // 모든 픽셀을 흰색으로 채움
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point; // 픽셀 아트 스타일

            // Sprite 생성
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 16, 16),
                new Vector2(0.5f, 0.5f), // Pivot을 중앙으로
                16f // Pixels per unit
            );

            return sprite;
        }
    }
}
