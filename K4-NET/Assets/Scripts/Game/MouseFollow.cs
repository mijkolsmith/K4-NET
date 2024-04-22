using UnityEngine;

public class MouseFollow : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        Cursor.visible = (spriteRenderer.sprite == null);
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 25;

        Vector2 cursorPos = Camera.main.ScreenToWorldPoint(mousePos);
        transform.position = cursorPos;
    }

    public void SetSprite(Sprite newSprite)
    {
        spriteRenderer.sprite = newSprite;
    }
}