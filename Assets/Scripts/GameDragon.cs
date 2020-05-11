using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDragon : MonoBehaviour
{
    private int x;
    private int y;
    private GameManager.DragonType type;
    private MovedDragon movedComponent;
    private ColorDragon coloredComponent;
    private ClearDragon clearComponent;
    public int X
    {
        get
        {
            return x;
        }
        set
        {
            if (CanMove())
            {
                x = value;
            }
        }
    }
    public int Y
    {
        get
        {
            return y;
        }
        set
        {
            if (CanMove())
            {
                y = value;
            }
        }
    }


    public GameManager.DragonType Type { get => type; }
    public MovedDragon MovedComponent { get => movedComponent; }
    public ColorDragon ColoredComponent { get => coloredComponent; }
    public ClearDragon ClearComponent { get => clearComponent; }

    [HideInInspector]
    public GameManager gameManager;

    public bool CanMove()
    {
        return movedComponent != null;
    }
    public bool CanColor()
    {
        return coloredComponent != null;
    }

    public bool CanClear()
    {
        return clearComponent != null;
    }
    private void Awake()
    {
        movedComponent = GetComponent<MovedDragon>();
        coloredComponent = GetComponent<ColorDragon>();
        clearComponent = GetComponent<ClearDragon>();
    }
    public void Init(int _x, int _y, GameManager _gameManager, GameManager.DragonType _type)
    {
        x = _x;
        y = _y;
        gameManager = _gameManager;
        type = _type;
    }

    private void OnMouseEnter()
    {
        gameManager.EnterDragon(this);
    }

    private void OnMouseDown()
    {
        gameManager.PressDragon(this);
    }

    private void OnMouseUp()
    {
        gameManager.ReleaseDragon();
    }
}
