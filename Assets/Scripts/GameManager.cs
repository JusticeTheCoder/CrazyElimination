using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum DragonType
    {
        EMPTY,
        NORMAL,
        BARRIOR,
        ROW_CLEAR,
        COLUMN_CLEAR,
        SPECIALDRAGON,
        COUNT //标记类型
    }

    //龙预制字典，通过种类来得到对应的游戏物体
    private Dictionary<DragonType, GameObject> dragonPrefabDict;

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
    public GameObject gridPrefab;

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

        Destroy(dragons[4, 4].gameObject);
        CreateNewDragon(4, 4, DragonType.BARRIOR);

        StartCoroutine(AllFill());
    }

    public Vector3 FixPosition(int x, int y)
    {
        return new Vector3(transform.position.x - xColumn / 2f + x, transform.position.y + yRow / 2f - y, 0);
    }

    // Update is called once per frame
    void Update()
    {

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
        while (Fill())
        {
            yield return new WaitForSeconds(fillTime);
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
            int tempX = dragon1.X;
            int tempY = dragon1.Y;
            dragon1.MovedComponent.Move(dragon2.X, dragon2.Y, fillTime);
            dragon2.MovedComponent.Move(tempX, tempY, fillTime);
        }
    }

    public void PressDragon(GameDragon dragon)
    {
        pressedDragon = dragon;
    }

    public void EnterDragon(GameDragon dragon)
    {
        enteredDragon = dragon;
    }

    public void ReleaseDragon()
    {
        if (IsFriend(pressedDragon, enteredDragon))
            SwapDragon(pressedDragon, enteredDragon);
    }
}
