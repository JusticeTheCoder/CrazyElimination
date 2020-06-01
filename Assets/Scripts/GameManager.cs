using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum DragonType
    {
        EMPTY,
        NORMAL,
        BARRIER,
        ROW_CLEAR,
        COLUMN_CLEAR,
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

    public GameObject clockAnime;
    //填充时间
    public float fillTime;
    private bool hasChangedBGM;
    public InputField nameField;
    public Text timeText;
    public Text scoreText;
    public Text gameoverText;
    public Image submitDialog;
    public GameObject gridPrefab;
    private bool gameover;
    private float gameTime = 90;
    private int score = 0;
    public AudioSource bgm;
    public AudioSource bgmOfGameOver;
    public AudioSource disappearAudio;
    private GameDragon[,] dragons;
    private bool hasSubmitted;
    private Dictionary<ColorDragon.ColorType, int> probability;
    private int probabilitySum;
    private GameDragon pressedDragon;
    private GameDragon enteredDragon;
    private void Awake()
    {
        _instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        hasChangedBGM = false;
        hasSubmitted = false;
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

        /**
         * 说明：使用一个字典储存概率，概率为int类型，字典中所有value的和为probailitySum
         * 实际概率 = value / probabilitySum
         * 将所有类型的概率初始化为60，因为60的因数比较多，可以把特殊类型设定为其二分之一、三分之一...
         * 现在将PINK设为15，实际上就是"其他概率相等，PINK是其他颜色概率的四分之一"
         */
        probability = new Dictionary<ColorDragon.ColorType, int>();
        probabilitySum = 0;
        GameObject tempDragon = Instantiate(dragonPrefabDict[DragonType.NORMAL], FixPosition(0, -1), Quaternion.identity);
        ColorDragon temp = tempDragon.GetComponent<GameDragon>().ColoredComponent;

        for (int i = 0; i < temp.NumColors; i++)
        {
            probability[temp.ColorSprites[i].color] = 60;
        }
        probability[ColorDragon.ColorType.PINK] = 15;
        foreach (var i in probability)
        {
            probabilitySum += i.Value;
        }
        Destroy(tempDragon);


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
        clockAnime.GetComponent<AnimationManager>().ClockPlay();
        bgmOfGameOver.Stop();
        bgm.Play();
        hasChangedBGM = false;
        hasSubmitted = false;
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
            if (!hasSubmitted)
                submitDialog.gameObject.SetActive(true);
            if (!hasChangedBGM)
            {
                hasChangedBGM = true;
                bgmOfGameOver.Play(0);
            }

            return;
        }
        submitDialog.gameObject.SetActive(false);
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
                        //TODO: 这里的代码括号嵌套太深了 有时间帮忙优化一下
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
                dragons[x, 0].ColoredComponent.SetColor(generateRandomColor());
                hasNotFinished = true;
            }
        }
        return hasNotFinished;
    }

    /**
     * 根据概率生成随机的颜色
     * 例如：如果有三个颜色 Red 10, Yellow 20, Blue, 30
     * 先生成一个[1, 60]的随机数，再遍历字典，看这个随机数落在哪个区间
     * 颜色顺序不影响概率
     */
    private ColorDragon.ColorType generateRandomColor()
    {
        int random = Random.Range(1, probabilitySum);
        foreach(var i in probability)
        {
            if (random <= i.Value) return i.Key;
            random -= i.Value;
        }
        return 0;
    }

    //是否相邻
    private bool IsAdjacent(GameDragon dragon1, GameDragon dragon2)
    {
        return dragon1.X == dragon2.X && Mathf.Abs(dragon1.Y - dragon2.Y) == 1
            || dragon1.Y == dragon2.Y && Mathf.Abs(dragon1.X - dragon2.X) == 1;
    }

    private void PreSwapDragon(GameDragon dragon1, GameDragon dragon2)
    {
        int tmpX = dragon2.X, tmpY = dragon2.Y;
        dragon2.X = dragon1.X; dragon2.Y = dragon1.Y;
        dragon1.X = tmpX; dragon1.Y = tmpY;
        dragons[dragon2.X, dragon2.Y] = dragon2;
        dragons[dragon1.X, dragon1.Y] = dragon1;
    }

    public void SwapDragon(GameDragon dragon1, GameDragon dragon2)
    {
        if (dragon1.CanMove() && dragon2.CanMove())
        {

            PreSwapDragon(dragon1, dragon2);
            List<GameDragon> matchedDragons1 = MatchDragons(dragon1);
            List<GameDragon> matchedDragons2 = MatchDragons(dragon2);
            PreSwapDragon(dragon1, dragon2);
            if (matchedDragons1 != null || matchedDragons2 != null)
            {
                int tempX = dragon1.X;
                int tempY = dragon1.Y;
                dragon1.MovedComponent.Move(dragon2.X, dragon2.Y, fillTime);
                dragon2.MovedComponent.Move(tempX, tempY, fillTime);
                dragons[dragon2.X, dragon2.Y] = dragon2;
                dragons[dragon1.X, dragon1.Y] = dragon1;

                if (matchedDragons1 != null)
                    ClearMatchedList(matchedDragons1, true);
                if (matchedDragons2 != null)
                    ClearMatchedList(matchedDragons2, true);
                StartCoroutine(AllFill());
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
        if (!gameover && IsAdjacent(pressedDragon, enteredDragon))
            SwapDragon(pressedDragon, enteredDragon);
    }
    #endregion

    //匹配和清除模块
    #region
    //匹配方法
    public List<GameDragon> MatchDragons(GameDragon dragon)
    {
        if (dragon.CanColor())
        {
            ColorDragon.ColorType color = dragon.ColoredComponent.Color;
            List<GameDragon> tempDragons = new List<GameDragon>();
            List<GameDragon> matchedDragons = new List<GameDragon>();
            List<GameDragon> extendedDragons = new List<GameDragon>();
            int newX = dragon.X;
            int newY = dragon.Y;
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
                matchedDragons.Add(dragon);
                foreach (GameDragon matchedDragon in matchedDragons)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int currentX = matchedDragon.X + xStep[i];
                        int currentY = matchedDragon.Y + yStep[i];
                        if (currentX < 0 || currentX >= xColumn || currentY < 0 || currentY >= yRow) continue;
                        GameDragon current = dragons[currentX, currentY];
                        if (!current.CanColor() || current.ColoredComponent.Color != color)
                        {
                            if (current.Type != DragonType.BARRIER && color != ColorDragon.ColorType.RED) continue;
                        }
                        if (matchedDragons.IndexOf(current) == -1 && extendedDragons.IndexOf(current)==-1)
                            extendedDragons.Add(current);
                    }

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
                    var matchList = MatchDragons(dragons[x, y]);
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
                if (matchedDragon.Type == DragonType.BARRIER)
                    count++;
                //  再加一个

                if (matchedDragon.CanColor() && matchedDragon.ColoredComponent.Color == ColorDragon.ColorType.PINK)
                    gameTime += 2;
                score += 5 * (int)Mathf.Pow(2, count);
                hasCleared = true;
            }
        }
        if (hasCleared&&needsPlaySound)
            disappearAudio.Play(0);
        return hasCleared;
    }
    #endregion

    public void OnClickSubmitButton()
    {
        Assets.Scripts.RankDAO.insertIntoTable(nameField.text, score);
        print(nameField.text);
        print(score);
        submitDialog.gameObject.SetActive(false);
        hasSubmitted = true;
    }

    public void OnClickGiveUp()
    {
        SceneManager.LoadScene("Main");
    }
}
