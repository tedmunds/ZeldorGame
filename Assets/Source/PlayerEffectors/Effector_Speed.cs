using UnityEngine;
using System.Collections;

public class Effector_Speed : PlayerEffector {

    private const float bonusMoveSpeed = 3.0f;
    private Color playerBaseColor;
    private Color platerSpeedColor = new Color(255, 255, 0, 128);

    public override void OnApplied(PlayerController player) {
        base.OnApplied(player);
        player.bonusMoveSpeed += bonusMoveSpeed;

        playerBaseColor = player.meshRenderer.material.color;
        player.meshRenderer.material.color = platerSpeedColor;
    }

    public override void OnRemoved(PlayerController player) {
        base.OnRemoved(player);
        player.bonusMoveSpeed -= bonusMoveSpeed;
        player.meshRenderer.material.color = playerBaseColor;
    }
}
