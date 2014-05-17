//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------

using UnityEngine;

/// <summary>
/// Holds a gaze point with a timestamp and converts to either Screen space, Viewport, or GUI space coordinates.
/// </summary>
public struct EyeXGazePoint
{
    private readonly Vector2 _gazePoint;
    private readonly double _timestamp;

    /// <summary>
    /// Represents an invalid gaze point.
    /// </summary>
    public static EyeXGazePoint Invalid
    {
        get
        {
            return new EyeXGazePoint(float.NaN, float.NaN, double.NaN);
        }
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="x">X coordinate in OS screen coordinates.</param>
    /// <param name="y">Y coordinate in OS screen coordinates.</param>
    /// <param name="timestamp">Timestamp in milliseconds.</param>
    public EyeXGazePoint(float x, float y, double timestamp)
    {
        _gazePoint = new Vector2(x, y);
        _timestamp = timestamp;
    }

    /// <summary>
    /// Gets the gaze point in screen space pixels.
    /// The bottom-left of the screen/camera is (0, 0); the right-top is (pixelWidth, pixelHeight).
    /// </summary>
    public Vector2 Screen
    {
        get
        {
            var point = GUI;
            point.y = UnityEngine.Screen.height - 1 - point.y;
            return point;
        }
    }

    /// <summary>
    /// Gets the gaze point in the viewport coordinate system.
    /// The bottom-left of the screen/camera is (0, 0); the top-right is (1, 1).
    /// </summary>
    public Vector2 Viewport
    {
        get
        {
            var point = Screen;
            point.x /= UnityEngine.Screen.width;
            point.y /= UnityEngine.Screen.height;
            return point;
        }
    }

    /// <summary>
    /// Gets the gaze point in GUI space pixels.
    /// The top-left of the screeen is (0, 0); the bottom-right is (pixelWidth, pixelHeight).
    /// </summary>
    public Vector2 GUI
    {
        get
        {
            var gameWindowPosition = ScreenHelpers.Instance.GetGameWindowPosition();
            var xPosPx = _gazePoint.x - gameWindowPosition.x;
            var yPosPx = _gazePoint.y - gameWindowPosition.y;

            return new Vector2(xPosPx, yPosPx);
        }
    }

    /// <summary>
    /// Timestamp in milliseconds.
    /// The timestamp can be used to uniquely identify this gaze point from a previous gaze point.
    /// </summary>
    public double Timestamp
    {
        get { return _timestamp; }
    }

    /// <summary>
    /// Indicates whether the point is valid or not.
    /// </summary>
    public bool IsValid
    {
        get { return !float.IsNaN(_gazePoint.x); }
    }

    /// <summary>
    /// Indicates whether the point is within the bounds of the game window or not.
    /// </summary>
    public bool IsWithinScreenBounds
    {
        get
        {
            var screenPoint = Screen;
            return IsValid &&
                   screenPoint.x >= 0 &&
                   screenPoint.x < UnityEngine.Screen.width &&
                   screenPoint.y >= 0 &&
                   screenPoint.y < UnityEngine.Screen.height;
        }
    }
}
