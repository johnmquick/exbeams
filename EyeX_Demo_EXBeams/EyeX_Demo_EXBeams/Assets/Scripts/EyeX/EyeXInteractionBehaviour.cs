//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using Tobii.EyeX.Client;
using Tobii.EyeX.Framework;
using UnityEngine;

/// <summary>
/// Base class for EyeX interactors using game object bounds and game object ID
/// </summary>
public abstract class EyeXInteractionBehaviour : MonoBehaviour
{
    // The set of interaction behaviors that this interactor should respond to.
    [BitMask(typeof(EyeXBehaviors))]
    public EyeXBehaviors eyeXBehaviors;

    // Size (resolution) of the mask used for this interactor. The default setting, which is 
    // "None", makes the interactor fill the entire projected bounds.
    public EyeXMaskType maskType;

    // Option to draw the projected bounds of the interactor. For debugging purposes.
    public bool showProjectedBounds = false;

    private EyeXHost _eyeXHost;
    private string _interactorId;

    public void Awake()
    {
        _eyeXHost = EyeXHost.GetInstance();
        _interactorId = gameObject.GetInstanceID().ToString();
    }

    protected void Update()
    {
        var interactor = _eyeXHost.GetInteractor(_interactorId);
        UpdateInteractorLocation(interactor);
    }

    /// <summary>
    /// Register the interactor when the game object is enabled.
    /// </summary>
    public void OnEnable()
    {
        var interactor = new EyeXInteractor(_interactorId, EyeXHost.NoParent, eyeXBehaviors, OnEyeXEvent);
        UpdateInteractorLocation(interactor);
        _eyeXHost.RegisterInteractor(interactor);
    }

    /// <summary>
    /// Unregister the interactor when the game object is disabled.
    /// </summary>
    public void OnDisable()
    {
        _eyeXHost.UnregisterInteractor(_interactorId);
    }

    /// <summary>
    /// Draw the projected bounds if 'showProjectedBounds' is enabled.
    /// NOTE: Could be replaced by Gizmos
    /// </summary>
    public void OnGUI()
    {
        if (showProjectedBounds)
        {
            var face = ProjectedRect.GetProjectedRect(gameObject, Camera.main);
            if (face.isValid)
            {
                GUI.Box(face.rect, _interactorId);
            }
        }
    }

    /// <summary>
    /// Override this event handler to handle the events
    /// </summary>
    /// <param name="interactorId">The id of the interactor</param>
    /// <param name="event">The interaction event</param>
    protected abstract void OnEyeXEvent(string interactorId, InteractionEvent event_);

    private void UpdateInteractorLocation(EyeXInteractor interactor)
    {
        var location = ProjectedRect.GetProjectedRect(gameObject, Camera.main);

        EyeXMask mask = null;
        if (location.isValid && 
            maskType != EyeXMaskType.None)
        {
            // Create a mask for the interactor and move it to the top.
            mask = CreateMask(location.rect, Screen.height);
            location.relativeZ = Camera.main.farClipPlane;
        }

        interactor.Location = location;
        interactor.Mask = mask;
    }

    private EyeXMask CreateMask(UnityEngine.Rect locationRect, int screenHeight)
    {
        var mask = new EyeXMask(maskType);

        int size = mask.Size;
        for (int row = 0; row < size; row++)
        {
            var y = screenHeight - (locationRect.yMin + (row + 0.5f) * locationRect.height / (size - 1));

            for (int col = 0; col < size; col++)
            {
                var x = locationRect.xMin + (col + 0.5f) * locationRect.width / (size - 1);

                var ray = Camera.main.ScreenPointToRay(new Vector3(x, y, 0));
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo) &&
                    hitInfo.collider.gameObject.Equals(gameObject))
                {
                    mask[row, col] = MaskWeights.Default;
                }
            }
        }

        return mask;
    }
}
