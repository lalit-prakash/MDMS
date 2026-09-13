"""
One-off supplement to seed_data.py: creates a real Zone -> Circle -> Division -> Sub Division ->
Section organizational hierarchy, links the already-seeded Substation to its Section, and sets
Feeder/DTR/Consumer master-data fields added for the tree-navigation + hierarchy-filter work —
all through the real HTTP API, matching the pattern every other seed script in this project
follows (never a direct DB write).

Usage: python seed_org_units_and_master_data.py
Requires: backend running at http://localhost:5004, seed_data.py already run once (needs the
Substation/Feeders/DTs/Consumers it creates), and an Admin user already claimed.
"""

import json
import urllib.request
import urllib.error
from datetime import date

BASE = "http://localhost:5004"
TOKEN = None


def call(method, path, body=None):
    url = f"{BASE}{path}"
    data = json.dumps(body, default=str).encode() if body is not None else None
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

    # --- Organizational hierarchy: 1 Zone -> 1 Circle -> 1 Division -> 1 Sub Division -> 1 Section ---
    zone = must("POST", "/api/v1/config/org-units", {"unitType": "Zone", "parentId": None, "code": "ZN-SEED-01", "name": "Seed Zone 1"})
    circle = must("POST", "/api/v1/config/org-units", {"unitType": "Circle", "parentId": zone["id"], "code": "CIR-SEED-01", "name": "Seed Circle 1"})
    division = must("POST", "/api/v1/config/org-units", {"unitType": "Division", "parentId": circle["id"], "code": "DIV-SEED-01", "name": "Seed Division 1"})
    subdivision = must("POST", "/api/v1/config/org-units", {"unitType": "SubDivision", "parentId": division["id"], "code": "SDV-SEED-01", "name": "Seed Sub Division 1"})
    section = must("POST", "/api/v1/config/org-units", {"unitType": "Section", "parentId": subdivision["id"], "code": "SEC-SEED-01", "name": "Seed Section 1"})
    print(f"Org hierarchy: {zone['name']} > {circle['name']} > {division['name']} > {subdivision['name']} > {section['name']}")

    # --- Link the seeded Substation to this Section ---
    substations = must("GET", "/api/v1/config/hierarchy?nodeType=Substation")
    if not substations:
        raise RuntimeError("No Substation found — run seed_data.py first.")
    substation = substations[0]
    must("POST", f"/api/v1/config/hierarchy/{substation['id']}/org-unit", {"orgUnitId": section["id"]})
    print(f"Linked {substation['code']} -> {section['name']}")

    # --- Feeder master data ---
    feeders = must("GET", "/api/v1/config/hierarchy?nodeType=Feeder")
    for i, f in enumerate(sorted(feeders, key=lambda x: x["code"]), start=1):
        must("POST", f"/api/v1/config/hierarchy/{f['id']}/master-data", {
            "capacityKva": 5000 + i * 500,
            "voltageLevel": "11kV",
            "make": "Seed Switchgear Co.",
            "commissionedOn": str(date(2018, 1, i)),
            "operationalStatus": "Active",
            "latitude": 26.63 + i * 0.01,
            "longitude": 92.80 + i * 0.01,
        })
        print(f"  Feeder {f['code']} master data set")

    # --- DTR master data ---
    dtrs = must("GET", "/api/v1/config/hierarchy?nodeType=DistributionTransformer")
    for i, dt in enumerate(sorted(dtrs, key=lambda x: x["code"]), start=1):
        must("POST", f"/api/v1/config/hierarchy/{dt['id']}/master-data", {
            "capacityKva": 100 + i * 25,
            "voltageLevel": "11kV/433V",
            "make": "Seed Transformers Ltd.",
            "commissionedOn": str(date(2019, 1, i)),
            "operationalStatus": "Active",
            "latitude": 26.64 + i * 0.01,
            "longitude": 92.81 + i * 0.01,
        })
        print(f"    DTR {dt['code']} master data set")

    # --- Consumer master data ---
    customers = must("GET", "/api/v1/customers")
    tariffs = ["Domestic", "Commercial", "Industrial"]
    for i, c in enumerate(sorted(customers, key=lambda x: x["accountNumber"]), start=1):
        must("POST", f"/api/v1/customers/{c['id']}/master-data", {
            "rrNumber": f"RR-SEED-{i:03d}",
            "mobileNumber": f"98765{i:05d}",
            "connectionStatus": "Active",
            "serviceDate": str(date(2020, (i % 12) + 1, min(i, 28))),
            "sanctionedLoadKw": 2.0 + (i % 5),
            "contractDemandKva": 3.0 + (i % 5),
            "connectedLoadKw": 2.5 + (i % 5),
            "loadType": "Residential" if i % 3 else "Commercial",
            "tariffCategoryCode": tariffs[i % 3],
            "communicationType": "GPRS",
            "paymentMode": "Prepaid" if i % 2 == 0 else "Postpaid",
            "isNetMeter": i % 6 == 0,
            "billDay": (i % 28) + 1,
            "billCycle": "Monthly",
            "latitude": 26.65 + i * 0.005,
            "longitude": 92.82 + i * 0.005,
        })
        print(f"Consumer {c['accountNumber']} master data set")

    print("Done.")


if __name__ == "__main__":
    main()
