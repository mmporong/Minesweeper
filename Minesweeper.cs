using HardBoiled.Tools;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Minesweeper : Singleton<Minesweeper>
{
    public GameObject tilePrefab;
    public int tileWidth;
    public int tileHeight;
    public int mineCount;
    public Tile[,] tiles;

    // UI Elements
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;
    public TMP_InputField minesInput;
    public TMP_InputField xInputField;
    public TMP_InputField yInputField;
    public Button openTileButton;
    public Button updateButton;

    public GameObject ParentObject;
    public List<Tile> MineList = new List<Tile>();

    public bool IsFirstClick = false;

    [SerializeField] private float spaceMultiplier = 0.6f;
    [SerializeField] private string minesText = "MinesText";

    void Start()
    {
        InitializeGame();
        updateButton.onClick.AddListener(InitializeGame); 
        openTileButton.onClick.AddListener(OpenTile); 

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            InitializeGame(); 
        }
    }

    public void InitializeGame()
    {
        IsFirstClick = false;
        Timer.Instance.ResetTimer();
        GameManager.Instance.IsGameOver = false;
        UIManager.Instance.VictoryImage.SetActive(false);

        // Ÿ�� �Է°� ó��
        tileWidth = GetInputValue(widthInput, 9);
        tileHeight = GetInputValue(heightInput, 9); 
        mineCount = GetInputValue(minesInput, 10); 

        // Ÿ�� �Է°� ����
        tileWidth = Mathf.Clamp(tileWidth, 1, 100);
        tileHeight = Mathf.Clamp(tileHeight, 1, 100); 

        int maxMines = tileWidth * tileHeight - 1;
        mineCount = Mathf.Clamp(mineCount, 1, maxMines);
        MineList.Clear();

        TextChangeEvent.Trigger(minesText, mineCount.ToString()); 

        GenerateTiles(); 
    }

    private int GetInputValue(TMP_InputField inputField, int defaultValue)
    {
        // �Է°� ó��
        return string.IsNullOrEmpty(inputField.text) ? defaultValue : int.Parse(inputField.text);
    }

    private void GenerateTiles()
    {
        // Ÿ�� �迭 �ʱ�ȭ
        ClearExistingTiles(); 
        ParentObject.transform.parent.GetComponent<RectTransform>().transform.localScale = Vector3.one;
        tiles = new Tile[tileWidth, tileHeight]; 
        float xOffset = (tileWidth - 1) / 2f * spaceMultiplier; 
        float yOffset = (tileHeight - 1) / 2f * spaceMultiplier;

        // Ÿ�� ����
        for (int x = 0; x < tileWidth; x++)
        {
            for (int y = 0; y < tileHeight; y++)
            {
                CreateTile(x, y, xOffset, yOffset);
            }
        }

        AdjustParentSize(); // �θ� ũ�� ����
        PlaceMines(); // ���� ��ġ
    }

    public void ClearExistingTiles()
    {
        int childCount = ParentObject.transform.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            GameObject child = ParentObject.transform.GetChild(i).gameObject;
            ObjectPoolManager.Instance.Despawn(child);
        }
    }

    private void CreateTile(int x, int y, float xOffset, float yOffset)
    {
        GameObject tileObject = ObjectPoolManager.Instance.Spawn("Tile");
        tileObject.transform.parent = ParentObject.transform; 
        tileObject.transform.position = new Vector3(x * spaceMultiplier - xOffset, y * spaceMultiplier - yOffset, 0);
        tileObject.transform.localScale = Vector3.one;

        Vector3 localPosition = tileObject.transform.localPosition;
        localPosition.z = 0;
        tileObject.transform.localPosition = localPosition; 

        Tile tile = tileObject.GetComponent<Tile>();
        tile.Initialize(x, y); 
        tiles[x, y] = tile; 
    }

    private void AdjustParentSize()
    {
        // �θ� ũ�� ����
        RectTransform parentRectTransform = ParentObject.GetComponent<RectTransform>();
        if (parentRectTransform != null)
        {
            float parentWidth = tileWidth * spaceMultiplier * 100;
            float parentHeight = tileHeight * spaceMultiplier * 100;
            parentRectTransform.sizeDelta = new Vector2(parentWidth, parentHeight); 
        }

        RectTransform grandParentRectTransform = ParentObject.transform.parent.GetComponent<RectTransform>();

        if (grandParentRectTransform != null)
        {
            float grandParentWidth = tileWidth * spaceMultiplier * 100;
            float grandParentHeight = tileHeight * spaceMultiplier * 100;
            grandParentRectTransform.sizeDelta = new Vector2(grandParentWidth, grandParentHeight); 
        }

        if (tileHeight > 12)
        {
            grandParentRectTransform.transform.localScale = new Vector3(1 * (1 - tileHeight * 0.01f), 1 * (1 - tileHeight * 0.01f), 1 * (1 - tileHeight * 0.01f));
        }
        else
        {
            grandParentRectTransform.transform.localScale = Vector3.one;
        }
        
    }

    private void PlaceMines()
    {
        // ���� ��ġ
        int placedMines = 0;
        while (placedMines < mineCount)
        {
            int x = Random.Range(0, tileWidth);
            int y = Random.Range(0, tileHeight);

            if (!tiles[x, y].isMine)
            {
                tiles[x, y].isMine = true;
                MineList.Add(tiles[x, y]); 
                placedMines++;
            }
        }

        UpdateAllNeighbors();
    }

    public void RelocateMine(Tile mineTile)
    {
        // ���� ���ġ
        List<Tile> emptyTiles = new List<Tile>();

        foreach (var tile in tiles)
        {
            if (!tile.isMine && !tile.isRevealed)
            {
                tile.neighboringMines = 0;
                emptyTiles.Add(tile); 
            }
        }

        if (emptyTiles.Count > 0)
        {
            Tile newMineTile = emptyTiles[Random.Range(0, emptyTiles.Count)];
            newMineTile.isMine = true;
            MineList.Add(newMineTile); 

            mineTile.isMine = false;
            MineList.Remove(mineTile); 
            mineTile.neighboringMines = 0;

            UpdateAllNeighbors(); 
        }
    }

    public void UpdateAllNeighbors()
    {
        // �� Ÿ���� �̿� ������Ʈ
        foreach (var tile in tiles)
        {
            tile.UpdateNeighbors(tiles); 
        }
    }

    public void RevealMines()
    {
        foreach (Tile tile in MineList)
        {
            if (tile.IsFlagged)
            {
                tile.GetComponent<Image>().color = new Color(136 / 255f, 51 / 255f, 51 / 255f, 1f); // �÷��װ� �ִ� ���� Ÿ�� ���� ����
            }
            else
            {
                tile.TileImage.gameObject.SetActive(true);
                tile.TileImage.sprite = tile.MineImage; // ���� �̹��� ǥ��
            }
        }
    }

    public void SetMinesText(int flagCount)
    {
        mineCount += flagCount;
        TextChangeEvent.Trigger(minesText, mineCount.ToString()); // ���� �� �ؽ�Ʈ ������Ʈ
    }

    public void CheckVictoryCondition()
    {
        // �¸����� üũ
        bool allNonMineTilesRevealed = true;

        // Iterate through all tiles
        foreach (Tile tile in tiles)
        {
            if (!tile.isMine && !tile.isRevealed)
            {
                allNonMineTilesRevealed = false;
                break;
            }
        }

        if (allNonMineTilesRevealed)
        {
            // �¸� 
            Debug.Log("Victory!"); 
            GameManager.Instance.IsGameOver = true; 
            UIManager.Instance.VictoryImage.SetActive(true);
            TextChangeEvent.Trigger(minesText, 0.ToString()); 
            Timer.Instance.StopTimer();

            // ��� ���� ��߷� ����
            foreach (Tile mineTile in MineList)
            {
                mineTile.TileImage.gameObject.SetActive(true);
                mineTile.TileImage.sprite = mineTile.FlagImage;
            }

        }
    }

    public void OpenTile()
    {
        int x = GetInputValue(xInputField, 0); // x �Է°� ��������
        int y = GetInputValue(yInputField, 0); // y �Է°� ��������

        // ��ȿ�� ���� ���� ��ǥ���� Ȯ��
        if (x >= 0 && x < tileWidth && y >= 0 && y < tileHeight)
        {
            tiles[x, y].Reveal(); // �Է��� ��ǥ�� Ÿ�� ����
        }
        else
        {
            Debug.LogWarning("�߸��� ��ǥ"); 
        }
    }
}
