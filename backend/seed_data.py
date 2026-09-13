"""
One-off seed script: 5 feeders, 3 DTs, 12 consumers + service points + meters, plus every
category of meter data this system now records — LS (Load Survey), DP (Daily Profile, née
DailyLoadProfile), IP (Instantaneous Profile), BP (Billing Profile), meter Events/Alarms, VEE
execution records (via the real estimation run against deliberately-gapped LS data), and
network energy readings — all through the actual API, not direct DB writes, so every business
rule (installation records, ingestion validation, VEE quality derivation, duplicate rejection)
runs exactly as it would for a real caller.

Usage: python seed_data.py
Requires: backend running at http://localhost:5004, with an Admin user already claimed
(this script logs in as lalit.prakash / Test@123 — the account created earlier this session).
"""

import json
import random
import urllib.request
import urllib.error
from datetime import datetime, timedelta, date, timezone

BASE = "http://localhost:5004"
TOKEN = None


def call(method, path, body=None, expect=(200, 201)):
    url = f"{BASE}{path}"
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if TOKEN:
        req.add_header("Authorization", f"Bearer {TOKEN}")
    try:
        with urllib.request.urlopen(req) as resp:
            raw = resp.read()
            return resp.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode(errors="replace")
        return e.code, raw


def must(method, path, body=None, expect=(200, 201)):
    status, payload = call(method, path, body)
    if status not in expect:
        raise RuntimeError(f"{method} {path} -> {status}: {payload}")
    return payload


def login():
    global TOKEN
    payload = must("POST", "/api/v1/auth/login", {"username": "lalit.prakash", "password": "Test@123"})
    TOKEN = payload["accessToken"]
    print(f"Logged in as {payload['user']['displayName']} ({payload['user']['role']})")


def main():
    login()

    # --- Electrical hierarchy: 1 substation -> 5 feeders -> 3 DTs (one per first 3 feeders) ---
    substation = must("POST", "/api/v1/config/hierarchy", {"nodeType": "Substation", "parentId": None, "code": "SS-SEED-01", "name": "Seed Substation 1"})
    print(f"Substation {substation['code']}")

    feeders = []
    for i in range(1, 6):
        f = must("POST", "/api/v1/config/hierarchy", {"nodeType": "Feeder", "parentId": substation["id"], "code": f"FDR-SEED-{i:02d}", "name": f"Seed Feeder {i}"})
        feeders.append(f)
        print(f"  Feeder {f['code']}")

    dts = []
    for i in range(1, 4):
        dt = must("POST", "/api/v1/config/hierarchy", {"nodeType": "DistributionTransformer", "parentId": feeders[i - 1]["id"], "code": f"DT-SEED-{i:02d}", "name": f"Seed DT {i}"})
        dts.append(dt)
        print(f"    DT {dt['code']} (under {feeders[i-1]['code']})")

    # --- 12 consumers, each with one service point, round-robin across the 3 DTs ---
    consumers = []
    for i in range(1, 13):
        cust = must("POST", "/api/v1/customers", {"accountNumber": f"C-SEED-{i:03d}", "name": f"Seed Consumer {i:02d}"})
        dt = dts[(i - 1) % 3]
        sp = must("POST", f"/api/v1/customers/{cust['id']}/service-points", {"address": f"Plot {i}, Seed Layout", "distributionTransformerNodeId": dt["id"]})
        consumers.append({"customer": cust, "servicePoint": sp, "dt": dt})
        print(f"Consumer {cust['accountNumber']} -> SP {sp['id'][:8]} -> {dt['code']}")

    # --- 12 meters, installed at each service point ---
    now = datetime.now(timezone.utc)
    install_from = now - timedelta(days=10)
    meters = []
    for i, c in enumerate(consumers, start=1):
        phase = "Single" if i % 3 != 0 else "Three"
        meter = must("POST", "/api/v1/meters", {"serialNumber": f"MTR-SEED-{i:03d}", "phase": phase})
        must("POST", f"/api/v1/meters/{meter['id']}/install", {
            "servicePointId": c["servicePoint"]["id"],
            "effectiveFromUtc": install_from.strftime("%Y-%m-%dT%H:%M:%SZ"),
            "openingReading": 0,
            "reason": "Seed data initial installation",
        })
        meters.append({"meter": meter, "servicePoint": c["servicePoint"], "consumer": c["customer"]})
        print(f"Meter {meter['serialNumber']} ({phase}) installed at SP {c['servicePoint']['id'][:8]}")

    # --- Load Survey: 3 days x 48 x 30-min intervals per meter, with a few gaps left for VEE estimation ---
    ls_days = 3
    for m in meters:
        meter_id = m["meter"]["id"]
        cumulative = 1000.0
        intervals = []
        gap_slots = set(random.sample(range(ls_days * 48), 4))  # leave 4 slots missing across the 3 days
        slot = 0
        day0 = (now - timedelta(days=ls_days)).replace(minute=0, second=0, microsecond=0)
        for d in range(ls_days):
            for h in range(48):
                start = day0 + timedelta(days=d, minutes=30 * h)
                end = start + timedelta(minutes=30)
                if slot not in gap_slots:
                    consumption = round(random.uniform(0.3, 2.5), 3)
                    cumulative += consumption
                    intervals.append({
                        "meterId": meter_id,
                        "intervalStartUtc": start.strftime("%Y-%m-%dT%H:%M:%SZ"),
                        "intervalEndUtc": end.strftime("%Y-%m-%dT%H:%M:%SZ"),
                        "cumulativeReading": round(cumulative, 3),
                    })
                slot += 1
        # Ingest in chunks to keep request bodies reasonable.
        for i in range(0, len(intervals), 50):
            must("POST", "/api/v1/meter-data/ls", intervals[i:i + 50])
        print(f"LS: {len(intervals)} intervals ingested for {m['meter']['serialNumber']} ({len(gap_slots)} slots left as gaps)")

        # Run missing-interval estimation for each day so real VeeExecutionRecord rows exist.
        for d in range(ls_days):
            day = (day0 + timedelta(days=d)).date()
            status, _ = call("POST", f"/api/v1/vee/estimation/ls/run?meterId={meter_id}&date={day.isoformat()}")

    # --- Daily Profile (DP): 7 days per meter, with the full kWh/kVAh import/export split ---
    for m in meters:
        for d in range(7, 0, -1):
            profile_date = (now - timedelta(days=d)).date()
            kwh_import = round(random.uniform(8, 40), 2)
            status, payload = call("POST", "/api/v1/meter-data/dlp", {
                "servicePointId": m["servicePoint"]["id"],
                "meterId": m["meter"]["id"],
                "profileDate": profile_date.isoformat(),
                "consumptionKwh": kwh_import,
                "kvahImport": round(kwh_import * 1.03, 2),
                "kwhExport": 0,
                "kvahExport": 0,
            })
            if status not in (200, 201, 409):  # 409 = already exists, fine on re-run
                raise RuntimeError(f"DP ingest failed: {status} {payload}")
        print(f"DP: 7 days ingested for {m['meter']['serialNumber']}")

    # --- Instantaneous Profile (IP): 1 day at 15-minute cadence per meter ---
    for m in meters:
        meter_id = m["meter"]["id"]
        readings = []
        base = (now - timedelta(days=1)).replace(minute=0, second=0, microsecond=0)
        cum_kwh, cum_kvah = 1000.0, 1030.0
        for slot in range(96):  # 24h * 4 (15-min)
            t = base + timedelta(minutes=15 * slot)
            kw = round(random.uniform(0.3, 2.5), 3)
            cum_kwh += kw * 0.25
            cum_kvah += kw * 0.25 * 1.03
            readings.append({
                "meterId": meter_id,
                "meterTimeUtc": t.strftime("%Y-%m-%dT%H:%M:%SZ"),
                "voltage": round(random.uniform(225, 235), 1),
                "phaseCurrent": round(random.uniform(1, 8), 2),
                "neutralCurrent": round(random.uniform(0, 0.5), 2),
                "powerFactor": round(random.uniform(0.92, 0.99), 3),
                "frequency": 50.0,
                "kw": kw,
                "kva": round(kw / 0.97, 3),
                "kwh": round(cum_kwh, 3),
                "kvah": round(cum_kvah, 3),
                "kwhExport": 0,
                "kvahExport": 0,
                "powerOnDurationMinutes": 15,
                "tamperCount": 0,
                "billingCount": 0,
                "programmingCount": 0,
                "loadLimitState": "Normal",
                "loadLimitValue": None,
            })
        for i in range(0, len(readings), 50):
            must("POST", "/api/v1/meter-data/ip", readings[i:i + 50])
        print(f"IP: {len(readings)} readings (15-min, 1 day) ingested for {m['meter']['serialNumber']}")

    # --- Billing Profile (BP): 2 monthly cycles per meter ---
    tz_labels = list(range(8))
    for m in meters:
        for months_ago in (2, 1):
            billing_date = (now.replace(day=1) - timedelta(days=1)).replace(day=1)  # normalize to a month start
            for _ in range(months_ago - 1):
                billing_date = (billing_date - timedelta(days=1)).replace(day=1)
            kwh_tz = [round(random.uniform(20, 150), 2) for _ in tz_labels]
            kvah_tz = [round(v * 1.03, 2) for v in kwh_tz]
            status, payload = call("POST", "/api/v1/meter-data/bp", {
                "meterId": m["meter"]["id"],
                "billingDate": billing_date.date().isoformat(),
                "cumulativeKwhImport": round(sum(kwh_tz), 2),
                "cumulativeKvahImport": round(sum(kvah_tz), 2),
                "cumulativeKwhExport": 0,
                "cumulativeKvahExport": 0,
                "averagePowerFactor": round(random.uniform(0.93, 0.99), 3),
                "kwhByTariffZone": kwh_tz,
                "kvahByTariffZone": kvah_tz,
                "maximumDemandKw": round(random.uniform(2, 6), 2),
                "maximumDemandKva": round(random.uniform(2.2, 6.5), 2),
                "billingPowerOnDurationMinutes": 43200,
            })
            if status not in (200, 201, 409):
                raise RuntimeError(f"BP ingest failed: {status} {payload}")
        print(f"BP: 2 billing cycles ingested for {m['meter']['serialNumber']}")

    # --- Meter Events / Alarms: a realistic mix of severities, a few pre-acknowledged ---
    event_catalog = [
        ("PowerFailure", "Info", "Supply interruption detected"),
        ("PowerRestore", "Info", "Supply restored"),
        ("TamperDetected", "Critical", "Meter cover tamper switch triggered"),
        ("CoverOpen", "Warning", "Meter cover opened"),
        ("OverVoltage", "Warning", "Voltage exceeded configured threshold"),
        ("BatteryLow", "Warning", "Backup battery voltage low"),
    ]
    for m in meters:
        meter_id = m["meter"]["id"]
        picks = random.sample(event_catalog, k=3)
        events = []
        for i, (etype, sev, desc) in enumerate(picks):
            t = now - timedelta(hours=random.randint(1, 72))
            events.append({"meterId": meter_id, "occurredAtUtc": t.strftime("%Y-%m-%dT%H:%M:%SZ"), "eventType": etype, "severity": sev, "description": desc})
        created = must("POST", "/api/v1/meter-data/events", events)
        # Acknowledge roughly a third of them so the report shows both open and acknowledged states.
        for e in created[::3]:
            must("POST", f"/api/v1/meter-data/events/{e['id']}/acknowledge")
        print(f"Events: 3 events/alarms ingested for {m['meter']['serialNumber']}")

    # --- Network energy readings, one per feeder per day for the last 5 days (for Energy Audit / dashboard trend) ---
    for f in feeders:
        for d in range(5, 0, -1):
            reading_date = (now - timedelta(days=d)).date()
            must("POST", "/api/v1/energy-audit/network-energy-readings", {
                "hierarchyNodeId": f["id"],
                "date": reading_date.isoformat(),
                "energyKwh": round(random.uniform(500, 2000), 2),
            })
        print(f"Network energy readings: 5 days for {f['code']}")

    print("\nSeed complete.")
    print(f"  1 substation, {len(feeders)} feeders, {len(dts)} DTs")
    print(f"  {len(consumers)} consumers/service points, {len(meters)} meters")
    print(f"  {ls_days} days of LS data + estimation runs, 7 days of DLP, 5 days of network energy readings per feeder")


if __name__ == "__main__":
    main()
