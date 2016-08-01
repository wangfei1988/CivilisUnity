﻿using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using NSubstitute;
using System.Collections.Generic;
using System;

[TestFixture]
[Category("Serialization Tests")]
public class TypeTesting {
    [Test]
    public void CollectionTypeTesting() {
        Type t = typeof(Dictionary<string, string>);
        Type kvpss = typeof(KeyValuePair<string, string>);
        Assert.That(TypeSystem.IsCollectionType(t));
        Assert.AreEqual(kvpss, TypeSystem.GetElementType(t));
        Assert.That(!TypeSystem.IsCollectionType(kvpss));
        Assert.That(!TypeSystem.IsEnumerableType(kvpss));
        Assert.AreEqual(kvpss, TypeSystem.GetElementType(kvpss));
    }
}