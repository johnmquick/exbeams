//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Tobii.EyeX.Client;
using Tobii.EyeX.Framework;

/// <summary>
/// Provides a single access point for the last gaze point. 
/// 
/// Recommended usage:
/// Retrieve the last gaze point in a game object's Update() method.
/// </summary>
public class EyeXGazePointProvider
{
    private static EyeXGazePointProvider _instance;
    private readonly Dictionary<EyeXGazePointType, EyeXGazePointDataStream> _gazePointDataStreams = new Dictionary<EyeXGazePointType, EyeXGazePointDataStream>();

    /// <summary>
    /// Gets the singleton EyeXGazePointProvider instance.
    /// Users of this class should store a reference to the singleton instance in their Awake() method, or similar.
    /// </summary>
    /// <returns>The instance.</returns>
    public static EyeXGazePointProvider GetInstance()
    {
        if (_instance == null)
        {
            _instance = new EyeXGazePointProvider();
        }

        return _instance;
    }

    /// <summary>
    /// Starts streaming gaze point data of a given type.
    /// </summary>
    /// <param name="gazePointType">Gaze point type.</param>
    public void StartStreaming(EyeXGazePointType gazePointType)
    {
        EyeXGazePointDataStream dataStream;
        if (_gazePointDataStreams.TryGetValue(gazePointType, out dataStream))
        {
            // already streaming this kind of data.
            dataStream.UsageCount++;
        }
        else
        {
            dataStream = new EyeXGazePointDataStream(EyeXHost.GetInstance(), gazePointType);
            _gazePointDataStreams.Add(gazePointType, dataStream);
        }
    }

    /// <summary>
    /// Stops streaming gaze point data of a given type.
    /// </summary>
    /// <param name="gazePointType">Gaze point type.</param>
    public void StopStreaming(EyeXGazePointType gazePointType)
    {
        EyeXGazePointDataStream dataStream;
        if (_gazePointDataStreams.TryGetValue(gazePointType, out dataStream))
        {
            if (dataStream.UsageCount > 1)
            {
                dataStream.UsageCount--;
            }
            else
            {
                _gazePointDataStreams.Remove(gazePointType);
                dataStream.Dispose();
            }
        }
    }

    /// <summary>
    /// Gets the most recent value from a data stream.
    /// </summary>
    /// <param name="gazePointType">Gaze point type.</param>
    /// <returns>The value.</returns>
    public EyeXGazePoint GetLastGazePoint(EyeXGazePointType gazePointType)
    {
        EyeXGazePointDataStream dataStream;
        if (_gazePointDataStreams.TryGetValue(gazePointType, out dataStream))
        {
            return dataStream.Last;
        }
        else
        {
            return EyeXGazePoint.Invalid;
        }
    }

    /// <summary>
    /// Gets the number of fixations since the stream started
    /// </summary>
    /// <param name="gazePointType">Gaze point type.</param>
    /// <returns>The value.</returns>
    public int GetLastFixationCount(EyeXGazePointType gazePointType)
    {
        EyeXGazePointDataStream dataStream;
        if (_gazePointDataStreams.TryGetValue(gazePointType, out dataStream))
        {
            return dataStream.FixationCount;
        }
        else
        {
            return -1;
        }
    }

    private EyeXGazePointProvider()
    {
    }
}

/// <summary>
/// Manages the subscription for a data stream and provides the most recent data point.
/// </summary>
public sealed class EyeXGazePointDataStream : IDisposable
{
    private readonly EyeXHost _eyeXHost;
    private readonly EyeXGazePointType _gazePointType;

    public EyeXGazePointDataStream(EyeXHost host, EyeXGazePointType gazePointType)
    {
        _eyeXHost = host;
        _gazePointType = gazePointType;

        // create a global interactor for the data stream and register it with the EyeXHost.
        var interactor = new EyeXGlobalInteractor(InteractorId, AssignGazePointDataBehavior, HandleEvent);
        _eyeXHost.RegisterGlobalInteractor(interactor);
    }

    public int UsageCount { get; set; }

    public EyeXGazePoint Last { get; private set; }

    public int FixationCount { get; private set; }

    public void Dispose()
    {
        _eyeXHost.UnregisterGlobalInteractor(InteractorId);
    }

    private string InteractorId
    {
        get { return string.Format("EyeXGazePointDataStream.{0}", _gazePointType); }
    }

    private void AssignGazePointDataBehavior(string interactorId, Interactor interactor)
    {
        switch (_gazePointType)
        {
            case EyeXGazePointType.GazeUnfiltered:
                AddGazePointDataBehavior(interactor, GazePointDataMode.Unfiltered);
                break;

            case EyeXGazePointType.GazeLightlyFiltered:
                AddGazePointDataBehavior(interactor, GazePointDataMode.LightlyFiltered);
                break;

            case EyeXGazePointType.FixationSlow:
                AddFixationDataBehavior(interactor, FixationDataMode.Slow);
                break;

            case EyeXGazePointType.FixationSensitive:
                AddFixationDataBehavior(interactor, FixationDataMode.Sensitive);
                break;
        }
    }

    /// <summary>
    /// Sets the GazePointData behavior on the global interactor and specifies the data mode 
    /// to be used.
    /// <param name="interactor">Interactor object to be fitted with the interaction behavior.</param>
    /// <param name="gazePointDataMode">Which gaze point data mode to use.</param>
    /// </summary>
    private static void AddGazePointDataBehavior(Interactor interactor, GazePointDataMode gazePointDataMode)
    {
        var behavior = interactor.CreateBehavior(InteractionBehaviorType.GazePointData);
        var behaviorParams = new GazePointDataParams() { GazePointDataMode = gazePointDataMode };
        behavior.SetGazePointDataParams(ref behaviorParams);
    }

    /// <summary>
    /// Sets the FixationData behavior on the global interactor and specifies the data mode 
    /// to be used.
    /// <param name="interactor">Interactor object to be fitted with the interaction behavior.</param>
    /// <param name="fixationDataMode">Which fixation data mode to use.</param>
    /// </summary>
    private void AddFixationDataBehavior(Interactor interactor, FixationDataMode fixationDataMode)
    {
        var behavior = interactor.CreateBehavior(InteractionBehaviorType.FixationData);
        var behaviorParams = new FixationDataParams() { FixationDataMode = fixationDataMode };
        behavior.SetFixationDataParams(ref behaviorParams);
    }

    private void HandleEvent(string interactorid, InteractionEvent event_)
    {
        // Note that this method is called on a worker thread, so we MAY NOT access any game objects from here.
        // Therefore, we store the data in a variable and read from the variable in the Update() method.

        foreach (var behavior in event_.Behaviors)
        {
            if (behavior.BehaviorType == InteractionBehaviorType.GazePointData)
            {
                GazePointDataEventParams eventParams;
                if (behavior.TryGetGazePointDataEventParams(out eventParams))
                {
                    Last = new EyeXGazePoint((float)eventParams.X, (float)eventParams.Y, eventParams.Timestamp);
                }
            }
            else if (behavior.BehaviorType == InteractionBehaviorType.FixationData)
            {
                FixationDataEventParams fixationEventParams;
                if (behavior.TryGetFixationDataEventParams(out fixationEventParams))
                {
                    if (fixationEventParams.EventType == FixationDataEventType.Begin)
                    {
                        FixationCount++;
                    }
                    else if (fixationEventParams.EventType == FixationDataEventType.End)
                    {
                        // Do nothing right now                        
                    }
                    else
                    {
                        Last = new EyeXGazePoint((float)fixationEventParams.X, (float)fixationEventParams.Y, fixationEventParams.Timestamp);            
                    }
                }
            }
        }
    }
}
