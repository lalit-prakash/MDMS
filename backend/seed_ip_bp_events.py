"""
Supplemental seed: adds IP (Instantaneous Profile), BP (Billing Profile), and Events/Alarms for
the meters already created by seed_data.py (serial numbers MTR-SEED-*). Run this instead of
re-running seed_data.py itself, which would duplicate the hierarchy/consumers/meters it already
created (hierarchy codes and meter serial numbers are unique, so a second full run would fail
partway through anyway).

Usage: python seed_ip_bp_events.py
"""

import json
import random
import urllib.request
import urllib.error
from datetime import datetime, timedelta, timezone

BASE = "http://localhost:5004"
TOKEN = None


def call(method, path, body=None):
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
        try:
            return e.code, json.loads(raw)
        except json.JSONDecodeError:
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

    all_meters = must("GET", "/api/v1/meters")
    meters = [m for m in all_meters if m["serialNumber"].startswith("MTR-SEED-")]
    if not meters:
        raise RuntimeError("No MTR-SEED-* meters found — run seed_data.py first.")
    print(f"Found {len(meters)} seeded meters.")

    now = datetime.now(timezone.utc)

    # --- Instantaneous Profile (IP): 1 day at 15-minute cadence per meter ---
    for meter in meters:
        meter_id = meter["id"]
        readings = []
        base = (now - timedelta(days=1)).replace(minute=0, second=0, microsecond=0)
        cum_kwh, cum_kvah = 1000.0, 1030.0
        for slot in range(96):
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
        print(f"IP: {len(readings)} readings ingested for {meter['serialNumber']}")

    # --- Billing Profile (BP): 2 monthly cycles per meter ---
    tz_labels = list(range(8))
    for meter in meters:
        for months_ago in (2, 1):
            billing_date = now.replace(day=1)
            for _ in range(months_ago):
                billing_date = (billing_date - timedelta(days=1)).replace(day=1)
            kwh_tz = [round(random.uniform(20, 150), 2) for _ in tz_labels]
            kvah_tz = [round(v * 1.03, 2) for v in kwh_tz]
            status, payload = call("POST", "/api/v1/meter-data/bp", {
                "meterId": meter["id"],
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
            if status not in (200, 201, 409):  # 409 = already exists, fine on re-run
                raise RuntimeError(f"BP ingest failed: {status} {payload}")
        print(f"BP: 2 billing cycles ingested for {meter['serialNumber']}")

    # --- Meter Events / Alarms ---
    event_catalog = [
        ("PowerFailure", "Info", "Supply interruption detected"),
        ("PowerRestore", "Info", "Supply restored"),
        ("TamperDetected", "Critical", "Meter cover tamper switch triggered"),
        ("CoverOpen", "Warning", "Meter cover opened"),
        ("OverVoltage", "Warning", "Voltage exceeded configured threshold"),
        ("BatteryLow", "Warning", "Backup battery voltage low"),
    ]
    for meter in meters:
        picks = random.sample(event_catalog, k=3)
        events = []
        for etype, sev, desc in picks:
            t = now - timedelta(hours=random.randint(1, 72))
            events.append({"meterId": meter["id"], "occurredAtUtc": t.strftime("%Y-%m-%dT%H:%M:%SZ"), "eventType": etype, "severity": sev, "description": desc})
        created = must("POST", "/api/v1/meter-data/events", events)
        for e in created[::3]:
            must("POST", f"/api/v1/meter-data/events/{e['id']}/acknowledge")
        print(f"Events: 3 events/alarms ingested for {meter['serialNumber']}")

    print("\nSupplemental seed complete.")


if __name__ == "__main__":
    main()
