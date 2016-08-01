﻿using UnityEngine;
using System.Collections;

/// <summary>
/// Order to generate spirit
/// </summary>
public class MeditateOrder : BaseOrder {
    public MeditateOrder(ActorController a, NeolithicObject target) : base(a) {
    }

    public override void DoStep() {
        GameController.instance.spirit += 0.03f;
    }
}