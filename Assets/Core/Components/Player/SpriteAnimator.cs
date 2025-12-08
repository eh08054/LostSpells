using UnityEngine;

namespace LostSpells.Components
{
    /// <summary>
    /// 간단한 스프라이트 애니메이션 컴포넌트
    /// 스프라이트 배열을 순환하며 애니메이션 재생
    /// </summary>
    public class SpriteAnimator : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Sprite[] sprites;
        private float frameRate = 12f;
        private float timer = 0f;
        private int currentFrame = 0;
        private bool loop = true;

        public void Initialize(Sprite[] sprites, float frameRate = 12f, bool loop = true)
        {
            this.sprites = sprites;
            this.frameRate = frameRate;
            this.loop = loop;

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (sprites != null && sprites.Length > 0)
            {
                spriteRenderer.sprite = sprites[0];
            }
        }

        private void Update()
        {
            if (sprites == null || sprites.Length <= 1)
                return;

            timer += Time.deltaTime;

            float frameTime = 1f / frameRate;
            if (timer >= frameTime)
            {
                timer -= frameTime;
                currentFrame++;

                if (currentFrame >= sprites.Length)
                {
                    if (loop)
                    {
                        currentFrame = 0;
                    }
                    else
                    {
                        currentFrame = sprites.Length - 1;
                        return;
                    }
                }

                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = sprites[currentFrame];
                }
            }
        }
    }
}
