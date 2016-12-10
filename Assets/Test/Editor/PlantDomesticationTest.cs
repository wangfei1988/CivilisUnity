﻿using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[TestFixture]
[Category("Domestication Tests")]
public class PlantDomesticationTests : NeolithicTest {
    StatManager stats;
    PlantDomesticationManager pdm;

    [SetUp]
    public override void SetUp() {
        base.SetUp();
        stats = MakeDummyStatManager();
        pdm = MakeTestComponent<PlantDomesticationManager>();

        Assert.IsNotNull(pdm.stats);
        stats.Awake();
        pdm.Start();
    }

    [Test]
    public void TestChangeNotification() {
        pdm.forestGardenThreshold = 1;
        stats.Stat("vegetables-harvested").Add(1);
        Assert.IsTrue(pdm.ForestGardensEnabled);
    }
}
