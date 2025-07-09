/*
* Author: Joshua Elkins
* Purpose: Script that works for the level select menu
* There is a function for scene transition for each major level
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelect : MonoBehaviour
{
    public void SelectLevel1()
    {
        SceneManager.LoadScene(1);
    }
	
	public void SelectLevel2(){
		SceneManager.LoadScene(2);
	}
	
	public void SelectLevel3(){
		SceneManager.LoadScene(3);
	}
	
	public void SelectLevel4(){
		SceneManager.LoadScene(4);
	}
	
	public void SelectLevel5(){
		SceneManager.LoadScene(5);
	}
}
