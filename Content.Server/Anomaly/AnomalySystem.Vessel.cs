﻿using Content.Server.Anomaly.Components;
using Content.Server.Construction;
using Content.Server.Power.EntitySystems;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;
using Content.Shared._NF.Anomaly; // Frontier

namespace Content.Server.Anomaly;

/// <summary>
/// This handles anomalous vessel as well as
/// the calculations for how many points they
/// should produce.
/// </summary>
public sealed partial class AnomalySystem
{
    private void InitializeVessel()
    {
        SubscribeLocalEvent<AnomalyVesselComponent, ComponentShutdown>(OnVesselShutdown);
        SubscribeLocalEvent<AnomalyVesselComponent, MapInitEvent>(OnVesselMapInit);
        SubscribeLocalEvent<AnomalyVesselComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<AnomalyVesselComponent, InteractUsingEvent>(OnVesselInteractUsing);
        SubscribeLocalEvent<AnomalyVesselComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AnomalyVesselComponent, ResearchServerGetPointsPerSecondEvent>(OnVesselGetPointsPerSecond);
        SubscribeLocalEvent<AnomalyShutdownEvent>(OnShutdown);
        SubscribeLocalEvent<AnomalyStabilityChangedEvent>(OnStabilityChanged);
        SubscribeLocalEvent<AnomalyVesselComponent, EntParentChangedMessage>(OnVesselParentChanged); // Frontier
    }

    private void OnStabilityChanged(ref AnomalyStabilityChangedEvent args)
    {
        OnVesselAnomalyStabilityChanged(ref args);
        OnScannerAnomalyStabilityChanged(ref args);
    }

    private void OnShutdown(ref AnomalyShutdownEvent args)
    {
        OnVesselAnomalyShutdown(ref args);
        OnScannerAnomalyShutdown(ref args);
    }

    private void OnExamined(EntityUid uid, AnomalyVesselComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(component.Anomaly == null
            ? Loc.GetString("anomaly-vessel-component-not-assigned")
            : Loc.GetString("anomaly-vessel-component-assigned"));
    }

    private void OnVesselShutdown(EntityUid uid, AnomalyVesselComponent component, ComponentShutdown args)
    {
        if (component.Anomaly is not { } anomaly)
            return;

        if (!TryComp<AnomalyComponent>(anomaly, out var anomalyComp))
            return;

        anomalyComp.ConnectedVessel = null;
    }

    private void OnVesselMapInit(EntityUid uid, AnomalyVesselComponent component, MapInitEvent args)
    {
        UpdateVesselAppearance(uid,  component);
    }

    private void OnUpgradeExamine(EntityUid uid, AnomalyVesselComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("anomaly-vessel-component-upgrade-output", component.PointMultiplier);
    }

    private void OnVesselInteractUsing(EntityUid uid, AnomalyVesselComponent component, InteractUsingEvent args)
    {
        if (component.Anomaly != null ||
            !TryComp<AnomalyScannerComponent>(args.Used, out var scanner) ||
            scanner.ScannedAnomaly is not { } anomaly)
        {
            return;
        }

        if (!TryComp<AnomalyComponent>(anomaly, out var anomalyComponent) || anomalyComponent.ConnectedVessel != null)
            return;

        // Frontier: check anomaly is on the same grid
        if (!TryComp(uid, out TransformComponent? xform)
            || !TryComp(anomaly, out TransformComponent? anomXform)
            || xform.GridUid != anomXform.GridUid)
        {
            Popup.PopupEntity(Loc.GetString("anomaly-vessel-component-off-grid"), uid);
            return;
        }
        // End Frontier: check anomaly is on the same grid

        component.Anomaly = scanner.ScannedAnomaly;
        anomalyComponent.ConnectedVessel = uid;
        _radiation.SetSourceEnabled(uid, true);
        UpdateVesselAppearance(uid,  component);
        Popup.PopupEntity(Loc.GetString("anomaly-vessel-component-anomaly-assigned"), uid);
    }

    private void OnVesselGetPointsPerSecond(EntityUid uid, AnomalyVesselComponent component, ref ResearchServerGetPointsPerSecondEvent args)
    {
        if (!this.IsPowered(uid, EntityManager) || component.Anomaly is not {} anomaly)
            return;

        var rawPointValue = GetAnomalyPointValue(anomaly); // Frontier: cache value
        args.Points += (int)(rawPointValue * component.PointMultiplier); // Frontier: GetAnomalyPointValue() < rawPointValue
        // Frontier: increase anomaly points
        if (TryComp<AnomalyComponent>(anomaly, out var anomalyComp)
            && anomalyComp.LastTickPointsEarned != Timing.CurTick)
        {
            anomalyComp.LastTickPointsEarned = Timing.CurTick;
            anomalyComp.PointsEarned += rawPointValue;
        }
        // End Frontier
    }

    private void OnVesselAnomalyShutdown(ref AnomalyShutdownEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyVesselComponent>();
        while (query.MoveNext(out var ent, out var component))
        {
            if (args.Anomaly != component.Anomaly)
                continue;

            component.Anomaly = null;
            UpdateVesselAppearance(ent,  component);
            _radiation.SetSourceEnabled(ent, false);

            if (!args.Supercritical)
                continue;
            _explosion.TriggerExplosive(ent);
        }
    }

    private void OnVesselAnomalyStabilityChanged(ref AnomalyStabilityChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyVesselComponent>();
        while (query.MoveNext(out var ent, out var component))
        {
            if (args.Anomaly != component.Anomaly)
                continue;

            UpdateVesselAppearance(ent,  component);
        }
    }

    /// <summary>
    /// Updates the appearance of an anomaly vessel
    /// based on whether or not it has an anomaly
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    public void UpdateVesselAppearance(EntityUid uid, AnomalyVesselComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var on = component.Anomaly != null;

        if (!TryComp<AppearanceComponent>(uid, out var appearanceComponent))
            return;

        Appearance.SetData(uid, AnomalyVesselVisuals.HasAnomaly, on, appearanceComponent);
        if (_pointLight.TryGetLight(uid, out var pointLightComponent))
            _pointLight.SetEnabled(uid, on, pointLightComponent);

        // arbitrary value for the generic visualizer to use.
        // i didn't feel like making an enum for this.
        var value = 1;
        if (TryComp<AnomalyComponent>(component.Anomaly, out var anomalyComp))
        {
            if (anomalyComp.Stability <= anomalyComp.DecayThreshold)
            {
                value = 2;
            }
            else if (anomalyComp.Stability >= anomalyComp.GrowthThreshold)
            {
                value = 3;
            }
        }
        Appearance.SetData(uid, AnomalyVesselVisuals.AnomalyState, value, appearanceComponent);

        _ambient.SetAmbience(uid, on);
    }

    private void UpdateVessels()
    {
        var query = EntityQueryEnumerator<AnomalyVesselComponent>();
        while (query.MoveNext(out var vesselEnt, out var vessel))
        {
            if (vessel.Anomaly is not { } anomUid)
                continue;

            if (!TryComp<AnomalyComponent>(anomUid, out var anomaly))
                continue;

            if (Timing.CurTime < vessel.NextBeep)
                continue;

            // a lerp between the max and min values for each threshold.
            // longer beeps that get shorter as the anomaly gets more extreme
            float timerPercentage;
            if (anomaly.Stability <= anomaly.DecayThreshold)
                timerPercentage = (anomaly.DecayThreshold - anomaly.Stability) / anomaly.DecayThreshold;
            else if (anomaly.Stability >= anomaly.GrowthThreshold)
                timerPercentage = (anomaly.Stability - anomaly.GrowthThreshold) / (1 - anomaly.GrowthThreshold);
            else //it's not unstable
                continue;

            Audio.PlayPvs(vessel.BeepSound, vesselEnt);
            var beepInterval = (vessel.MaxBeepInterval - vessel.MinBeepInterval) * (1 - timerPercentage) + vessel.MinBeepInterval;
            vessel.NextBeep = beepInterval + Timing.CurTime;
        }
    }

    // Frontier: disable anomaly if it goes off-grid
    private void OnVesselParentChanged(Entity<AnomalyVesselComponent> ent, ref EntParentChangedMessage args)
    {
        if (TerminatingOrDeleted(ent) || ent.Comp.Anomaly is not { } anom)
            return;

        if (!TryComp(ent, out TransformComponent? xform)
            || !TryComp(anom, out TransformComponent? anomXform)
            || xform.GridUid != anomXform.GridUid)
        {
            //_radiation.SetSourceEnabled(ent.Owner, false); // Moved vessel radiation handling to the AnomalyLinkExpiry system
            var expiryComp = EnsureComp<AnomalyLinkExpiryComponent>(ent);
            expiryComp.EndTime = _timing.CurTime + expiryComp.CheckFrequency;
        }
    }
    // End Frontier: disable anomaly if it goes off-grid
}
