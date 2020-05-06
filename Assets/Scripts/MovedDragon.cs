using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovedDragon : MonoBehaviour
{
    private GameDragon dragon;

    private IEnumerator moveCoroutine;
    private void Awake()
    {
        dragon = GetComponent<GameDragon>();
    }

    //开启或者结束协程
    public void Move(int newX, int newY, float time)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        moveCoroutine = MoveCoroutine(newX, newY, time);
        StartCoroutine(moveCoroutine);

    }

    //负责移动的协程
    private IEnumerator MoveCoroutine(int newX, int newY, float time)
    {
        dragon.X = newX;
        dragon.Y = newY;

        Vector3 startPos = transform.position;
        Vector3 endPos = dragon.gameManager.FixPosition(newX, newY);
        for (float t = 0; t < time; t += Time.deltaTime)
        {
            dragon.transform.position = Vector3.Lerp(startPos, endPos, t / time);
            yield return 0;
        }

        dragon.transform.position = endPos;
    }
}
