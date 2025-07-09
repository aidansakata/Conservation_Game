
/*
* Author: Joshua Elkins
* Purpose: Script that controls the behavior of the game's endlevel screen
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class EndLevelMenu : MonoBehaviour
{
	// Field of Unity entities
	[SerializeField] private Canvas mainCanvas;
	[SerializeField] private Canvas EndCanvas;
	[SerializeField] private Button retryButton;
	[SerializeField] private Button nextButton;
	[SerializeField] private Button quitButton;
	[SerializeField] private TMP_Text endScore;
	[SerializeField] private Text mainScore;
	[SerializeField] private Button submitButton;
	
    // Start is called before the first frame update
	// Instantiates all my event listeners, and sets my two canvas states
    void Start()
    {
        EndCanvas.gameObject.SetActive(false);
		mainCanvas.gameObject.SetActive(true);
		retryButton.onClick.AddListener(retryLevel);
		quitButton.onClick.AddListener(returnToMain);
		nextButton.onClick.AddListener(nextLevel);
		submitButton.onClick.AddListener(loadEndCanvas);
    }

    // Update is called once per frame
	// empty function
    void Update()
    {
       
    }
	
	// When the submit button is pushed, this function is ran
	// It hides the main UI and reveals the end screen. 
	// It also sets the endScore text field on the end screen. 
	public void loadEndCanvas(){
		mainCanvas.gameObject.SetActive(false);
		EndCanvas.gameObject.SetActive(true);
		endScore.text = mainScore.text;
	}
	
	// When the next level button is pushed, the next scene gets loaded
	// if the last scene is loaded, load level 1 again.
	public void nextLevel(){
		if(SceneManager.GetActiveScene().buildIndex != 5)
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex+1);
		else
			SceneManager.LoadScene(1);
	}
	
	// It returns you to main when the main menu button is pressed.
	public void returnToMain(){
		SceneManager.LoadScene(0);
	}
	
	// It reloads the current scene. 
	public void retryLevel(){
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}
}
