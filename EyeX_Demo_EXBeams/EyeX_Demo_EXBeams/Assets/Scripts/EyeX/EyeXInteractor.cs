//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------
using Tobii.EyeX.Client;
using Tobii.EyeX.Framework;
using UnityEngine;
using Rect = UnityEngine.Rect;

/// <summary>
/// Represents an EyeX interactor in a Unity game/application. Used with the EyeX host.
/// </summary>
public class EyeXInteractor
{
    private string _id;
    private string _parentId;
    public EyeXBehaviors _behaviors;
    private BehaviorAssignmentCallback _behaviorCallback;
    private EyeXEventHandler _eventHandler;

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="interactorId">Interactor ID.</param>
    /// <param name="parentId">Parent interactor ID.</param>
    /// <param name="behaviors">Pre-defined interaction behaviors to be assigned to the interactor.</param>
    /// <param name="eventHandler">Event handler function.</param>
    public EyeXInteractor(string interactorId, string parentId, EyeXBehaviors behaviors, EyeXEventHandler eventHandler)
    {
        _id = interactorId;
        _parentId = parentId;
        _behaviors = behaviors;
        _eventHandler = eventHandler;
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="interactorId">Interactor ID.</param>
    /// <param name="parentId">Parent interactor ID.</param>
    /// <param name="behaviors">Pre-defined interaction behaviors to be assigned to the interactor.</param>
    /// <param name="behaviorCallback">Function for assigning custom behaviors to the interactor.</param>
    /// <param name="eventHandler">Event handler function.</param>
    public EyeXInteractor(string interactorId, string parentId, EyeXBehaviors behaviors, BehaviorAssignmentCallback behaviorCallback, EyeXEventHandler eventHandler)
    {
        _id = interactorId;
        _parentId = parentId;
        _behaviors = behaviors;
        _behaviorCallback = behaviorCallback;
        _eventHandler = eventHandler;
    }

    /// <summary>
    /// Interactor ID.
    /// </summary>
    public string Id
    {
        get { return _id; }
    }

    /// <summary>
    /// Interactor location in game coordinates.
    /// </summary>
    public ProjectedRect Location { get; set; }

    /// <summary>
    /// Interactor stencil mask.
    /// </summary>
    public EyeXMask Mask { get; set; }

    /// <summary>
    /// Adds the interactor to the given snapshot.
    /// </summary>
    /// <param name="snapshot">Interaction snapshot.</param>
    /// <param name="windowId">ID of the game window.</param>
    /// <param name="gameWindowPosition">Position of the game window in screen coordinates.</param>
    public void AddToSnapshot(InteractionSnapshot snapshot, string windowId, Vector2 gameWindowPosition)
    {
        var interactor = snapshot.CreateInteractor(_id, _parentId, windowId);

        var bounds = interactor.CreateBounds(InteractionBoundsType.Rectangular);
        bounds.SetRectangularData(Location.rect.x + gameWindowPosition.x, Location.rect.y + gameWindowPosition.y, Location.rect.width, Location.rect.height);

        interactor.Z = Location.relativeZ;

        if (Mask != null && 
            Mask.Type != EyeXMaskType.None)
        {
            interactor.CreateMask(MaskType.Default, Mask.Size, Mask.Size, Mask.MaskData);
        }

        if ((_behaviors & EyeXBehaviors.Activatable) != 0)
        {
            var behaviorParams = new ActivatableParams { EnableTentativeFocus = EyeXBoolean.False };
            interactor.SetActivatableBehavior(ref behaviorParams);
        }

        if ((_behaviors & EyeXBehaviors.ActivatableWithTentativeFocus) != 0)
        {
            var behaviorParams = new ActivatableParams { EnableTentativeFocus = EyeXBoolean.True };
            interactor.SetActivatableBehavior(ref behaviorParams);
        }

        if ((_behaviors & EyeXBehaviors.GazeAware) != 0)
        {
            interactor.CreateBehavior(InteractionBehaviorType.GazeAware);
        }

        if ((_behaviors & EyeXBehaviors.GazeAwareWithInertia) != 0)
        {
            var gazeAwareParams = new GazeAwareParams() { GazeAwareMode = GazeAwareMode.Delayed, DelayTime = 500 };
            interactor.SetGazeAwareBehavior(ref gazeAwareParams);
        }

        if (_behaviorCallback != null)
        {
            _behaviorCallback(_id, interactor);
        }
    }

    /// <summary>
    /// Invokes the event handler function.
    /// </summary>
    /// <param name="event">Event object.</param>
    public void HandleEvent(InteractionEvent event_)
    {
        if (_eventHandler != null)
        {
            _eventHandler(_id, event_);
        }
    }

    /// <summary>
    /// Tells whether the interactor intersects with a given rectangle.
    /// </summary>
    /// <param name="rectangle">Bounds in GUI coordinates.</param>
    /// <returns>Result.</returns>
    public bool IntersectsWith(Rect rectangle)
    {
        return Location.isValid &&
            rectangle.Overlaps(Location.rect);
    }
}
