"""
Supplement: ingests a few fresh Load Survey intervals per seeded meter that carry the additional
LS/BLP parameters (average voltage/current, kVAh import, kWh/kVAh export), so those columns show
real values on a database seeded before they existed. Existing rows are never modified (LS is
append-only via the API). Usage: python seed_ls_additional_measures.py
"""
import json, random, urllib.request, urllib.error
from datetime import datetime, timedelta, timezone

BASE = "http://localhost:5004"

def call(method, path, body=None, token=None):
    req = urllib.request.Request(BASE + path, data=json.dumps(body).encode() if body is not None else None, method=method)
    req.add_header("Content-Type", "application/json")
    if token: req.add_header("Authorization", f"Bearer {token}")
    try:
        with urllib.request.urlopen(req) as r:
            raw = r.read(); return r.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode(errors="replace")

_, login = call("POST", "/api/v1/auth/login", {"username": "lalit.prakash", "password": "Test@123"})
tok = login["accessToken"]
_, meters = call("GET", "/api/v1/meters", token=tok)
for m in [x for x in meters if x["serialNumber"].startswith("MTR-SEED-")]:
    _, page = call("GET", f"/api/v1/meter-data/ls?meterId={m['id']}&pageSize=1", token=tok)
    last = page["data"][0]
    end = datetime.fromisoformat(last["intervalEndUtc"].replace("Z", "+00:00"))
    cum = last["cumulativeReading"]
    batch = []
    for i in range(12):
        start = end + timedelta(minutes=30 * i)
        cum += round(random.uniform(0.3, 2.5), 3)
        batch.append({"meterId": m["id"],
            "intervalStartUtc": start.strftime("%Y-%m-%dT%H:%M:%SZ"),
            "intervalEndUtc": (start + timedelta(minutes=30)).strftime("%Y-%m-%dT%H:%M:%SZ"),
            "cumulativeReading": round(cum, 3),
            "averageVoltage": round(random.uniform(228, 242), 2),
            "averageCurrent": round(random.uniform(0.5, 6.0), 3),
            "cumulativeKvahImport": round(cum * 1.05, 3),
            "cumulativeKwhExport": round(cum * 0.02, 3),
            "cumulativeKvahExport": round(cum * 0.021, 3)})
    s, _ = call("POST", "/api/v1/meter-data/ls", batch, tok)
    print(m["serialNumber"], s)
