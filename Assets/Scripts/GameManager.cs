using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum DragonType
    {
        EMPTY,
        NORMAL,
        BARRIER,
        ROW_CLEAR,
        COLUMN_CLEAR,
        SPECIALDRAGON,
        COUNT //标记类型
    }

    //龙预制字典，通过种类来得到对应的游戏物体
    private Dictionary<DragonType, GameObject> dragonPrefabDict;

    private int[] xStep = { -1, 1, 0, 0 };
    private int[] yStep = { 0, 0, -1, 1 };

    [System.Serializable]
    public struct DragonPrefab
    {
        public DragonType type;
        public GameObject prefab;
    }

    public DragonPrefab[] dragonPrefabs;
    //单例
    private static GameManager _instance;
    public static GameManager Instance { get => _instance; set => _instance = value; }

    //网格的行列数
    public int xColumn;
    public int yRow;

    //填充时间
    public float fillTime;

    public Text timeText;
    public Text scoreText;
    public Text gameoverText;
    public GameObject gridPrefab;
    private bool gameover;
    private float gameTime = 90;
    private int score = 0;
    public AudioSource bgm;
    public AudioSource disappearAudio;
    private GameDragon[,] dragons;

    private GameDragon pressedDragon;
    private GameDragon enteredDragon;
    private void Awake()
    {
        _instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        bgm.loop = true;
        bgm.Play(0);
        dragonPrefabDict = new Dictionary<DragonType, GameObject>();
        for (int i = 0; i < dragonPrefabs.Length; i++)
        {
            if (!dragonPrefabDict.ContainsKey(dragonPrefabs[i].type))
            {
                dragonPrefabDict.Add(dragonPrefabs[i].type, dragonPrefabs[i].prefab);
            }
        }
        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                GameObject backFrame = Instantiate(gridPrefab, FixPosition(x, y), Quaternion.identity);
                backFrame.transform.SetParent(transform);
            }
        }

        dragons = new GameDragon[xColumn, yRow];
        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                CreateNewDragon(x, y, DragonType.EMPTY);
            }
        }
        CreateBarrier();
        StartCoroutine(AllFill());
    }

    public void Restart()
    {
        score = 0;
        gameTime = 90;
        gameover = false;
        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                Destroy(dragons[x, y].gameObject);
                CreateNewDragon(x, y, DragonType.EMPTY);
            }
        }
        CreateBarrier();
        StartCoroutine(AllFill());
    }

    private void CreateBarrier()
    {
        int temp;
        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                temp = Random.Range(0, 99);
                if(temp < 5)
                {
                    Destroy(dragons[x, y].gameObject);
                    CreateNewDragon(x, y, DragonType.BARRIER);
                }
            }
        }
    }
    void Update()
    {
        if (gameover)
        {
            gameoverText.color = new Color(255, 255, 255, 255);
            return;
        }
            
        if (gameTime < 0)
        {
            gameTime = 0;
            gameover = true;
            return;
        }
        gameoverText.color = new Color(255, 255, 255, 0);
        gameTime -= Time.deltaTime;
        timeText.text = gameTime.ToString("0");
        scoreText.text = score.ToString();
    }
    public Vector3 FixPosition(int x, int y)
    {
        return new Vector3(transform.position.x - xColumn / 2f + x, transform.position.y + yRow / 2f - y, 0);
    }
    
    //产生龙
    public GameDragon CreateNewDragon(int x, int y, DragonType type)
    {
        GameObject newDragon = Instantiate(dragonPrefabDict[type], FixPosition(x, y), Quaternion.identity);
        newDragon.transform.parent = transform;

        dragons[x, y] = newDragon.GetComponent<GameDragon>();
        dragons[x, y].Init(x, y, this, type);

        return dragons[x, y];

    }

    //全部填充
    public IEnumerator AllFill()
    {
        bool needRefill = true;
        while (needRefill)
        {
            yield return new WaitForSeconds(fillTime);
            while (Fill())
            {
                yield return new WaitForSeconds(fillTime);
            }
            needRefill = ClearAllMatched();
        }

    }

    public bool Fill()
    {
        bool hasNotFinished = false;//判断本次填充是否完成
        for (int y = yRow - 2; y >= 0; y--)
        {
            for (int x = 0; x < xColumn; x++)
            {
                GameDragon dragon = dragons[x, y];
                if (dragon.CanMove())
                {
                    GameDragon dragonBelow = dragons[x, y + 1];

                    if (dragonBelow.Type == DragonType.EMPTY)
                    {
                        Destroy(dragonBelow.gameObject);
                        dragon.MovedComponent.Move(x, y + 1, fillTime);
                        dragons[x, y + 1] = dragon;
                        CreateNewDragon(x, y, DragonType.EMPTY);
                        hasNotFinished = true;
                    }
                    else
                    {
                        for (int down = -1; down <= 1; down++)
                        {
                            if (down != 0)
                            {
                                int downX = x + down;

                                if (downX >= 0 && downX < xColumn)
                                {
                                    GameDragon downDragon = dragons[downX, y + 1];
                                    if (downDragon.Type == DragonType.EMPTY)
                                    {
                                        bool canfill = true;
                                        for (int aboveY = y; aboveY >= 0; aboveY--)
                                        {
                                            GameDragon dragonAbove = dragons[downX, aboveY];
                                            if (dragonAbove.CanMove()) break;
                                            else if (!dragonAbove.CanMove() && dragonAbove.Type != DragonType.EMPTY)
                                            {
                                                canfill = false;
                                                break;
                                            }
                                        }
                                        if (!canfill)
                                        {
                                            Destroy(downDragon.gameObject);
                                            dragon.MovedComponent.Move(downX, y + 1, fillTime);
                                            dragons[downX, y + 1] = dragon;
                                            CreateNewDragon(x, y, DragonType.EMPTY);
                                            hasNotFinished = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }
        //最上排
        for (int x = 0; x < xColumn; x++)
        {
            GameDragon dragon = dragons[x, 0];
            if (dragon.Type == DragonType.EMPTY)
            {
                GameObject newDragon = Instantiate(dragonPrefabDict[DragonType.NORMAL], FixPosition(x, -1), Quaternion.identity);
                newDragon.transform.parent = transform;
                dragons[x, 0] = newDragon.GetComponent<GameDragon>();
                dragons[x, 0].Init(x, -1, this, DragonType.NORMAL);
                dragons[x, 0].MovedComponent.Move(x, 0, fillTime);
                dragons[x, 0].ColoredComponent.SetColor((ColorDragon.ColorType)Random.Range(0, dragons[x, 0].ColoredComponent.NumColors));
                hasNotFinished = true;
            }
        }
        return hasNotFinished;
    }

    //是否相邻
    private bool IsFriend(GameDragon dragon1, GameDragon dragon2)
    {
        return dragon1.X == dragon2.X && Mathf.Abs(dragon1.Y - dragon2.Y) == 1
            || dragon1.Y == dragon2.Y && Mathf.Abs(dragon1.X - dragon2.X) == 1;
    }

    public void SwapDragon(GameDragon dragon1, GameDragon dragon2)
    {
        if (dragon1.CanMove() && dragon2.CanMove())
        {
            dragons[dragon1.X, dragon1.Y] = dragon2;
            dragons[dragon2.X, dragon2.Y] = dragon1;
            List<GameDragon> matchedDragons1 = MatchDragons(dragon1, dragon2.X, dragon2.Y);
            List<GameDragon> matchedDragons2 = MatchDragons(dragon2, dragon1.X, dragon1.Y);
            if (matchedDragons1 != null || matchedDragons2 != null)
            {
                int tempX = dragon1.X;
                int tempY = dragon1.Y;
                dragon1.MovedComponent.Move(dragon2.X, dragon2.Y, fillTime);
                dragon2.MovedComponent.Move(tempX, tempY, fillTime);
                if (matchedDragons1 != null)
                    ClearMatchedList(matchedDragons1, true);
                if (matchedDragons2 != null)
                    ClearMatchedList(matchedDragons2, true);
                StartCoroutine(AllFill());
            }
            else
            {
                dragons[dragon1.X, dragon1.Y] = dragon1;
                dragons[dragon2.X, dragon2.Y] = dragon2;
            }

        }
    }

    //鼠标事件方法
    #region
    public void PressDragon(GameDragon dragon)
    {
        if(!gameover)
            pressedDragon = dragon;
    }

    public void EnterDragon(GameDragon dragon)
    {
        if (!gameover)
            enteredDragon = dragon;
    }

    public void ReleaseDragon()
    {
        if (!gameover && IsFriend(pressedDragon, enteredDragon))
            SwapDragon(pressedDragon, enteredDragon);
    }
    #endregion

    //匹配和清除模块
    #region
    //匹配方法
    public List<GameDragon> MatchDragons(GameDragon dragon, int newX, int newY)
    {
        if (dragon.CanColor())
        {
            ColorDragon.ColorType color = dragon.ColoredComponent.Color;
            List<GameDragon> tempDragons = new List<GameDragon>();
            List<GameDragon> matchedDragons = new List<GameDragon>();
            List<GameDragon> extendedDragons = new List<GameDragon>();
            for (int i = 0; i < 4; i++)
            {
                if (i % 2 == 0)
                    tempDragons.Clear();

                int step = 1;
                while (true)
                {
                    int currentX = newX + xStep[i] * step;
                    int currentY = newY + yStep[i] * step;
                    if (currentX < 0 || currentX >= xColumn || currentY < 0 || currentY >= yRow) break;
                    GameDragon current = dragons[currentX, currentY];
                    if (!current.CanColor() || current.ColoredComponent.Color != color) break;
                    step++;
                    tempDragons.Add(current);
                }
                if (tempDragons.Count >= 2)
                {
                    foreach (GameDragon tempDragon in tempDragons)
                        matchedDragons.Add(tempDragon);
                }
            }
            if (matchedDragons.Count != 0)
            {
                //matchedDragons.Add(dragons[newX, newY]);
                foreach (GameDragon matchedDragon in matchedDragons)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int currentX = matchedDragon.X + xStep[i];
                        int currentY = matchedDragon.Y + yStep[i];
                        if (currentX < 0 || currentX >= xColumn || currentY < 0 || currentY >= yRow) continue;
                        GameDragon current = dragons[currentX, currentY];
                        if (!current.CanColor() || current.ColoredComponent.Color != color) continue;
                        if (matchedDragons.IndexOf(current) == -1 && extendedDragons.IndexOf(current)==-1)
                            extendedDragons.Add(current);
                    }

                }
                //初始位置必须从newX开始算
                matchedDragons.Add(dragons[newX, newY]);
                for (int i = 0; i < 4; i++)
                {
                    int currentX = newX + xStep[i];
                    int currentY = newY + yStep[i];
                    if (currentX < 0 || currentX >= xColumn || currentY < 0 || currentY >= yRow) continue;
                    GameDragon current = dragons[currentX, currentY];
                    if (!current.CanColor() || current.ColoredComponent.Color != color) continue;
                    if (matchedDragons.IndexOf(current) == -1 && extendedDragons.IndexOf(current) == -1)
                        extendedDragons.Add(current);
                }

                foreach (GameDragon extendedDragon in extendedDragons)
                    matchedDragons.Add(extendedDragon);
            }
            if (matchedDragons.Count != 0)
                return matchedDragons;
        }
        return null;
    }

    public bool ClearDragon(int x, int y)
    {
        if (dragons[x, y].CanClear() && !dragons[x, y].ClearComponent.IsClearing)
        {
            dragons[x, y].ClearComponent.Clear();
            CreateNewDragon(x, y, DragonType.EMPTY);
            return true;
        }
        return false;
    }

    //清除列表中所有的块
    private bool ClearAllMatched()
    {
        bool needRefill = false;
        for (int y = 0; y < yRow; y++)
        {
            for (int x = 0; x < xColumn; x++)
            {
                if (dragons[x, y].CanClear())
                {
                    var matchList = MatchDragons(dragons[x, y], x, y);
                    if (matchList != null)
                    {
                        needRefill = ClearMatchedList(matchList, false);
                    }
                }
            }
        }
        if (needRefill)
            disappearAudio.Play(0);
        return needRefill;
    }

    private bool ClearMatchedList(List<GameDragon> matchedDragons, bool needsPlaySound)
    {
        bool hasCleared = false;
        int count = 0;
        foreach (GameDragon matchedDragon in matchedDragons)
        {
            if (ClearDragon(matchedDragon.X, matchedDragon.Y))
            {
                count++;
                score += 5 * (int)Mathf.Pow(2, count);
                hasCleared = true;
            }
        }
        if (hasCleared&&needsPlaySound)
            disappearAudio.Play(0);
        return hasCleared;
    }
    #endregion
}
