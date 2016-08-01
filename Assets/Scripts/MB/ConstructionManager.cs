﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ConstructionManager))]
public class ConstructionManagerEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        ConstructionManager cm = (ConstructionManager)target;
        if (GUILayout.Button("Ghost Good")) {
            cm.GhostGood();
        }
        if (GUILayout.Button("Ghost Bad")) {
            cm.GhostBad();
        }
        if (GUILayout.Button("Ungost")) {
            cm.UnGhost();
        }
    }
}
#endif

[Serializable]
public class BuildingRequirement: ICloneable {
    public string name;
    public float amount;

    public object Clone() {
        var br = new BuildingRequirement();
        br.name = this.name;
        br.amount = this.amount;
        return br;
    }
}

public class ConstructionManager : MonoBehaviour {
    [SerializeField]
    private bool instabuild = false;
    [SerializeField]
    private List<string> techRequirements = new List<string>();
    [SerializeField]
    private BuildingRequirement[] statRequirements;
    [SerializeField]
    private BuildingRequirement[] resourceRequirements;
    [SerializeField]
    private List<ConstructionReservation> reservations;
    [SerializeField]
    private BuildingRequirement[] unfilledResourceReqs;

    [SerializeField]
    private string[] cachedActions;//cache of targetActions
    [SerializeField]
    private List<MonoBehaviour> cachedComponents;

    public void Start() {
        var cloneList = new List<BuildingRequirement>();
        foreach (var req in resourceRequirements) {
            cloneList.Add((BuildingRequirement)req.Clone());
        }
        unfilledResourceReqs = cloneList.ToArray();
    }

    public void GhostGood() {
        var r = GetComponentsInChildren<MeshRenderer>();
        foreach (var q in r) {
            q.material.shader = Shader.Find("Custom/BuildingGhost");
            q.material.SetColor("_GhostColor", Color.green);
        }
    }

    public void GhostBad() {
        var r = GetComponentsInChildren<MeshRenderer>();
        foreach (var q in r) {
            q.material.shader = Shader.Find("Custom/BuildingGhost");
            q.material.SetColor("_GhostColor", Color.red);
        }
    }
    public void GhostBuilding() {
        var r = GetComponentsInChildren<MeshRenderer>();
        foreach (var q in r) {
            q.material.shader = Shader.Find("Custom/BuildingGhost");
            q.material.SetColor("_GhostColor", Color.white);
        }
    }

    public void UnGhost() {
        var r = GetComponentsInChildren<MeshRenderer>();
        foreach (var q in r) {
            q.material.shader = Shader.Find("Standard");
        }
    }

    public void FixedUpdate() {
        reservations.RemoveAll((r) => {
            return r.Released || r.Cancelled;
        });
    }

    public bool ConstructionFinished() {
        float neededResources = 0.0f;
        foreach (BuildingRequirement req in unfilledResourceReqs) {
            neededResources += req.amount;
        }
        return neededResources <= 0.0f;
    }

	public bool ElligibleToBuild() {
        TechManager tm = GameController.instance.techmanager;
        foreach (var r in techRequirements) {
            if (!tm.TechResearched(r)) {
                return false;
            }
        }
        foreach (var r in statRequirements) {
            if (StatManager.Instance.Stat(r.name).Value < (decimal)r.amount) {
                return false;
            }
        }
        return true;
    }

    public bool IsBuildable(Vector3 position) {
        if (position.y <= GroundController.instance.waterLevel) {
            return false;
        }
        if (instabuild) {
            var availResources = GameController.instance.GetAllAvailableResources();
            foreach (var r in resourceRequirements) {
                if (  !availResources.ContainsKey(r.name) 
                    || availResources[r.name] < r.amount) 
                {
                    return false;
                }
            }
        }
        return true;
    }

    public void StartPlacement() {
        NeolithicObject no = GetComponent<NeolithicObject>();
        no.selectable = false;
        cachedActions = no.targetActions;
        no.targetActions = new string[] {};

        cachedComponents = new List<MonoBehaviour>();
        foreach (var r in GetComponents<Reservoir>()) {
            r.enabled = false;
            cachedComponents.Add(r);
        }
        foreach (var r in GetComponents<Warehouse>()) {
            r.enabled = false;
            cachedComponents.Add(r);
        }

        GhostBad();
    }

    public void StartConstruction() {
        if (instabuild) {
            foreach (var r in resourceRequirements) {
                var rp = new ResourceProfile(r.name, r.amount);
                if (!GameController.instance.WithdrawFromAnyWarehouse(rp)) {
                    throw new Exception("Failed to build building, unable to withdraw "+r.name);
                }
            }

            FinishContruction();
        } else {
            NeolithicObject no = GetComponent<NeolithicObject>();
            no.targetActions = new string[] { "Construct" };
            GhostBuilding();
        }
    }

    public void FinishContruction() {
        NeolithicObject no = GetComponent<NeolithicObject>();
        no.selectable = true;
        no.targetActions = cachedActions;
        foreach (var r in cachedComponents) {
            r.enabled = true;
        }
        UnGhost();
        Destroy(this);
    }

    public bool GetJobReservation(ActorController actor) {
        var avails = GameController.instance.GetAllAvailableResources();
        foreach (var kvp in avails) {
            string resourceTag = kvp.Key;
            float amount = kvp.Value;
            Debug.Log("Checking if I need " + amount + " " + resourceTag);
            float needed = GetNeededResource(resourceTag);
            Debug.Log("I need " + needed + " " + resourceTag);
            if (needed > 0.0f) {
                Debug.Log("Making a ConstructionReservation");
                var res = actor.gameObject.AddComponent<ConstructionReservation>();
                reservations.Add(res);
                res.resourceTag = resourceTag;
                res.amount = 1.0f;
                return true;
            }
        }
        return false;
    }

    public float GetNeededResource(string resourceTag) {
        float needed = 0.0f;
        foreach (var requirement in unfilledResourceReqs) {
            if (requirement.name == resourceTag) {
                needed += requirement.amount;
            }
        }
        foreach (var res in reservations) {
            if (    res.resourceTag == resourceTag 
                && !res.Released 
                && !res.Cancelled)
            {
                needed -= res.amount;
            }
        }
        return needed;
    }

    /// <summary>
    /// Fills the given reservation and removes it from the list of unfilled resource requirements
    /// </summary>
    /// <param name="res"></param>
    public void FillReservation(ConstructionReservation res) {
        if (!reservations.Contains(res)) {
            throw new ArgumentException("Reservation does not belong to this construction object");
        }
        foreach (var requirement in unfilledResourceReqs) {
            if (requirement.name == res.resourceTag) {
                res.Released = true;
                requirement.amount -= res.amount;
                reservations.Remove(res);
                break;
            }
        }

        if (ConstructionFinished()) {
            FinishContruction();
        }
    }
}