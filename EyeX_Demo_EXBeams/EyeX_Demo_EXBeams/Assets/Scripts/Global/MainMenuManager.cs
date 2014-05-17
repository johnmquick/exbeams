/*
 * 
 * All content created and copyright © 2014 by John M. Quick.
 * 
*/

using UnityEngine;
using System.Collections;

//manages the application states from the main menu
public class MainMenuManager : MonoBehaviour {
    //constants
    private const float PIXELS_TO_UNITS = 1.0f / 100.0f; //default pixels to units conversion for rendered textures

    //awake
    //called before start function
    void Awake() {
        //set the camera's size based on the current resolution and pixels to units ratio
        Camera.main.orthographicSize = Screen.height * PIXELS_TO_UNITS / 2; //half of the current window size
    }

	//init
	void Start () {
        //show mouse cursor
        Screen.showCursor = true;

        //transition into scene via state manager
        if (StateManager.Instance != null) {
            TransitionFade theTransition = StateManager.Instance.gameObject.GetComponent<TransitionFade>();
            theTransition.toggleFade();
        }

        //audio
        if (AudioManager.Instance != null && 
            AudioManager.Instance.bgmSource.volume != 0.8f) { //for first fade in from loader, ignore toggle; afterwards toggle when returning back to main menu
            AudioManager.Instance.toggleFade();
        }


	} //end function

    //GUI
    void OnGUI() {
        //set gui skin
        if (GameObject.FindWithTag("audioManager")) {
            GUI.skin = GameObject.FindWithTag("audioManager").GetComponent<AudioManager>().menuGUI;
        }

        //create buttons
        int btnW = 300;
        int btnH = 100;
        float btnX = Screen.width / 2 - btnW / 2;
        float btnY = Screen.height / 2 + btnH / 2; //aligned center

        //play button
        Rect btnPlayRect = new Rect(btnX, btnY, btnW, btnH);
        string btnPlayText = "Play";
        
        //quit button
        float btnQuitW = 100;
        float btnQuitH = 50;
        float btnQuitBuffer = 10;
        float btnQuitX = Screen.width - btnQuitW - btnQuitBuffer; //aligned to bottom-right of screen
        float btnQuitY = Screen.height - btnQuitH - btnQuitBuffer;
        Rect btnQuitRect = new Rect(btnQuitX, btnQuitY, btnQuitW, btnQuitH);
        string btnQuitText = "Quit";
        
        //play button pressed
        if (GUI.Button(btnPlayRect, btnPlayText)) {
            //proceed to game scene
            Debug.Log("Load Game");
            //transition to next scene
            TransitionFade theTransition = StateManager.Instance.gameObject.GetComponent<TransitionFade>();
            theTransition.toggleFade();
            StateManager.Instance.switchSceneAfterDelay("game", theTransition.duration);
            //audio
            AudioManager.Instance.playBtnClick(); //sfx
            AudioManager.Instance.toggleFade(); //bgm
            AudioManager.Instance.switchBgmAfterDelay(AudioManager.Instance.bgmGame, AudioManager.Instance.bgmFadeDuration);
        }
        
        //quit button pressed
        if (GUI.Button(btnQuitRect, btnQuitText)) {
            //quit application
            Debug.Log("Quit Application");
            Application.Quit();
        }
        
    } //end function

} //end class
