﻿using UnityEngine;
using System.Collections;

public class CatchFishOrder : BaseOrder {
    float progress = 0.0f;

    public CatchFishOrder(ActorController a, NeolithicObject fishingHole) : base(a) {
    }

    public override void DoStep() {
        progress += Time.fixedDeltaTime;
        if (progress >= 1.25f) {
            string rtag = "fish";
            GameObject res = GameController.instance.CreateResourcePile(rtag, 1.0f);
            actor.PickupResource(res);
            this.completed = true;
        }
    }
}