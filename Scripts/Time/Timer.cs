using Godot;
using System;

public partial class Timer : Node
{

    private static bool timerRunning = false;
    private static float currentTime = .0f;
    public override void _EnterTree()
    {
        Lobby.Instance.ServerStartedTimer+=StartTimer;

    }
    public override void _Process(double delta)
    {
        if (timerRunning)
        {
            currentTime+=(float)delta;
        }
    }

    public static void StartTimer()
    {
        timerRunning = true;
        currentTime=0;
    }

    public static void StopTimer()
    {
        timerRunning = false;
    }

    public static float GetCurrentTime()
    {
        return currentTime;
    }
}
