using UnityEngine;

public class SwitchAction : TileAction
{
    private bool used;

    public override void OnPlayerStepped()
    {
        if (used) return;
        used = true;
        BoardManager.I.ActivateSwitch();
    }
}
