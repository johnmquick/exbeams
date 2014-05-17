//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Tobii.EyeX.Client;
using Tobii.EyeX.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Rect = UnityEngine.Rect;

/// <summary>
/// Provides the main point of contact with the EyeX Engine. 
/// Hosts an EyeX context and responds to engine queries using a repository of interactors.
/// </summary>
public class EyeXHost : MonoBehaviour
{
    /// <summary>
    /// Special interactor ID indicating that an interactor doesn't have a parent.
    /// </summary>
    public const string NoParent = Literals.RootId;

    private static EyeXHost _instance;

    private object _lock = new object();
    private InteractionSystem _system;
    private InteractionContext _context;
    private Dictionary<string, EyeXGlobalInteractor> _globalInteractors = new Dictionary<string, EyeXGlobalInteractor>();
    private Dictionary<string, EyeXInteractor> _interactors = new Dictionary<string, EyeXInteractor>();
    private Vector2 _gameWindowPosition = new Vector2(float.NaN, float.NaN);
    private bool _isConnected;

    /// <summary>
    /// Gets the singleton EyeXHost instance.
    /// Users of this class should store a reference to the singleton instance in their Awake() method, or similar.
    /// </summary>
    /// <returns>The instance.</returns>
    public static EyeXHost GetInstance()
    {
        if (_instance == null)
        {
            // create a game object with a new instance of this class attached as a component.
            // (there's no need to keep a reference to the game object, because game objects are not garbage collected.)
            print("Creating new EyeXHost instance.");
            var container = new GameObject();
            container.name = "EyeXHostContainer";
            DontDestroyOnLoad(container);
            _instance = container.AddComponent(typeof(EyeXHost)) as EyeXHost;
        }

        return _instance;
    }

    public void Start()
    {
        InitializeEyeX();
        InitializeContext();
    }

    public void Update()
    {
        // update the game window position, in case the game window has been moved or resized.
        _gameWindowPosition = ScreenHelpers.Instance.GetGameWindowPosition();
    }

    public void OnApplicationQuit()
    {
        ShutdownEyeX();
    }

    /// <summary>
    /// Registers an interactor with the repository.
    /// </summary>
    /// <param name="interactor">Interactor.</param>
    public void RegisterInteractor(EyeXInteractor interactor)
    {
        lock (_lock)
        {
            _interactors[interactor.Id] = interactor;
        }
    }

    /// <summary>
    /// Gets an interactor from the repository.
    /// </summary>
    /// <param name="interactorId">ID of the interactor.</param>
    /// <returns>Interactor, or null if not found.</returns>
    public EyeXInteractor GetInteractor(string interactorId)
    {
        lock (_lock)
        {
            EyeXInteractor interactor = null;
            _interactors.TryGetValue(interactorId, out interactor);
            return interactor;
        }
    }

    /// <summary>
    /// Removes an interactor from the repository.
    /// </summary>
    /// <param name="interactorId">ID of the interactor.</param>
    public void UnregisterInteractor(string interactorId)
    {
        lock (_lock)
        {
            _interactors.Remove(interactorId);
        }
    }

    /// <summary>
    /// Registers a global interactor with the repository.
    /// </summary>
    /// <param name="interactor">Interactor.</param>
    public void RegisterGlobalInteractor(EyeXGlobalInteractor globalInteractor)
    {
        lock (_lock)
        {
            _globalInteractors[globalInteractor.Id] = globalInteractor;
        }

        if (_isConnected)
        {
            CommitGlobalInteractors(new[] { globalInteractor });
        }
    }

    /// <summary>
    /// Removes a global interactor from the repository. 
    /// </summary>
    /// <param name="interactorId">ID of the interactor.</param>
    public void UnregisterGlobalInteractor(string interactorId)
    {
        var interactorToDelete = GetGlobalInteractor(interactorId);

        lock (_lock)
        {
            _globalInteractors.Remove(interactorId);
        }

        if (_isConnected && interactorToDelete != null)
        {
            interactorToDelete.IsMarkedForDeletion = true;
            CommitGlobalInteractors(new[] { interactorToDelete });
        }
    }

    /// <summary>
    /// Gets an interactor from the repository.
    /// </summary>
    /// <param name="interactorId">ID of the interactor.</param>
    /// <returns>Interactor, or null if not found.</returns>
    public EyeXGlobalInteractor GetGlobalInteractor(string interactorId)
    {
        lock (_lock)
        {
            EyeXGlobalInteractor interactor = null;
            _globalInteractors.TryGetValue(interactorId, out interactor);
            return interactor;
        }
    }

    private EyeXHost()
    {
    }

    private static string GetErrorMessage(AsyncData asyncData)
    {
        string errorMessage;
        if (asyncData.TryGetPropertyValue<string>(Literals.ErrorMessage, out errorMessage))
        {
            return errorMessage;
        }
        else
        {
            return "Unspecified error.";
        }
    }

    private void CommitGlobalInteractors(IEnumerable<EyeXGlobalInteractor> globalInteractors)
    {
        try
        {
            var snapshot = CreateGlobalInteractorSnapshot();
            foreach (var globalInteractor in globalInteractors)
            {
                globalInteractor.AddToSnapshot(snapshot);
            }

            snapshot.CommitAsync(OnSnapshotCommitted);
        }
        catch (InteractionApiException ex)
        {
            print("EyeX operation failed: " + ex.Message);
        }
    }

    private InteractionSnapshot CreateGlobalInteractorSnapshot()
    {
        var snapshot = _context.CreateSnapshot();
        snapshot.CreateBounds(InteractionBoundsType.None);
        snapshot.AddWindowId(Literals.GlobalInteractorWindowId);

        return snapshot;
    }

    private void InitializeEyeX()
    {
        try
        {
            // Note: the Unity console logger can be enabled if/when needed.
            // it is disabled by default, because it leaves a dangling reference 
            // the first time the game is run, and might cause Unity to crash when 
            // the game is restarted.
            _system = InteractionSystem.Initialize();
            //_system = InteractionSystem.Initialize(CreateUnityConsoleLogger());

            print("EyeX initialization succeeded.");
        }
        catch (InteractionApiException ex)
        {
            print("EyeX initialization failed: " + ex.Message);
        }
    }

    private void InitializeContext()
    {
        try
        {
            _context = new InteractionContext(false);
            _context.RegisterQueryHandlerForCurrentProcess(HandleQuery);
            _context.RegisterEventHandler(HandleEvent);
            _context.ConnectionStateChanged += OnConnectionStateChanged;
            _context.EnableConnection();

            print("EyeX context initialization succeeded.");
        }
        catch (InteractionApiException ex)
        {
            print("EyeX context initialization failed: " + ex.Message);
        }
    }

    private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
    {
        Console.WriteLine("The connection state is now {0}.", e.State);

        if (e.State == ConnectionState.Connected)
        {
            _isConnected = true;

            if (_globalInteractors.Count == 0) return;

            // commit the snapshot with the global interactor as soon as the connection to the engine is established.
            // (it cannot be done earlier because committing means "send to the engine".)
            // make a copy of the collection of interactors to avoid race conditions.
            List<EyeXGlobalInteractor> globalInteractorsCopy;
            lock (_lock)
            {
                globalInteractorsCopy = new List<EyeXGlobalInteractor>(_globalInteractors.Values);
            }

            CommitGlobalInteractors(globalInteractorsCopy);
        }
        else
        {
            _isConnected = false;
        }
    }

    private void OnSnapshotCommitted(AsyncData asyncData)
    {
        ResultCode resultCode;
        if (!asyncData.TryGetResultCode(out resultCode)) return;

        if (resultCode == ResultCode.InvalidSnapshot)
        {
            Debug.LogWarning("Snapshot validation failed: " + GetErrorMessage(asyncData));
        }
        else if (resultCode != ResultCode.Ok)
        {
            Debug.LogWarning("Could not commit snapshot: " + GetErrorMessage(asyncData));
        }
    }

    private void ShutdownEyeX()
    {
        try
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }

            if (_system != null)
            {
                _system.Dispose();
                _system = null;
            }

            print("EyeX shutdown finished.");
        }
        catch (InteractionApiException ex)
        {
            print("EyeX shutdown failed: " + ex.Message);
        }
    }

    private void HandleQuery(InteractionQuery query)
    {
        // NOTE: this method is called from a worker thread, so it must not access any game objects.

        try
        {
            // The mechanism that we use for getting the window ID assumes that the game window is on top 
            // when the scripts start running. It usually does the right thing, but not always. 
            // So adjust if necessary.
            var queryWindowIdEnum = query.WindowIds.GetEnumerator();
            if (queryWindowIdEnum.MoveNext())
            {
                if (queryWindowIdEnum.Current != ScreenHelpers.Instance.GameWindowId)
                {
                    print(string.Format("Window ID mismatch: queried for {0}, expected {1}. Adjusting.", queryWindowIdEnum.Current, ScreenHelpers.Instance.GameWindowId));
                    ScreenHelpers.Instance.GameWindowId = queryWindowIdEnum.Current;
                    _gameWindowPosition = new Vector2(float.NaN, float.NaN);
                }
            }

            if (float.IsNaN(_gameWindowPosition.x))
            {
                // We don't have a valid game window position, so we cannot respond to any queries at this time.
                return;
            }

            // Get query bounds and map them to GUI coordinates.
            double boundsX, boundsY, boundsWidth, boundsHeight;
            query.Bounds.TryGetRectangularData(out boundsX, out boundsY, out boundsWidth, out boundsHeight);
            var queryRectInGuiCoordinates = new Rect(
                (float)(boundsX - _gameWindowPosition.x),
                (float)(boundsY - _gameWindowPosition.y),
                (float)boundsWidth,
                (float)boundsHeight);

            // Make a copy of the collection of interactors to avoid race conditions.
            List<EyeXInteractor> interactorsCopy;
            lock (_lock)
            {
                interactorsCopy = new List<EyeXInteractor>(_interactors.Values);
            }

            // Create the snapshot and add the interactors that intersect with the query bounds.
            var snapshot = _context.CreateSnapshotWithQueryBounds(query);
            snapshot.AddWindowId(ScreenHelpers.Instance.GameWindowId);
            foreach (var interactor in interactorsCopy)
            {
                if (interactor.IntersectsWith(queryRectInGuiCoordinates))
                {
                    interactor.AddToSnapshot(snapshot, ScreenHelpers.Instance.GameWindowId, _gameWindowPosition);
                }
            }

            // Commit the snapshot.
            snapshot.CommitAsync(null);
        }
        catch (InteractionApiException ex)
        {
            print("EyeX query handler failed: " + ex.Message);
        }
    }

    private void HandleEvent(InteractionEvent event_)
    {
        // NOTE: this method is called from a worker thread, so it must not access any game objects.

        try
        {
            // Route the event to the appropriate interactor, if any.
            var interactorId = event_.InteractorId;
            var interactor = GetInteractor(interactorId);
            if (interactor != null)
            {
                interactor.HandleEvent(event_);
            }
            else
            {
                var globalInteractor = GetGlobalInteractor(interactorId);
                if (globalInteractor != null)
                {
                    globalInteractor.HandleEvent(event_);
                }
            }
        }
        catch (InteractionApiException ex)
        {
            print("EyeX event handler failed: " + ex.Message);
        }
    }

    private static LogWriterHandler CreateUnityConsoleLogger()
    {
        return new LogWriterHandler((level, scope, message) =>
        {
            if (level >= LogLevel.Warning)
            {
                print(string.Format("[{0}] {1}: {2}\n", level, scope, message));
            }
        });
    }
}

/// <summary>
/// A function that assigns behaviors to an interactor.
/// </summary>
/// <param name="interactorId">ID of the interactor.</param>
/// <param name="interactor">Interactor object.</param>
public delegate void BehaviorAssignmentCallback(string interactorId, Interactor interactor);

/// <summary>
/// A function that handles events from the EyeX Engine.
/// </summary>
/// <param name="interactorId">ID of the interactor.</param>
/// <param name="event">Event object.</param>
public delegate void EyeXEventHandler(string interactorId, InteractionEvent event_);
