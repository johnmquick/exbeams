/*
 * 
 * All content created and copyright © 2014 by John M. Quick.
 * 
*/

using UnityEngine;
using System.Collections;

//manages the application loader to establish singleton objects
public class LoaderManager : MonoBehaviour {
    //constants
    private const float PIXELS_TO_UNITS = 1.0f / 100.0f; //default pixels to units conversion for rendered textures

    //awake
    //called before start function
    void Awake() {
        //set the camera's size based on the current resolution and pixels to units ratio
        Camera.main.orthographicSize = Screen.height * PIXELS_TO_UNITS / 2; //half of the current window size

        //transition into scene via state manager
        if (StateManager.Instance != null) {
            TransitionFade theTransition = StateManager.Instance.gameObject.GetComponent<TransitionFade>();
            theTransition.toggleFade();
        }

        //audio manager
        //initialize since this is the first scene
        if (AudioManager.Instance != null) {
            //set the transition effects for the audio manager
            //bgm
            AudioManager.Instance.switchBgmAfterDelay(AudioManager.Instance.bgmMenu, 0.0f);
            AudioManager.Instance.bgmIsFadingIn = true;
            AudioManager.Instance.bgmIsHoldFade = false;
        }

    }

	//init
	void Start () {
        //show mouse cursor
        Screen.showCursor = false;

        //transition after delay
        Invoke("transition", 3.0f);

	} //end function

    //transition
    private void transition() {

        //proceed to main menu scene
        Debug.Log("[LoaderManager] Load Main Menu");
        
        //transition to next scene
        TransitionFade theTransition = StateManager.Instance.gameObject.GetComponent<TransitionFade>();
        theTransition.toggleFade();
        StateManager.Instance.switchSceneAfterDelay("mainMenu", theTransition.duration);

    } //end function

} //end class
