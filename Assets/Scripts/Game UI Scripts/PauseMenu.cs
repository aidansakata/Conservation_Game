/*
* Author: Joshua Elkins
* Purpose: Script that controls the behavior of the game's pause screen
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
	// Set of Unity entities
	[SerializeField] private Canvas mainCanvas;
	[SerializeField] private Canvas pauseCanvas;
	[SerializeField] private Button resumeButton;
	[SerializeField] private Button quitButton;
	bool isPaused = false;
	
    // Start is called before the first frame update
	// Instantiates all my buttons and sets state
	// of canvases
    void Start()
    {
        pauseCanvas.gameObject.SetActive(false);
		mainCanvas.gameObject.SetActive(true);
		resumeButton.onClick.AddListener(resumeGame);
		quitButton.onClick.AddListener(returnToMain);
    }

    // Update is called once per frame
	// Checks for the escape button press and loads/unloads the pause
	// menu if it detects it
    void Update()
    {
        if(Input.GetKeyDown("escape")){
			if(isPaused == false){
				pauseCanvas.gameObject.SetActive(true);
				mainCanvas.gameObject.SetActive(false);
				isPaused = true;
			}
			else{
				mainCanvas.gameObject.SetActive(true);
				pauseCanvas.gameObject.SetActive(false);
				isPaused = false;
			}
		}
    }
	
	// This function runs on a resume button press
	// It adjusts the visibilities of the canvases
	// and sets a "isPaused" variable to false. 
	public void resumeGame(){
		mainCanvas.gameObject.SetActive(true);
		pauseCanvas.gameObject.SetActive(false);
		isPaused = false;
	}
	
	// When Main menu button is pressed, return to main menu scene
	public void returnToMain(){
		SceneManager.LoadScene(0);
	}
}
