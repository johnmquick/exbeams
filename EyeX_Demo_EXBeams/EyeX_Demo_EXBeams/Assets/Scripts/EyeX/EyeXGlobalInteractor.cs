//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------
using Tobii.EyeX.Client;
using Tobii.EyeX.Framework;

/// <summary>
/// Represents an EyeX global interactor (= data stream) in a Unity game/application. Used with the EyeX host.
/// </summary>
public class EyeXGlobalInteractor
{
    private readonly string _id;
    private readonly BehaviorAssignmentCallback _behaviorCallback;
    private readonly EyeXEventHandler _eventHandler;

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="interactorId">Interactor ID.</param>
    /// <param name="behaviorCallback">Function for assigning custom behaviors to the interactor.</param>
    /// <param name="eventHandler">Event handler function.</param>
    public EyeXGlobalInteractor(string interactorId, BehaviorAssignmentCallback behaviorCallback, EyeXEventHandler eventHandler)
    {
        _id = interactorId;
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
    /// If set to true, the global interactor will be removed.
    /// </summary>
    public bool IsMarkedForDeletion { get; set; }

    /// <summary>
    /// Adds the interactor to the given snapshot.
    /// </summary>
    /// <param name="snapshot">Interaction snapshot.</param>
    public void AddToSnapshot(InteractionSnapshot snapshot)
    {
        var interactor = snapshot.CreateInteractor(_id, Literals.RootId, Literals.GlobalInteractorWindowId);
        interactor.CreateBounds(InteractionBoundsType.None);

        if (_behaviorCallback != null)
        {
            _behaviorCallback(_id, interactor);
        }

        if (IsMarkedForDeletion)
        {
            interactor.IsDeleted = true;
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
}
