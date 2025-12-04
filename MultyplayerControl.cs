using Godot;
using System;

public partial class MultyplayerControl : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
        Multiplayer.PeerConnected += PeerConnected;
		Multiplayer.PeerDisconnected += PeerDisconnected;
		Multiplayer.ConnectedToServer += ConnectedToServer;
		Multiplayer.ConnectionFailed += ConnectionFailed;
    }

    private void ConnectionFailed()
    {
        throw new NotImplementedException();
    }


    private void ConnectedToServer()
    {
        throw new NotImplementedException();
    }


    private void PeerDisconnected(long id)
    {
        throw new NotImplementedException();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}

	public void PeerConnected(long id)
    {
        throw new NotImplementedException();
    }

	public void _on_host_button_down()
    {
        
    }

	public void _on_join_button_down()
    {
        
    }

	public void _on_start_game_button_down()
    {
        
    }
}
