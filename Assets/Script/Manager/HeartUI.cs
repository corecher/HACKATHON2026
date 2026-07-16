using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartUI : MonoBehaviour
{
    [Header("연결 대상")]
    public PlayerHealth playerHealth;

    [Header("하트 아이콘")]
    public Image heartIconPrefab;
    public Transform heartContainer;
    public Color fullColor = Color.red;
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    private readonly List<Image> heartIcons = new List<Image>();

    void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHeartsChanged += UpdateHearts;
            UpdateHearts(playerHealth.CurrentHearts, playerHealth.MaxHearts);
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHeartsChanged -= UpdateHearts;
        }
    }

    private void UpdateHearts(int current, int max)
    {
        // 최대 하트 수가 바뀌었으면(단련 강화 등) 아이콘 다시 생성
        if (heartIcons.Count != max)
        {
            foreach (var icon in heartIcons)
            {
                Destroy(icon.gameObject);
            }
            heartIcons.Clear();

            for (int i = 0; i < max; i++)
            {
                Image icon = Instantiate(heartIconPrefab, heartContainer);
                heartIcons.Add(icon);
            }
        }

        for (int i = 0; i < heartIcons.Count; i++)
        {
            heartIcons[i].color = i < current ? fullColor : emptyColor;
        }
    }
}
