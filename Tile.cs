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
        // 타일 초기화
        this.x = x;
        this.y = y;
        isMine = false;
        isRevealed = false;
        neighboringMines = 0;
        IsFlagged = false;
        GetComponent<Image>().color = new Color(76 / 255f, 84 / 255f, 92 / 255f, 1f);

        if (TileImage.gameObject.activeSelf) TileImage.gameObject.SetActive(false); 
        if (text.gameObject.activeSelf) text.gameObject.SetActive(false);

        // 타일 클릭 이벤트 리스너 추가
        GetComponent<Button>().onClick.RemoveListener(Reveal);
        GetComponent<Button>().onClick.AddListener(Reveal); 

        // 타일 우클릭 처리
        EventTrigger trigger = gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = gameObject.AddComponent<EventTrigger>();

        // 초기화 
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

    // 이웃 타일 지뢰 계산
    public void UpdateNeighbors(Tile[,] tiles) 
    {
        List<Tile> neighborsList = new List<Tile>();

        // 현재 타일 좌표를 기준으로 이웃 타일 탐색
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                if (offsetX == 0 && offsetY == 0) continue;

                // 새로운 좌표 계산
                int neighborX = x + offsetX;
                int neighborY = y + offsetY;

                // 좌표가 범위 내에 있는지 확인
                if (neighborX >= 0 && neighborY >= 0 && neighborX < tiles.GetLength(0) && neighborY < tiles.GetLength(1))
                {
                    Tile neighbor = tiles[neighborX, neighborY];
                    neighborsList.Add(neighbor); 
                }
            }
        }

        neighbors = neighborsList.ToArray();
        neighboringMines = 0;

        // 이웃 타일 중 지뢰 타일 수를 계산
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

        // 타일 관리 스택
        Stack<Tile> tilesToReveal = new Stack<Tile>();
        tilesToReveal.Push(this);

        bool isFirstClick = !Minesweeper.Instance.IsFirstClick;

        if (isFirstClick)
        {
            Minesweeper.Instance.IsFirstClick = true;
            Timer.Instance.StartTimer(); // 첫 클릭 시 타이머 시작
        }

        while (tilesToReveal.Count > 0)
        {
            Tile currentTile = tilesToReveal.Pop();
            if (currentTile.isRevealed) continue;

            currentTile.isRevealed = true;
            currentTile.GetComponent<Image>().color = revealedColor;

            if (isFirstClick && currentTile.isMine)
            {
                // 첫 클릭이 지뢰일 경우 지뢰 재배치
                Minesweeper.Instance.RelocateMine(currentTile);
                currentTile.isMine = false;
                currentTile.neighboringMines = 0;
                currentTile.UpdateNeighbors(Minesweeper.Instance.tiles);
            }

            if (currentTile.isMine)
            {
                // 지뢰
                currentTile.GetComponent<Image>().color = new Color(238 / 255f, 102 / 255f, 102 / 255f, 1f); // 타일 색상
                Timer.Instance.StopTimer(); 
                Minesweeper.Instance.RevealMines(); // 모든 지뢰 열기

                currentTile.TileImage.gameObject.SetActive(true);
                currentTile.TileImage.sprite = ClickedMineImage; // 클릭된 지뢰 이미지 표시

                GameManager.Instance.IsGameOver = true;
                return;
            }
            else 
            {
                // 지뢰가 아니면
                currentTile.TileImage.sprite = null;
                currentTile.TileImage.gameObject.SetActive(false);

                // 타일에 깃발이 있으면 제거 후 지뢰 수 갱신
                if (currentTile.IsFlagged)
                {
                    currentTile.IsFlagged = false;
                    Minesweeper.Instance.SetMinesText(1);
                }

                // 주변 지뢰 숫자 표시
                if (currentTile.neighboringMines > 0)
                {
                    SetTile(currentTile);
                }
                else
                {
                    // 지뢰가 없으면 이웃 타일을 스택에 추가
                    foreach (var neighbor in currentTile.neighbors)
                    {
                        if (!neighbor.isMine && !neighbor.isRevealed)
                        {
                            tilesToReveal.Push(neighbor);
                        }
                    }
                }

                // 승리 조건 확인
                Minesweeper.Instance.CheckVictoryCondition();
            }
        }
    }

    private void OnRightClick()
    {
        // 우클릭 깃발 처리
        if (isRevealed || GameManager.Instance.IsGameOver) return;

        IsFlagged = !IsFlagged;
        if (IsFlagged)
        {
            TileImage.gameObject.SetActive(true);
            TileImage.sprite = FlagImage; // 플래그 이미지 표시
            Minesweeper.Instance.SetMinesText(-1); // 남은 지뢰 수 감소
        }
        else
        {
            TileImage.gameObject.SetActive(false);
            Minesweeper.Instance.SetMinesText(1); // 남은 지뢰 수 증가
        }
    }

    private void SetTile(Tile currentTile)
    {
        // 타일 색상 
        currentTile.text.gameObject.SetActive(true);
        currentTile.text.text = currentTile.neighboringMines.ToString(); 
        Color _textColor = currentTile.text.color;

        // 지뢰 숫자마다 다른 색상
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
