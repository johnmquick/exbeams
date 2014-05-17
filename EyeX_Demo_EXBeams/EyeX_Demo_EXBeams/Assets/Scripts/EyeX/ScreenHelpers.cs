//-----------------------------------------------------------------------
// Copyright 2014 Tobii Technology AB. All rights reserved.
//-----------------------------------------------------------------------
using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Provides utility functions related to screen and window handling.
/// </summary>
public class ScreenHelpers
{
    private static ScreenHelpers _instance;

    private string _windowId;
    private IntPtr _hwnd;

#if UNITY_EDITOR
    private UnityEditor.EditorWindow _gameWindow;
#endif

    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static ScreenHelpers Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ScreenHelpers();
            }

            return _instance;
        }
    }

    private ScreenHelpers()
    {
        _hwnd = Win32Helpers.GetForegroundWindow();
        _windowId = _hwnd.ToString();

#if UNITY_EDITOR
        _gameWindow = GetMainGameView();
#endif
    }

    /// <summary>
    /// Window ID for the game window.
    /// </summary>
    public string GameWindowId
    {
        get
        {
            return _windowId;
        }

        set
        {
            int hwnd;
            if (int.TryParse(value, out hwnd))
            {
                _windowId = value;
                _hwnd = new IntPtr(hwnd);
            }
        }
    }

    /// <summary>
    /// Returns the position of the game window in screen coordinates.
    /// </summary>
    public Vector2 GetGameWindowPosition()
    {
#if UNITY_EDITOR
        var gameWindowPosition = _gameWindow.position;
        var heightOffset = gameWindowPosition.height - Screen.height;
        return new Vector2(gameWindowPosition.x, gameWindowPosition.y + heightOffset);
#else
        var windowClientPosition = new Win32Helpers.POINT();
        Win32Helpers.ClientToScreen(_hwnd, ref windowClientPosition);
        return new Vector2(windowClientPosition.x, windowClientPosition.y);
#endif
    }

#if UNITY_EDITOR
    private static UnityEditor.EditorWindow GetMainGameView()
    {
        var unityEditorType = Type.GetType("UnityEditor.GameView,UnityEditor");
        var getMainGameViewMethod = unityEditorType.GetMethod("GetMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = getMainGameViewMethod.Invoke(null, null);
        return (UnityEditor.EditorWindow)result;
    }
#endif
}