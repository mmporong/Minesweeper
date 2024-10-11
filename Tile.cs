using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using HardBoiled.Tools;

public class Tile : MonoBehaviour
{
    // Public variables
    public Image TileImage;
    public Sprite MineImage;
    public Sprite ClickedMineImage;
    public Sprite FlagImage;
    public TextMeshProUGUI text;

    public int x;
    public int y;
    public bool isMine;
    public bool isRevealed;
    public bool IsFlagged;
    public int neighboringMines;
    public Tile[] neighbors;

    // Constants
    private readonly Color defaultColor = new Color(76 / 255f, 84 / 255f, 92 / 255f, 1f);
    private readonly Color revealedColor = new Color(56 / 255f, 63 / 255f, 69 / 255f, 1f);
    private readonly Color[] numberColors = {
        new Color(124 / 255f, 199 / 255f, 255f / 255f, 1f),   // 1
        new Color(102 / 255f, 194 / 255f, 102 / 255f, 1f),   // 2
        new Color(255 / 255f, 119 / 255f, 136 / 255f, 1f),   // 3
        new Color(238 / 255f, 136 / 255f, 255 / 255f, 1f),   // 4
        new Color(221 / 255f, 170 / 255f, 34 / 255f, 1f),    // 5
        new Color(102 / 255f, 204 / 255f, 204 / 255f, 1f),   // 6
        new Color(153 / 255f, 153 / 255f, 153 / 255f, 1f),   // 7
        Color.white                                          // 8
    };

    public void Initialize(int x, int y)
    {
        // Ÿ�� �ʱ�ȭ
        this.x = x;
        this.y = y;
        isMine = false;
        isRevealed = false;
        neighboringMines = 0;
        IsFlagged = false;
        GetComponent<Image>().color = new Color(76 / 255f, 84 / 255f, 92 / 255f, 1f);

        if (TileImage.gameObject.activeSelf) TileImage.gameObject.SetActive(false); 
        if (text.gameObject.activeSelf) text.gameObject.SetActive(false);

        // Ÿ�� Ŭ�� �̺�Ʈ ������ �߰�
        GetComponent<Button>().onClick.RemoveListener(Reveal);
        GetComponent<Button>().onClick.AddListener(Reveal); 

        // Ÿ�� ��Ŭ�� ó��
        EventTrigger trigger = gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = gameObject.AddComponent<EventTrigger>();

        // �ʱ�ȭ 
        trigger.triggers.Clear();

        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        entry.callback.AddListener((data) =>
        {
            PointerEventData eventData = (PointerEventData)data;
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightClick(); 
                eventData.Use();
            }
        });
        trigger.triggers.Add(entry);
    }

    // �̿� Ÿ�� ���� ���
    public void UpdateNeighbors(Tile[,] tiles) 
    {
        List<Tile> neighborsList = new List<Tile>();

        // ���� Ÿ�� ��ǥ�� �������� �̿� Ÿ�� Ž��
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                if (offsetX == 0 && offsetY == 0) continue;

                // ���ο� ��ǥ ���
                int neighborX = x + offsetX;
                int neighborY = y + offsetY;

                // ��ǥ�� ���� ���� �ִ��� Ȯ��
                if (neighborX >= 0 && neighborY >= 0 && neighborX < tiles.GetLength(0) && neighborY < tiles.GetLength(1))
                {
                    Tile neighbor = tiles[neighborX, neighborY];
                    neighborsList.Add(neighbor); 
                }
            }
        }

        neighbors = neighborsList.ToArray();
        neighboringMines = 0;

        // �̿� Ÿ�� �� ���� Ÿ�� ���� ���
        foreach (var neighbor in neighbors)
        {
            if (neighbor.isMine)
            {
                neighboringMines++; 
            }
        }
    }

    public void Reveal()
    {
        if (isRevealed || GameManager.Instance.IsGameOver) return;

        // Ÿ�� ���� ����
        Stack<Tile> tilesToReveal = new Stack<Tile>();
        tilesToReveal.Push(this);

        bool isFirstClick = !Minesweeper.Instance.IsFirstClick;

        if (isFirstClick)
        {
            Minesweeper.Instance.IsFirstClick = true;
            Timer.Instance.StartTimer(); // ù Ŭ�� �� Ÿ�̸� ����
        }

        while (tilesToReveal.Count > 0)
        {
            Tile currentTile = tilesToReveal.Pop();
            if (currentTile.isRevealed) continue;

            currentTile.isRevealed = true;
            currentTile.GetComponent<Image>().color = revealedColor;

            if (isFirstClick && currentTile.isMine)
            {
                // ù Ŭ���� ������ ��� ���� ���ġ
                Minesweeper.Instance.RelocateMine(currentTile);
                currentTile.isMine = false;
                currentTile.neighboringMines = 0;
                currentTile.UpdateNeighbors(Minesweeper.Instance.tiles);
            }

            if (currentTile.isMine)
            {
                // ����
                currentTile.GetComponent<Image>().color = new Color(238 / 255f, 102 / 255f, 102 / 255f, 1f); // Ÿ�� ����
                Timer.Instance.StopTimer(); 
                Minesweeper.Instance.RevealMines(); // ��� ���� ����

                currentTile.TileImage.gameObject.SetActive(true);
                currentTile.TileImage.sprite = ClickedMineImage; // Ŭ���� ���� �̹��� ǥ��

                GameManager.Instance.IsGameOver = true;
                return;
            }
            else 
            {
                // ���ڰ� �ƴϸ�
                currentTile.TileImage.sprite = null;
                currentTile.TileImage.gameObject.SetActive(false);

                // Ÿ�Ͽ� ����� ������ ���� �� ���� �� ����
                if (currentTile.IsFlagged)
                {
                    currentTile.IsFlagged = false;
                    Minesweeper.Instance.SetMinesText(1);
                }

                // �ֺ� ���� ���� ǥ��
                if (currentTile.neighboringMines > 0)
                {
                    SetTile(currentTile);
                }
                else
                {
                    // ���ڰ� ������ �̿� Ÿ���� ���ÿ� �߰�
                    foreach (var neighbor in currentTile.neighbors)
                    {
                        if (!neighbor.isMine && !neighbor.isRevealed)
                        {
                            tilesToReveal.Push(neighbor);
                        }
                    }
                }

                // �¸� ���� Ȯ��
                Minesweeper.Instance.CheckVictoryCondition();
            }
        }
    }

    private void OnRightClick()
    {
        // ��Ŭ�� ��� ó��
        if (isRevealed || GameManager.Instance.IsGameOver) return;

        IsFlagged = !IsFlagged;
        if (IsFlagged)
        {
            TileImage.gameObject.SetActive(true);
            TileImage.sprite = FlagImage; // �÷��� �̹��� ǥ��
            Minesweeper.Instance.SetMinesText(-1); // ���� ���� �� ����
        }
        else
        {
            TileImage.gameObject.SetActive(false);
            Minesweeper.Instance.SetMinesText(1); // ���� ���� �� ����
        }
    }

    private void SetTile(Tile currentTile)
    {
        // Ÿ�� ���� 
        currentTile.text.gameObject.SetActive(true);
        currentTile.text.text = currentTile.neighboringMines.ToString(); 
        Color _textColor = currentTile.text.color;

        // ���� ���ڸ��� �ٸ� ����
        switch (currentTile.neighboringMines)
        {
            case 1:
                _textColor = new Color(124 / 255f, 199 / 255f, 255f / 255f, 1f);
                break;
            case 2:
                _textColor = new Color(102 / 255f, 194 / 255f, 102 / 255f, 1f);
                break;
            case 3:
                _textColor = new Color(255 / 255f, 119 / 255f, 136 / 255f, 1f);
                break;
            case 4:
                _textColor = new Color(238 / 255f, 136 / 255f, 255 / 255f, 1f);
                break;
            case 5:
                _textColor = new Color(221 / 255f, 170 / 255f, 34 / 255f, 1f);
                break;
            case 6:
                _textColor = new Color(102 / 255f, 204 / 255f, 204 / 255f, 1f);
                break;
            case 7:
                _textColor = new Color(153 / 255f, 153 / 255f, 153 / 255f, 1f);
                break;
            case 8:
                _textColor = Color.white;
                break;
            default:
                break;
        }

        currentTile.text.color = _textColor;
    }
}
