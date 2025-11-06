using UnityEngine;

public class CloudScroll : MonoBehaviour
{
    [Header("스크롤 설정")]
    [Tooltip("스크롤 속도 (음수: 왼쪽, 양수: 오른쪽)")]
    public float scrollSpeed = -0.5f;

    private Material material;

    void Start()
    {
        // Sprite Renderer 가져오기
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null)
        {
            Debug.LogError("[CloudScroll] Sprite Renderer가 없습니다!");
            enabled = false;
            return;
        }

        if (sr.sprite == null)
        {
            Debug.LogError("[CloudScroll] Sprite가 할당되지 않았습니다!");
            enabled = false;
            return;
        }

        // Material 생성 (인스턴스 생성으로 공유 방지)
        material = new Material(Shader.Find("Sprites/Default"));
        material.mainTexture = sr.sprite.texture;
        sr.material = material;

        // Texture Wrap Mode를 Repeat로 설정
        if (material.mainTexture != null)
        {
            material.mainTexture.wrapMode = TextureWrapMode.Repeat;
            Debug.Log("[CloudScroll] 초기화 성공!");
        }
    }

    void Update()
    {
        if (material != null && material.mainTexture != null)
        {
            // Texture Offset을 계속 변경해서 스크롤 효과
            float x = Time.time * scrollSpeed;
            material.mainTextureOffset = new Vector2(x, 0f);
        }
    }

    void OnDestroy()
    {
        // Material 메모리 정리
        if (material != null)
        {
            Destroy(material);
        }
    }
}
