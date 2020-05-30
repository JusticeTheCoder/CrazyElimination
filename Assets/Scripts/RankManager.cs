using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RankManager : MonoBehaviour
{
    // Start is called before the first frame update
    public Text rankText;
    void Start()
    {
        rankText = transform.GetComponent<Text>();
        rankText.text = Assets.Scripts.RankDAO.selectFromDb();
    }

    // Update is called once per frame
    void Update()
    {
        
    }    
    
}


