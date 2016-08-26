﻿using UnityEngine;
using System.Collections;

public class SlaughterHuntedAnimalOrder : BaseOrder {
    float progress = 0;
    Herd herd;

    public SlaughterHuntedAnimalOrder(ActorController a, Herd targetHerd) : base(a) {
        a.GetComponent<NeolithicObject>().statusString = "Killing snorgle";
        herd = targetHerd;
    }

    public override void DoStep() {
        progress += Time.fixedDeltaTime;
        if (progress > 1.25f) {
            if (herd.KillAnimal()) {
                string rtag = herd.resourceTag;
                GameObject res = actor.gameController.CreateResourcePile(rtag, 1);
                actor.PickupResource(res);
                this.completed = true;
            }
            else {
                progress /= 2.0f;
            }
        }
    }
}