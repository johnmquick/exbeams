/*
 * 
 * All content created and copyright © 2014 by John M. Quick.
 * 
*/

using UnityEngine;
using System.Collections;

//for managing object movement based on gaze data
//converts gaze coordinates into a world position based on specified limits
public class GazeMove : MonoBehaviour {
    //for moving object based on gaze coordinates from tracker
    //based on resolution of window
    //receives object, renderer, bounds script, and data script
    //returns vector3 position in world coordinates
    public Vector3 positionObjectWithRendererBoundsData(GameObject theObject, Renderer theRenderer, BoundsCheck theBounds, EyeXGazePoint theData) {

        //check bounds
        //use the object with the outermost bounds and a renderer to make the check
        Vector3 boundsVector = theBounds.checkBoundsForRenderer(theRenderer, theData.Viewport);

        //convert viewport vector to world position vector
        Vector3 worldPos = Camera.main.ViewportToWorldPoint(boundsVector);
        worldPos.z = theObject.transform.position.z; //maintain z position for object

        //return new world position
        return worldPos;

    } //end function
} //end class
